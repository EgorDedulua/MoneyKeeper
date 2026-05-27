using MoneyKeeper.Application.Common.Validation;

namespace MoneyKeeper.ValidationModels
{
    public class TransitionDeletionValidationModel : ITransitionOwnershipValidationModel
    {
        public int UserId { get; set; }

        public int TransitionId { get; set; }
    }
}
