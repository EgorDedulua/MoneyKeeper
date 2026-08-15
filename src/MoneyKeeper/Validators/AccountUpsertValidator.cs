using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Contracts.Account;

namespace MoneyKeeper.Validators
{
    public class AccountUpsertValidator : AbstractValidator<AccountUpsertRequest>
    {
        public AccountUpsertValidator() 
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Имя счета не может быть пустым").WithErrorCode(ErrorCodes.ACCOUNT_NAME_IS_EMPTY)
                .MaximumLength(50).WithMessage("Имя счета не может быть длиннее 50 символов").WithErrorCode(ErrorCodes.ACCOUNT_NAME_IS_TOO_LONG);

            RuleFor(x => x.Balance)
                .GreaterThanOrEqualTo(0).WithMessage("Баланс счета не может быть отрицательным").WithErrorCode(ErrorCodes.INVALID_ACCOUNT_BALANCE);

            RuleFor(x => x.Target)
                .GreaterThanOrEqualTo(0).WithMessage("Целевой баланс счета не может быть отрицательным").WithErrorCode(ErrorCodes.INVALID_ACCOUNT_TARGET);

            RuleFor(x => x.Description)
                .NotEmpty().When(x => x.Description is not null)
                    .WithMessage("Описание операции не может быть пустым").WithErrorCode(ErrorCodes.ACCOUNT_DESCRIPTION_IS_EMPTY)
                .MaximumLength(8000).WithMessage("Описание операции не может быть длиннее 8000 символов").WithErrorCode(ErrorCodes.ACCOUNT_DESCRIPTION_IS_TOO_LONG);
        }
    }
}
