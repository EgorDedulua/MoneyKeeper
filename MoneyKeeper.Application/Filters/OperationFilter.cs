using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Application.Filters
{
    public class OperationFilter : IFilter<Operation>
    {
        public List<int> AccountIds { get; set; } = [];

        public List<int> CategoryIds { get; set; } = [];

        public decimal? MinSum { get; set; }

        public decimal? MaxSum { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public IQueryable<Operation> ApplyTo(IQueryable<Operation> query)
        {
            if (AccountIds.Any())
                query = query.Where(o => AccountIds.Contains(o.AccountId));

            if (CategoryIds.Any())
                query = query.Where(o => CategoryIds.Contains(o.CategoryId));

            if (MinSum.HasValue)
                query = query.Where(o => o.Sum >= MinSum.Value);

            if (MaxSum.HasValue)
                query = query.Where(o => o.Sum <= MaxSum.Value);

            if (FromDate.HasValue)
                query = query.Where(o => o.Date >= FromDate.Value);

            if (ToDate.HasValue)
                query = query.Where(o => o.Date <= ToDate.Value);

            return query;
        }
    }
}
