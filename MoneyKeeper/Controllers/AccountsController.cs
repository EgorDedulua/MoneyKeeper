using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Contracts.Account;
using MoneyKeeper.Contracts.Account;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Extensions;

namespace MoneyKeeper.Controllers
{
    [Route("api/accounts")]
    [ApiController]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountsService _accountsService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AccountsController> _logger;
        
        public AccountsController(IAccountsService accountsService, ICurrentUserService currentUserService, ILogger<AccountsController> logger) 
        {
            _accountsService = accountsService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] AccountQueryParameters parameters, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            Result<PagedResult<AccountResponse>> result = await _accountsService.GetAll(parameters, userId, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            _logger.LogWarning("Ошибка при получении пользователем с id {UserId} своих категорий: {ErrorCode}", userId, result.Error!.ErrorCode);
            return result.ToErrorActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AccountUpsertRequest request, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            AccountCreationCommand command =
               new AccountCreationCommand(userId, request.Name, request.Balance, request.Target, request.Description?.Trim());

            Result<AccountResponse> result = await _accountsService.Add(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            _logger.LogWarning("Ошибка добавления счета пользователем с id {UserId}: {ErrorCode}", userId, result.Error!.ErrorCode);
            return result.ToErrorActionResult();
        }

        [HttpDelete("{accountId}")]
        public async Task<IActionResult> Delete(int accountId, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            Result<bool> result = await _accountsService.Delete(new AccountDeletionCommand(userId, accountId), cancellationToken);

            if (result.IsSuccess)
                return NoContent();

            _logger.LogWarning
                ("Ошибка удаления счета с id {AccountId} пользователем с id {UserId}: {ErrorCode}", accountId, userId, result.Error!.ErrorCode);
            return result.ToErrorActionResult();
        }

        [HttpPut("{accountId}")]
        public async Task<IActionResult> Update(int accountId, [FromBody] AccountUpsertRequest request, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            AccountUpdateCommand command =
                new AccountUpdateCommand(userId, accountId, request.Name, request.Balance, request.Target, request.Description);

            Result<AccountResponse> result = await _accountsService.Update(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            _logger.LogWarning
               ("Ошибка обновления счета с id {AccountId} пользователем с id {UserId}: {ErrorCode}", accountId, userId, result.Error!.ErrorCode);
            return result.ToErrorActionResult();
        }
    }
}
