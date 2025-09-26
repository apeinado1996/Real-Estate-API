using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PropertiesInformation.Api.Class;
using PropertiesInformation.Api.DTOs;
using PropertiesInformation.Core.Entities;
using PropertiesInformation.Core.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PropertiesInformation.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher<User> _hasher;
        private readonly JwtOptions _options;
        private readonly SigningCredentials _creds;

        public AuthController(IUserRepository users, IPasswordHasher<User> hasher, IOptions<JwtOptions> options)
        {
            _users = users;
            _hasher = hasher;
            _options = options.Value;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
            _creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

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

            var (token, exp) = GenerateJwtToken(user);
            return Ok(new AuthLoginResponse(token, exp));
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var id = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var name = User.Identity?.Name ?? User.FindFirstValue(JwtRegisteredClaimNames.UniqueName);
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
            return Ok(new { id, name, roles });
        }

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

            var (token, exp) = GenerateJwtToken(user);
            return Ok(new AuthLoginResponse(token, exp));
        }

        private (string token, DateTime expiresUtc) GenerateJwtToken(User user)
        {
            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_options.AccessTokenMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };

            claims.Add(new Claim("Rol", user.Rol));

            var jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: _creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
        }
    }
}
