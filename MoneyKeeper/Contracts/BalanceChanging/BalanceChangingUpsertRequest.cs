namespace MoneyKeeper.Contracts.BalanceChanging
{
    public record BalanceChangingUpsertRequest(int AccountId, decimal NewAccountBalance, string? Description);
}
