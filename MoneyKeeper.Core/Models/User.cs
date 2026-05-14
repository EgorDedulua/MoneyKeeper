namespace MoneyKeeper.Core.Models;

public class User
{
    public int Id { get; set; }

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
