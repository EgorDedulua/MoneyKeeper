namespace MoneyKeeper.Application.Contracts.BalanceChanging
{
    public record BalanceChangingResponse(int Id, int AccountId, DateTime Date,
        decimal OldAccountBalance, decimal NewAccountBalance, string AccountName);
}
