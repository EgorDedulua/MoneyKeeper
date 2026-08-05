namespace MoneyKeeper.Application.Common
{
    public interface IFilter<T>
    {
        IQueryable<T> ApplyTo(IQueryable<T> query);
    }
}
