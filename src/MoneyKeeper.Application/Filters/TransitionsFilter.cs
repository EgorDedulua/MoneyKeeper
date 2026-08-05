using MoneyKeeper.Application.Common;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Application.Filters
{
    public class TransitionsFilter : IFilter<Transition>
    {
        public List<int> SourceAccountIds { get; set; } = [];

        public List<int> DestinationAccountIds { get; set; } = [];

        public decimal? MinSum { get; set; }

        public decimal? MaxSum { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public IQueryable<Transition> ApplyTo(IQueryable<Transition> query)
        {
            if (SourceAccountIds.Any())
                query = query.Where(t => SourceAccountIds.Contains(t.SourceAccountId));

            if (DestinationAccountIds.Any())
                query = query.Where(t => DestinationAccountIds.Contains(t.DestinationAccountId));

            if (MinSum.HasValue)
                query = query.Where(t => t.Sum >= MinSum.Value);

            if (MaxSum.HasValue)
                query = query.Where(t => t.Sum <= MaxSum.Value);

            if (FromDate.HasValue)
                query = query.Where(t => t.Date >= FromDate.Value);

            if (ToDate.HasValue)
                query = query.Where(t => t.Date <= ToDate.Value);

            return query;
        }
    }
}
