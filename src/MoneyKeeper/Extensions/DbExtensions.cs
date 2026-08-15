using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Infrastructure.Data;

namespace MoneyKeeper.Extensions
{
    public static class DbExtensions
    {
        public static IServiceCollection AddDb(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            return services;
        }

        public static WebApplication MigrateDb(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                try
                {
                    dbContext.Database.Migrate();
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Ошибка при применении миграций");
                    throw;
                }
            }
            return app;
        }
    }
}
