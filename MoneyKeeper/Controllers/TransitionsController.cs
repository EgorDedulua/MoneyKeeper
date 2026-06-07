using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Application.Contracts.Transition;
using MoneyKeeper.Contracts.Transition;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Extensions;
using MoneyKeeper.ValidationModels;

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

            return result.ToErrorActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] TransitionUpsertRequest request, IValidator<TransitionUpsertRequest> dataValidator,
            IValidator<ITransitionAccountsValidationModel> accountsValidator, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            TransitionCreationCommand command =
                new TransitionCreationCommand(userId, request.SourceAccountId, request.DestinationAccountId, request.Sum, request.Description);

            ValidationResult[] validationResults = new ValidationResult[]
            {
                await dataValidator.ValidateAsync(request, cancellationToken),
                await accountsValidator.ValidateAsync(command, cancellationToken)
            };

            IActionResult? errorResult = validationResults.ToErrorActionResult();
            if (errorResult is not null)
                return errorResult;

            Result<TransitionResponse> result = await _transitionsService.Add(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult();
        }

        [HttpPut("{transitionId}")]
        public async Task<IActionResult> Update(int transitionId, [FromBody] TransitionUpsertRequest request, 
            IValidator<ITransitionOwnershipValidationModel> transitionValidator, IValidator<TransitionUpsertRequest> dataValidator,
            IValidator<ITransitionAccountsValidationModel> accountsValidator, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            TransitionUpdateCommand command =
                new TransitionUpdateCommand(transitionId, userId, request.SourceAccountId, request.DestinationAccountId, request.Sum, request.Description);

            ValidationResult[] validationResults = new ValidationResult[]
            {
                await dataValidator.ValidateAsync(request, cancellationToken),
                await transitionValidator.ValidateAsync(command, cancellationToken),
                await accountsValidator.ValidateAsync(command, cancellationToken)
            };

            IActionResult? errorResult = validationResults.ToErrorActionResult();
            if (errorResult is not null)
                return errorResult;

            Result<TransitionResponse> result = await _transitionsService.Update(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult();
        }

        [HttpDelete("{transitionId}")]
        public async Task<IActionResult> Delete(int transitionId, IValidator<ITransitionOwnershipValidationModel> validator, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            ValidationResult validationResult =
                await validator.ValidateAsync(new TransitionDeletionValidationModel { UserId = userId, TransitionId = transitionId }, cancellationToken);
            if (!validationResult.IsValid)
                return validationResult.ToErrorActionResult();

            Result<bool> result = await _transitionsService.Delete(transitionId, cancellationToken);

            if (result.IsSuccess)
                return NoContent();

            return result.ToErrorActionResult();
        }
    }
}
