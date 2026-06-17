using MoneyKeeper.Application.Contracts.Operation;
using MoneyKeeper.Core.Common;

namespace MoneyKeeper.Application.Common.Services
{
    public interface IOperationsService
    {
        Task<Result<PagedResult<OperationResponse>>> GetAll(OperationQueryParameters parameters, int userId, CancellationToken cancellationToken = default);

        Task<Result<OperationResponse>> Add(OperationCreationCommand command, CancellationToken cancellationToken = default);

        Task<Result<bool>> Delete(OperationDeletionCommand command, CancellationToken cancellationToken = default);

        Task<Result<OperationResponse>> Update(OperationUpdateCommand command, CancellationToken cancellationToken = default);
    }
}
