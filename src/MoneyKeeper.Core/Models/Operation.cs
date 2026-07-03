using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Enums;

namespace MoneyKeeper.Core.Models;

public class Operation : IBalanceEvent
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public decimal Sum { get; set; }

    public int CategoryId { get; set; }

    public string? Description { get; set; }

    public DateTime Date { get; set; }

    public decimal OldAccountBalance { get; set; }

    public decimal NewAccountBalance { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;

    public void ApplyForAccount(int accountId, ref decimal runningBalance)
    {
        OldAccountBalance = runningBalance;
        if (Category.Type == CategoryType.Income)
            runningBalance += Sum;
        else
            runningBalance -= Sum;
        NewAccountBalance = runningBalance;
    }
}
