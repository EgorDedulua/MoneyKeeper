using MoneyKeeper.Extensions;
using MoneyKeeper.Filters;
using MoneyKeeper.Infrastructure.Auth;
using MoneyKeeper.Infrastructure.Data;
using MoneyKeeper.Middlewares;
using Serilog;

namespace MoneyKeeper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddServices(builder.Configuration);
            builder.Services.AddDb(builder.Configuration);
            builder.Services.AddAppHealthChecks();
            builder.UseAppLogging();
            builder.Services.Configure<JwtOptions>(
                builder.Configuration.GetSection(nameof(JwtOptions)));
            builder.Services.AddAuth(builder.Configuration);
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<RequestValidationActionFilter>();
            });
            builder.Services.AddOpenApi();

            var app = builder.Build();
            app.MigrateDb();
            app.MapAppHealthChecks();
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            app.UseExceptionHandler();
            app.UseMiddleware<LogContextEnrichmentMiddleware>();
            app.UseAppRequestLogging();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);
            app.Run();
        }
    }
}
