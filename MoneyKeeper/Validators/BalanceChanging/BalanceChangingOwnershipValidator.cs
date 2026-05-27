using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Core.Common.Repositories;

namespace MoneyKeeper.Validators.BalanceChanging
{
    public class BalanceChangingOwnershipValidator : AbstractValidator<IBalanceChangingOwnershipValidationModel>
    {
        public BalanceChangingOwnershipValidator(IBalanceChangingsRepository balanceChangingsRepository) 
        {
            RuleFor(model => model)
                .Cascade(CascadeMode.Stop)
                .MustAsync(async (model, cancellationToken) =>
                {
                    Core.Models.BalanceChanging? balanceChanging = await balanceChangingsRepository.GetByIdAsync(model.BalanceChangingId, cancellationToken);
                    return balanceChanging is not null && balanceChanging.Account.UserId == model.UserId;
                })
                .WithMessage(model => $"Не найдено изменение баланса с id {model.BalanceChangingId} принадлежащее пользователю с id {model.UserId}")
                    .WithErrorCode(ErrorCodes.BALANCE_CHANGING_NOT_FOUND);
        }
    }
}
