using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Core.Common.Repositories
{
    public interface IUsersRepository
    {
        Task AddAsync(User user, CancellationToken cancellationToken = default);

        Task DeleteByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}
