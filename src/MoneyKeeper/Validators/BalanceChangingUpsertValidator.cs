using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Contracts.BalanceChanging;

namespace MoneyKeeper.Validators
{
    public class BalanceChangingUpsertValidator : AbstractValidator<BalanceChangingUpsertRequest>
    {
        public BalanceChangingUpsertValidator() 
        {
            RuleFor(x => x.NewAccountBalance)
                .GreaterThanOrEqualTo(0).WithMessage("Новый баланс счета не может быть отрицательным")
                    .WithErrorCode(ErrorCodes.INVALID_ACCOUNT_BALANCE);
        }
    }
}
