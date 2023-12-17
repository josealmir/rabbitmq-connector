using Sample;
using RabbitMq.Connector.IoC;


IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddRabbitEventBus(option => 
        {
            option.HostName = "127.0.0.1";
            option.QueueName = "queue-name";
            option.ExchangeName = "exchange-name";
            option.Password = "guest";
            option.Username = "guest";
            option.ConsumersCount = 10;
            option.Port = 5672;
        });
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
