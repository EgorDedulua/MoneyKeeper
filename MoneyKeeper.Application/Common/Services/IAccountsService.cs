using MoneyKeeper.Application.Contracts.Account;
using MoneyKeeper.Core.Common;

namespace MoneyKeeper.Application.Common.Services
{
    public interface IAccountsService
    {
        Task<Result<AccountResponse>> Add(AccountCreationCommand command, CancellationToken cancellationToken = default);

        Task<Result<bool>> Delete(int accountId, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<AccountResponse>>> GetAll(AccountQueryParameters parameters,int userId, CancellationToken cancellationToken = default);

        Task<Result<AccountResponse>> Update(AccountUpdateCommand command, CancellationToken cancellationToken = default);
    }
}
