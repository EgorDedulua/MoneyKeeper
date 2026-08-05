using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Application.Contracts.Category;
using MoneyKeeper.Application.Extensions;
using MoneyKeeper.Application.Filters;
using MoneyKeeper.Application.Sorting;
using MoneyKeeper.Core.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;

namespace MoneyKeeper.Application.Services
{
    public class CategoriesService : ICategoriesService
    {
        private readonly ICategoriesRepository _categoriesRepository;
        private readonly IOperationsRepository _operationsRepository;
        private readonly IValidator<ICategoryOwnershipValidationModel> _categoryOwnershipValidator;
        private readonly ILogger<CategoriesService> _logger;
        private readonly IMapper _mapper;
        public CategoriesService(ICategoriesRepository categoriesRepository, IValidator<ICategoryOwnershipValidationModel> categoryOwnershipValidator
            , ILogger<CategoriesService> logger, IOperationsRepository operationsRepository, IMapper mapper)
        {
            _categoriesRepository = categoriesRepository;
            _operationsRepository = operationsRepository;
            _categoryOwnershipValidator = categoryOwnershipValidator;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<Result<CategoryResponse>> Add(CategoryCreationCommand command, CancellationToken cancellationToken)
        {
            if (await _categoriesRepository.ExistsWithName(command.Name, command.UserId, cancellationToken))
            {
                return Result<CategoryResponse>.Failure
                    (Error.Conflict($"Категория с именем {command.Name} уже существует", ErrorCodes.CATEGORY_NAME_ALREADY_EXISTS));
            }

            Category category = new Category
            {
                Description = command.Description,
                Name = command.Name,
                UserId = command.UserId,
                Type = command.Type,
            };

            await _categoriesRepository.AddAsync(category, cancellationToken);
            _logger.LogInformation("Пользователь с id {UserId} создал категорию с id {CategoryId} и с именем {CategoryName}", command.UserId, category.Id, command.Name);

            return Result<CategoryResponse>.Success(_mapper.Map<CategoryResponse>(category));
        }

        public async Task<Result<bool>> Delete(CategoryDeletionCommand command, CancellationToken cancellationToken)
        {
            ValidationResult validationResult = await _categoryOwnershipValidator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<bool>.Failure(validationResult.ToError());
            }

            if (await _operationsRepository.AreAnyOperationsWithCategoryAsync(command.CategoryId))
            {
                return Result<bool>.Failure
                    (Error.UnprocessableEntity($"Нельзя удалить категорию с id {command.CategoryId} так как есть операции с этой категорией",
                        ErrorCodes.CATEGORY_HAS_OPERATIONS));
            }

            await _categoriesRepository.DeleteAsync(command.CategoryId, cancellationToken);
            _logger.LogInformation("Пользователь с id {UserId} удалил категорию с id {CategoryId}", command.UserId, command.CategoryId);

            return Result<bool>.Success(true);
        }

        public async Task<Result<PagedResult<CategoryResponse>>> GetAll(CategoryQueryParameters parameters, int userId, CancellationToken cancellationToken)
        {
            CategoriesFilter filter = _mapper.Map<CategoriesFilter>(parameters);

            IQueryable<Category> query = _categoriesRepository.GetAllByUserId(userId);
            query = filter.ApplyTo(query);
            query = query.ApplySorting(parameters.SortBy);

            var (items, totalCount) = await _categoriesRepository
                .GetAllPagedAsync(query, parameters.Page, parameters.PageSize, cancellationToken);

            List<CategoryResponse> responseItems = _mapper.Map<List<CategoryResponse>>(items);

            return Result<PagedResult<CategoryResponse>>.Success
                (new PagedResult<CategoryResponse>(responseItems, totalCount, parameters.Page, parameters.PageSize));
        }

        public async Task<Result<CategoryResponse>> Update(CategoryUpdateCommand command, CancellationToken cancellationToken)
        {
            ValidationResult validationResult = await _categoryOwnershipValidator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<CategoryResponse>.Failure(validationResult.ToError());
            }

            if (await _categoriesRepository
                .ExistsAnotherWithName(command.Name, command.CategoryId, command.UserId, cancellationToken))
            {
                return Result<CategoryResponse>.Failure
                    (Error.Conflict($"Категория с именем {command.Name} уже существует", ErrorCodes.CATEGORY_NAME_ALREADY_EXISTS));
            }

            Category category = (await _categoriesRepository.GetByIdAsync(command.CategoryId, cancellationToken))!;
            
            if (command.Type != category.Type && await _operationsRepository.AreAnyOperationsWithCategoryAsync(command.CategoryId, cancellationToken))
            {
                return Result<CategoryResponse>.Failure
                    (Error.UnprocessableEntity($"Невозможно изменить тип категории с id {command.CategoryId} так как есть операции с этой категорией",
                        ErrorCodes.CATEGORY_HAS_OPERATIONS));
            }

            category.Name = command.Name;
            category.Description = command.Description;
            category.Type = command.Type;

            Category updatedCategory = await _categoriesRepository.UpdateAsync(category, cancellationToken);
            _logger.LogInformation("Пользователь с id {UserId} обновил категорию с id {CategoryId}", command.UserId, command.CategoryId);

            return Result<CategoryResponse>.Success(_mapper.Map<CategoryResponse>(updatedCategory));
        }
    }
}
