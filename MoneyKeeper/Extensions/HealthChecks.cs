using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MoneyKeeper.Infrastructure.Data;

namespace MoneyKeeper.Extensions
{
    public static class HealthChecks
    {
        public static IServiceCollection AddAppHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck("Self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
                .AddDbContextCheck<AppDbContext>("Database", tags: new[] { "ready" });

            return services;
        }

        public static WebApplication MapAppHealthChecks(this WebApplication app)
        {
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            return app;
        }
    }
}
