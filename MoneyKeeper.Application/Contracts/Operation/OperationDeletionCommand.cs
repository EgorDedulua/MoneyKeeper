using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.Application.Contracts.Operation
{
    public class OperationDeletionCommand : IOperationOwnershipValidationModel
    {
        public OperationDeletionCommand(int userId, int operationId)
        {
            UserId = userId; OperationId = operationId;
        }

        public int UserId { get; set; }

        public int OperationId { get; set; }
    }
}
