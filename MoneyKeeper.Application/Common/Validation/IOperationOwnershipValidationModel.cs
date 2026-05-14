namespace MoneyKeeper.Application.Common.Validation
{
    public interface IOperationOwnershipValidationModel
    {
        int UserId { get; set; }

        int OperationId { get; set; }
    }
}
