using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Core.Common;

namespace MoneyKeeper.Extensions
{
    public static class ErrorExtensions
    {
        public static IActionResult ToErrorActionResult(this ValidationResult validationResult)
        {
            ValidationFailure validationError = validationResult.Errors.First();
            int statusCode = validationError.ErrorCode switch
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
            return Error.Create(validationError.ErrorMessage, statusCode, validationError.ErrorCode).ToErrorActionResult();
        }

        public static IActionResult? ToErrorActionResult(this ValidationResult[] validationResults)
        {
            foreach (ValidationResult validationResult in validationResults)
            {
                if (validationResult.Errors.Count != 0)
                {
                    return validationResult.ToErrorActionResult();
                }
            }
            return null!;
        }

        public static IActionResult ToErrorActionResult(this Error error)
        {
            return error.StatusCode switch
            {
                400 => new BadRequestObjectResult(new
                {
                    error = error.Message,
                    code = error.ErrorCode
                }),

                401 => new UnauthorizedObjectResult(new
                {
                    error = error.Message,
                    code = error.ErrorCode
                }),

                403 => new ObjectResult(new
                {
                    error = error.Message,
                    code = error.ErrorCode
                })
                { StatusCode = StatusCodes.Status403Forbidden },

                404 => new NotFoundObjectResult(new
                {
                    error = error.Message,
                    code = error.ErrorCode
                }),

                409 => new ConflictObjectResult(new
                {
                    error = error.Message,
                    code = error.ErrorCode
                }),

                422 => new UnprocessableEntityObjectResult(new
                {
                    error = error.Message,
                    code = error.ErrorCode
                }),
                _ => new ObjectResult(new { error = error.Message, code = error.ErrorCode })
                { StatusCode = error.StatusCode }
            };
        }
    }
}
