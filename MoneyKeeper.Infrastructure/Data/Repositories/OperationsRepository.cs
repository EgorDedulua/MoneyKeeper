using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;
using MoneyKeeper.Core.Enums;

namespace MoneyKeeper.Infrastructure.Data.Repositories
{
    public class OperationsRepository : IOperationsRepository
    {
        private readonly AppDbContext _db;

        public OperationsRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Operation operation, CancellationToken cancellationToken)
        {
            operation.Date = DateTime.Now;
            await _db.Operations.AddAsync(operation, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> AreAnyConsumptionOperationsAfterAsync(int accountId, DateTime date, CancellationToken cancellationToken)
        { 
            return await _db.Operations
                .AnyAsync(o => o.AccountId == accountId && o.Date > date
                    && o.Category.Type == CategoryType.Consumption, cancellationToken);
        }

        public async Task DeleteAsync(int operationId, CancellationToken cancellationToken)
        {
            await _db.Operations
                .Where(o => o.Id == operationId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public IQueryable<Operation> GetAllByUserId(int userId)
        {
            return _db.Operations
                .AsNoTracking()
                .Include(o => o.Account)
                .Include(o => o.Category)
                .Where(o => o.Account.UserId == userId);
        }

        public async Task<(List<Operation> items, int totalCount)> GetAllPagedAsync(IQueryable<Operation> query, int page, int pageSize, CancellationToken cancellationToken)
        {
            int totalCount = await query.CountAsync(cancellationToken);
            List<Operation> items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return (items, totalCount);
        }

        public async Task<Operation?> GetByIdAsync(int operationId, CancellationToken cancellationToken)
        {
            return await _db.Operations
                .AsNoTracking()
                .Include(a => a.Category)
                .Include(a => a.Account)
                .FirstOrDefaultAsync(o => o.Id == operationId, cancellationToken);
        }

        public async Task<Operation> UpdateAsync(Operation operation, CancellationToken cancellationToken)
        {
            await _db.Operations
                .Where(o => o.Id == operation.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.Sum, operation.Sum)
                    .SetProperty(o => o.NewAccountBalance, operation.NewAccountBalance)
                    .SetProperty(o => o.OldAccountBalance, operation.OldAccountBalance)
                    .SetProperty(o => o.Description, operation.Description)
                    .SetProperty(o => o.CategoryId, operation.CategoryId)
                    .SetProperty(o => o.AccountId, operation.AccountId), cancellationToken);

            return await _db.Operations
                .AsNoTracking()
                .Include(o => o.Account)
                .Include (o => o.Category)
                .FirstAsync(o => o.Id == operation.Id, cancellationToken);
        }

        public async Task<bool> AreAnyOperationsWithCategory(int categoryId, CancellationToken cancellationToken)
        {
            return await _db.Operations
                .AnyAsync(o => o.CategoryId == categoryId, cancellationToken);
        }
    }
}
