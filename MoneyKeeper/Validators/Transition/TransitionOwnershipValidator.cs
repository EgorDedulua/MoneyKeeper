using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Core.Common.Repositories;

namespace MoneyKeeper.Validators.Transition
{
    public class TransitionOwnershipValidator : AbstractValidator<ITransitionOwnershipValidationModel>
    {
        public TransitionOwnershipValidator(ITransitionsRepository transitionsRepository) 
        {
            RuleFor(model => model)
                .Cascade(CascadeMode.Stop)
                .MustAsync(async (model, cancellationToken) =>
                {
                    Core.Models.Transition? transition = await transitionsRepository.GetByIdAsync(model.TransitionId, cancellationToken);
                    return transition is not null && 
                        transition.SourceAccount.UserId == model.UserId && transition.DestinationAccount.UserId == model.UserId;
                })
                .WithMessage(model => $"Не найден перевод с id {model.TransitionId} принадлежащий пользователю с id {model.UserId}")
                    .WithErrorCode(ErrorCodes.TRANSITION_NOT_FOUND);
        }
    }
}
