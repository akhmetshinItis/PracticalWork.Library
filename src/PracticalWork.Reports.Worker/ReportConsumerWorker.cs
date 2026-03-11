using EasyNetQ;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.Events;
using PracticalWork.Library.MessageBroker.Options;
using PracticalWork.Library.MessageBroker.Utils;

namespace PracticalWork.Reports.Worker;

public sealed class ReportConsumerWorker : BackgroundService
{
    private readonly IBus _bus;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqOptions _options;
    private readonly RabbitMqInfrastructureInitializer _initializer;
    private readonly ILogger<ReportConsumerWorker> _logger;

    private IDisposable? _subscription;

    public ReportConsumerWorker(
        IBus bus,
        IServiceProvider serviceProvider,
        IOptions<RabbitMqOptions> options,
        RabbitMqInfrastructureInitializer initializer,
        ILogger<ReportConsumerWorker> logger)
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

        _subscription = _bus.PubSub.Subscribe<ReportCreateEvent>(
            "report_create_consumer",
            (message, ct) => HandleMessageAsync(message, ct),
            cfg => cfg.WithQueueName(_options.Reports.QueueName),
            stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        await base.StopAsync(cancellationToken);
    }

    private async Task HandleMessageAsync(ReportCreateEvent message, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<ReportCreateEvent>>();
            await handler.HandleAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки сообщения {MessageType}", nameof(ReportCreateEvent));
            throw;
        }
    }
}
