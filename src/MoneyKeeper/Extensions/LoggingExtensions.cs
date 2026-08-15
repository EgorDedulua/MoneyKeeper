using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Security.Claims;

namespace MoneyKeeper.Extensions
{
    public static class LoggingExtensions
    {
        public static WebApplicationBuilder UseAppLogging(this WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithEnvironmentName()
                    .Enrich.WithThreadId()
                    .Enrich.WithProperty("Application", "MoneyKeeper.Business")
                    .WriteTo.Console()
                    .WriteTo.Async(a => a.File(
                        path: "logs/log-.json",
                        rollingInterval: RollingInterval.Day,
                        restrictedToMinimumLevel: LogEventLevel.Information,
                        formatter: new CompactJsonFormatter(),
                        rollOnFileSizeLimit: true,
                        fileSizeLimitBytes: 50 * 1024 * 1024
                    ));
            });

            return builder;
        }

        public static WebApplication UseAppRequestLogging(this WebApplication app)
        {
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
                    diagnosticContext.Set("UserId", httpContext.User.Identity?.IsAuthenticated == true
                        ? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        : null);
                    diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress);
                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                    diagnosticContext.Set("Host", httpContext.Request.Host.Value);
                };

                options.GetLevel = (httpContext, elapsed, exception) =>
                {
                    if (exception is not null || httpContext.Response.StatusCode >= 500)
                        return LogEventLevel.Error;

                    if (httpContext.Response.StatusCode >= 400)
                        return LogEventLevel.Warning;

                    if (httpContext.Request.Path.StartsWithSegments("/health"))
                        return LogEventLevel.Verbose;

                    return LogEventLevel.Information;
                };
            });
            return app;
        }
    }
}
