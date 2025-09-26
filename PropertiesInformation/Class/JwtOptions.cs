namespace PropertiesInformation.Api.Class
{
    public sealed class JwtOptions
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenMinutes { get; set; }
        public string SecretKey { get; set; } = string.Empty;
    }
}
