using MediatR;
using OperationResult;
using RabbitMQ.Client;

namespace RabbitMq.Connector.Model
{
    public abstract class Event : IEventBasicPropertie, IRequest<Result>
    {
        public Event()
            => Name = GetType().Name;

        public virtual string Name { get; private set; }
        public virtual DateTime Date { get; private set; } = DateTime.UtcNow;
        public IBasicProperties BasicProperties { get; set;}
    }
}