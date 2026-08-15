using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MoneyKeeper.Filters
{
    public class RequestValidationActionFilter : IAsyncActionFilter
    {
        private readonly IProblemDetailsService _problemDetailsService;

        public RequestValidationActionFilter(IProblemDetailsService problemDetailsService)
        {
            _problemDetailsService = problemDetailsService;
        }

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
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                    var errorCodes = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorCode).ToArray());

                    ValidationProblemDetails problemDetails = new ValidationProblemDetails
                    {
                        Title = "Validation error",
                        Errors = errors,
                        Detail = "Some objects are invalid",
                        Status = StatusCodes.Status400BadRequest,
                    };

                    problemDetails.Extensions["errorCodes"] = errorCodes;

                    await _problemDetailsService.WriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = context.HttpContext,
                        ProblemDetails = problemDetails,
                    });

                    context.Result = new EmptyResult();
                    return;
                }
            }

            await next();
        }
    }
}
