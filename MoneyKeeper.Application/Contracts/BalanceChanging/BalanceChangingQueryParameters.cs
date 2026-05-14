using MoneyKeeper.Application.Contracts.Common;
using MoneyKeeper.Application.Sorting;

namespace MoneyKeeper.Application.Contracts.BalanceChanging
{
    public class BalanceChangingQueryParameters : IPagedQueryParameters
    {
        public List<int> AccountIds { get; set; } = [];

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public int Page {  get; set; }

        public int PageSize { get; set; }

        public List<SortCriterion> SortBy { get; set; } = [];
    }
}
