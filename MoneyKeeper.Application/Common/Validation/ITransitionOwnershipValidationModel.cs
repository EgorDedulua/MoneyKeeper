namespace MoneyKeeper.Application.Common.Validation
{
    public interface ITransitionOwnershipValidationModel
    {
        int UserId { get; }

        int TransitionId { get; }
    }
}
