using PracticalWork.Library.Events;

namespace PracticalWork.Library.Abstractions.MessageBroker;

/// <summary>
/// Интерфейс продюсера сообщений для публикации событий библиотеки и отчетов
/// </summary>
public interface IMessageProducer
{
    /// <summary>
    /// Публикует событие создания книги
    /// </summary>
    /// <param name="message">Событие создания книги</param>
    /// <returns>Задача, представляющая асинхронную операцию</returns>
    Task ProduceBookCreateAsync(BookCreatedEvent message);

    /// <summary>
    /// Публикует событие архивирования книги
    /// </summary>
    /// <param name="message">Событие архивирования книги</param>
    Task ProduceBookArchiveAsync(BookArchivedEvent message);

    /// <summary>
    /// Публикует событие выдачи книги
    /// </summary>
    /// <param name="message">Событие выдачи книги</param>
    Task ProduceBookBorrowAsync(BookBorrowedEvent message);

    /// <summary>
    /// Публикует событие возврата книги
    /// </summary>
    /// <param name="message">Событие возврата книги</param>
    Task ProduceBookReturnAsync(BookReturnedEvent message);

    /// <summary>
    /// Публикует событие создания читателя
    /// </summary>
    /// <param name="message">Событие создания читателя</param>
    Task ProduceReaderCreateAsync(ReaderCreatedEvent message);

    /// <summary>
    /// Публикует событие закрытия читателя
    /// </summary>
    /// <param name="message">Событие закрытия читателя</param>
    Task ProduceReaderCloseAsync(ReaderClosedEvent message);

    /// <summary>
    /// Публикует событие создания отчета
    /// </summary>
    /// <param name="message">Событие создания отчета</param>
    Task ProduceReportCreateAsync(ReportCreateEvent message);
}