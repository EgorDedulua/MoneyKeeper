using Microsoft.AspNetCore.Mvc;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Contracts.BalanceChanging;
using MoneyKeeper.Contracts.BalanceChanging;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Extensions;

namespace MoneyKeeper.Controllers
{
    [Route("api/balanceChangings")]
    [ApiController]
    public class BalanceChangingsController : ControllerBase
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IBalanceChangingsService _balanceChangingsService;

        public BalanceChangingsController(ICurrentUserService currentUserService, IBalanceChangingsService balanceChangingsService)
        {
            _currentUserService = currentUserService;
            _balanceChangingsService = balanceChangingsService;
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] BalanceChangingUpsertRequest request, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            BalanceChangingCreationCommand command =
                new BalanceChangingCreationCommand(userId, request.AccountId, request.NewAccountBalance);

            Result<BalanceChangingResponse> result = await _balanceChangingsService.Add(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult();
        }

        [HttpDelete("{balanceChangingId}")]
        public async Task<IActionResult> Delete(int balanceChangingId, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            Result<bool> result = await _balanceChangingsService.Delete(new BalanceChangingDeletionCommand(userId, balanceChangingId), cancellationToken);

            if (result.IsSuccess)
                return NoContent();

            return result.ToErrorActionResult();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] BalanceChangingQueryParameters parameters, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            Result<PagedResult<BalanceChangingResponse>> result = await _balanceChangingsService.GetAll(parameters, userId, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult();
        }

        [HttpPut("{balanceChangingId}")]
        public async Task<IActionResult> Update(int balanceChangingId, [FromBody] BalanceChangingUpsertRequest request, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            BalanceChangingUpdateCommand command =
                new BalanceChangingUpdateCommand(userId, balanceChangingId, request.AccountId, request.NewAccountBalance);

            Result<BalanceChangingResponse> result = await _balanceChangingsService.Update(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult();
        }
    }
}
