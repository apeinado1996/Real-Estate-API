namespace PropertiesInformation.Api.DTOs
{
	public sealed record AuthRegisterRequest(string UserName, string Email, string Password, string Rol);
	public sealed record AuthLoginRequest(string UserName, string Password);
	public sealed record AuthLoginResponse(string AccessToken, DateTime ExpiresAtUtc);
}
