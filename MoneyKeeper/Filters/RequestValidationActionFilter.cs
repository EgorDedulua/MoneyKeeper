using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MoneyKeeper.Filters
{
    public class RequestValidationActionFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var arg in context.ActionArguments.Values)
            {
                if (arg is null)
                    continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
                var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

                if (validator is null)
                    continue;

                ValidationResult validationResult = await validator.ValidateAsync(
                    new ValidationContext<object>(arg), context.HttpContext.RequestAborted);

                if (!validationResult.IsValid)
                {
                    ValidationFailure firstError = validationResult.Errors.First();
                    ProblemDetails problem = new ProblemDetails
                    {
                        Title = "Ошибка валидации",
                        Status = 400,
                        Detail = firstError.ErrorMessage
                    };
                    problem.Extensions["errorCode"] = firstError.ErrorCode;

                    context.Result = new BadRequestObjectResult(problem);
                    return;
                }
            }

            await next();
        }
    }
}
