namespace MoneyKeeper.Application.Contracts.User
{
    public record AuthenticationResult(string Login, string Name, int Id, DateTime CreatedAt, string Token);
}
