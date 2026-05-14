namespace MoneyKeeper.Core.Common
{
    public interface IBalanceEvent
    {
        DateTime Date { get; }

        int Id { get; }

        void ApplyForAccount(int accountId, ref decimal runningBalance);
    }
}
