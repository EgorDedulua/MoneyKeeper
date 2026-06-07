using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Enums;
using MoneyKeeper.Core.Models;

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

        public async Task<bool> IsTailValidAfterChange(int accountId, DateTime fromDate, decimal newInitialBalance, CancellationToken ct)
        {
            var operations = await _context.Operations
                .Where(o => o.AccountId == accountId && o.Date > fromDate)
                .Include(o => o.Category)
                .AsNoTracking()
                .ToListAsync(ct);

            var outgoingTransitions = await _context.Transitions
                .Where(t => t.SourceAccountId == accountId && t.Date > fromDate)
                .AsNoTracking()
                .ToListAsync(ct);

            var incomingTransitions = await _context.Transitions
                .Where(t => t.DestinationAccountId == accountId && t.Date > fromDate)
                .AsNoTracking()
                .ToListAsync(ct);

            var balanceChangings = await _context.BalanceChangings
                .Where(bc => bc.AccountId == accountId && bc.Date > fromDate)
                .AsNoTracking()
                .ToListAsync(ct);

            var events = new List<(DateTime Date, int TypeOrder, int Id, object Event)>();

            foreach (var op in operations)
                events.Add((op.Date, 1, op.Id, op));
            foreach (var t in outgoingTransitions)
                events.Add((t.Date, 2, t.Id, (Transition: t, IsSource: true)));
            foreach (var t in incomingTransitions)
                events.Add((t.Date, 2, t.Id, (Transition: t, IsSource: false)));
            foreach (var bc in balanceChangings)
                events.Add((bc.Date, 3, bc.Id, bc));

            var sorted = events
                .OrderBy(e => e.Date)
                .ThenBy(e => e.TypeOrder)
                .ThenBy(e => e.Id)
                .ToList();

            decimal running = newInitialBalance;
            foreach (var (_, _, _, evt) in sorted)
            {
                switch (evt)
                {
                    case Operation op:
                        if (op.Category.Type == CategoryType.Income)
                            running += op.Sum;
                        else
                            running -= op.Sum;
                        break;

                    case (Transition t, bool isSource):
                        if (isSource)
                            running -= t.Sum;
                        else
                            running += t.Sum;
                        break;

                    case BalanceChanging bc:
                        running = bc.NewAccountBalance;
                        break;
                }

                if (running < 0)
                    return false;
            }

            return true;
        }
    }
}
