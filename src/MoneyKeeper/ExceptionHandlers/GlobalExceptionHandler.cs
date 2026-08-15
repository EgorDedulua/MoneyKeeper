using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MoneyKeeper.ExceptionHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionHandler(IProblemDetailsService problemDetailsService,
            ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment environment)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
            _environment = environment;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException)
            {
                if (httpContext.RequestAborted.IsCancellationRequested)
                {
                    _logger.LogInformation("Запрос к {Path} был отменён клиентом", httpContext.Request.Path);
                }
                else
                {
                    _logger.LogWarning(exception, "Таймаут при выполнении запроса к {Path}", httpContext.Request.Path);
                }
                return true;
            }

            _logger.LogError(exception, "Произошло необработанное исключение: {Message}", exception.Message);
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal server error",
                    Detail = _environment.IsDevelopment() ? exception.ToString() : "Unexpected error"
                }
            });
        }
    }
}
