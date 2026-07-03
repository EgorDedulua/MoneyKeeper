using Serilog;

namespace MoneyKeeper.Extensions
{
    public static class BuilderExtensions
    {
        public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((context, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day);
            });
            return builder;
        }
    }
}
