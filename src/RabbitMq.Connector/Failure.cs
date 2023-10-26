using MediatR;
using RabbitMq.Connector.Model;

namespace RabbitMq.Connector;

public class Failure<T> : INotification where T : Event
{
    public Failure(T @event, int retryAttempt, Exception exception)
    {
        Event = @event;
        RetryAttempt = retryAttempt;
        Exception = exception;
    }

    public T Event { get; }
    public int RetryAttempt { get; }
    public Exception Exception { get; }
}