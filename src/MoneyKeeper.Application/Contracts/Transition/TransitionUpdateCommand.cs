using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.Application.Contracts.Transition
{
    public class TransitionUpdateCommand : ITransitionAccountsValidationModel, ITransitionOwnershipValidationModel
    {
        public TransitionUpdateCommand(int transitionId, int userId, int sourceAccountId, int destinationAccountId, decimal sum, string? description) 
        { 
            TransitionId = transitionId; UserId = userId; SourceAccountId = sourceAccountId; DestinationAccountId = destinationAccountId; Sum = sum; Description = description;
        }

        public int TransitionId { get; set; }

        public int UserId { get; set; }

        public int SourceAccountId { get; set; }

        public int DestinationAccountId { get; set; }

        public decimal Sum { get; set; }

        public string? Description { get; set; }
    }
}
