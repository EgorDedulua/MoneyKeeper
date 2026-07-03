namespace MoneyKeeper.Application.Contracts.Account
{
    public record AccountCreationCommand(int UserId, string Name, decimal Balance, decimal? Target, string? Description);
}
