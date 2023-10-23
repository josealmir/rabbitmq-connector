using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMq.Connector.Model;
using RabbitMq.Connector.Rabbit;
using RabbitMq.Connector.Tests.Builders;
using RabbitMQ.Client;

namespace RabbitMq.Connector.Tests;

public class RabbitMqEventSubscriberTest
{
    private ISubscriptionManager _subscriptionManager;
    private IRabbitMqPersistentConnection _rabbitMqPersistentConnection;
    private readonly IOptions<RabbitMqEventBusOptions> _rabbitMqEventBusOptions;
    private RabbitMqEventSubscriber _sut;
    private IRabbitConsumerInitializer _rabbitConsumerInitializer;
    private IModel _model;
    private Subscription<Event> _subscription;

    public RabbitMqEventSubscriberTest()
    {
        _subscriptionManager = Substitute.For<ISubscriptionManager>();
        _rabbitMqPersistentConnection = Substitute.For<IRabbitMqPersistentConnection>();
        _rabbitConsumerInitializer = Substitute.For<IRabbitConsumerInitializer>();
        _rabbitMqEventBusOptions = Substitute.For<IOptions<RabbitMqEventBusOptions>>();
        _rabbitMqEventBusOptions.Value.Returns(RabbitMqEventBusOptionBuilder.BuilderRabbitMqEventBusOptions);
        _model = Substitute.For<IModel>();
        _rabbitMqPersistentConnection.CreateModel().Returns(_model);
        _subscription = new Subscription<Event>();
        _subscriptionManager.AddSubscription<Event>().Returns(_subscription);


        _sut = new RabbitMqEventSubscriber(
            _subscriptionManager, 
            _rabbitMqPersistentConnection, 
            _rabbitConsumerInitializer,
            _rabbitMqEventBusOptions, 
            Substitute.For<ILogger<RabbitMqEventSubscriber>>());
    }

    [Fact]
    public void WhenSubscribingToEventShouldSubscribeToSubscriptionManager()
    {
        _sut.Subscribe<Event>();

        _subscriptionManager.Received(1).AddSubscription<Event>();
    }

    [Fact]
    public void WhenSubscribingToEventAndNotConnectedToRabbitShouldConnect()
    {
        _rabbitMqPersistentConnection.IsConnected.Returns(false);
        _sut.Subscribe<Event>();

        _rabbitMqPersistentConnection.Received(1).TryConnect();
    }

    [Fact]
    public void WhenSubscribindToEventAndConnectedToRabbitShouldNotConnect()
    {
        _rabbitMqPersistentConnection.IsConnected.Returns(true);
        _sut.Subscribe<Event>();

        _rabbitMqPersistentConnection.DidNotReceive().TryConnect();
    }

    [Fact]
    public void WhenSubscribingToEventShouldBindRabbitQueueToExchangeWithRoutingKey()
    {
        _sut.Subscribe<Event>();
        _model.Received(1).QueueBind(
            queue: _rabbitMqEventBusOptions.Value.QueueName,
            exchange: _rabbitMqEventBusOptions.Value.ExchangeName,
            routingKey: _subscription.EventName);
    }
}
