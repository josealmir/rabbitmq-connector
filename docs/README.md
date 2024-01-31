[![Maintainability](https://api.codeclimate.com/v1/badges/d721340646b2a2b189e4/maintainability)](https://codeclimate.com/github/josealmir/rabbitmq-connector/maintainability)
[![Test Coverage](https://api.codeclimate.com/v1/badges/d721340646b2a2b189e4/test_coverage)](https://codeclimate.com/github/josealmir/rabbitmq-connector/test_coverage)

# RabbitMQ Connector

This package is an implementation of the connection whit RabbitMQ that using MediatR package

## Get Started

RabbitMq.Connector can be installed using the Nuget package manager or the dotnet CLI.

```
dotnet add package RabbitMq.Connector 
```

## Exemplo

### Configuration connection credentials with RabbitMQ
```csharp
using RabbitMq.Connector.IoC;

public class Startup 
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Connect with UserName and Password
        services.AddRabbitEventBus(option => 
        {
            option.Username = "";
            option.Password = "";
            option.Port = 5672;
            option.HostName = "";
            option.ExchangeName = "";
            option.QueueName  = "";
            option.VirtualHost = "";
            option.ConsumersCount = 10;
            option.DeadLetterName = "";
        });

        // Or connect with Uri

        services.AddRabbitEventBusUseUri(option => 
        {
            option.QueueName = "queue-name";
            option.ExchangeName = "exchange-name";
            option.ConnectionUri = "amqp://guest:guest@localhost:5672/";
        });
    }
}
```
### Create your event by inheriting of the class `Event`
```csharp
using RabbitMq.Connector.Model;

// Here exist the possible add Header property in message

public class RabbitMQEvent : Event 
{
   public int Count { get; set; }
}
```
### Publish your `Event`
```csharp
public class BusinessLayer
{
    public BusinessLayer(IMediator mediator)
    {
        mediator.Publish(new RabbitMQEvent { Count = 2; });
    }
}
```
### Consumer your `Event` created
```csharp
public class Worker
{        
    private readonly IEventSubscriber _eventSubscriber;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _eventSubscriber.Subscribe<RabbitMQEvent>()
                        .OnFailure(config => config.RetryForever());
    }
}
```
