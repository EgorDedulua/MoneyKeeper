using Microsoft.AspNetCore.Mvc;
using MoneyKeeper.Core.Common;

namespace MoneyKeeper.Extensions
{
    public static class ResultExtensions
    {
        public static IActionResult ToErrorActionResult<T>(this Result<T> result)
        {
            if (result.IsSuccess)
                throw new ArgumentException("ToErrorActionResult called on successful result. " +
                    "Handle success case explicitly with Ok(), Created(), etc.");

            if (result is null)
                throw new ArgumentNullException("ToErrorActionResult called on null result");

            if (result.Error is null)
                throw new ArgumentNullException("ToErrorActionResult called on null Error property of result");

            return result.Error.StatusCode switch
            {
                400 => new BadRequestObjectResult(new
                {
                    error = result.Error.Message,
                    code = result.Error.ErrorCode
                }),

                401 => new UnauthorizedObjectResult(new
                {
                    error = result.Error.Message,
                    code = result.Error.ErrorCode
                }),

                403 => new ObjectResult(new
                {
                    error = result.Error.Message,
                    code = result.Error.ErrorCode
                })
                { StatusCode = StatusCodes.Status403Forbidden},

                404 => new NotFoundObjectResult(new
                {
                    error = result.Error.Message,
                    code = result.Error.ErrorCode
                }),

                409 => new ConflictObjectResult(new
                {
                    error = result.Error.Message,
                    code = result.Error.ErrorCode
                }),

                422 => new UnprocessableEntityObjectResult(new 
                {
                    error = result.Error.Message,
                    code = result.Error.ErrorCode
                }),
                _ => new ObjectResult(new { error = result.Error.Message, code = result.Error.ErrorCode })
                { StatusCode = result.Error.StatusCode }
            };
        }
    }
}
