using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.ValidationModels
{
    public class OperationDeletionValidationModel : IOperationOwnershipValidationModel
    {
        public int UserId { get; set ; }

        public int OperationId { get ; set ; }
    }
}
