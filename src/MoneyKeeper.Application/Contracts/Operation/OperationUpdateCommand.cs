using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.Application.Contracts.Operation
{
    public class OperationUpdateCommand : ICategoryOwnershipValidationModel, IAccountOwnershipValidationModel, IOperationOwnershipValidationModel
    {
        public OperationUpdateCommand(int operationId, int userId, int accountId, int categoryId, decimal sum, string? description)
        {
            OperationId = operationId; UserId = userId; AccountId = accountId; CategoryId = categoryId; Sum = sum; Description = description;
        }

        public int OperationId { get; set; }

        public int UserId { get; set; }

        public int CategoryId { get; set; }

        public int AccountId { get; set; }

        public decimal Sum { get; set; }

        public string? Description { get; set; }
    }
}
