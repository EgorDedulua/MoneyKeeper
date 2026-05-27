namespace MoneyKeeper.Application.Common.Validation
{
    public interface ITransitionAccountsValidationModel
    {
        int UserId { get; }

        int SourceAccountId { get; }

        int DestinationAccountId { get; }
    }
}
