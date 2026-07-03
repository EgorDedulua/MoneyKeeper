using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.Application.Contracts.Account
{
    public class AccountDeletionCommand : IAccountOwnershipValidationModel
    {
        public AccountDeletionCommand(int userId, int accountId)
        {
            UserId = userId;
            AccountId = accountId;
        }

        public int UserId { get; set; }

        public int AccountId { get; set; }
    }
}
