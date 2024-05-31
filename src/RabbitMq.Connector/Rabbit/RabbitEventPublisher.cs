using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMq.Connector.Extensions;
using RabbitMQ.Client;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using RabbitMq.Connector.Model;

[assembly: InternalsVisibleTo("EventBus.RabbitMQ.Extensions.DependencyInjection")]
namespace RabbitMq.Connector.Rabbit
{
    public sealed class RabbitEventPublisher : IEventPublisher
    {
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly ILogger<RabbitEventPublisher> _logger;
        private readonly IExchangeQueueCreator _exchangeQueueCreator;
        private readonly RabbitMqEventBusOptions _options;

        public RabbitEventPublisher(
            IOptions<RabbitMqEventBusOptions> options,
            IRabbitMqPersistentConnection persistentConnection,
            ILogger<RabbitEventPublisher> logger, IExchangeQueueCreator exchangeQueueCreator)
        {
            _persistentConnection = persistentConnection;
            _logger = logger;
            _exchangeQueueCreator = exchangeQueueCreator;
            _options = options.Value;
        }

        public async Task PublishAsync<T>(T @event) where T : Event
        {
            _exchangeQueueCreator.EnsureExchangeIsCreated();
            var eventName = @event.GetType().Name;
            if (!_persistentConnection.IsConnected)
                _persistentConnection.TryConnect();

            using var channel = _persistentConnection.CreateModel();
            var props = @event.MapTo(channel);

            props.Headers = @event.Headers;
            var body = JsonSerializer.SerializeToUtf8Bytes(@event, options: new JsonSerializerOptions().Configure());
            channel.BasicPublish(
                _options.ExchangeName,
                routingKey: eventName,
                basicProperties: props,
                body: body);
            _logger.LogDebug("Event published");
        }


        public Task PublishManyAsync(Event[] events)
            => PublishManyAsync(events.Select(e => new EventPublishRequest(JsonSerializer.Serialize(e, options: new JsonSerializerOptions().Configure()), e.Name, e.Headers)).ToArray());

        public async Task PublishManyAsync(EventPublishRequest[] publishRequests)
        {
            await Task.Run(() =>
            {
                _exchangeQueueCreator.EnsureExchangeIsCreated();
                _logger.LogDebug("Publishing {0} events", publishRequests.Length);
                if (!_persistentConnection.IsConnected)
                    _persistentConnection.TryConnect();

                using var channel = _persistentConnection.CreateModel();
                var batchPublish = channel.CreateBasicPublishBatch();
            
                foreach (var publishRequest in publishRequests)
                {
                    var props = publishRequest.MapTo(channel);

                    var eventName = publishRequest.EventName;
                    _logger.LogDebug($"Adding event {eventName}");
                    
                    var body = Encoding.UTF8.GetBytes(publishRequest.EventBody).AsMemory();
                    batchPublish.Add(_options.ExchangeName, routingKey: eventName, mandatory: false, properties: props, body: body);
                }

                _logger.LogDebug("Publishing batch events");
                batchPublish.Publish();
                _logger.LogDebug("All events were published");
            });
        }
    }
}
