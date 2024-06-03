using System.Text;
using System.Text.Json.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMq.Connector.Model
{
    public sealed class EventPublishRequest : IEventBasicPropertie
    {
        public EventPublishRequest(
            string eventBody,
            string eventName, 
            IBasicProperties basicPorperties)
        {
            EventBody = eventBody ?? EventBody;
            EventName = eventName;
            BasicProperties = basicPorperties;
        }

        public string EventBody { get; private set; } = string.Empty;
        
        public string EventName { get; private set; } = string.Empty;
        
        [JsonIgnore]        
        public IBasicProperties BasicProperties { get; set;}
        public static EventPublishRequest From(BasicDeliverEventArgs eventArgs, string eventName)
        {
            var eventBody = Encoding.UTF8.GetString(eventArgs.Body.ToArray()) ?? string.Empty;
            return new(eventBody, eventName, eventArgs.BasicProperties);
        }
    }
}