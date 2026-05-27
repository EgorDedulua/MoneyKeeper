using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.Application.Contracts.Transition
{
    public class TransitionCreationCommand : ITransitionAccountsValidationModel
    {
        public TransitionCreationCommand(int userId, int sourceAccountId, int destinationAccountId, decimal sum, string? description) 
        {
            UserId = userId; SourceAccountId = sourceAccountId; DestinationAccountId = destinationAccountId; Sum = sum; Description = description;
        }

        public int UserId { get; set; }

        public int SourceAccountId { get; set; }

        public int DestinationAccountId { get; set; }

        public decimal Sum {  get; set; }

        public string? Description { get; set; }
    }
}
