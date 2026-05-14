namespace MoneyKeeper.Application.Contracts.Account
{
    public record AccountResponse(int Id, string Name, decimal Balance, decimal? Target, string? Description, DateTime CreatedAt);
}
