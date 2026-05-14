namespace MoneyKeeper.Application.Filters
{
    public interface IFilter<T>
    {
        IQueryable<T> ApplyTo(IQueryable<T> query);
    }
}
