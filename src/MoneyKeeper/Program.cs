using MoneyKeeper.Extensions;
using MoneyKeeper.Filters;
using MoneyKeeper.Infrastructure.Auth;
using MoneyKeeper.Infrastructure.Data;
using MoneyKeeper.Middlewares;

namespace MoneyKeeper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddServices(builder.Configuration);
            builder.Services.AddAppHealthChecks();
            builder.AddLogging();
            builder.Services.Configure<JwtOptions>(
                builder.Configuration.GetSection(nameof(JwtOptions)));
            builder.Services.AddAuth(builder.Configuration);
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<RequestValidationActionFilter>();
            });
            builder.Services.AddOpenApi();

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                try
                {
                    dbContext.Database.EnsureCreated();                           
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Ошибка при применении миграций");
                    throw;
                }
            }
            app.MapAppHealthChecks();
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            app.UseExceptionHandler();
            app.UseMiddleware<TraceMiddleware>();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
