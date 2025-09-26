using PropertiesInformation.Core.Entities;

namespace PropertiesInformation.Core.Interface
{
    public interface IUserRepository
    {
        Task<User?> GetByUserNameAsync(string normalizedUserName, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default);
        Task<bool> ExistsUserNameAsync(string normalizedUserName, CancellationToken ct = default);
        Task<bool> ExistsEmailAsync(string normalizedEmail, CancellationToken ct = default);
        Task UpdateAsync(User user, CancellationToken ct = default);
        Task<User> AddAsync(User user, CancellationToken ct = default);
    }
}
