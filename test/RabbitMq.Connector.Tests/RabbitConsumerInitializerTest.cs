using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMq.Connector.Rabbit;
using RabbitMq.Connector.Tests.Builders;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMq.Connector.Tests;

public class RabbitConsumerInitializerTest
{
    private readonly IRabbitMqPersistentConnection _persistentConnection;
    private readonly IRabbitConsumerHandler _rabbitConsumerHandler;
    private readonly ILogger<RabbitConsumerInitializer> _logger;
    private readonly IExchangeQueueCreator _exchangeQueueCreator;
    private readonly IOptions<RabbitMqEventBusOptions> _rabbitMqEventBusOptions;
    private readonly IModel _model;
    private EventingBasicConsumer _consumer;
    private readonly RabbitConsumerInitializer _sut;
   
    public RabbitConsumerInitializerTest()
    {        
        _rabbitConsumerHandler = Substitute.For<IRabbitConsumerHandler>();
        _logger = Substitute.For<ILogger<RabbitConsumerInitializer>>();
        _exchangeQueueCreator = Substitute.For<IExchangeQueueCreator>();
        _rabbitMqEventBusOptions = Substitute.For<IOptions<RabbitMqEventBusOptions>>();
        _rabbitMqEventBusOptions.Value.Returns(RabbitMqEventBusOptionBuilder.BuilderRabbitMqEventBusOptions);
        _model = Substitute.For<IModel>();
        _persistentConnection = Substitute.For<IRabbitMqPersistentConnection>();
        _persistentConnection.CreateModel().Returns(_model);
        _consumer = Substitute.For<EventingBasicConsumer>(Substitute.For<IModel>());
        _persistentConnection.CreateConsumer(_model).Returns(_consumer);
        _sut = new RabbitConsumerInitializer(_persistentConnection,_rabbitMqEventBusOptions, _rabbitConsumerHandler,_logger, _exchangeQueueCreator);
    }

    [Fact]
    public async Task WhenInitializingConsumerChannelShouldTryConnectIfNotConnectedToRabbit()
    {
        _persistentConnection.IsConnected.Returns(false);
        await _sut.InitializeConsumersChannelsAsync();
        _persistentConnection.Received(2).TryConnect();
    }

    [Fact]
    public async Task WhenInitializingConsumerChannelShouldNotTryConnectIfAlreadyConnectedToRabbit()
    {
        _persistentConnection.IsConnected.Returns(true);
        await _sut.InitializeConsumersChannelsAsync();
        _persistentConnection.Received(1).TryConnect();
    }

    [Fact]
    public async Task WhenInitializingConsumerChannelEnsureThatQueueAndExchangeAreCreatedBeforeConsumerStarts()
    {
        var queueName = _rabbitMqEventBusOptions.Value.QueueName;
        var exchangeName = _rabbitMqEventBusOptions.Value.ExchangeName;
        await _sut.InitializeConsumersChannelsAsync();
        Received.InOrder(() =>
        {            
            _model.ExchangeDeclare(exchange: exchangeName, type: "topic");
            _model.BasicConsume(queue: queueName, autoAck: false, _consumer);
            // _model.QueueDeclare(queueName);            
        });
    }
}