using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using Serilog.Context;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMq.Connector.Extensions;
using RabbitMq.Connector.Model;
using System.Diagnostics;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;

namespace RabbitMq.Connector.Rabbit
{
    public sealed class RabbitConsumerHandler : IRabbitConsumerHandler
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ILogger<RabbitConsumerHandler> _logger;
        private static readonly ActivitySource _activitySource = new(nameof(RabbitConsumerHandler));
        private static readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;

        public RabbitConsumerHandler(
            IEnumerable<IServiceScopeFactory> serviceScopeFactory,
            ISubscriptionManager subscriptionManager,
            ILogger<RabbitConsumerHandler> logger)
        {
            if (serviceScopeFactory is null)
                throw new ArgumentNullException(nameof(serviceScopeFactory));

            _serviceScopeFactory = serviceScopeFactory.First();
            _subscriptionManager = subscriptionManager;
            _logger = logger;
        }

        public async Task HandleAsync(IModel consumerChannel, BasicDeliverEventArgs eventArgs)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var eventName = eventArgs.RoutingKey;
            var request = EventPublishRequest.From(eventArgs, eventName);

            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = String.Empty, ["RequestPath"] = eventName }))
            {
                var parentContext = _propagator.Extract(default, eventArgs.BasicProperties, TryExtractActivityToHeader);
                Baggage.Current = parentContext.Baggage;
                var activityEventName = $"Consumer Event {request.GetType().Name}";
                
                using (var activity = _activitySource.StartActivity(activityEventName, ActivityKind.Consumer, parentContext.ActivityContext))
                {
                    try
                    {
                        _logger.LogDebug("New event arrived");
                        if (TryRetriveEventType(eventName, out var eventType) &&
                            TryDeserializeEvent(eventArgs, eventType, out object? @event))
                        {
                            await TryHandleEvent(consumerChannel, eventArgs, scope, @event);
                        }
                        else
                        {
                            _logger.LogDebug("Removing unreadable event from queue");
                            consumerChannel.BasicNack(eventArgs.DeliveryTag, false, false);
                        }
                        _logger.LogDebug("Finishing handling event");                   
    
                        consumerChannel.TxCommit();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to process event");
                    }
                }                
            }
        }

        private async Task TryHandleEvent(IModel consumerChannel, BasicDeliverEventArgs eventArgs, IServiceScope scope, object? @event)
        {
            var serviceProvider = scope.ServiceProvider;
            var failureEventServiceFactory = new Func<IFailureEventService>(() => serviceProvider.GetRequiredService<IFailureEventService>());
            var mediator = serviceProvider.GetRequiredService<IMediator>();

            try
            {
                if (@event is null)
                {
                    _logger.LogInformation("Event is null and not send");
                    return;
                }

                dynamic? result = await mediator.Send(@event);

                if (result?.IsSuccess)
                {
                    _logger.LogDebug("Event successfully handled");
                    consumerChannel.BasicAck(eventArgs.DeliveryTag, false);
                }
                else
                {
                    _logger.LogWarning("Failed to handled Event {0}", result?.Exception as Exception);
                    await failureEventServiceFactory().HandleExceptionEventAsync(consumerChannel, eventArgs, @event, result?.Exception);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle event");
                await failureEventServiceFactory().HandleExceptionEventAsync(consumerChannel, eventArgs, @event, ex);
            }
        }

        private bool TryDeserializeEvent(BasicDeliverEventArgs eventArgs, Type? eventType, out dynamic? @event)
        {
            _logger.LogDebug("Trying to deserialize event");
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            var correlationId = eventArgs.BasicProperties.CorrelationId;
            
            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId, ["Message"] = message }))
            {
                try
                {
                    _logger.LogDebug("Message converted from bytes to string, deserializing");
                    @event = JsonSerializer.Deserialize(message, eventType ?? typeof(string) , options: new JsonSerializerOptions().Configure());
                    _logger.LogDebug("Event was deserialized");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize event with body");
                    @event = null;
                    return false;
                }
            }
            return true;
        }

        private bool TryRetriveEventType(string eventName, out Type? eventType)
        {
            _logger.LogDebug("Trying to find event subscription");
            var eventSubscription = _subscriptionManager.FindSubscription(eventName);
            eventType = eventSubscription?.EventType;

            var subscriptionFound = eventType != null;

            if (subscriptionFound)
            {
                using (_logger.BeginScope(new Dictionary<string, object> { ["EventType"] = eventType?.FullName ?? "" }))
                {
                    _logger.LogDebug("Event subscription found");
                }
            }
            else
                _logger.LogError("Event subscription was not found");

            return subscriptionFound;
        }

        internal IEnumerable<string> TryExtractActivityToHeader(IBasicProperties props, string key)
        {
            try
            {
                if (props.Headers.TryGetValue(key, out var value))
                {
                    var bytes = value as byte[];
                    return new[] { Encoding.UTF8.GetString(bytes) };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract trace context.");
            }

            return Enumerable.Empty<string>();
        }
        
    }
}