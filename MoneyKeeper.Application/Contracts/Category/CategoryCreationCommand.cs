using MoneyKeeper.Core.Enums;

namespace MoneyKeeper.Application.Contracts.Category
{
    public record CategoryCreationCommand(int UserId, string Name, string? Description, CategoryType Type);
}
