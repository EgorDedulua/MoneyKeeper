using Microsoft.AspNetCore.Authentication.JwtBearer;
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
using MoneyKeeper.Application.Validators;
using MoneyKeeper.ExceptionHandlers;
using MoneyKeeper.Validators;
using MoneyKeeper.Application.Mappings;
using System.Diagnostics;
using MoneyKeeper.Infrastructure.Messaging;
using MoneyKeeper.Application.Events;

namespace MoneyKeeper.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RabbitMqSettings>(configuration.GetSection(nameof(RabbitMqSettings)));
            services.AddScoped<IUsersRepository, UsersRepository>();
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
            services.AddScoped<IInboxRepository, InboxRepository>();
            services.AddScoped<IUserEventHandler, UserEventHandler>();
            services.AddValidatorsFromAssemblyContaining<AccountOwnershipValidator>();
            services.AddValidatorsFromAssemblyContaining<OperationUpsertValidator>();
            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Extensions["traceId"] =
                        Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

                    if (context.Exception is not null && !context.ProblemDetails.Extensions.ContainsKey("errorCode"))
                    {
                        context.ProblemDetails.Extensions["errorCode"] = "UNHANDLED_EXCEPTION";
                    }
                };
            });
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<FiltersProfile>();
                cfg.AddProfile<ModelsProfile>();
            });
            return services;
        }

        public static IServiceCollection AddAuth(this IServiceCollection services,
            IConfiguration configuration)
        {
            JwtOptions jwtOptions = configuration.GetSection(nameof(JwtOptions))
                .Get<JwtOptions>()!;
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.ValidIssuer,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
            });

            services.AddAuthorization();
            return services;
        }
    }
}
