using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Infrastructure.Data.Repositories
{
    public class BalanceChangingsRepository : IBalanceChangingsRepository
    {
        private readonly AppDbContext _db;

        public BalanceChangingsRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(BalanceChanging balanceChanging, CancellationToken cancellationToken)
        {
            balanceChanging.Date = DateTime.Now;
            await _db.BalanceChangings.AddAsync(balanceChanging, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> AreAnyAfterAsync(int accountId, DateTime date, CancellationToken cancellationToken)
        {
            return await _db.BalanceChangings
                .AnyAsync(b => b.AccountId == accountId && b.Date > date, cancellationToken);
        }

        public async Task DeleteAsync(int balanceChangingId, CancellationToken cancellationToken)
        {
            await _db.BalanceChangings
                .Where(b => b.Id == balanceChangingId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public IQueryable<BalanceChanging> GetAllByUserId(int userId)
        {
            return _db.BalanceChangings
                .AsNoTracking()
                .Include(b => b.Account)
                .Where(b => b.Account.UserId == userId);
        }

        public async Task<(List<BalanceChanging> items, int totalCount)> GetAllPagedAsync(IQueryable<BalanceChanging> query, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            int totalCount = await query.CountAsync(cancellationToken);
            List<BalanceChanging> items = await query
                .AsNoTracking()
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(b => b.Account)
                .ToListAsync(cancellationToken);
            return (items, totalCount);
        }

        public async Task<BalanceChanging?> GetByIdAsync(int balanceChangingId, CancellationToken cancellationToken)
        {
            return await _db.BalanceChangings
                .AsNoTracking()
                .Include(b => b.Account)
                .FirstOrDefaultAsync(b => b.Id == balanceChangingId, cancellationToken);
        }

        public async Task<BalanceChanging> UpdateAsync(BalanceChanging balanceChanging, CancellationToken cancellationToken = default)
        {
            await _db.BalanceChangings
                .Where(b => b.Id == balanceChanging.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.OldAccountBalance, balanceChanging.OldAccountBalance)
                    .SetProperty(b => b.NewAccountBalance, balanceChanging.NewAccountBalance)
                    .SetProperty(b => b.AccountId, balanceChanging.AccountId), cancellationToken);

            return await _db.BalanceChangings
                .AsNoTracking()
                .Include(b => b.Account)
                .FirstAsync(b => b.Id == balanceChanging.Id);
        }
    }
}
