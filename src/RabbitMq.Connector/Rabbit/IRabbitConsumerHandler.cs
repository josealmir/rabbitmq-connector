using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMq.Connector.Rabbit
{
    public interface IRabbitConsumerHandler
    {
        Task HandleAsync(IModel consumerChannel, BasicDeliverEventArgs eventArgs);
    }
}