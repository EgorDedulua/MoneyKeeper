using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Core.Common.Repositories
{
    public interface IOperationsRepository
    {
        Task AddAsync(Operation operation, CancellationToken cancellationToken = default);

        Task DeleteAsync(int operationId, CancellationToken cancellationToken = default);

        IQueryable<Operation> GetAllByUserId(int userId);

        Task<(List<Operation> items, int totalCount)> GetAllPagedAsync(IQueryable<Operation> query, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<Operation?> GetByIdAsync(int operationId, CancellationToken cancellationToken = default);

        Task<Operation> UpdateAsync(Operation operation, CancellationToken cancellationToken = default);

        Task<bool> AreAnyConsumptionOperationsAfterAsync(int accountId, DateTime date, CancellationToken cancellationToken = default);

        Task<bool> AreAnyOperationsWithCategory(int categoryId, CancellationToken cancellationToken = default);
    }
}
