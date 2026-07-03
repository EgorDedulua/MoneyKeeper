using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Core.Common.Repositories
{
    public interface IUsersRepository
    {
        Task AddAsync(User user, CancellationToken cancellationToken = default);

        Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default);
    }
}
