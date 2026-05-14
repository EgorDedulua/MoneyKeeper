using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Core.Common;
using System.Runtime.CompilerServices;

namespace MoneyKeeper.Infrastructure.Data.Repositories
{
    public class CommonBalanceOperationsRepository : ICommonBalanceOperationsRepository
    {
        private readonly AppDbContext _context;

        public CommonBalanceOperationsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task RecalculateAllTailsAsync(int accountId, DateTime fromDate, decimal initialBalance, CancellationToken cancellationToken)
        {
            List<IBalanceEvent> events = new List<IBalanceEvent>();

            events.AddRange(await _context.Operations
                .Where(o => o.AccountId == accountId && o.Date > fromDate)
                .ToListAsync(cancellationToken));

            events.AddRange(await _context.Transitions
                .Where(t => t.SourceAccountId == accountId && t.Date > fromDate)
                .ToListAsync(cancellationToken));

            events.AddRange(await _context.Transitions
                .Where(t => t.DestinationAccountId == accountId && t.Date > fromDate)
                .ToListAsync(cancellationToken));

            events.AddRange(await _context.BalanceChangings
                .Where(b => b.AccountId == accountId && b.Date > fromDate)
                .ToListAsync(cancellationToken));

            List<IBalanceEvent> sortedEvents = events
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Id)
                .ToList();

            decimal runningBalance = initialBalance;

            foreach (IBalanceEvent e in sortedEvents)
                e.ApplyForAccount(accountId, ref runningBalance);

            await _context.SaveChangesAsync(cancellationToken);

            decimal newBalance = sortedEvents.Any() ? runningBalance : initialBalance;
            await _context.Accounts
                .Where(a => a.Id == accountId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Balance, newBalance), cancellationToken);
        }
    }
}
