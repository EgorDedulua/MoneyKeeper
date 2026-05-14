namespace MoneyKeeper.Application.Common
{
    public interface IUnitOfWork
    {
        Task BeginTransactionAsync();

        Task CommitTransactionAsync();

        Task RollbackTransactionAsync();

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
