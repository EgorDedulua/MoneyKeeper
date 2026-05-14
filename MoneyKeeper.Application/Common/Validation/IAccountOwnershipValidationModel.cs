namespace MoneyKeeper.Application.Common.Validation
{
    public interface IAccountOwnershipValidationModel
    {
        int UserId { get; set; }

        int AccountId { get; set; }
    }
}
