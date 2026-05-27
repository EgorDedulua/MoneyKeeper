using MoneyKeeper.Application.Contracts.Transition;
using MoneyKeeper.Core.Common;

namespace MoneyKeeper.Application.Common.Services
{
    public interface ITransitionsService
    {
        Task<Result<PagedResult<TransitionResponse>>> GetAll(TransitionQueryParameters parameters, int userId, CancellationToken cancellationToken = default);

        Task<Result<TransitionResponse>> Add(TransitionCreationCommand command, CancellationToken cancellationToken = default);

        Task<Result<bool>> Delete(int transitionId, CancellationToken cancellationToken = default);

        Task<Result<TransitionResponse>> Update(TransitionUpdateCommand command, CancellationToken cancellationToken = default);
    }
}
