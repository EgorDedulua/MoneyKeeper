using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Application.Contracts.Operation;
using MoneyKeeper.Contracts.Operation;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Extensions;
using MoneyKeeper.ValidationModels;

namespace MoneyKeeper.Controllers
{
    [Route("api/operations")]
    [ApiController]
    [Authorize]
    public class OperationsController : ControllerBase
    {
        private readonly IOperationsService _operationsService;
        private readonly ICurrentUserService _currentUserService;

        public OperationsController(IOperationsService operationsService, ICurrentUserService currentUserService)
        {
            _operationsService = operationsService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] OperationQueryParameters parameters, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            Result<PagedResult<OperationResponse>> result = 
                await _operationsService.GetAll(parameters, userId, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] OperationUpsertRequest request, IValidator<OperationUpsertRequest> dataValidator,
            IValidator<ICategoryOwnershipValidationModel> categoryValidator, IValidator<IAccountOwnershipValidationModel> accountValidator, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            OperationCreationCommand command = 
                new OperationCreationCommand(userId, request.AccountId, request.CategoryId, request.Sum, request.Description);

            ValidationResult[] validationResults = new ValidationResult[]
            {
                await dataValidator.ValidateAsync(request, cancellationToken),
                await categoryValidator.ValidateAsync(command, cancellationToken),
                await accountValidator.ValidateAsync(command, cancellationToken)
            };

            IActionResult? errorResult = validationResults.ToErrorActionResult();
            if (errorResult is not null)
                return errorResult;

            Result<OperationResponse> result = await _operationsService.Add(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult();
        }

        [HttpDelete("{operationId}")]
        public async Task<IActionResult> Delete(int operationId, IValidator<IOperationOwnershipValidationModel> validator, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            ValidationResult validationResult = await validator.ValidateAsync(new OperationDeletionValidationModel { UserId = userId, OperationId = operationId }, cancellationToken);
            if (!validationResult.IsValid)
                return validationResult.ToErrorActionResult();

            Result<bool> result = await _operationsService.Delete(operationId, cancellationToken);

            if (result.IsSuccess)
                return NoContent();

            return result.ToErrorActionResult();
        }

        [HttpPut("{operationId}")]
        public async Task<IActionResult> Update(int operationId, [FromBody] OperationUpsertRequest request, IValidator<IOperationOwnershipValidationModel> operationValidator,
            IValidator<ICategoryOwnershipValidationModel> categoryValidator, IValidator<IAccountOwnershipValidationModel> accountValidator,
            IValidator<OperationUpsertRequest> dataValidator, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            OperationUpdateCommand command =
                new OperationUpdateCommand(operationId, userId, request.AccountId, request.CategoryId, request.Sum, request.Description);

            ValidationResult[] validationResults = new ValidationResult[]
            {
                await dataValidator.ValidateAsync(request, cancellationToken),
                await categoryValidator.ValidateAsync(command, cancellationToken),
                await accountValidator.ValidateAsync(command, cancellationToken),
                await operationValidator.ValidateAsync(command, cancellationToken),
            };

            IActionResult? errorResult = validationResults.ToErrorActionResult();
            if (errorResult is not null)
                return errorResult;

            Result<OperationResponse> result = await _operationsService.Update(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult();
        }
    }
}
