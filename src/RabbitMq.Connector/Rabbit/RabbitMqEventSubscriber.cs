using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMq.Connector.Model;
using RabbitMQ.Client;

[assembly: InternalsVisibleTo("EventBus.RabbitMQ.Test")]
namespace RabbitMq.Connector.Rabbit
{
    public sealed class RabbitMqEventSubscriber : IEventSubscriber
    {
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly IRabbitConsumerInitializer _rabbitConsumerInitializer;
        private readonly ILogger<RabbitMqEventSubscriber> _logger;
        private readonly IExchangeQueueCreator _exchangeQueueCreator;
        private readonly RabbitMqEventBusOptions _rabbitMqEventBusOptions;

        public RabbitMqEventSubscriber(
            ISubscriptionManager subscriptionManager,
            IRabbitMqPersistentConnection persistentConnection,
            IRabbitConsumerInitializer rabbitConsumerInitializer,
            IOptions<RabbitMqEventBusOptions> rabbitMqEventBusOptons, ILogger<RabbitMqEventSubscriber> logger, IExchangeQueueCreator exchangeQueueCreator)
        {
            _subscriptionManager = subscriptionManager;
            _persistentConnection = persistentConnection;
            _rabbitConsumerInitializer = rabbitConsumerInitializer;
            _logger = logger;
            _exchangeQueueCreator = exchangeQueueCreator;
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
            _exchangeQueueCreator.EnsureExchangeIsCreated();
            _exchangeQueueCreator.EnsureQueueIsCreated();

            _logger.LogDebug($"Binding queue to exchange with event {subscription.EventName}");
            using var channel = _persistentConnection.CreateModel();
            channel.QueueBind(
                queue: _rabbitMqEventBusOptions.QueueName,
                exchange: _rabbitMqEventBusOptions.ExchangeName,
                routingKey: subscription.EventName
            );
            channel.QueueBind(
                queue: _rabbitMqEventBusOptions.DeadLetterName,
                exchange: _rabbitMqEventBusOptions.DeadLetterName,
                routingKey: subscription.EventName
            );
            _logger.LogDebug($"Queue successfully bound to exchange with event {subscription.EventName}");
        }
    }
}
