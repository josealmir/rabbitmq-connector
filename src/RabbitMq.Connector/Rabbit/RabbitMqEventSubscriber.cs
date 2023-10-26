using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMq.Connector.Model;
using RabbitMQ.Client;

[assembly:InternalsVisibleTo("EventBus.RabbitMQ.Test")]
namespace RabbitMq.Connector.Rabbit;

public sealed class RabbitMqEventSubscriber : IEventSubscriber
{
    private readonly ISubscriptionManager _subscriptionManager;
    private readonly IRabbitMqPersistentConnection _persistentConnection;
    private readonly IRabbitConsumerInitializer _rabbitConsumerInitializer;
    private readonly ILogger<RabbitMqEventSubscriber> _logger;
    private readonly RabbitMqEventBusOptions _rabbitMqEventBusOptions;

    public RabbitMqEventSubscriber(
        ISubscriptionManager subscriptionManager, 
        IRabbitMqPersistentConnection persistentConnection,
        IRabbitConsumerInitializer rabbitConsumerInitializer,
        IOptions<RabbitMqEventBusOptions> rabbitMqEventBusOptons, ILogger<RabbitMqEventSubscriber> logger)
    {
        _subscriptionManager = subscriptionManager;
        _persistentConnection = persistentConnection;
        _rabbitConsumerInitializer = rabbitConsumerInitializer;
        _logger = logger;
        _rabbitMqEventBusOptions = rabbitMqEventBusOptons.Value;
    }

    public Subscription<T> Subscribe<T>() where T : Event
    {
        var subscription = _subscriptionManager.AddSubscription<T>();
        SubscribeToRabbitMq(subscription);
        return subscription;
    }

    public Task StartListeningAsync() 
        => _rabbitConsumerInitializer.InitializeConsumersChannelsAsync();

    private void SubscribeToRabbitMq<T>(Subscription<T> subscription) where T : Event
    {
        if (!_persistentConnection.IsConnected) 
            _persistentConnection.TryConnect();

        _logger.LogInformation($"Binding queue to exchange with event {subscription.EventName}");
        using var channel = _persistentConnection.CreateModel();
        channel.QueueBind(
            queue: _rabbitMqEventBusOptions.QueueName,
            exchange: _rabbitMqEventBusOptions.ExchangeName,
            routingKey: subscription.EventName
        );
        _logger.LogInformation($"Queue successfully bound to exchange with event {subscription.EventName}");
    }
}