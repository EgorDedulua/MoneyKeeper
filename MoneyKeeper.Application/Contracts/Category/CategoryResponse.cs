using MoneyKeeper.Core.Enums;

namespace MoneyKeeper.Application.Contracts.Category
{
    public record CategoryResponse(int Id, string Name, string? Description, CategoryType Type);
}
