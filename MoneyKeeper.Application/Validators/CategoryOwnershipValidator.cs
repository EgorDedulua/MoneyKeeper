using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Core.Common.Repositories;

namespace MoneyKeeper.Application.Validators
{
    public class CategoryOwnershipValidator : AbstractValidator<ICategoryOwnershipValidationModel>
    {
        public CategoryOwnershipValidator(ICategoriesRepository categoriesRepository) 
        {
            RuleFor(model => model)
                .Cascade(CascadeMode.Stop)
                .MustAsync(async (model, cancellationToken) =>
                {
                    Core.Models.Category? category = await categoriesRepository.GetByIdAsync(model.CategoryId, cancellationToken);
                    return category is not null && category.UserId == model.UserId;
                })
                .WithMessage(model => $"Не найдена категория с id {model.CategoryId} принадлежащая пользователю с id {model.UserId}")
                    .WithErrorCode(ErrorCodes.CATEGORY_NOT_FOUND);
        }
    }
}
