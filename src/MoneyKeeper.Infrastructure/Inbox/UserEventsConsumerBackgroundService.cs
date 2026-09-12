using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace MoneyKeeper.Infrastructure.Inbox
{
    public class UserEventsConsumerBackgroundService : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly RabbitMqSettings _settings;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UserEventsConsumerBackgroundService> _logger;
        private IChannel? _channel;

        public UserEventsConsumerBackgroundService(IConnection connection, IOptions<RabbitMqSettings> options,
            IServiceScopeFactory scopeFactory, ILogger<UserEventsConsumerBackgroundService> logger)
        {
            _connection = connection;
            _settings = options.Value;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(
                prefetchSize: 0, 
                prefetchCount: _settings.PrefetchCount, 
                global: false,
                cancellationToken: stoppingToken
            );

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageRecievedAsync;

            await _channel.BasicConsumeAsync(
                queue: _settings.UserRegisteredQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken
            );

            await _channel.BasicConsumeAsync(
                queue: _settings.UserDeletedQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken
            );
        }

        private async Task OnMessageRecievedAsync(object sender, BasicDeliverEventArgs args)
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IUserEventHandler>();

            string messageIdRaw = args.BasicProperties.MessageId ?? Guid.Empty.ToString();
            string messageType = ExtractMessageType(args.BasicProperties);

            try
            {
                if (!Guid.TryParse(messageIdRaw, out Guid messageId))
                {
                    _logger.LogError("Сообщение с routingKey {RoutingKey} имеет некорректный MessageId '{MessageId}', отправляем в DLQ",
                        args.RoutingKey, messageIdRaw);

                    await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                await handler.HandleAsync(messageId, messageType, args.RoutingKey, args.Body.ToArray());

                await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки сообщения {MessageId} с routingKey {RoutingKey}",
                    messageIdRaw, args.RoutingKey);

                await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
            }
        }

        private string ExtractMessageType(IReadOnlyBasicProperties basicProperties)
        {
            if (basicProperties.Headers is not null 
                && basicProperties.Headers.TryGetValue("x-message-type", out var raw) && raw is byte[] bytes)
            {
                return Encoding.UTF8.GetString(bytes);
            }

            return "Unknown";
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel is not null)
                await _channel.CloseAsync(cancellationToken);

            await base.StopAsync(cancellationToken);
        }
    }
}
