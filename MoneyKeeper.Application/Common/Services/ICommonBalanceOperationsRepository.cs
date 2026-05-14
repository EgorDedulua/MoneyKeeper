namespace MoneyKeeper.Application.Common.Services
{
    public interface ICommonBalanceOperationsRepository
    {
        Task RecalculateAllTailsAsync(int accountId, DateTime fromDate, decimal initialBalance, 
            CancellationToken cancellationToken = default);
    }
}
