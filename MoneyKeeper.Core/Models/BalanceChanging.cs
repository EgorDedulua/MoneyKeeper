using MoneyKeeper.Core.Common;

namespace MoneyKeeper.Core.Models
{
    public class BalanceChanging : IBalanceEvent
    {
        public int Id { get; set; }

        public int AccountId { get; set; }

        public Account Account { get; set; } = null!;

        public DateTime Date { get; set; }

        public decimal OldAccountBalance { get; set; }

        public decimal NewAccountBalance { get; set; }

        public void ApplyForAccount(int accountId, ref decimal runningBalance)
        {
            OldAccountBalance = runningBalance;
            runningBalance = NewAccountBalance;
        }
    }
}
