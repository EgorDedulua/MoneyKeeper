using MoneyKeeper.Application.Contracts.Common;
using MoneyKeeper.Application.Sorting;

namespace MoneyKeeper.Application.Contracts.Category
{
    public class CategoryQueryParameters : IPagedQueryParameters
    {
        public bool? IsOnlyIncome { get; set; }

        public string? NameSubstring { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public List<SortCriterion> SortBy { get; set; } = [];
    }
}
