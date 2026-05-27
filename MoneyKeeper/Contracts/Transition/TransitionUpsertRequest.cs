namespace MoneyKeeper.Contracts.Transition
{
    public record TransitionUpsertRequest(int SourceAccountId, int DestinationAccountId, decimal Sum, string? Description);
}
