namespace PracticalWork.Library.Abstractions.MessageBroker
{
    /// <summary>
    /// Обработчик для сообщений
    /// </summary>
    /// <typeparam name="TMessage">Сообщение</typeparam>
    public interface IMessageHandler<in TMessage>
    {
        /// <summary>
        /// Обработать сообщение
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="cancellationToken">Токен отмены</param>
        Task HandleAsync(TMessage message, CancellationToken cancellationToken);
    }
}