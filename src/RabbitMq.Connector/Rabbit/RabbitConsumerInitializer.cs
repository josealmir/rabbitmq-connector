using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMq.Connector.Rabbit
{
    public sealed class RabbitConsumerInitializer : IRabbitConsumerInitializer, IDisposable
    {
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly IRabbitConsumerHandler _rabbitConsumerHandler;
        private readonly ILogger<RabbitConsumerInitializer> _logger;
        private readonly IExchangeQueueCreator _exchangeQueueCreator;
        private readonly RabbitMqEventBusOptions _rabbitMqEventBusOptions;
        private readonly List<IModel> _channels = new List<IModel>();

        public RabbitConsumerInitializer(
            IRabbitMqPersistentConnection persistentConnection,
            IOptions<RabbitMqEventBusOptions> options,
            IRabbitConsumerHandler rabbitConsumerHandler, ILogger<RabbitConsumerInitializer> logger, IExchangeQueueCreator exchangeQueueCreator)
        {
            _persistentConnection = persistentConnection;
            _rabbitConsumerHandler = rabbitConsumerHandler;
            _logger = logger;
            _exchangeQueueCreator = exchangeQueueCreator;
            _rabbitMqEventBusOptions = options.Value;
        }

        public async Task InitializeConsumersChannelsAsync()
        {
            _exchangeQueueCreator.EnsureExchangeIsCreated();
            _exchangeQueueCreator.EnsureQueueIsCreated();

            _logger.LogDebug("Initializing consumer");

            var consumerStarts = new List<Task>();
            for (int i = 0; i < _rabbitMqEventBusOptions.ConsumersCount; i++)
            {
                consumerStarts.Add(InitializeConsumer());
            }

            await Task.WhenAll(consumerStarts);
        }

        private async Task InitializeConsumer()
        {
            await Task.Factory.StartNew(() =>
            {
                var channel = _persistentConnection.CreateModel();
                _channels.Add(channel);
                channel.BasicQos(0, 1, false);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (sender, ea) => _rabbitConsumerHandler.HandleAsync(channel, ea);

                channel.CallbackException += async (sender, ea) =>
                {
                    if (channel.IsOpen)
                        channel.Dispose();
                    _channels.Remove(channel);
                    await InitializeConsumer();
                };

                channel.TxSelect();
                channel.BasicConsume(queue: _rabbitMqEventBusOptions.QueueName, autoAck: false, consumer);

                _logger.LogDebug("Consumer initialized successfully");
            });
        }

        public void Dispose()
        {
            foreach (var channel in _channels)
            {
                if (channel.IsOpen)
                    channel.Dispose();
            }
        }
    }
}