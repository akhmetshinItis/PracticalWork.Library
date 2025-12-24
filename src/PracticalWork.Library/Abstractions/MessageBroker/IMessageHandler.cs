namespace PracticalWork.Library.Abstractions.MessageBroker
{
    public interface IMessageHandler<in TMessage>
    {
        Task HandleAsync(TMessage message, CancellationToken cancellationToken);
    }
}