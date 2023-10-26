using RabbitMq.Connector.Model;

namespace RabbitMq.Connector;

public interface IEventSubscriber
{
    Subscription<T> Subscribe<T>() where T : Event;
    Task StartListeningAsync();
}