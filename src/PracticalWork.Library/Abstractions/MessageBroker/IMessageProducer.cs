using PracticalWork.Library.Events;

namespace PracticalWork.Library.Abstractions.MessageBroker;

public interface IMessageProducer
{
    Task ProduceBookCreateAsync(BookCreatedEvent message);
    Task ProduceBookArchiveAsync(BookArchivedEvent message);
    Task ProduceBookBorrowAsync(BookBorrowedEvent message);
    Task ProduceBookReturnAsync(BookReturnedEvent message);
    Task ProduceReaderCreateAsync(ReaderCreatedEvent message);
    Task ProduceReaderCloseAsync(ReaderClosedEvent message);
    Task ProduceReportCreateAsync(ReportCreateEvent message);
}