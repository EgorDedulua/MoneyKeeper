using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Core.Common.Repositories
{
    public interface ICategoriesRepository
    {
        Task AddAsync(Category category, CancellationToken cancellationToken = default);

        Task DeleteAsync(int categoryId, CancellationToken cancellationToken = default);

        IQueryable<Category> GetAllByUserId(int userId);

        Task<(List<Category> items, int totalCount)> GetAllPagedAsync(IQueryable<Category> query, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<Category?> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default);

        Task<Category> UpdateAsync(Category category, CancellationToken cancellationToken = default);
    }
}
