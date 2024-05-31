using MediatR;
using OperationResult;
using RabbitMq.Connector;
using RabbitMq.Connector.Model;

namespace Sample;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IEventSubscriber _eventSubscriber;
    private bool _startedListening;

    public Worker(ILogger<Worker> logger, IEventSubscriber eventSubscriber)
    {
        _logger = logger;
        _eventSubscriber = eventSubscriber;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_startedListening)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);    
            
                _eventSubscriber.Subscribe<EventTeste>()
                                .OnFailure(retry => retry.ShouldRetry());
                
                await _eventSubscriber.StartListeningAsync();
                _startedListening = true;
            }

            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }

    public class EventTeste: Event, INotification
    { 
        public int Count { get; set; }
    }

    public class HandlerTest : IRequestHandler<EventTeste, Result>
    {
        public async Task<Result> Handle(EventTeste request, CancellationToken cancellationToken)
        {
            return Result.Success();
        }
    }
}

