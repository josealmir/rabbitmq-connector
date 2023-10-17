using RabbitMq.Connector.Model;

namespace RabbitMq.Connector
{
    public class Subscription<T> : ISubscription where T : Event
    {
        private readonly Type _eventType;
        internal RetryPolicyConfiguration<T> RetryPolicyConfiguration { get; }

        public string EventName => _eventType.Name;

        public Type EventType => _eventType;

        public Subscription()
        {
            RetryPolicyConfiguration = new RetryPolicyConfiguration<T>();
            _eventType = typeof(T);
        }


        public void OnFailure(Action<RetryPolicyConfiguration<T>> config)
            => config(RetryPolicyConfiguration);
    }
}