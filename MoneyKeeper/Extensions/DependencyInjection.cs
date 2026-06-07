using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Application.Common.Auth;
using MoneyKeeper.Application.Common.Services;
using MoneyKeeper.Application.Services;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Infrastructure.Auth;
using MoneyKeeper.Infrastructure.Data;
using MoneyKeeper.Infrastructure.Data.Repositories;
using System.Text;
using FluentValidation;
using MoneyKeeper.Validators.Account;

namespace MoneyKeeper.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUsersRepository, UsersRepository>();
            services.AddScoped<IUsersService, UsersService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IAccountsRepository, AccountsRepository>();
            services.AddScoped<IAccountsService, AccountsService>();
            services.AddScoped<ICategoriesRepository, CategoriesRepository>();
            services.AddScoped<ICategoriesService, CategoriesService>();
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();
            services.AddScoped<IOperationsRepository, OperationsRepository>();
            services.AddScoped<IOperationsService, OperationsService>();
            services.AddScoped<IBalanceChangingsRepository, BalanceChangingsRepository>();
            services.AddScoped<IBalanceChangingsService, BalanceChangingsService>();
            services.AddScoped<ITransitionsRepository, TransitionsRepository>();
            services.AddScoped<ICommonBalanceOperationsRepository, CommonBalanceOperationsRepository>();
            services.AddScoped<ITransitionsService,  TransitionsService>();
            services.AddValidatorsFromAssemblyContaining<AccountOwnershipValidator>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            return services;
        }

        public static IServiceCollection AddAuth(this IServiceCollection services,
            IConfiguration configuration)
        {
            JwtOptions jwtOptions = configuration.GetSection(nameof(JwtOptions))
                .Get<JwtOptions>()!;
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            context.Token = context.Request.Cookies["tasty-cookies"];
                            return Task.CompletedTask;
                        }
                    };
                });
            services.AddAuthorization();
            return services;
        }
    }
}
