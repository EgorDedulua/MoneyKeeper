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
                    return await balanceChangingsRepository.GetByIdAsync(model.BalanceChangingId, cancellationToken) is not null;
                })
                .WithMessage(model => $"Не найдено изменение баланса с id {model.BalanceChangingId}").WithErrorCode(ErrorCodes.BALANCE_CHANGING_NOT_FOUND)
                .MustAsync(async (model, cancellationToken) =>
                {
                    Core.Models.BalanceChanging balanceChanging = (await balanceChangingsRepository.GetByIdAsync(model.BalanceChangingId, cancellationToken))!;
                    return balanceChanging.Account.UserId == model.UserId;
                })
                .WithMessage(model => $"Изменение баланса с id {model.BalanceChangingId} не принадлежит пользователю с id {model.UserId}")
                    .WithErrorCode(ErrorCodes.USER_ACCESS_DENIED);
        }
    }
}
