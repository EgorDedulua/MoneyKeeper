using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Infrastructure.Data.Repositories
{
    public class InboxRepository : IInboxRepository
    {
        private readonly AppDbContext _db;

        public InboxRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(InboxMessage message, CancellationToken cancellationToken = default)
        {
            await _db.AddAsync(message, cancellationToken)
                .ConfigureAwait(false);

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            return await _db.InboxMessages
                .AnyAsync(m => m.Id == messageId, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
