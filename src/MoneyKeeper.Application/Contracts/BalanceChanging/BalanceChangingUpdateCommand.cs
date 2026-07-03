using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.Application.Contracts.BalanceChanging
{
    public class BalanceChangingUpdateCommand : IBalanceChangingOwnershipValidationModel, IAccountOwnershipValidationModel
    {
        public BalanceChangingUpdateCommand(int userId, int balanceChangingId, int accountId, decimal newAccountBalance)
        {
            UserId = userId;
            BalanceChangingId = balanceChangingId;
            AccountId = accountId;
            NewAccountBalance = newAccountBalance;
        }

        public int UserId { get; set; }

        public int BalanceChangingId { get; set; }

        public int AccountId { get; set; }

        public decimal NewAccountBalance { get; set; }
    }
}
