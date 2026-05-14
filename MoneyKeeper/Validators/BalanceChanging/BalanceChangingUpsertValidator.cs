using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Contracts.BalanceChanging;

namespace MoneyKeeper.Validators.BalanceChanging
{
    public class BalanceChangingUpsertValidator : AbstractValidator<BalanceChangingUpsertRequest>
    {
        public BalanceChangingUpsertValidator() 
        {
            RuleFor(x => x.NewAccountBalance)
                .GreaterThanOrEqualTo(0).WithMessage("Новый баланс счета не может быть отрицательным").WithErrorCode(ErrorCodes.INVALID_ACCOUNT_BALANCE);

            RuleFor(x => x.Description)
                .NotEmpty().When(x => x.Description is not null).WithMessage("Описание изменения счета не может быть пустым").WithErrorCode(ErrorCodes.BALANCE_CHANGING_DESCRIPTION_IS_EMPTY)
                .MaximumLength(8000).WithMessage("Описание изменения баланса не может быть длиннее 8000 символов").WithErrorCode(ErrorCodes.BALANCE_CHANGING_DESCRIPTION_IS_TOO_LONG);
        }
    }
}
