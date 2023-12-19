using RabbitMq.Connector;
using RabbitMq.Connector.Model;

namespace Sample;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IEventSubscriber _eventSubscriber;

    public Worker(ILogger<Worker> logger, IEventSubscriber eventSubscriber)
    {
        _logger = logger;
        _eventSubscriber = eventSubscriber;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);    
            
            _eventSubscriber.Subscribe<EventTeste>()
                            .OnFailure(retry => retry.ShouldRetry());

            await Task.Delay(1000, stoppingToken);                            
        }
    }

    public class EventTeste: Event 
    { }
}
