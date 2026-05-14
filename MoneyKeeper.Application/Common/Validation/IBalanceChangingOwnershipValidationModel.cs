namespace MoneyKeeper.Application.Common.Validation
{
    public interface IBalanceChangingOwnershipValidationModel
    {
        int UserId { get; set; }

        int BalanceChangingId { get; set; }
    }
}
