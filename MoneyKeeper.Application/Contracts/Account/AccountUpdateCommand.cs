using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.Application.Contracts.Account
{
    public class AccountUpdateCommand : IAccountOwnershipValidationModel
    {
        public AccountUpdateCommand(int userId, int accountId, string name, decimal balance, decimal? target, string? description)
        {
            UserId = userId; AccountId = accountId; Name = name; Balance = balance; Target = target; Description = description;
        }

        public int UserId { get; set; }

        public int AccountId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Balance { get; set; }

        public decimal? Target {  get; set; }

        public string? Description { get; set; }
    }
}
