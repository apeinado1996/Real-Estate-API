using PropertiesInformation.Core.Entities;

namespace PropertiesInformation.Api.Authentication
{
    public interface IJwtTokenService
    {
        (string token, DateTime expiresUtc) GenerateJwtToken(User user);
    }
}
