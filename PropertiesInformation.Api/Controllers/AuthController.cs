using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PropertiesInformation.Api.Authentication;
using PropertiesInformation.Api.Class;
using PropertiesInformation.Api.DTOs;
using PropertiesInformation.Core.Entities;
using PropertiesInformation.Core.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PropertiesInformation.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher<User> _hasher;
        private readonly JwtOptions _options;
        private readonly IJwtTokenService _tokens;

        public AuthController(IUserRepository users, IPasswordHasher<User> hasher, IOptions<JwtOptions> options, IJwtTokenService tokens)
        {
            _users = users;
            _hasher = hasher;
            _options = options.Value;
            _tokens = tokens;
        }

        /// <summary>
        /// Authenticate
        /// </summary>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] AuthLoginRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Invalid credentials.");

            var normalized = req.UserName.ToUpperInvariant();
            User? user = req.UserName.Contains('@')
                ? await _users.GetByEmailAsync(normalized, ct)
                : await _users.GetByUserNameAsync(normalized, ct);

            if (user is null || !user.IsActive)
                return Unauthorized(ApiResponse<object>.Fail(401, "Invalid username or password.", HttpContext));

            var check = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
            if (check == PasswordVerificationResult.Failed)
                return Unauthorized(ApiResponse<object>.Fail(401, "Invalid username or password.", HttpContext));

            if (check == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _hasher.HashPassword(user, req.Password);
                await _users.UpdateAsync(user, ct);
            }

            var (token, exp) = _tokens.GenerateJwtToken(user);
            return Ok(new AuthLoginResponse(token, exp));
        }

        /// <summary>
        /// Healt status api
        /// </summary>
        /// <returns></returns>
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health()
        {
            return Ok(new { status = 200, message = "OK" });
        }

        /// <summary>
        /// Get profile
        /// </summary>
        /// <returns></returns>
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var id = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var name = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name);
            var rol = string.Empty;
            var rols = User.FindAll(ClaimTypes.Role).Select(c => c.Value);

            if (rols.Count() > 0)
                rol = rols.FirstOrDefault("Rol");

            return Ok(new { id, name, rol });
        }

        /// <summary>
        /// Register new users
        /// </summary>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] AuthRegisterRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.UserName) ||
                string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(ApiResponse<object>.Fail(401, "Username, Email and Password are required.", HttpContext));

            var nUser = req.UserName.ToUpperInvariant();
            var nEmail = req.Email.ToUpperInvariant();

            if (await _users.ExistsUserNameAsync(nUser, ct)) return Conflict(ApiResponse<object>.Fail(401, "Username already exists.", HttpContext));
            if (await _users.ExistsEmailAsync(nEmail, ct)) return Conflict(ApiResponse<object>.Fail(401, "Email already exists.", HttpContext));

            var user = new User
            {
                UserName = req.UserName.Trim(),
                Email = req.Email.Trim(),
                Rol = req.Rol,
                IsActive = true
            };

            user.PasswordHash = _hasher.HashPassword(user, req.Password);
            user = await _users.AddAsync(user, ct);

            var (token, exp) = _tokens.GenerateJwtToken(user);
            return Ok(new AuthLoginResponse(token, exp));
        }       
    }
}
