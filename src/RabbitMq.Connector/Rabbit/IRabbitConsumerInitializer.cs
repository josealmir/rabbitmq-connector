
namespace RabbitMq.Connector.Rabbit
{
    public interface IRabbitConsumerInitializer
    {
        Task InitializeConsumersChannelsAsync();
    }
}