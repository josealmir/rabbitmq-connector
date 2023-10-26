namespace RabbitMq.Connector.Model;

public sealed class EventPublishRequest
{
    public EventPublishRequest(string eventBody, string eventId, string eventName, IDictionary<string, object> headers)
    {
        EventBody = eventBody;
        EventName = eventName;
        EventId = eventId;
        Headers = headers;
    }

    public string EventBody { get; }
    public string EventId { get; }
    public string EventName { get; }
    public IDictionary<string, object> Headers { get; private set; } = new Dictionary<string, object>();

}