using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Core.Common.Repositories
{
    public interface ITransitionsRepository
    {
        Task<bool> AreAnySourceTransitionsAfterAsync(int accountId, DateTime date, CancellationToken cancellationToken = default);

        IQueryable<Transition> GetAllByUserId(int userId);

        Task DeleteAsync(int transitionId, CancellationToken cancellationToken = default);

        Task AddAsync(Transition transition, CancellationToken cancellationToken = default);

        Task<Transition?> GetByIdAsync(int transitionId, CancellationToken cancellationToken = default);

        Task<(List<Transition> items, int totalCount)> GetAllPagedAsync(IQueryable<Transition> query, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<Transition> UpdateAsync(Transition transition, CancellationToken cancellationToken = default);
    }
}
