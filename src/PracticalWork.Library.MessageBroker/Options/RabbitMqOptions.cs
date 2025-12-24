namespace PracticalWork.Library.MessageBroker.Options;


public class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public int? Port { get; set; } = 5672;
    public string AppName { get; set; } = "library";
    public LibraryOptions Library { get; set; } = new();
    public ReportsOptions Reports { get; set; } = new();
}

public class LibraryOptions
{
    public string ExchangeName { get; set; } = "library.exchange";
    
    public BookCreateOptions BookCreate { get; set; } = new();
    public BookArchiveOptions BookArchive { get; set; } = new();
    public BookBorrowOptions BookBorrow { get; set; } = new();
    public BookReturnOptions BookReturn { get; set; } = new();
    public ReaderCreateOptions ReaderCreate { get; set; } = new();
    public ReaderCloseOptions ReaderClose { get; set; } = new();
}

public class BookCreateOptions
{
    public string QueueName { get; set; } = "book.create.queue";
    public string RoutingKey { get; set; } = "book.create.key";
}

public class BookArchiveOptions
{
    public string QueueName { get; set; } = "book.archive.queue";
    public string RoutingKey { get; set; } = "book.archive.key";
}

public class BookBorrowOptions
{
    public string QueueName { get; set; } = "book.borrow.queue";
    public string RoutingKey { get; set; } = "book.borrow.key";
}

public class BookReturnOptions
{
    public string QueueName { get; set; } = "book.return.queue";
    public string RoutingKey { get; set; } = "book.return.key";
}

public class ReaderCreateOptions
{
    public string QueueName { get; set; } = "reader.create.queue";
    public string RoutingKey { get; set; } = "reader.create.key";
}

public class ReaderCloseOptions
{
    public string QueueName { get; set; } = "reader.close.queue";
    public string RoutingKey { get; set; } = "reader.close.key";
}

public class ReportsOptions
{
    public string QueueName { get; set; } = "reports.queue";
    public string Exchange { get; set; } = "reports.exchange";
    public string RoutingKey { get; set; } = "reports.create.key";
}