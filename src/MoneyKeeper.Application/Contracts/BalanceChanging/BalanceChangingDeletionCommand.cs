using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.Application.Contracts.BalanceChanging
{
    public class BalanceChangingDeletionCommand : IBalanceChangingOwnershipValidationModel
    {
        public BalanceChangingDeletionCommand(int userId, int balanceChangingId)
        {
            UserId = userId; BalanceChangingId = balanceChangingId;
        }

        public int UserId { get; set; }

        public int BalanceChangingId { get; set; }
    }
}
