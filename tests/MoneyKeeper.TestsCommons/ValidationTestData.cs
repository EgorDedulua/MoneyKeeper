using FluentValidation.Results;
using MoneyKeeper.Application.Common;

namespace MoneyKeeper.TestsCommons
{
    public static class ValidationTestData
    {
        public static ValidationResult ValidResult => new();

        public static ValidationResult CreateFailedResult(string property, string message, string errorCode)
        {
            ValidationFailure failure = new ValidationFailure(property, message)
            {
                ErrorCode = errorCode
            };
            return new ValidationResult(new[] { failure });
        }

        public static ValidationResult UserAccessDenied => CreateFailedResult("UserId", "Неверный пользователь", ErrorCodes.USER_ACCESS_DENIED);

        public static ValidationResult AccountNotFound => CreateFailedResult("AccountId", "Счет не найден", ErrorCodes.ACCOUNT_NOT_FOUND);

        public static ValidationResult CategoryNotFound => CreateFailedResult("CategoryId", "Категория не найдена", ErrorCodes.CATEGORY_NOT_FOUND);

        public static ValidationResult OperationNotFound => CreateFailedResult("OperationId", "Операция не найдена", ErrorCodes.OPERATION_NOT_FOUND);

        public static ValidationResult TransitionNotFound => CreateFailedResult("TransitionId", "Перевод не найден", ErrorCodes.OPERATION_NOT_FOUND);

        public static ValidationResult BalanceChangingNotFound => CreateFailedResult("BalanceChangingId", "Изменение баланса не найдена", ErrorCodes.BALANCE_CHANGING_NOT_FOUND);
    }
}
