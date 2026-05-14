using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Core.Common.Repositories
{
    public interface IBalanceChangingsRepository
    {
        Task AddAsync(BalanceChanging balanceChanging, CancellationToken cancellationToken = default);

        Task DeleteAsync(int balanceChangingId, CancellationToken cancellationToken = default);

        IQueryable<BalanceChanging> GetAllByUserId(int userId);

        Task<(List<BalanceChanging> items, int totalCount)> GetAllPagedAsync(IQueryable<BalanceChanging> query, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<BalanceChanging?> GetByIdAsync(int balanceChangingId, CancellationToken cancellationToken = default);

        Task<bool> AreAnyAfterAsync(int accountId, DateTime date, CancellationToken cancellationToken = default);
    }
}
