using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.ValidationModels
{
    public class AccountDeletionValidationModel : IAccountOwnershipValidationModel
    {
        public int UserId { get; set; }

        public int AccountId { get; set; }
    }
}
