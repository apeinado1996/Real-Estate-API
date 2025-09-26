namespace PropertiesInformation.Api.DTOs
{
    public sealed class PropertyImageUploadRequest
    {
        public IFormFile File { get; set; } = default!; public bool Enabled { get; set; } = false;
    }

    public sealed class PropertyImageReplaceFileRequest
    {
        public IFormFile File { get; set; } = default!;
    }

    public sealed record PropertyImageEnableRequest(bool Enabled);

    public sealed record PropertyImageResponse(int Id, int IdProperty, string? FileName, string? ContentType, bool Enabled);

    public sealed record PropertyImageWithBase64Response(int Id, int IdProperty, string? FileName, string? ContentType, bool Enabled, string FileBase64);
}
