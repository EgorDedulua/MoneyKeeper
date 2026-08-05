using MoneyKeeper.Application.Common;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Application.Filters
{
    public class BalanceChangingsFilter : IFilter<BalanceChanging>
    {
        public List<int> AccountIds { get; set; } = [];

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public IQueryable<BalanceChanging> ApplyTo(IQueryable<BalanceChanging> query)
        {
            if (AccountIds.Any())
                query = query.Where(b => AccountIds.Contains(b.AccountId));

            if (FromDate.HasValue)
                query = query.Where(b => b.Date >= FromDate.Value);

            if (ToDate.HasValue)
                query = query.Where(b => b.Date <= ToDate.Value);

            return query;
        }
    }
}
