using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using RabbitMq.Connector.Extensions;

namespace RabbitMq.Connector.Rabbit
{
    public class FailureEventService : IFailureEventService
    {
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ILogger<FailureEventService> _logger;
        private readonly IMediator _mediator;
        private readonly RabbitMqEventBusOptions _options;

        public FailureEventService(
            ISubscriptionManager subscriptionManager,
            ILogger<FailureEventService> logger,
            IOptions<RabbitMqEventBusOptions> options,
            IMediator mediator)
        {
            _subscriptionManager = subscriptionManager;
            _logger = logger;
            _mediator = mediator;
            _options = options.Value;
        }

        public async Task HandleExceptionEventAsync(IModel consumerChannel, BasicDeliverEventArgs eventArgs, object @event, Exception ex)
        {
            var eventName = eventArgs.RoutingKey;
            var subscription = _subscriptionManager.FindSubscription(eventName);
            dynamic dynamicSubscription = subscription;
            dynamic retryConfiguration = dynamicSubscription.RetryPolicyConfiguration;
            var newAttemptCount = IncrementAttempt(eventArgs);
            var failure = CreateFailure(subscription, @event, newAttemptCount, ex);
            var shouldRetry = retryConfiguration.Retry(failure);
            var retryAttemptsExceeded =
                !retryConfiguration.ForeverRetry && newAttemptCount >= retryConfiguration.MaxRetryTimes;

            _logger.LogDebug("Dispatching failure event");
            await _mediator.Publish((object)failure);
            _logger.LogDebug("Event failure dispatched");

            if (shouldRetry && !retryAttemptsExceeded)
            {
                var retryWait = RetrieveRetryWaitingTime(retryConfiguration.RetryTime, failure);
                _logger.LogDebug("Re-queueing event with delay");
                PublishToWaitingDeadLetter(consumerChannel, eventArgs, retryWait, eventName, @event);
            }
            else if (retryConfiguration.DiscardEvent(failure))
            {
                consumerChannel.BasicAck(eventArgs.DeliveryTag, false);
            }
            else
            {
                consumerChannel.BasicNack(eventArgs.DeliveryTag, false, false);
            }
        }

        private TimeSpan RetrieveRetryWaitingTime(dynamic retryFunc, dynamic failure)
        {
            _logger.LogDebug("Retrieving retry waiting time");
            TimeSpan retryWait;
            try
            {
                retryWait = retryFunc != null ? retryFunc(failure) : throw new Exception("Retry Wait cannot be null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "There was a error calculating the waiting time. Default will be set");
                retryWait = TimeSpan.FromSeconds(5);
            }
            _logger.LogDebug($"The retry waiting will bee {retryWait.TotalMilliseconds} milliseconds");
            return retryWait.TotalMilliseconds > 0 ? retryWait : TimeSpan.FromSeconds(5);
        }

        private dynamic CreateFailure(ISubscription subscription, object @event, int attempts, Exception exception)
        {
            var eventTypeArgs = new[] { subscription.EventType };
            _logger.LogDebug("Creating failure object");
            var constructedEventType = typeof(Failure<>).MakeGenericType(eventTypeArgs);
            dynamic failure = Activator.CreateInstance(constructedEventType, @event, attempts, exception);
            return failure;
        }

        private void PublishToWaitingDeadLetter(IModel consumerChannel, BasicDeliverEventArgs eventArgs, TimeSpan retryWait, string eventName, object @event)
        {
            var deadLetterName = $"{_options.QueueName}_{retryWait.TotalSeconds}.error";
            DeclareWaitingDeadLetter(consumerChannel, retryWait, eventName, deadLetterName);
            _logger.LogDebug("Publishing to deadletter exchange");
            consumerChannel.BasicPublish(deadLetterName, eventName, mandatory: true,
                body: JsonSerializer.SerializeToUtf8Bytes(@event, options: new JsonSerializerOptions().Configure()), basicProperties: eventArgs.BasicProperties);
            consumerChannel.BasicAck(eventArgs.DeliveryTag, false);
        }

        private void DeclareWaitingDeadLetter(IModel consumerChannel, TimeSpan retryWait, string eventName, string deadLetterName)
        {
            _logger.LogDebug("Declaring waiting queue deadletter");
            var totalSecondsQueue = Convert.ToInt32(retryWait.TotalSeconds + 30);
            consumerChannel.QueueDeclare(deadLetterName,
                arguments: new Dictionary<string, object>()
                {
                    {"x-dead-letter-exchange", _options.ExchangeName},
                    {"x-message-ttl", Convert.ToInt64(retryWait.TotalMilliseconds)},
                    {"x-expires", Convert.ToInt64(TimeSpan.FromSeconds(totalSecondsQueue).TotalMilliseconds)},
                }, durable: true, exclusive: false, autoDelete: false);
            _logger.LogDebug("Declaring waiting exchange deadletter");
            consumerChannel.ExchangeDeclare(deadLetterName, type: "topic", autoDelete: true);
            _logger.LogDebug("Binding waiting exchange deadletter to queue");
            consumerChannel.QueueBind(deadLetterName, deadLetterName, eventName);
        }

        private int IncrementAttempt(BasicDeliverEventArgs eventArgs)
        {
            _logger.LogDebug("Incrementing event attempt");
            eventArgs.BasicProperties.Headers ??= new Dictionary<string, object>();
            if (eventArgs.BasicProperties.Headers.TryGetValue("attempts", out var attempts))
            {
                _logger.LogDebug("Attempt is already defined, incrementing 1");
                attempts = (int)attempts + 1;
                eventArgs.BasicProperties.Headers["attempts"] = attempts;
            }
            else
            {
                _logger.LogDebug("Attempt is not defined yet, setting to first try");
                attempts = 1;
                eventArgs.BasicProperties.Headers.Add("attempts", attempts);
            }

            var newAttempt = (int)attempts;
            _logger.LogDebug($"New retry attempt count: {newAttempt}");
            return newAttempt;
        }
    }
}