using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace MoneyKeeper.Infrastructure.Extensions
{
    public static class QueryableExtensions
    {
        public static async Task<(List<T> items, int totalCount)> GetPagedAsync<T>(this IQueryable<T> query,
            int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1)
                page = 1;
            if (pageSize < 1)
                pageSize = 20;

            int totalCount = await query.CountAsync(cancellationToken);
            List<T> items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
