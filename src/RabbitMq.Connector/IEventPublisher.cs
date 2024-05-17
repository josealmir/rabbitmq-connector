using RabbitMq.Connector.Model;

namespace RabbitMq.Connector
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T @event) where T : Event;
        Task PublishManyAsync(Event[] @events);
        Task PublishManyAsync(EventPublishRequest[] publishRequests);
    }
}
