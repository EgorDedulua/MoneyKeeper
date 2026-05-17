using MoneyKeeper.Application.Contracts.BalanceChanging;
using MoneyKeeper.Core.Common;

namespace MoneyKeeper.Application.Common.Services
{
    public interface IBalanceChangingsService
    {
        Task<Result<PagedResult<BalanceChangingResponse>>> GetAll(BalanceChangingQueryParameters parameters, int userId, CancellationToken cancellationToken = default);

        Task<Result<BalanceChangingResponse>> Add(BalanceChangingCreationCommand command, CancellationToken cancellationToken = default);

        Task<Result<bool>> Delete(int balanceChangingId, CancellationToken cancellationToken = default);
        
        Task<Result<BalanceChangingResponse>> Update(BalanceChangingUpdateCommand command, CancellationToken cancellationToken = default);
    }
}
