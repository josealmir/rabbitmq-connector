using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMq.Connector.Extensions;
using RabbitMQ.Client;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using RabbitMq.Connector.Model;
using System.Diagnostics;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;

[assembly: InternalsVisibleTo("EventBus.RabbitMQ.Extensions.DependencyInjection")]
namespace RabbitMq.Connector.Rabbit
{
    public sealed class RabbitEventPublisher : IEventPublisher
    {
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly ILogger<RabbitEventPublisher> _logger;
        private readonly IExchangeQueueCreator _exchangeQueueCreator;
        private readonly RabbitMqEventBusOptions _options;
        private readonly ActivitySource _activitySource = new(nameof(RabbitEventPublisher));
        private readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;

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
            var activityName = $"RabbitMQ Publish Event {@event.GetType().Name}";            
            await Task.Run(() =>
            {
                using (var activity = _activitySource.StartActivity(activityName, ActivityKind.Producer))
                {
                    _exchangeQueueCreator.EnsureExchangeIsCreated();
                    var eventName = @event.GetType().Name;
                    if (!_persistentConnection.IsConnected)
                        _persistentConnection.TryConnect();

                    using var channel = _persistentConnection.CreateModel();                
                    var props = @event.MapTo(channel);

                    AddActivityToHeader(activity, props);
                    var body = JsonSerializer.SerializeToUtf8Bytes(@event, options: new JsonSerializerOptions().Configure());
                    channel.BasicPublish(
                        _options.ExchangeName,
                        routingKey: eventName,
                        basicProperties: props,
                        body: body);
                    _logger.LogDebug("Event published");
                };
            });
        }

        public Task PublishManyAsync(Event[] events)
            => PublishManyAsync(events.Select(e => new EventPublishRequest(JsonSerializer.Serialize(e, options: new JsonSerializerOptions().Configure()), e.Name, e.Headers)).ToArray());

        public async Task PublishManyAsync(EventPublishRequest[] publishRequests)
        {
            await Task.Run(() =>
            {
                using (var activity = _activitySource.StartActivity("PublishManyAsync", ActivityKind.Producer))
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
                        AddActivityToHeader(activity, props);
                        var eventName = publishRequest.EventName;
                        _logger.LogDebug($"Adding event {eventName}");
                        
                        var body = Encoding.UTF8.GetBytes(publishRequest.EventBody).AsMemory();
                        batchPublish.Add(_options.ExchangeName, routingKey: eventName, mandatory: false, properties: props, body: body);
                    }

                    _logger.LogDebug("Publishing batch events");
                    batchPublish.Publish();
                    _logger.LogDebug("All events were published");
                }                
            });
        }
        
        internal void AddActivityToHeader(Activity activity, IBasicProperties props)
        {
            ActivityContext activityContextToInject = default;
                        
            if (activity is not null)
            {
                activityContextToInject = activity.Context;
            }
            else if (Activity.Current is not null)
            {
                activityContextToInject = Activity.Current.Context;
            }

            _propagator.Inject(new PropagationContext(activityContextToInject, Baggage.Current), props, InjectContextInHeader);
            
            void InjectContextInHeader(IBasicProperties props, string key, string value) 
            {
                try
                {
                    props.Headers ??= new Dictionary<string, object>();
                    props.Headers[key] = value;
                }
                catch (Exception ex)
                {                    
                    _logger.LogError(ex, "Failed to inject trace context.");
                }
            }
        }
    }
}
