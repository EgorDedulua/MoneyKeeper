using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Core.Common.Repositories;

namespace MoneyKeeper.Application.Validators.Transition
{
    public class TransitionAccountsValidator : AbstractValidator<ITransitionAccountsValidationModel>
    {
        public TransitionAccountsValidator(IAccountsRepository accountsRepository) 
        {
            RuleFor(model => model)
                .Cascade(CascadeMode.Stop)
                .Must((model) =>
                {
                    return model.SourceAccountId != model.DestinationAccountId;
                })
                .WithMessage("Счет-отправитель и счет-источник должны быть разными счетами")
                    .WithErrorCode(ErrorCodes.SAME_TRANSITION_ACCOUNTS)
                .MustAsync(async (model, cancellationToken) =>
                {
                    Core.Models.Account? sourceAccount = await accountsRepository.GetByIdAsync(model.SourceAccountId, cancellationToken);
                    if (sourceAccount is null || sourceAccount.UserId != model.UserId)
                        return false;

                    Core.Models.Account? destinationAccount = await accountsRepository.GetByIdAsync(model.DestinationAccountId, cancellationToken);
                    if (destinationAccount is null || destinationAccount.UserId != model.UserId)
                        return false;

                    return true;
                })
                .WithMessage(model => $"Не найден счет-отправитель с id {model.SourceAccountId} или счет-получатель с id {model.DestinationAccountId} " +
                $"принадлежащие пользователю с id {model.UserId}")
                    .WithErrorCode(ErrorCodes.ACCOUNT_NOT_FOUND);
        }
    }
}
