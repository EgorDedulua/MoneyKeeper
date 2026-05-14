using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Core.Common.Repositories;

namespace MoneyKeeper.Validators.Category
{
    public class CategoryOwnershipValidator : AbstractValidator<ICategoryOwnershipValidationModel>
    {
        public CategoryOwnershipValidator(ICategoriesRepository categoriesRepository) 
        {
            RuleFor(model => model)
                .Cascade(CascadeMode.Stop)
                .MustAsync(async (model, cancellationToken) =>
                {
                    return await categoriesRepository.GetByIdAsync(model.CategoryId, cancellationToken) is not null;
                })
                .WithMessage(model => $"Не найдена категория с id {model.CategoryId}").WithErrorCode(ErrorCodes.CATEGORY_NOT_FOUND)
                .MustAsync(async (model, cancellationToken) =>
                {
                    Core.Models.Category category = (await categoriesRepository.GetByIdAsync(model.CategoryId, cancellationToken))!;
                    return category.UserId == model.UserId;
                })
                .WithMessage(model => $"Категория с id {model.CategoryId} не принадлежит пользователю с id {model.UserId}").WithErrorCode(ErrorCodes.USER_ACCESS_DENIED);
        }
    }
}
