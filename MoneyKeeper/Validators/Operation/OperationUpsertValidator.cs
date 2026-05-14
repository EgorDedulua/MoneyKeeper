using FluentValidation;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Contracts.Operation;

namespace MoneyKeeper.Validators.Operation
{
    public class OperationUpsertValidator : AbstractValidator<OperationUpsertRequest>
    {
        public OperationUpsertValidator() 
        {
            RuleFor(x => x.Sum)
                .GreaterThan(0).WithMessage("Сумма операции должна быть больше нуля").WithErrorCode(ErrorCodes.INVALID_OPERATION_SUM);

            RuleFor(x => x.Description)
                .NotEmpty().When(x => x.Description is not null).WithMessage("Описание операции не может быть пустым").WithErrorCode(ErrorCodes.OPERATION_DESCRIPTION_IS_EMPTY)
                .MaximumLength(8000).WithMessage("Описание операции не может быть длиннее 8000 символов").WithErrorCode(ErrorCodes.OPERATION_DESCRIPTION_IS_TOO_LONG);
        }
    }
}
