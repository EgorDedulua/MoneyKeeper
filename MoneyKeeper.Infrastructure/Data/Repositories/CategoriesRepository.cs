using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Infrastructure.Data.Repositories
{
    public class CategoriesRepository : ICategoriesRepository
    {
        private readonly AppDbContext _db;

        public CategoriesRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Category category, CancellationToken cancellationToken)
        {
            await _db.Categories.AddAsync(category, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(int categoryId, CancellationToken cancellationToken)
        {
            await _db.Categories.Where(c => c.Id == categoryId).ExecuteDeleteAsync(cancellationToken);
        }

        public IQueryable<Category> GetAllByUserId(int userId)
        {
            return _db.Categories
                .AsNoTracking()
                .Where(c => c.UserId == userId);
        }

        public async Task<(List<Category> items, int totalCount)> GetAllPagedAsync(IQueryable<Category> query, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            int totalCount = await query.CountAsync(cancellationToken);
            List<Category> items = await query
                .AsNoTracking()
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return (items, totalCount);
        }

        public Task<Category?> GetByIdAsync(int categoryId, CancellationToken cancellationToken)
        {
            return _db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
        }

        public async Task<Category> UpdateAsync(Category category, CancellationToken cancellationToken)
        {
            await _db.Categories
                .Where(c => c.Id == category.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Description, category.Description)
                    .SetProperty(c => c.Name, category.Name)
                    .SetProperty(c => c.Type, category.Type), cancellationToken);

            return await _db.Categories
                .AsNoTracking()
                .FirstAsync(c => c.Id == category.Id, cancellationToken);
        }
    }
}
