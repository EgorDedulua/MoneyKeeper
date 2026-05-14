using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Core.Common.Repositories;

namespace MoneyKeeper.Validators.Account
{
    public class AccountOwnershipValidator : AbstractValidator<IAccountOwnershipValidationModel>
    {
        public AccountOwnershipValidator(IAccountsRepository accountsRepository) 
        {
            RuleFor(model => model)
                .Cascade(CascadeMode.Stop)
                .MustAsync(async (model, cancellationToken) =>
                {
                    return await accountsRepository.GetByIdAsync(model.AccountId, cancellationToken) is not null;
                })
                .WithMessage(model => $"Не найден счет с id {model.AccountId}").WithErrorCode(ErrorCodes.ACCOUNT_NOT_FOUND)
                .MustAsync(async (model, cancellationToken) =>
                {
                    Core.Models.Account account = (await accountsRepository.GetByIdAsync(model.AccountId, cancellationToken))!;
                    return account.UserId == model.UserId;
                })
                .WithMessage(model => $"Счет с id {model.AccountId} не принадлежит пользователю с id {model.UserId}").WithErrorCode(ErrorCodes.USER_ACCESS_DENIED);
        }
    }
}
