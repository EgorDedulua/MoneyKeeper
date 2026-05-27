using MoneyKeeper.Application.Contracts.Common;
using MoneyKeeper.Application.Sorting;

namespace MoneyKeeper.Application.Contracts.Transition
{
    public class TransitionQueryParameters : IPagedQueryParameters
    {
        public List<int> SourceAccountIds { get; set; } = [];

        public List<int> DestinationAccountIds { get; set; } = [];

        public decimal? MinSum { get; set; }

        public decimal? MaxSum { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public List<SortCriterion> SortBy { get; set; } = [];

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
