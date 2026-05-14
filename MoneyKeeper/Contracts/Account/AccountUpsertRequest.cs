namespace MoneyKeeper.Contracts.Account
{
    public record AccountUpsertRequest(string Name, decimal Balance, decimal? Target, string? Description);
}
