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
                .MustAsync(async (model, cancellationToken) =>
                {
                    Core.Models.Account? account = await accountsRepository.GetByIdAsync(model.AccountId, cancellationToken);
                    return account is not null && account.UserId == model.UserId;
                })
                .WithMessage(model => $"Не найден счет с id {model.AccountId} принадлежащий пользователю с id {model.UserId}")
                    .WithErrorCode(ErrorCodes.ACCOUNT_NOT_FOUND);
        }
    }
}
