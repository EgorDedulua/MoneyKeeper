using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.Application.Contracts.Operation
{
    public class OperationCreationCommand : ICategoryOwnershipValidationModel, IAccountOwnershipValidationModel
    {
        public OperationCreationCommand(int userId, int accountId, int categoryId, decimal sum, string? description)
        {
            UserId = userId; AccountId = accountId; CategoryId = categoryId; Sum = sum; Description = description;
        }

        public int UserId { get; set; }

        public int AccountId { get; set; }

        public int CategoryId { get; set; }

        public decimal Sum { get; set; }

        public string? Description { get; set; }
    }
}
