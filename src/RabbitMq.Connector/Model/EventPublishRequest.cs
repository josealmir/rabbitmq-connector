using System.Text;
using System.Text.Json.Serialization;
using RabbitMQ.Client.Events;

namespace RabbitMq.Connector.Model
{
    public sealed class EventPublishRequest : IEventBasicPropertie
    {
        public EventPublishRequest(
            string eventBody,
            string eventName, 
            IDictionary<string, object> headers)
        {
            EventBody = eventBody ?? EventBody;
            EventName = eventName;
            Headers = headers;
        }

        public string EventBody { get; private set; } = string.Empty;
        
        public string EventName { get; private set; } = string.Empty;
        
        [JsonIgnore]
        public IDictionary<string, object> Headers { get; set; } = new Dictionary<string, object>();
        
        [JsonIgnore]
        public IDictionary<BasicPorperties, string> BasicPropertie { get; set; } = new Dictionary<BasicPorperties, string>(12);
        
        public static EventPublishRequest From(BasicDeliverEventArgs eventArgs, string eventName)
        {
            var eventBody = Encoding.UTF8.GetString(eventArgs.Body.ToArray()) ?? string.Empty;
            return new(eventBody, eventName, eventArgs.BasicProperties.Headers);
        }
    }
}