namespace MoneyKeeper.Application.Contracts.User
{
    public record RegisterUserCommand(string Login, string Name, string Password);
}
