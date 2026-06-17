using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MoneyKeeper.ExceptionHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException)
                return false;

            var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

            _logger.LogError(exception, "Необработанное исключение. TraceId: {TraceId}", traceId);

            ProblemDetails problemDetails = new ProblemDetails
            {
                Title = "Неизвестная ошибка",
                Status = StatusCodes.Status500InternalServerError,
                Detail = _environment.IsDevelopment() ? exception.ToString() : "Обратитесь в поддержку",
                Instance = httpContext.Request.Path
            };
            problemDetails.Extensions["traceId"] = traceId;

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
