using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Application.Contracts.Account;
using MoneyKeeper.Contracts.Account;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Extensions;
using MoneyKeeper.ValidationModels;

namespace MoneyKeeper.Controllers
{
    [Route("api/accounts")]
    [ApiController]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountsService _accountsService;
        private readonly ICurrentUserService _currentUserService;

        public AccountsController(IAccountsService accountsService, ICurrentUserService currentUserService) 
        {
            _accountsService = accountsService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] AccountQueryParameters parameters, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            Result<PagedResult<AccountResponse>> result = await _accountsService.GetAll(parameters, userId, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);
            
            return result.ToErrorActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AccountUpsertRequest request, IValidator<AccountCreationCommand> accountValidator,
            IValidator<AccountUpsertRequest> dataValidator, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            AccountCreationCommand command =
               new AccountCreationCommand(userId, request.Name, request.Balance, request.Target, request.Description?.Trim());

            ValidationResult[] validationResults = new ValidationResult[]
            {
                await dataValidator.ValidateAsync(request, cancellationToken),
                await accountValidator.ValidateAsync(command, cancellationToken)
            };

            IActionResult? errorResult = validationResults.ToErrorActionResult();
            if (errorResult is not null)
                return errorResult;

            Result<AccountResponse> result = await _accountsService.Add(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult();
        }

        [HttpDelete("{accountId}")]
        public async Task<IActionResult> Delete(int accountId, IValidator<IAccountOwnershipValidationModel> validator, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            ValidationResult validationResult = await validator.ValidateAsync(new AccountDeletionValidationModel { UserId = userId, AccountId = accountId}, cancellationToken);
            if (!validationResult.IsValid)
                return validationResult.ToErrorActionResult();

            Result<bool> result = await _accountsService.Delete(accountId, cancellationToken);

            if (result.IsSuccess)
                return NoContent();

            return result.ToErrorActionResult();
        }

        [HttpPut("{accountId}")]
        public async Task<IActionResult> Update(int accountId, [FromBody] AccountUpsertRequest request,
            IValidator<IAccountOwnershipValidationModel> accountValidator, IValidator<AccountUpsertRequest> dataValidator, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            AccountUpdateCommand command =
                new AccountUpdateCommand(userId, accountId, request.Name, request.Balance, request.Target, request.Description);

            ValidationResult[] validationResults = new ValidationResult[]
            {
                await dataValidator.ValidateAsync(request, cancellationToken),
                await accountValidator.ValidateAsync(command, cancellationToken)
            };

            IActionResult? errorResult = validationResults.ToErrorActionResult();
            if (errorResult is not null)
                return errorResult;

            Result<AccountResponse> result = await _accountsService.Update(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult();
        }
    }
}
