using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace RabbitMq.Connector.Rabbit
{
    public class ExchangeQueueCreator : IExchangeQueueCreator
    {
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly ILogger<ExchangeQueueCreator> _logger;
        private bool _queueIsCreated;
        private bool _exchangeIsCreated;
        private readonly RabbitMqEventBusOptions _rabbitMqEventBusOptions;

        public ExchangeQueueCreator(IRabbitMqPersistentConnection persistentConnection, IOptions<RabbitMqEventBusOptions> options, ILogger<ExchangeQueueCreator> logger)
        {
            _persistentConnection = persistentConnection;
            _logger = logger;
            _rabbitMqEventBusOptions = options.Value;
        }

        private void DeclareDeadLetter(IModel channel)
        {
            _logger.LogDebug("Declaring permanent deadletter queue");
            channel.QueueDeclare(_rabbitMqEventBusOptions.DeadLetterName, durable: true, exclusive: false, autoDelete: false);
            _logger.LogDebug("Declaring permanent deadletter exchange");
            channel.ExchangeDeclare(_rabbitMqEventBusOptions.DeadLetterName, type: "topic", autoDelete: false);
        }

        public void EnsureExchangeIsCreated()
        {
            if (!_exchangeIsCreated)
            {
                if (!_persistentConnection.IsConnected)
                    _persistentConnection.TryConnect();

                using var channel = _persistentConnection.CreateModel();
                channel.ExchangeDeclare(exchange: _rabbitMqEventBusOptions.ExchangeName, type: "topic");
                _exchangeIsCreated = true;
            }
        }

        public void EnsureQueueIsCreated()
        {
            if (!_queueIsCreated)
            {
                if (!_persistentConnection.IsConnected)
                    _persistentConnection.TryConnect();

                using var channel = _persistentConnection.CreateModel();
                DeclareDeadLetter(channel);
                channel.QueueDeclare(
                    _rabbitMqEventBusOptions.QueueName,
                    arguments: new Dictionary<string, object>
                    {
                        ["x-dead-letter-exchange"] = _rabbitMqEventBusOptions.DeadLetterName
                    },
                    durable: true, autoDelete: false, exclusive: false);
                _queueIsCreated = true;
            }
        }
    }
}