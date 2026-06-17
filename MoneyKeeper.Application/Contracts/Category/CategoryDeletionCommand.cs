using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.Application.Contracts.Category
{
    public class CategoryDeletionCommand : ICategoryOwnershipValidationModel
    {
        public CategoryDeletionCommand(int userId, int categoryId)
        {
            UserId = userId; CategoryId = categoryId;
        }

        public int UserId { get; set; }

        public int CategoryId { get; set; }
    }
}
