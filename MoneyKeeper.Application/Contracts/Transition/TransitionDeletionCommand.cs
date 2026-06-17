using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.Application.Contracts.Transition
{
    public class TransitionDeletionCommand : ITransitionOwnershipValidationModel
    {
        public TransitionDeletionCommand(int userId, int transitionId)
        {
            UserId = userId;
            TransitionId = transitionId;
        }

        public int UserId { get; set; }

        public int TransitionId { get; set; }
    }
}
