using MoneyKeeper.Core.Enums;

namespace MoneyKeeper.Core.Models;

public class Category
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public CategoryType Type { get; set; }

    public virtual User? User { get; set; }
}
