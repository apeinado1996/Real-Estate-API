using PropertiesInformation.Core.Entities;

namespace PropertiesInformation.Core.Interface
{
    public interface IPropertyTraceRepository
    {
        Task<int> AddAsync(PropertyTrace t, CancellationToken ct = default);
        Task UpdateAsync(PropertyTrace t, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        Task<PropertyTrace?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<PropertyTrace>> ListByPropertyAsync(int idProperty, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);
        Task<(int CountTraces, decimal TotalValue, decimal TotalTax)> SummaryByPropertyAsync(int idProperty, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);
    }
}
