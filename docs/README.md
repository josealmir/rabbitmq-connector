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
    public void ConfigureServices IServiceCollection services)
    {
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
    }
}
```
### Create your event by inheriting of the class `Event`
```csharp
using RabbitMq.Connector.Model;

// Here exist the possible add Headar property in message

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
### Consumer your `Event` ceated
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
