using MoneyKeeper.Application.Contracts.User;
using MoneyKeeper.Core.Common;

namespace MoneyKeeper.Application.Common.Services
{
    public interface IUsersService
    {
        Task<Result<AuthenticationResult>> Register(RegisterUserCommand command, CancellationToken cancellationToken = default);

        Task<Result<AuthenticationResult>> Login(LoginUserCommand command, CancellationToken cancellationToken = default);
    }
}
