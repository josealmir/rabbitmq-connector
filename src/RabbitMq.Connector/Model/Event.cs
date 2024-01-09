using System;
using System.Collections.Generic;
using MediatR;
using OperationResult;

namespace RabbitMq.Connector.Model
{
    public abstract class Event : IRequest<Result>
    {
        public Event()
        {            
            EventId = Guid.NewGuid().ToString();
            Name = GetType().Name;
        }

        public virtual string EventId { get; private set; }
        public virtual DateTime Date { get; private set; } = DateTime.UtcNow;
        public virtual string Name { get; private set; }       
        public IDictionary<string, object> Headers { get; private set; } = new Dictionary<string, object>();
    }
}