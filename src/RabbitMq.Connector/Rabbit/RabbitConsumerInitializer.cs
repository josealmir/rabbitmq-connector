using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace RabbitMq.Connector.Rabbit;

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
        EnsureQueueAndExchangeAreCreated();
    }

    public async Task InitializeConsumersChannelsAsync()
    {
        if (!_persistentConnection.IsConnected)
            _persistentConnection.TryConnect();

        _logger.LogInformation("Initializing consumer");

        var consumerStarts = new List<Task>();
        for (int i = 0; i < _rabbitMqEventBusOptions.ConsumersCount; i++)
        {
            var channel = _persistentConnection.CreateModel();
            consumerStarts.Add(Task.Run(() => InitializeConsumer(channel)));
        }

        await Task.WhenAll(consumerStarts);
    }

    private void InitializeConsumer(IModel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        channel.BasicQos(0, 1, false);
        var consumer = _persistentConnection.CreateConsumer(channel);
        consumer.Received += (sender, ea) => _rabbitConsumerHandler.HandleAsync(channel, ea);

        channel.BasicConsume(queue: _rabbitMqEventBusOptions.QueueName, autoAck: false, consumer);
        channel.CallbackException += (sender, ea) =>
        {
            channel.Dispose();
            InitializeConsumer(_persistentConnection.CreateModel());
        };

        _logger.LogInformation("Consumer initialized successfully");
    }

    private void EnsureQueueAndExchangeAreCreated()
    {
        if (_persistentConnection.IsConnected)
            _persistentConnection.TryConnect();
        
        using var channel = _persistentConnection.CreateModel();
        channel.ExchangeDeclare(exchange: _rabbitMqEventBusOptions.ExchangeName, type: "topic");
        channel.QueueDeclare(_rabbitMqEventBusOptions.QueueName, durable: true, autoDelete: false, exclusive: false);
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