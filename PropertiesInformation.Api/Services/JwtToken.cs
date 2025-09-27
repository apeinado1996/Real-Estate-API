using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PropertiesInformation.Api.Authentication;
using PropertiesInformation.Api.Class;
using PropertiesInformation.Core.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PropertiesInformation.Api.Services
{
    public class JwtToken : IJwtTokenService
    {
        private readonly JwtOptions _options;
        private readonly SigningCredentials _creds;
        public JwtToken(IOptions<JwtOptions> options)
        {
            _options = options.Value;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
            _creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        public (string token, DateTime expiresUtc) GenerateJwtToken(User user)
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

            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Name, user.UserName));
            claims.Add(new Claim(ClaimTypes.Role, user.Rol));

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
