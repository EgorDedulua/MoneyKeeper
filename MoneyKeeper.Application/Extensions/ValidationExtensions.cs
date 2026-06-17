using FluentValidation.Results;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Core.Common;
namespace MoneyKeeper.Application.Extensions
{
    public static class ValidationExtensions
    {
        public static int ToStatusCode(this ValidationFailure validationFailure)
        {
            return validationFailure.ErrorCode switch
            {
                ErrorCodes.USER_ACCESS_DENIED => 403,
                ErrorCodes.LOGIN_ALREADY_EXISTS => 409,
                ErrorCodes.ACCOUNT_NAME_ALREADY_EXISTS => 409,
                ErrorCodes.CATEGORY_NAME_ALREADY_EXISTS => 409,
                ErrorCodes.ACCOUNT_NOT_FOUND => 404,
                ErrorCodes.CATEGORY_NOT_FOUND => 404,
                ErrorCodes.OPERATION_NOT_FOUND => 404,
                ErrorCodes.TRANSITION_NOT_FOUND => 404,
                ErrorCodes.BALANCE_CHANGING_NOT_FOUND => 404,
                ErrorCodes.NOT_ENOUGH_MONEY => 422,
                ErrorCodes.OPERATION_CANCELING_DENIED => 422,
                ErrorCodes.OPERATION_UPDATING_DENIED => 422,
                ErrorCodes.TRANSITION_CANCELING_DENIED => 422,
                ErrorCodes.BALANCE_CHANGING_CANCELING_DENIED => 422,
                ErrorCodes.SAME_TRANSITION_ACCOUNTS => 422,
                ErrorCodes.UNKNOWN_OPERATION_DELETING_ERROR => 500,
                ErrorCodes.UNKNOWN_OPERATION_CREATION_ERROR => 500,
                ErrorCodes.UNKNOWN_OPERATION_UPDATING_ERROR => 500,
                ErrorCodes.UNKNOWN_BALANCE_CHANGING_CREATION_ERROR => 500,
                ErrorCodes.UNKNOWN_BALANCE_CHANGING_DELETION_ERROR => 500,
                ErrorCodes.UNKNOWN_TRANSITION_DELETING_ERROR => 500,
                ErrorCodes.UNKNOWN_TRANSITION_UPDATING_ERROR => 500,
                ErrorCodes.INVALID_LOGIN_OR_PASSWORD => 401,
                _ => 400
            };
        }

        public static Error ToError(this ValidationResult validationResult)
        {
            ValidationFailure validationFailure = validationResult.Errors.First();
            return Error.Create(validationFailure.ErrorMessage, validationFailure.ToStatusCode(), validationFailure.ErrorCode);
        }

        public static Error? ToError(this List<ValidationResult> validationResults)
        {
            foreach (ValidationResult validationResult in validationResults)
            {
                if (!validationResult.IsValid)
                    return validationResult.ToError();
            }
            return null!;
        }
    }
}
