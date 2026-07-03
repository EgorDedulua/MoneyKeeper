using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.Application.Contracts.BalanceChanging
{
    public class BalanceChangingCreationCommand : IAccountOwnershipValidationModel
    {
        public BalanceChangingCreationCommand(int userId, int accountId, decimal newAccountBalance)
        {
            UserId = userId; AccountId = accountId; NewAccountBalance = newAccountBalance;
        }

        public int UserId { get; set; }

        public int AccountId { get; set; }

        public decimal NewAccountBalance { get; set; }
    }
}
