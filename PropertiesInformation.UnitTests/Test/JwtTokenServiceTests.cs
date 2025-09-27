using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PropertiesInformation.Api.Authentication;
using PropertiesInformation.Api.Class;
using PropertiesInformation.Api.Services;
using PropertiesInformation.Core.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PropertiesInformation.UnitTests.Test
{
    public class JwtTokenServiceTests
    {
        private IJwtTokenService _svc = default!;
        private JwtOptions _opt = default!;

        [SetUp]
        public void Setup()
        {
            _opt = new JwtOptions
            {
                Issuer = "PropertiesInformation.Apis",
                Audience = "PropertiesInformation.Client",
                SecretKey = "THIS-IS-A-VERY-LONG-TEST-SECRET-KEY-32+CHARS",
                AccessTokenMinutes = 30
            };

            _svc = new JwtToken(Options.Create(_opt));
        }

        [Test]
        public void CreateAccessToken_should_return_token_and_valid_expiration()
        {
            var user = new User { Id = 7, UserName = "andres", Email = "a@a.com", Rol = "Admin" };

            var (token, expiresUtc) = _svc.GenerateJwtToken(user);

            token.Should().NotBeNullOrWhiteSpace();
            expiresUtc.Should().BeAfter(DateTime.UtcNow.AddMinutes(25))
                             .And.BeBefore(DateTime.UtcNow.AddMinutes(31));
        }

        [Test]
        public void CreateAccessToken_should_contain_expected_claims_and_validate_signature()
        {
            var user = new User { Id = 7, UserName = "andres", Email = "a@a.com", Rol = "Admin" };
            var (token, _) = _svc.GenerateJwtToken(user);

            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SecretKey));
            var parms = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _opt.Issuer,
                ValidAudience = _opt.Audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromSeconds(5),
                NameClaimType = JwtRegisteredClaimNames.UniqueName,
                RoleClaimType = ClaimTypes.Role
            };

            var principal = handler.ValidateToken(token, parms, out var _);

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;

            sub.Should().Be("7");

            var name = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? principal.FindFirst(ClaimTypes.Name)?.Value ?? principal.Identity?.Name;
            name.Should().Be("andres");

            var roles = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? principal.FindFirst(ClaimTypes.Role)?.Value ?? principal.Identity?.Name;
            roles.Should().Contain("Admin");
        }
    }
}
