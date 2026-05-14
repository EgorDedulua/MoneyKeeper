using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.ValidationModels
{
    public class BalanceChangingDeletionValidationModel : IBalanceChangingOwnershipValidationModel
    {
        public int UserId { get; set; }

        public int BalanceChangingId { get ; set; }
    }
}
