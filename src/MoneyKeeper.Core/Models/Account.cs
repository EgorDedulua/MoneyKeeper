namespace MoneyKeeper.Core.Models;

public class Account
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Balance { get; set; }

    public decimal? Target { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
