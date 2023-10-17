using RabbitMq.Connector.Model;

namespace RabbitMq.Connector
{
    public sealed class SubscriptionManager : ISubscriptionManager
    {
        private readonly IList<ISubscription> _subscription;

        public SubscriptionManager()
            => _subscription = new List<ISubscription>();

        public Subscription<T> AddSubscription<T>() where T : Event
        {
            var subscription = new Subscription<T>();
            _subscription.Add(subscription);
            return subscription;
        }

        public Subscription<T> FindSubscription<T>() where T : Event 
            => _subscription
                        .OfType<Subscription<T>>()
                        .FirstOrDefault(s => s.EventName == typeof(T).Name);

        public ISubscription FindSubscription(string eventName)
            => _subscription.FirstOrDefault(s => s.EventName == eventName);
    }
}