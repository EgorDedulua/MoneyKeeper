using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Infrastructure.Data.Repositories
{
    public class TransitionsRepository : ITransitionsRepository
    {
        private readonly AppDbContext _db;

        public TransitionsRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Transition transition, CancellationToken cancellationToken)
        {
            transition.Date = DateTime.Now;
            await _db.Transitions.AddAsync(transition, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> AreAnySourceTransitionsAfterAsync(int accountId, DateTime date, CancellationToken cancellationToken)
        {
            return await _db.Transitions
                .AnyAsync(t => t.SourceAccountId == accountId && t.Date > date, cancellationToken);
        }

        public async Task DeleteAsync(int transitionId, CancellationToken cancellationToken)
        {
            await _db.Transitions
                .Where(t => t.Id == transitionId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public IQueryable<Transition> GetAllByUserId(int userId)
        {
            return _db.Transitions
                .AsNoTracking()
                .Include(t => t.SourceAccount)
                .Include(t => t.DestinationAccount)
                .Where(t => t.SourceAccount.UserId == userId);
        }

        public async Task<(List<Transition> items, int totalCount)> GetAllPagedAsync(IQueryable<Transition> query, int page, int pageSize, CancellationToken cancellationToken)
        {
            int totalCount = await query.CountAsync(cancellationToken);
            List<Transition> items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return (items, totalCount);
        }

        public async Task<Transition?> GetByIdAsync(int transitionId, CancellationToken cancellationToken)
        {
            return await _db.Transitions
                .AsNoTracking()
                .Include(t => t.SourceAccount)
                .Include(t => t.DestinationAccount)
                .FirstOrDefaultAsync(t => t.Id == transitionId, cancellationToken);
        }

        public async Task<Transition> UpdateAsync(Transition transition, CancellationToken cancellationToken = default)
        {
            await _db.Transitions
                .Where(t => t.Id == transition.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Description, transition.Description)
                    .SetProperty(t => t.Sum, transition.Sum)
                    .SetProperty(t => t.SourceAccountId, transition.SourceAccountId)
                    .SetProperty(t => t.DestinationAccountId, transition.DestinationAccountId)
                    .SetProperty(t => t.OldDestinationAccountBalance, transition.OldDestinationAccountBalance)
                    .SetProperty(t => t.NewDestinationAccountBalance, transition.NewDestinationAccountBalance)
                    .SetProperty(t => t.OldSourceAccountBalance, transition.OldSourceAccountBalance)
                    .SetProperty(t => t.NewSourceAccountBalance, transition.NewSourceAccountBalance), cancellationToken);

            return await _db.Transitions
                .AsNoTracking()
                .Include(t => t.SourceAccount)
                .Include(t => t.DestinationAccount)
                .FirstAsync(t => t.Id == transition.Id, cancellationToken);
        }
    }
}
