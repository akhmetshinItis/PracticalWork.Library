using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Events;
using PracticalWork.Library.MessageBroker.Options;
using PracticalWork.Library.MessageBroker.Utils;
using PracticalWork.Library.Models.ReportModels;

namespace PracticalWork.Library.MessageBroker.Workers;

public class ConsumerWorker: BackgroundService
{
    private readonly IBus _bus;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqOptions _options;
    private readonly List<IDisposable> _subscriptions = new();
    private readonly ILogger<ConsumerWorker> _logger;
    private readonly RabbitMqInfrastructureInitializer _initializer;
    
    public ConsumerWorker(
        IBus bus,
        IServiceProvider serviceProvider,
        IOptions<RabbitMqOptions> options,
        ILogger<ConsumerWorker> logger,
        RabbitMqInfrastructureInitializer initializer)
    {
        _bus = bus;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
        _initializer = initializer;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _initializer.InitializeAsync();
        SubscribeToAllQueues(stoppingToken);
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        
        _subscriptions.Clear();
        
        await base.StopAsync(cancellationToken);
    }
    
    private void SubscribeToAllQueues(CancellationToken stoppingToken)
    {
        SubscribeToBookCreateQueue(stoppingToken);
        SubscribeToBookArchiveQueue(stoppingToken);
        SubscribeToBookBorrowQueue(stoppingToken);
        SubscribeToBookReturnQueue(stoppingToken);
        SubscribeToReaderCreateQueue(stoppingToken);
        SubscribeToReaderCloseQueue(stoppingToken);
        
        SubscribeToReportsQueue(stoppingToken);
    }
    
    private void SubscribeToBookCreateQueue(CancellationToken stoppingToken)
    {
        var subscription = _bus.PubSub.Subscribe<BookCreatedEvent>(
            "book_create_consumer",
            async message => await HandleLibraryEventAsync(message, stoppingToken),
            cfg => cfg.WithQueueName(_options.Library.BookCreate.QueueName));
        
        _subscriptions.Add(subscription);
    }
    
    private void SubscribeToBookArchiveQueue(CancellationToken stoppingToken)
    {
        var subscription = _bus.PubSub.Subscribe<BookArchivedEvent>(
            "book_archive_consumer",
            async message => await HandleLibraryEventAsync(message, stoppingToken),
            cfg => cfg.WithQueueName(_options.Library.BookArchive.QueueName));
        
        _subscriptions.Add(subscription);
    }
    
    private void SubscribeToBookBorrowQueue(CancellationToken stoppingToken)
    {
        var subscription = _bus.PubSub.Subscribe<BookBorrowedEvent>(
            "book_borrow_consumer",
            async message => await HandleLibraryEventAsync(message, stoppingToken),
            cfg => cfg.WithQueueName(_options.Library.BookBorrow.QueueName));
        
        _subscriptions.Add(subscription);
    }
    
    private void SubscribeToBookReturnQueue(CancellationToken stoppingToken)
    {
        var subscription = _bus.PubSub.Subscribe<BookReturnedEvent>(
            "book_return_consumer",
            async message => await HandleLibraryEventAsync(message, stoppingToken),
            cfg => cfg.WithQueueName(_options.Library.BookReturn.QueueName));
        
        _subscriptions.Add(subscription);
    }
    
    private void SubscribeToReaderCreateQueue(CancellationToken stoppingToken)
    {
        var subscription = _bus.PubSub.Subscribe<ReaderCreatedEvent>(
            "reader_create_consumer",
            async message => await HandleLibraryEventAsync(message, stoppingToken),
            cfg => cfg.WithQueueName(_options.Library.ReaderCreate.QueueName));
        
        _subscriptions.Add(subscription);
    }
    
    private void SubscribeToReaderCloseQueue(CancellationToken stoppingToken)
    {
        var subscription = _bus.PubSub.Subscribe<ReaderClosedEvent>(
            "reader_close_consumer",
            async message => await HandleLibraryEventAsync(message, stoppingToken),
            cfg => cfg.WithQueueName(_options.Library.ReaderClose.QueueName));
        
        _subscriptions.Add(subscription);
    }
    
    private void SubscribeToReportsQueue(CancellationToken stoppingToken)
    {
        var subscription = _bus.PubSub.Subscribe<ReportCreateEvent>(
            "report_create_consumer",
            async message => await HandleReportEventAsync(message, stoppingToken),
            cfg => cfg.WithQueueName(_options.Reports.QueueName));
        
        _subscriptions.Add(subscription);
    }

    private async Task HandleLibraryEventAsync(BaseLibraryEvent libraryEvent, CancellationToken stoppingToken)
    {
        var scope = _serviceProvider.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        if (libraryEvent is { Source: "library-service" })
        {
            var log = new ActivityLog
            {
                Event = libraryEvent,
                EventDate = libraryEvent.OccurredOn,
                EventType = libraryEvent.EventType
            };
            await reportService.WriteSystemActivityLogs(log);
        }
        else
        {
            _logger.LogError("Пришло невалидное сообщение лога события системы");
        }
    }
    
    private async Task HandleReportEventAsync(ReportCreateEvent reportEvent, CancellationToken stoppingToken)
    {
        var scope = _serviceProvider.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        if (reportEvent is { Source: "report-service" })
        {
            await reportService.GenerateReport(
                reportEvent.Id, reportEvent.PeriodFrom, 
                reportEvent.PeriodTo, reportEvent.EventTypes.ToArray());
        }
        else
        {
            _logger.LogError("Пришло невалидное сообщение от сервиса отчетов");
        }
    }
}