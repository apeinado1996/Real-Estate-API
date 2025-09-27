using PropertiesInformation.Core.Entities;

namespace PropertiesInformation.Core.Interface
{
    public interface IPropertyRepository
    {
        Task<int> AddAsync(Property p, CancellationToken ct = default);
        Task UpdateAsync(Property p, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        Task<Property?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<Property>> ListAsync(string? codeInternal, int? ownerId, decimal? minPrice, decimal? maxPrice, string? address, int? year, string? name, CancellationToken ct = default);
        Task ChangePriceAsync(int idProperty, decimal newPrice, string traceName, decimal traceTax, CancellationToken ct = default);
    }
}
