using MoneyKeeper.Core.Enums;

namespace MoneyKeeper.Contracts.Category
{
    public record CategoryUpsertRequest(string Name, string? Description, CategoryType Type);
}
