using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Contracts.Transition;

namespace MoneyKeeper.Validators
{
    public class TransitionUpsertValidator : AbstractValidator<TransitionUpsertRequest>
    {
        public TransitionUpsertValidator()
        {
            RuleFor(x => x.Sum)
                .GreaterThan(0).WithMessage("Сумма перевода должна быть больше нуля").WithErrorCode(ErrorCodes.INVALID_TRANSITION_SUM);

            RuleFor(x => x.Description)
                .NotEmpty().When(x => x.Description is not null).WithMessage("Описание перевода не может быть пустым")
                    .WithErrorCode(ErrorCodes.TRANSITION_DESCRIPTION_IS_EMPTY)
                .MaximumLength(8000).WithMessage("Описание перевода не может быть длиннее 8000 символов")
                    .WithErrorCode(ErrorCodes.TRANSITION_DESCRIPTION_IS_TOO_LONG);
        }
    }
}
