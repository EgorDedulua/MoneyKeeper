namespace MoneyKeeper.Application.Common
{
    public interface IUserEventHandler
    {
        Task HandleAsync(Guid messageId, string messageType, string routingKey, byte[] payload, CancellationToken cancellationToken = default);
    }
}
