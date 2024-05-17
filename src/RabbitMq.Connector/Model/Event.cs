using System.Diagnostics;
using MediatR;
using OperationResult;

namespace RabbitMq.Connector.Model
{
    public abstract class Event : IRequest<Result>
    {
        public Event()
        {                        
            Name = GetType().Name;
            Headers.Add(PropertieEvent.correlation_id, Activity.Current?.TraceId.ToString() ?? string.Empty);
            Headers.Add(PropertieEvent.message_id, Guid.NewGuid().ToString());
        }

        public virtual string Name { get; private set; }
        public virtual DateTime Date { get; private set; } = DateTime.UtcNow;        
        public IDictionary<PropertieEvent, object> Headers { get; private set; } = new Dictionary<PropertieEvent, object>();
    }
}