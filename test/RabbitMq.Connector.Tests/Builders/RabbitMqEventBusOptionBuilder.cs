using RabbitMq.Connector.Rabbit;

namespace RabbitMq.Connector.Tests.Builders;

public static class RabbitMqEventBusOptionBuilder
{
    public static RabbitMqEventBusOptions BuilderRabbitMqEventBusOptions =>
        new()
        {
            ExchangeName = "TesteExchange",
            QueueName = "TesteQueue",  
            ConsumersCount = 1
        };
}
