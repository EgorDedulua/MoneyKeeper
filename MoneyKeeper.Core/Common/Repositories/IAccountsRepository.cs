using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Core.Common.Repositories
{
    public interface IAccountsRepository
    {
        Task AddAsync(Account account, CancellationToken cancellationToken = default);

        Task DeleteAsync(int accountId, CancellationToken cancellationToken = default);

        IQueryable<Account> GetAllByUserId(int userId);

        Task<(List<Account> items, int TotalCount)> GetAllPagedAsync(IQueryable<Account> query, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<Account?> GetByIdAsync(int accountId, CancellationToken cancellationToken = default);

        Task<Account> UpdateAsync(Account account, CancellationToken cancellationToken = default);

        Task<decimal?> TryWithdrawAsync(int accountId, decimal amount, CancellationToken cancellationToken = default);

        Task<decimal?> TryDepositAsync(int accountId, decimal amount, CancellationToken cancellationToken = default);

        Task UpdateBalanceAsync(int accountId, decimal newBalance, CancellationToken cancellationToken = default);

        Task<bool> ExistsAnotherWithNameAsync(string name, int accountId, int userId, CancellationToken cancellationToken = default);

        Task<bool> ExistsWithNameAsync(string name, int userId, CancellationToken cancellationToken = default);
    }
}
