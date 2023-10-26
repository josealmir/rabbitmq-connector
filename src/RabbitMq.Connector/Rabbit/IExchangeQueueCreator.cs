namespace RabbitMq.Connector.Rabbit;

public interface IExchangeQueueCreator
{
    void EnsureQueueIsCreated();
    void EnsureExchangeIsCreated();
}