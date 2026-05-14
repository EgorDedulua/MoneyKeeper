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
                    return await operationsRepository.GetByIdAsync(model.OperationId, cancellationToken) is not null;
                })
                .WithMessage(model => $"Не найдена операция с id {model.OperationId}").WithErrorCode(ErrorCodes.OPERATION_NOT_FOUND)
                .MustAsync(async (model, cancellationToken) =>
                {
                    Core.Models.Operation operation = (await operationsRepository.GetByIdAsync(model.OperationId, cancellationToken))!;
                    return operation.Account.UserId == model.UserId;
                })
                .WithMessage(model => $"Операция с id {model.OperationId} не принадлежит пользователю с id {model.UserId}").WithErrorCode(ErrorCodes.USER_ACCESS_DENIED);
        }
    }
}
