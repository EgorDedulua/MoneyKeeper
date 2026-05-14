using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Contracts.Category;
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

        public CategoriesService(ICategoriesRepository categoriesRepository)
        {
            _categoriesRepository = categoriesRepository;
        }

        public async Task<Result<CategoryResponse>> Add(CategoryCreationCommand command, CancellationToken cancellationToken)
        {
            Category category = new Category
            {
                Description = command.Description,
                Name = command.Name,
                UserId = command.UserId,
                Type = command.Type,
            };

            await _categoriesRepository.AddAsync(category, cancellationToken);
            return Result<CategoryResponse>.Success(new CategoryResponse(category.Id, category.Name, category.Description, category.Type));
        }

        public async Task<Result<bool>> Delete(int categoryId, CancellationToken cancellationToken)
        {
            await _categoriesRepository.DeleteAsync(categoryId, cancellationToken);
            return Result<bool>.Success(true);
        }

        public async Task<Result<PagedResult<CategoryResponse>>> GetAll(CategoryQueryParameters parameters, int userId, CancellationToken cancellationToken)
        {
            CategoryFilter filter = new CategoryFilter
            {
                IsOnlyIncome = parameters.IsOnlyIncome,
                NameSubstring = parameters.NameSubstring,
            };

            IQueryable<Category> query = _categoriesRepository.GetAllByUserId(userId);
            query = filter.ApplyTo(query);
            query = query.ApplySorting(parameters.SortBy);

            var (items, totalCount) = await _categoriesRepository
                .GetAllPagedAsync(query, parameters.Page, parameters.PageSize, cancellationToken);

            List<CategoryResponse> responseItems = items
                .Select(c => new CategoryResponse(c.Id, c.Name, c.Description, c.Type))
                .ToList();

            return Result<PagedResult<CategoryResponse>>.Success
                (new PagedResult<CategoryResponse>(responseItems, totalCount, parameters.Page, parameters.PageSize));
        }

        public async Task<Result<CategoryResponse>> Update(CategoryUpdateCommand command, CancellationToken cancellationToken)
        {
            Category category = (await _categoriesRepository.GetByIdAsync(command.CategoryId, cancellationToken))!;
            category.Name = command.Name;
            category.Description = command.Description;
            category.Type = command.Type;

            Category updatedCategory = await _categoriesRepository.UpdateAsync(category, cancellationToken);
            return Result<CategoryResponse>.Success(new CategoryResponse(category.Id, category.Name, category.Description, category.Type));
        }
    }
}
