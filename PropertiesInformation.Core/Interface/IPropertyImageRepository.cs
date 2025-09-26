using PropertiesInformation.Core.Entities;

namespace PropertiesInformation.Core.Interface
{
    public interface IPropertyImageRepository
    {
        Task<int> AddAsync(int idProperty, byte[] fileBytes, string? fileName, string? contentType, bool enabled, CancellationToken ct = default);
        Task ReplaceFileAsync(int idImage, byte[] fileBytes, string? fileName, string? contentType, CancellationToken ct = default);
        Task SetEnabledAsync(int idImage, bool enabled, CancellationToken ct = default);
        Task DeleteAsync(int idImage, CancellationToken ct = default);

        Task<PropertyImage?> GetByIdAsync(int idImage, CancellationToken ct = default);               // metadata
        Task<(byte[]? bytes, string? fileName, string? contentType)> GetFileAsync(int idImage, CancellationToken ct = default);
        Task<IReadOnlyList<PropertyImage>> ListByPropertyAsync(int idProperty, CancellationToken ct = default);
    }
}
