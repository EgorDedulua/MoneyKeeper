using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Validation;
using MoneyKeeper.Core.Common.Repositories;

namespace MoneyKeeper.Validators.Operation
{
    public class OperationOwnershipValidator : AbstractValidator<IOperationOwnershipValidationModel>
    {
        public OperationOwnershipValidator(IOperationsRepository operationsRepository) 
        {
            RuleFor(model => model)
                .Cascade(CascadeMode.Stop)
                .MustAsync(async (model, cancellationToken) =>
                {
                    Core.Models.Operation? operation = await operationsRepository.GetByIdAsync(model.OperationId, cancellationToken);
                    return operation is not null && operation.Account.UserId == model.UserId;
                })
                .WithMessage(model => $"Не найдена операция с id {model.OperationId} принадлежащая пользователю с id {model.UserId}")
                    .WithErrorCode(ErrorCodes.OPERATION_NOT_FOUND);
        }
    }
}
