using RabbitMQ.Client;

namespace RabbitMq.Connector.Rabbit
{
    public interface IRabbitMqPersistentConnection : IDisposable
    {
        bool IsConnected { get; }
        void TryConnect();
        IModel CreateModel();
    }
}