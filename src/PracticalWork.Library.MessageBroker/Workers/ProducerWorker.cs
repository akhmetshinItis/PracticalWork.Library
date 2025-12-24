using Microsoft.Extensions.Hosting;
using PracticalWork.Library.MessageBroker.Utils;

namespace PracticalWork.Library.MessageBroker.Workers;

public class ProducerWorker: BackgroundService
{
    private readonly RabbitMqInfrastructureInitializer _initializer;
    
    public ProducerWorker(
        RabbitMqInfrastructureInitializer initializer)
    {
        _initializer = initializer;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _initializer.InitializeAsync();
    }
}