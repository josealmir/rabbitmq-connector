using System.Text.Json.Serialization;
using MediatR;
using OperationResult;

namespace RabbitMq.Connector.Model
{
    public abstract class Event :IEventBasicPropertie, IRequest<Result>
    {
        public Event()
            => Name = GetType().Name;

        public virtual string Name { get; private set; }
        public virtual DateTime Date { get; private set; } = DateTime.UtcNow;
        
        [JsonIgnore]
        public IDictionary<BasicPorperties, string> BasicPropertie { get; set; } = new Dictionary<BasicPorperties, string>(12);

        [JsonIgnore]
        public IDictionary<string, object> Headers { get; set; } = new Dictionary<string, object>(12);
    }

    public enum BasicPorperties
    {
        content_encoding,
        correlation_id,
        content_type,
        message_id,
        cluster_id,
        expiration,
        timestamp,
        priority,
        reply_to,
        user_id,
        app_id,
        type,
    }
}