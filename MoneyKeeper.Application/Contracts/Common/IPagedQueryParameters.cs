namespace MoneyKeeper.Application.Contracts.Common
{
    public interface IPagedQueryParameters
    {
        int Page { get; }

        int PageSize { get; }
    }
}
