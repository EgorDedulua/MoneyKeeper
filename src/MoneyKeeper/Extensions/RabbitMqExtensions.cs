using MoneyKeeper.Infrastructure.Inbox;
using MoneyKeeper.Infrastructure.Messaging;
using RabbitMQ.Client;

namespace MoneyKeeper.Extensions
{
    public static class RabbitMqExtensions
    {
        public static IServiceCollection AddRabbitMq(this IServiceCollection services)
        {
            services.AddSingleton<RabbitMqConnectionFactory>();
            services.AddSingleton<IConnection>(sp =>
            {
                var factory = sp.GetRequiredService<RabbitMqConnectionFactory>();
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });
            services.AddHostedService<RabbitMqTopologyInitializer>();
            services.AddHostedService<UserEventsConsumerBackgroundService>();
            return services;
        }
    }
}
