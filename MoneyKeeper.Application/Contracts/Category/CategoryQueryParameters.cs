using MoneyKeeper.Application.Contracts.Common;
using MoneyKeeper.Application.Sorting;

namespace MoneyKeeper.Application.Contracts.Category
{
    public class CategoryQueryParameters : IPagedQueryParameters
    {
        public bool? IsOnlyIncome { get; set; }

        public string? NameSubstring { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public List<SortCriterion> SortBy { get; set; } = [];
    }
}
