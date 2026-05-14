namespace MoneyKeeper.Contracts.Operation
{
    public record OperationUpsertRequest(int AccountId, int CategoryId, decimal Sum, string? Description);
}
