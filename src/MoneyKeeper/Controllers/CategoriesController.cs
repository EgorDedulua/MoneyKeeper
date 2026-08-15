using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Contracts.Category;
using MoneyKeeper.Contracts.Category;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Models;
using MoneyKeeper.Extensions;

namespace MoneyKeeper.Controllers
{
    [Route("api/categories")]
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoriesService _categoriesService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ICategoriesService categoriesService, ICurrentUserService currentUserService, ILogger<CategoriesController> logger)
        {
            _categoriesService = categoriesService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] CategoryQueryParameters parameters, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            Result<PagedResult<CategoryResponse>> result = 
                await _categoriesService.GetAll(parameters, userId, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            _logger.LogWarning("Ошибка при получении пользователем с id {UserId} своих категорий: {ErrorCode}", userId, result.Error!.ErrorCode);
            return result.ToErrorActionResult(this);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CategoryUpsertRequest request, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            CategoryCreationCommand command =
                new CategoryCreationCommand(userId, request.Name, request.Description, request.Type);

            Result<CategoryResponse> result = await _categoriesService.Add(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Value);

            _logger.LogWarning("Ошибка добавления счета пользователем с id {UserId}: {ErrorCode}", userId, result.Error!.ErrorCode);
            return result.ToErrorActionResult(this);
        }

        [HttpDelete("{categoryId}")]
        public async Task<IActionResult> Delete(int categoryId, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            Result<bool> result = await _categoriesService.Delete(new CategoryDeletionCommand(userId, categoryId), cancellationToken);

            if (result.IsSuccess)
                return NoContent();

            _logger.LogWarning
                ("Ошибка удаления категории с id {CategoryId} пользователем с id {UserId}: {ErrorCode}", categoryId, userId, result.Error!.ErrorCode);
            return result.ToErrorActionResult(this);
        }

        [HttpPut("{categoryId}")]
        public async Task<IActionResult> Update(int categoryId, [FromBody] CategoryUpsertRequest request, CancellationToken cancellationToken)
        {
            int userId = _currentUserService.UserId;

            CategoryUpdateCommand command = 
                new CategoryUpdateCommand(userId, categoryId, request.Name, request.Description, request.Type);

            Result<CategoryResponse> result = await _categoriesService.Update(command, cancellationToken);
            
            if (result.IsSuccess)
                return Ok(result.Value);

            _logger.LogWarning
              ("Ошибка обновления категории с id {CategoryId} пользователем с id {UserId}: {ErrorCode}", categoryId, userId, result.Error!.ErrorCode);
            return result.ToErrorActionResult(this);
        }
    }
}
