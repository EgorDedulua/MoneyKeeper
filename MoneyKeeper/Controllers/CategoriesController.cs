using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Contracts.Category;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Extensions;
using MoneyKeeper.Application.Contracts.Category;
using FluentValidation;
using FluentValidation.Results;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.ValidationModels;
using MoneyKeeper.Application.Common;

namespace MoneyKeeper.Controllers
{
    [Route("api/categories")]
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoriesService _categoriesService;
        private readonly ICurrentUserService _currentUserService;

        public CategoriesController(ICategoriesService categoriesService, ICurrentUserService currentUserService)
        {
            _categoriesService = categoriesService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] CategoryQueryParameters parameters, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            Result<PagedResult<CategoryResponse>> result = 
                await _categoriesService.GetAll(parameters, userId, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);
            
            return result.ToErrorActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CategoryUpsertRequest request, IValidator<CategoryUpsertRequest> dataValidator,
            IValidator<CategoryCreationCommand> categoryValidator, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            CategoryCreationCommand command =
                new CategoryCreationCommand(userId, request.Name, request.Description, request.Type);

            ValidationResult[] validationResults = new ValidationResult[]
            {
                await dataValidator.ValidateAsync(request, cancellationToken),
                await categoryValidator.ValidateAsync(command, cancellationToken)
            };

            IActionResult? errorResult = validationResults.ToErrorActionResult();
            if (errorResult is not null)
                return errorResult;

            Result<CategoryResponse> result = await _categoriesService.Add(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult();
        }

        [HttpDelete("{categoryId}")]
        public async Task<IActionResult> Delete(int categoryId, IValidator<ICategoryOwnershipValidationModel> validator, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            ValidationResult validationResult = await validator.ValidateAsync(new CategoryDeletionValidationModel { UserId = userId, CategoryId = categoryId }
            , cancellationToken);
            if (!validationResult.IsValid)
                return validationResult.ToErrorActionResult();

            Result<bool> result = await _categoriesService.Delete(categoryId, cancellationToken);

            if (result.IsSuccess)
                return NoContent();

            return result.ToErrorActionResult();
        }

        [HttpPut("{categoryId}")]
        public async Task<IActionResult> Update(int categoryId, [FromBody] CategoryUpsertRequest request,
            IValidator<CategoryUpsertRequest> dataValidator, IValidator<ICategoryOwnershipValidationModel> categoryValidator, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            CategoryUpdateCommand command = 
                new CategoryUpdateCommand(userId, categoryId, request.Name, request.Description, request.Type);

            ValidationResult[] validationResults = new ValidationResult[]
            {
                await dataValidator.ValidateAsync(request, cancellationToken),
                await categoryValidator.ValidateAsync(command, cancellationToken)
            };

            IActionResult? errorResult = validationResults.ToErrorActionResult();
            if (errorResult is not null)
                return errorResult;

            Result<CategoryResponse> result = await _categoriesService.Update(command, cancellationToken);
            
            if (result.IsSuccess)
                return Ok(result.Value);

            return result.ToErrorActionResult();
        }
    }
}
