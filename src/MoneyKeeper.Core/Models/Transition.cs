using MoneyKeeper.Core.Common;

namespace MoneyKeeper.Core.Models;

public class Transition : IBalanceEvent
{
    public int Id { get; set; }

    public int SourceAccountId { get; set; }

    public int DestinationAccountId { get; set; }

    public decimal Sum { get; set; }

    public string? Description { get; set; }

    public DateTime Date { get; set; }

    public decimal OldSourceAccountBalance { get; set; }

    public decimal NewSourceAccountBalance { get; set; }

    public decimal OldDestinationAccountBalance { get; set; }

    public decimal NewDestinationAccountBalance { get; set; }

    public virtual Account DestinationAccount { get; set; } = null!;

    public virtual Account SourceAccount { get; set; } = null!;

    public void ApplyForAccount(int accountId, ref decimal runningBalance)
    {
        if (accountId == SourceAccountId)
        {
            OldSourceAccountBalance = runningBalance;
            runningBalance -= Sum;
            NewSourceAccountBalance = runningBalance;
        }
        else
        {
            OldDestinationAccountBalance = runningBalance;
            runningBalance += Sum;
            NewDestinationAccountBalance = runningBalance;
        }
    }
}
