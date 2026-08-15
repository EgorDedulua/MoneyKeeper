using Serilog.Context;
using System.Diagnostics;

namespace MoneyKeeper.Middlewares
{
    public class LogContextEnrichmentMiddleware
    {
        private readonly RequestDelegate _next;

        public LogContextEnrichmentMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

            using (LogContext.PushProperty("TraceId", traceId))
            {
                await _next(context);
            }
        }
    }
}
