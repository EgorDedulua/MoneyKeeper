using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Infrastructure.Data.Repositories
{
    public class AccountsRepository : IAccountsRepository
    {
        private readonly AppDbContext _db;

        public AccountsRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Account account, CancellationToken cancellationToken)
        {
            account.CreatedAt = DateTime.Now;
            await _db.Accounts.AddAsync(account, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(int accountId, CancellationToken cancellationToken)
        {
            await _db.Accounts.Where(a => a.Id == accountId).ExecuteDeleteAsync(cancellationToken);
        }

        public IQueryable<Account> GetAllByUserId(int userId)
        {
            return _db.Accounts
                .AsNoTracking()
                .Where(a => a.UserId == userId);
        }

        public async Task<(List<Account> items, int TotalCount)> GetAllPagedAsync(IQueryable<Account> query, int page, int pageSize, CancellationToken cancellationToken)
        {
            int totalCount = await query.CountAsync(cancellationToken);
            List<Account> items = await query
                .AsNoTracking()
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return (items, totalCount);
        }

        public async Task<Account?> GetByIdAsync(int accountId, CancellationToken cancellationToken)
        {
            return await _db.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        }

        public async Task<Account> UpdateAsync(Account account, CancellationToken cancellationToken)
        {
            await _db.Accounts
                .Where(a => a.Id == account.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Target, account.Target)
                    .SetProperty(a => a.Description, account.Description)
                    .SetProperty(a => a.Name, account.Name), cancellationToken);

            return await _db.Accounts
                .AsNoTracking()
                .FirstAsync(a => a.Id == account.Id);
        }

        public async Task<decimal?> TryWithdrawAsync(int accountId, decimal amount, CancellationToken cancellationToken)
        {
            int updated = await _db.Accounts
                .Where(a => a.Id == accountId && a.Balance >= amount)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.Balance, a => a.Balance - amount), cancellationToken);
            if (updated == 0)
                return null;

            return await _db.Accounts
                .Where(a => a.Id == accountId)
                .Select(a => a.Balance)
                .FirstAsync(cancellationToken);
        }

        public async Task<decimal?> TryDepositAsync(int accountId, decimal amount, CancellationToken cancellationToken)
        {
            int updated = await _db.Accounts
                .Where(a => a.Id == accountId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.Balance, a => a.Balance + amount), cancellationToken);
            if (updated == 0)
                return null;

            return await _db.Accounts
                .Where(a => a.Id == accountId)
                .Select(a => a.Balance)
                .FirstAsync(cancellationToken);
        }

        public async Task UpdateBalanceAsync(int accountId, decimal newBalance, CancellationToken cancellationToken)
        {
            await _db.Accounts
                .Where(a => a.Id == accountId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.Balance, newBalance), cancellationToken);
        }

        public async Task<bool> ExistsAnotherWithNameAsync(string name, int accountId, int userId, CancellationToken cancellationToken)
        {
            return await _db.Accounts
                .AnyAsync(a => a.UserId == userId && a.Name == name && a.Id != accountId, cancellationToken);
        }

        public async Task<bool> ExistsWithNameAsync(string name, int userId, CancellationToken cancellationToken)
        {
            return await _db.Accounts
                .AnyAsync(a => a.UserId == userId && a.Name == name, cancellationToken);
        }
    }
}
