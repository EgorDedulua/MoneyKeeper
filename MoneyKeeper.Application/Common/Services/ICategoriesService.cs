using MoneyKeeper.Application.Contracts.Category;
using MoneyKeeper.Core.Common;

namespace MoneyKeeper.Application.Common.Services
{
    public interface ICategoriesService
    {
        Task<Result<CategoryResponse>> Add(CategoryCreationCommand command, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<CategoryResponse>>> GetAll(CategoryQueryParameters parameters, int userId, CancellationToken cancellationToken = default);

        Task<Result<bool>> Delete(int categoryId, CancellationToken cancellationToken = default);

        Task<Result<CategoryResponse>> Update(CategoryUpdateCommand command, CancellationToken cancellationToken = default);
    }
}
