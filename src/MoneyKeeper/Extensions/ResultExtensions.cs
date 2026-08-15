using Microsoft.AspNetCore.Mvc;
using MoneyKeeper.Core.Common;

namespace MoneyKeeper.Extensions
{
    public static class ResultExtensions
    {
        public static IActionResult ToErrorActionResult<T>(this Result<T> result, ControllerBase controller)
        {
            if (result is null)
                throw new ArgumentNullException(nameof(result), "Result cannot be null");

            if (result.IsSuccess)
                throw new InvalidOperationException(
                    "ToErrorActionResult called on successful result. Handle success case explicitly.");

            if (result.Error is null)
                throw new InvalidOperationException("Result.Error cannot be null.");

            return controller.Problem(
                detail: result.Error.Message,
                title: result.Error.StatusCode.ToErrorTitle(),
                statusCode: result.Error.StatusCode,
                extensions: new Dictionary<string, object?>
                {
                    ["errorCode"] = result.Error.ErrorCode
                }
            );
        }

        private static string ToErrorTitle(this int statusCode) => statusCode switch
        {
            400 => "Bad request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not found",
            409 => "Conflict",
            422 => "Unprocessable entity",
            _ => "Unexpected error"
        };
    }
}
