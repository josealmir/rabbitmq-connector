using RabbitMq.Connector.Model;

namespace RabbitMq.Connector;

public interface ISubscriptionManager
{
    Subscription<T> AddSubscription<T>() where T : Event;
    Subscription<T> FindSubscription<T>() where T : Event;
    ISubscription FindSubscription(string eventName);
}