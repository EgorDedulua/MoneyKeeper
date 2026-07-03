using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Contracts.Operation;
using MoneyKeeper.Contracts.Operation;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Extensions;

namespace MoneyKeeper.Controllers
{
    [Route("api/operations")]
    [ApiController]
    [Authorize]
    public class OperationsController : ControllerBase
    {
        private readonly IOperationsService _operationsService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<OperationsController> _logger;

        public OperationsController(IOperationsService operationsService, ICurrentUserService currentUserService, ILogger<OperationsController> logger)
        {
            _operationsService = operationsService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] OperationQueryParameters parameters, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            Result<PagedResult<OperationResponse>> result = 
                await _operationsService.GetAll(parameters, userId, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            _logger.LogWarning("Ошибка при получении пользователем с id {UserId} своих операций: {ErrorCode}", userId, result.Error!.ErrorCode);
            
            return result.ToErrorActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] OperationUpsertRequest request, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            OperationCreationCommand command = 
                new OperationCreationCommand(userId, request.AccountId, request.CategoryId, request.Sum, request.Description);

            Result<OperationResponse> result = await _operationsService.Add(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            _logger.LogWarning(
                "Ошибка добавления операции пользователем {UserId} (счёт {AccountId}, категория {CategoryId}): {ErrorCode}",
                userId, request.AccountId, request.CategoryId, result.Error!.ErrorCode);

            return result.ToErrorActionResult();
        }

        [HttpDelete("{operationId}")]
        public async Task<IActionResult> Delete(int operationId, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            Result<bool> result = await _operationsService.Delete(new OperationDeletionCommand(userId, operationId), cancellationToken);

            if (result.IsSuccess)
                return NoContent();

            _logger.LogWarning(
                "Ошибка удаления операции {OperationId} пользователем {UserId}: {ErrorCode}",
                operationId, userId, result.Error!.ErrorCode);

            return result.ToErrorActionResult();
        }

        [HttpPut("{operationId}")]
        public async Task<IActionResult> Update(int operationId, [FromBody] OperationUpsertRequest request, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            OperationUpdateCommand command =
                new OperationUpdateCommand(operationId, userId, request.AccountId, request.CategoryId, request.Sum, request.Description);

            Result<OperationResponse> result = await _operationsService.Update(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            _logger.LogWarning(
                "Ошибка обновления операции {OperationId} пользователем {UserId} (счёт {AccountId}): {ErrorCode}",
                operationId, userId, request.AccountId, result.Error!.ErrorCode);

            return result.ToErrorActionResult();
        }
    }
}
