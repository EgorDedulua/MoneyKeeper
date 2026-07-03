namespace MoneyKeeper.Application.Contracts.Transition
{
    public record TransitionResponse(int Id, int SourceAccountId, int DestinationAccountId, string SourceAccountName, 
        string DestinationAccountName, decimal Sum, string? Description, decimal OldSourceAccountBalance, decimal NewSourceAccountBalance,
        decimal OldDestinationAccountBalance, decimal NewDestinationAccountBalance, DateTime Date);
}
