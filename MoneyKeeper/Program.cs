using MoneyKeeper.Extensions;
using MoneyKeeper.Filters;
using MoneyKeeper.Infrastructure.Auth;
using MoneyKeeper.Middlewares;

namespace MoneyKeeper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddServices(builder.Configuration);
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
