using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Core.Common.Repositories
{
    public interface IInboxRepository
    {
        Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default);

        Task AddAsync(InboxMessage message, CancellationToken cancellationToken = default);
    }
}
