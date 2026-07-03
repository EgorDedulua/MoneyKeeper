namespace MoneyKeeper.Application.Common.Services
{
    public interface ICommonBalanceOperationsRepository
    {
        Task RecalculateAllTailsAsync(int accountId, DateTime fromDate, decimal initialBalance, 
            CancellationToken cancellationToken = default);

        Task<bool> IsTailValidAfterChange(int accountId, DateTime fromDate, decimal newInitialBalance, CancellationToken ct);
    }
}
