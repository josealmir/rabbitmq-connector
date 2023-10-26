using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMq.Connector.Rabbit;

public interface IFailureEventService
{
    Task HandleExceptionEventAsync(IModel consumerChannel, BasicDeliverEventArgs eventArgs, object @event, Exception ex);
}