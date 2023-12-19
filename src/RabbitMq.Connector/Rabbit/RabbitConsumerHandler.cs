using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using RabbitMq.Connector.Extensions;
using System.Text;
using Serilog.Context;

namespace RabbitMq.Connector.Rabbit;

public sealed class RabbitConsumerHandler : IRabbitConsumerHandler
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ISubscriptionManager _subscriptionManager;
    private readonly ILogger<RabbitConsumerHandler> _logger;

    public RabbitConsumerHandler(
        IEnumerable<IServiceScopeFactory> serviceScopeFactory,
        ISubscriptionManager subscriptionManager,
        ILogger<RabbitConsumerHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);
        _serviceScopeFactory = serviceScopeFactory.First();
        _subscriptionManager = subscriptionManager;
        _logger = logger;
    }

    public async Task HandleAsync(IModel consumerChannel, BasicDeliverEventArgs eventArgs)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var correlationId = eventArgs.Correlation();
        var eventName = eventArgs.RoutingKey;
        LogContext.PushProperty("X-Correlation-Id", correlationId);
        
        using (_logger.BeginScope(new Dictionary<string, object> { ["X-Correlation-Id"] = correlationId, ["RequestPath"] = eventName }))
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

    private async Task TryHandleEvent(IModel consumerChannel, BasicDeliverEventArgs eventArgs, IServiceScope scope, object @event)
    {
        var serviceProvider = scope.ServiceProvider;
        var failureEventServiceFactory = new Func<IFailureEventService>(() => serviceProvider.GetRequiredService<IFailureEventService>());
        var mediator = serviceProvider.GetService<IMediator>();

        try
        {
           
            dynamic result = await mediator.Send(@event);

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

    private bool TryDeserializeEvent(BasicDeliverEventArgs eventArgs, Type eventType, out dynamic? @event)
    {
        _logger.LogDebug("Trying to deserialize event");
        var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        var correlationId = eventArgs.Correlation();
        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId, ["Message"] = message }))
        {
            try
            {
                _logger.LogDebug("Message converted from bytes to string, deserializing");
                @event = JsonSerializer.Deserialize(message, eventType, options: new JsonSerializerOptions().Configure());
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
            using (_logger.BeginScope(new Dictionary<string, object> { ["EventType"] = eventType?.FullName }))
            {
                _logger.LogDebug("Event subscription found");
            }
        }
        else
            _logger.LogError("Event subscription was not found");

        return subscriptionFound;
    }

}