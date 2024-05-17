
namespace RabbitMq.Connector.Model
{
    public sealed class EventPublishRequest
    {
        public EventPublishRequest(string eventBody, string eventName, IDictionary<PropertieEvent, object> headers)
        {
            EventBody = eventBody;
            EventName = eventName;
            Headers = headers;
        }

        public string EventBody { get; }
        public string EventName { get; }
        public IDictionary<PropertieEvent, object> Headers { get; private set; } = new Dictionary<PropertieEvent, object>();
    }
}