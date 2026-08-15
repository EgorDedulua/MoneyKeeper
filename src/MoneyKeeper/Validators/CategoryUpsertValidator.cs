using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Contracts.Category;

namespace MoneyKeeper.Validators    
{
    public class CategoryUpsertValidator : AbstractValidator<CategoryUpsertRequest>
    {
        public CategoryUpsertValidator() 
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Имя категории не может быть пустым").WithErrorCode(ErrorCodes.CATEGORY_NAME_IS_EMPTY)
                .MaximumLength(50).WithMessage("Имя категории не может быть длиннее 50 символов").WithErrorCode(ErrorCodes.CATEGORY_NAME_IS_TOO_LONG);

            RuleFor(x => x.Description)
                .NotEmpty().When(x => x.Description is not null).WithMessage("Описание категории не может быть пустым")
                    .WithErrorCode(ErrorCodes.CATEGORY_DESCRIPTION_IS_EMPTY)
                .MaximumLength(8000).WithMessage("Описание категории не может быть длиннее 8000 символов")
                    .WithErrorCode(ErrorCodes.CATEGORY_DESCRIPTION_IS_TOO_LONG);

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Недопустимый тип категории. Разрешены значения: Income (0), Consumption(1)")
                .WithErrorCode(ErrorCodes.INVALID_CATEGORY_TYPE);
        }
    }
}
