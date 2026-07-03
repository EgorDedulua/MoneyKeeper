using Serilog.Context;
using System.Diagnostics;

namespace MoneyKeeper.Middlewares
{
    public class TraceMiddleware
    {
        private readonly RequestDelegate _next;

        public TraceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

            using (LogContext.PushProperty("TraceId", traceId))
            {
                context.Response.OnStarting(() =>
                {
                    if (!context.Response.Headers.ContainsKey("X-Trace-Id"))
                        context.Response.Headers.Append("X-Trace-Id", traceId);
                    return Task.CompletedTask;
                });

                await _next(context);
            }
        }
    }
}
