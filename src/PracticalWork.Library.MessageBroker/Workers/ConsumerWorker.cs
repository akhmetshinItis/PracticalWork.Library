using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.Events;
using PracticalWork.Library.MessageBroker.Options;
using PracticalWork.Library.MessageBroker.Utils;

namespace PracticalWork.Library.MessageBroker.Workers;

public class ConsumerWorker : BackgroundService
{
    private readonly IBus _bus;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqOptions _options;
    private readonly RabbitMqInfrastructureInitializer _initializer;
    private readonly ILogger<ConsumerWorker> _logger;

    private readonly List<IDisposable> _subscriptions = new();

    public ConsumerWorker(
        IBus bus,
        IServiceProvider serviceProvider,
        IOptions<RabbitMqOptions> options,
        RabbitMqInfrastructureInitializer initializer,
        ILogger<ConsumerWorker> logger)
    {
        _bus = bus;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _initializer = initializer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _initializer.InitializeAsync();

        Subscribe<BookCreatedEvent>(
            "book_create_consumer",
            _options.Library.BookCreate.QueueName,
            stoppingToken);

        Subscribe<BookArchivedEvent>(
            "book_archive_consumer",
            _options.Library.BookArchive.QueueName,
            stoppingToken);

        Subscribe<BookBorrowedEvent>(
            "book_borrow_consumer",
            _options.Library.BookBorrow.QueueName,
            stoppingToken);

        Subscribe<BookReturnedEvent>(
            "book_return_consumer",
            _options.Library.BookReturn.QueueName,
            stoppingToken);

        Subscribe<ReaderCreatedEvent>(
            "reader_create_consumer",
            _options.Library.ReaderCreate.QueueName,
            stoppingToken);

        Subscribe<ReaderClosedEvent>(
            "reader_close_consumer",
            _options.Library.ReaderClose.QueueName,
            stoppingToken);

        Subscribe<ReportCreateEvent>(
            "report_create_consumer",
            _options.Reports.QueueName,
            stoppingToken);
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

    private void Subscribe<T>(
        string subscriptionId,
        string queueName,
        CancellationToken stoppingToken)
    {
        var subscription = _bus.PubSub.Subscribe<T>(
            subscriptionId,
            (message, ct) => HandleMessageAsync(message, ct),
            cfg => cfg.WithQueueName(queueName),
            stoppingToken);

        _subscriptions.Add(subscription);
    }

    private async Task HandleMessageAsync<T>(
        T message,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();

            var handler = scope.ServiceProvider
                .GetRequiredService<IMessageHandler<T>>();

            await handler.HandleAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Ошибка обработки сообщения {MessageType}",
                typeof(T).Name);

            throw;
        }
    }
}