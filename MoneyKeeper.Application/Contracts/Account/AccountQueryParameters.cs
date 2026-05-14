using MoneyKeeper.Application.Contracts.Common;
using MoneyKeeper.Application.Sorting;

namespace MoneyKeeper.Application.Contracts.Account
{
    public class AccountQueryParameters : IPagedQueryParameters
    {
        public decimal? MinBalance { get; set; }

        public decimal? MaxBalance { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string? NameSubstring { get; set; }

        public bool? HasTargetedBalance { get; set; }

        public List<SortCriterion> SortBy { get; set; } = [];

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
