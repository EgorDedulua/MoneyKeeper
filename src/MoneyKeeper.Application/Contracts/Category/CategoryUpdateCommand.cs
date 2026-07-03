using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Core.Enums;

namespace MoneyKeeper.Application.Contracts.Category
{
    public class CategoryUpdateCommand : ICategoryOwnershipValidationModel
    { 
        public CategoryUpdateCommand(int userId, int categoryId, string name, string? description, CategoryType type)
        {
            UserId = userId; CategoryId = categoryId; Name = name; Description = description; Type = type;
        }

        public int UserId { get; set; }

        public int CategoryId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description {  get; set; }

        public CategoryType Type { get; set; }
    }

}
