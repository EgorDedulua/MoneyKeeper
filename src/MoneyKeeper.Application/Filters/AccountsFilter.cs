using MoneyKeeper.Application.Common;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Application.Filters
{
    public class AccountsFilter : IFilter<Account>
    {
        public decimal? MinBalance { get; set; }

        public decimal? MaxBalance { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string? NameSubstring { get; set; }

        public bool? HasTargetedBalance { get; set; }

        public IQueryable<Account> ApplyTo(IQueryable<Account> query)
        {
            if (MinBalance.HasValue)
                query = query.Where(a => a.Balance >= MinBalance.Value);

            if (MaxBalance.HasValue)
                query = query.Where(a => a.Balance <= MaxBalance.Value);

            if (FromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= FromDate.Value);

            if (ToDate.HasValue)
                query = query.Where(a => a.CreatedAt <= ToDate.Value);

            if (HasTargetedBalance.HasValue)
                query = query.Where(a => (a.Target != null) == HasTargetedBalance.Value);

            if (!string.IsNullOrWhiteSpace(NameSubstring))
                query = query.Where(a => a.Name.ToLower().Contains(NameSubstring.ToLower()));

            return query;
        }
    }
}
