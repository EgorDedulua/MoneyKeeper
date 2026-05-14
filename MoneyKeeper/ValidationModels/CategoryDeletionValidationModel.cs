using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.ValidationModels
{
    public class CategoryDeletionValidationModel : ICategoryOwnershipValidationModel
    {
        public int UserId { get; set; }

        public int CategoryId { get; set; }
    }
}
