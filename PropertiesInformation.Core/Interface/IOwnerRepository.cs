using PropertiesInformation.Core.Entities;

namespace PropertiesInformation.Core.Interface
{
    public interface IOwnerRepository
    {
        Task<int> AddAsync(Owner owner, CancellationToken ct = default);
        Task UpdateAsync(Owner owner, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        Task<Owner?> GetByIdAsync(int id, bool includePhoto = false, CancellationToken ct = default);
        Task<IReadOnlyList<Owner>> ListAsync(string? search, CancellationToken ct = default);
        Task UpdatePhotoAsync(int id, byte[] photo, CancellationToken ct = default);
        Task<byte[]?> GetPhotoAsync(int id, CancellationToken ct = default);
    }
}
