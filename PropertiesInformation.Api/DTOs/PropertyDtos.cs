namespace PropertiesInformation.Api.DTOs
{
    public sealed record PropertyCreateRequest(string Name, string? Address, decimal Price, string CodeInternal, short? Year, int IdOwner);
    public sealed record PropertyUpdateRequest(string Name, string? Address, decimal Price, short? Year, int IdOwner);
    public sealed record PropertyResponse(int Id, string Name, string? Address, decimal Price, string CodeInternal, short? Year, int IdOwner);
    public sealed record PropertyChangePriceRequest(decimal NewPrice, string? TraceName, decimal? TraceTax);
}
