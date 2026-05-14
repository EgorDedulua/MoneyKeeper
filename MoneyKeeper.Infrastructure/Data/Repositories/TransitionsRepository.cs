using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Common.Repositories;

namespace MoneyKeeper.Infrastructure.Data.Repositories
{
    public class TransitionsRepository : ITransitionsRepository
    {
        private readonly AppDbContext _db;

        public TransitionsRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<bool> AreAnySourceTransitionsAfter(int accountId, DateTime date, CancellationToken cancellationToken)
        {
            return await _db.Transitions
                .AnyAsync(t => t.SourceAccountId == accountId && t.Date > date, cancellationToken);
        }
    }
}
