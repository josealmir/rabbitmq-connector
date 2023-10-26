namespace RabbitMq.Connector;

public interface ISubscription
{
    Type EventType { get; }
    string EventName { get; }
}