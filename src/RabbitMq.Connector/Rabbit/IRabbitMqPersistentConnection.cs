using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMq.Connector.Rabbit
{
    public interface IRabbitMqPersistentConnection : IDisposable
    {
        bool IsConnected { get; }
        void TryConnect();
        IModel CreateModel();
        EventingBasicConsumer CreateConsumer(IModel model);
    }
}