using MoneyKeeper.Core.Models;
using MoneyKeeper.Core.Enums;
using MoneyKeeper.Application.Common;

namespace MoneyKeeper.Application.Filters
{
    public class CategoriesFilter : IFilter<Category>
    {
        public bool? IsOnlyIncome { get; set; }

        public string? NameSubstring { get; set; }

        public IQueryable<Category> ApplyTo(IQueryable<Category> query)
        {
            if (IsOnlyIncome.HasValue)
                query = query.Where(c => (c.Type == CategoryType.Income) == IsOnlyIncome.Value);

            if (!string.IsNullOrWhiteSpace(NameSubstring))
                query = query.Where(c => c.Name.ToLower().Contains(NameSubstring.ToLower()));

            return query;
        }
    }
}
