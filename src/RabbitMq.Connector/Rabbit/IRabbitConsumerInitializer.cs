
using System.Threading.Tasks;

namespace RabbitMq.Connector.Rabbit
{
    public interface IRabbitConsumerInitializer
    {
        Task InitializeConsumersChannelsAsync();
    }
}