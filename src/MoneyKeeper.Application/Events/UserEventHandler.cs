using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MoneyKeeper.Application.Common;
using MoneyKeeper.Core.Common.Repositories;
using MoneyKeeper.Core.Models;
using System.Text.Json;

namespace MoneyKeeper.Application.Events
{
    public class UserEventHandler : IUserEventHandler
    {
        private readonly IInboxRepository _inboxRepository;
        private readonly IUsersRepository _usersRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserEventHandler> _logger;

        public UserEventHandler(IInboxRepository inboxRepository, IUsersRepository userRepository,
            IUnitOfWork unitOfWork, ILogger<UserEventHandler> logger)
        {
            _inboxRepository = inboxRepository;
            _usersRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleAsync(Guid messageId, string messageType, string routingKey, byte[] payload, CancellationToken cancellationToken = default)
        {
            if (await _inboxRepository.ExistsAsync(messageId, cancellationToken))
            {
                _logger.LogInformation("Сообщение {MessageId} было обработано раннее", messageId.ToString());
                return;
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                switch (messageType)
                {
                    case nameof(UserRegisteredEvent):
                        await HandleUserRegisteredEvent(messageId, payload ,cancellationToken);
                        break;
                    case nameof(UserDeletedEvent):
                        await HandleUserDeletedAsync(messageId, payload, cancellationToken);
                        break;
                    default:
                        _logger.LogWarning("Получено сообщение неизвестного типа {MessageType}, routingKey {RoutingKey}",
                            messageId.ToString(), routingKey);
                        break;
                }

                InboxMessage message = new InboxMessage
                {
                    Id = messageId,
                    MesssageType = messageType,
                    RecievedAt = DateTime.UtcNow,
                };

                await _inboxRepository.AddAsync(message, cancellationToken);

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601))
            {
                _logger.LogInformation("Сообщение {MessageId} было обработано параллельно, конфликт проигнорирован", messageId.ToString());
                await _unitOfWork.RollbackTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private async Task HandleUserRegisteredEvent(Guid messageId, byte[] payload, CancellationToken cancellationToken)
        {
            UserRegisteredEvent userRegisteredEvent = JsonSerializer.Deserialize<UserRegisteredEvent>(payload)
                ?? throw new InvalidOperationException($"Не удалось десериализовать {nameof(UserRegisteredEvent)}");

            User? existing = await _usersRepository.GetByIdAsync(userRegisteredEvent.UserId, cancellationToken);
            if (existing is not null)
            {
                _logger.LogWarning("Сообщение {MessageId} уже было обработано, но не найдено в Inbox", messageId.ToString());
                return;
            }

            User user = new User
            {
                Id = userRegisteredEvent.UserId,
                Email = userRegisteredEvent.Email,
            };

            await _usersRepository.AddAsync(user, cancellationToken);
        }

        private async Task HandleUserDeletedAsync(Guid messageId, byte[] payload, CancellationToken cancellationToken = default)
        {
            UserDeletedEvent userDeletedEvent = JsonSerializer.Deserialize<UserDeletedEvent>(payload)
                ?? throw new InvalidOperationException($"Не удалось десериализовать {nameof(UserDeletedEvent)}");

            User? existing = await _usersRepository.GetByIdAsync(userDeletedEvent.UserId, cancellationToken);
            if (existing is null)
                return;

            await _usersRepository.DeleteByIdAsync(userDeletedEvent.UserId, cancellationToken);
        }
    }
}
