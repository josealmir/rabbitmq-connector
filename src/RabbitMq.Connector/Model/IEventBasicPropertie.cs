using RabbitMQ.Client;

namespace RabbitMq.Connector.Model
{
    public interface IEventBasicPropertie
    {
        public IBasicProperties BasicProperties { get; set; }
    }
}