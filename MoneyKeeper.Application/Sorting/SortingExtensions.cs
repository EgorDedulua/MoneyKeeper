using System.Linq.Dynamic.Core;

namespace MoneyKeeper.Application.Sorting
{
    public static class SortingExtensions
    {
        public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, List<SortCriterion> sortBy)
        {
            if (sortBy is null || !sortBy.Any())
                return query;

            string orderClause = string.Join(", ", sortBy
                .Select(s => $"{s.Field} {(s.Descending? "desc" : "asc")}"));
            query.OrderBy(orderClause);
            return query;
        }
    }
}
