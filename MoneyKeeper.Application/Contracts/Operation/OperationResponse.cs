namespace MoneyKeeper.Application.Contracts.Operation
{
    public record OperationResponse(int Id, int AccountId, int? CategoryId, decimal Sum, string? Description, 
        DateTime Date, decimal OldAccountBalance, decimal NewAccountBalance, string AccountName, string CategoryName);
}
