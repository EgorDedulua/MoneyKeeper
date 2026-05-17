namespace MoneyKeeper.Core.Common.Repositories
{
    public interface ITransitionsRepository
    {
        Task<bool> AreAnySourceTransitionsAfterAsync(int accountId, DateTime date, CancellationToken cancellationToken = default);
    }
}
