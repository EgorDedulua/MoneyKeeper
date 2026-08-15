using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Contracts.Transition;
using MoneyKeeper.Contracts.Transition;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Extensions;

namespace MoneyKeeper.Controllers
{
    [Route("api/transitions")]
    [ApiController]
    [Authorize]
    public class TransitionsController : ControllerBase
    {
        private readonly ITransitionsService _transitionsService;
        private readonly ICurrentUserService _currentUserService;

        public TransitionsController(ITransitionsService transitionsService, ICurrentUserService currentUserService)
        {
            _transitionsService = transitionsService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] TransitionQueryParameters parameters, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            Result<PagedResult<TransitionResponse>> result =
                await _transitionsService.GetAll(parameters, userId, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult(this);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] TransitionUpsertRequest request, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            TransitionCreationCommand command =
                new TransitionCreationCommand(userId, request.SourceAccountId, request.DestinationAccountId, request.Sum, request.Description);

            Result<TransitionResponse> result = await _transitionsService.Add(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult(this);
        }

        [HttpPut("{transitionId}")]
        public async Task<IActionResult> Update(int transitionId, [FromBody] TransitionUpsertRequest request, 
            CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            TransitionUpdateCommand command =
                new TransitionUpdateCommand(transitionId, userId, request.SourceAccountId, request.DestinationAccountId, request.Sum, request.Description);

            Result<TransitionResponse> result = await _transitionsService.Update(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult(this);
        }

        [HttpDelete("{transitionId}")]
        public async Task<IActionResult> Delete(int transitionId, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            Result<bool> result = await _transitionsService.Delete(new TransitionDeletionCommand(userId, transitionId), cancellationToken);

            if (result.IsSuccess)
                return NoContent();

            return result.ToErrorActionResult(this);
        }
    }
}
