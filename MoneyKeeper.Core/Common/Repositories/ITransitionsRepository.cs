namespace MoneyKeeper.Core.Common.Repositories
{
    public interface ITransitionsRepository
    {
        Task<bool> AreAnySourceTransitionsAfter(int accountId, DateTime date, CancellationToken cancellationToken = default);
    }
}
