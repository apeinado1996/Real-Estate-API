namespace PropertiesInformation.Api.DTOs
{
    public sealed record OwnerCreateRequest(string Name, string? Address, IFormFile? Photo, DateTime? Birthday);
    public sealed record OwnerUpdateRequest(string Name, string? Address, DateTime? Birthday);
    public sealed record OwnerResponse(int Id, string Name, string? Address, DateTime? Birthday);
    public sealed record OwnerFileUploadRequest(IFormFile? Photo);
}
