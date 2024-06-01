using Sample;
using RabbitMq.Connector.IoC;
using static Sample.Worker;
using RabbitMq.Connector;


IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // services.AddRabbitEventBus(option => 
        // {
        //     option.HostName = "127.0.0.1";
        //     option.QueueName = "queue-name";
        //     option.ExchangeName = "exchange-name";
        //     option.Password = "guest";
        //     option.Username = "guest";
        //     option.ConsumersCount = 10;
        //     option.Port = 5672;
        // });
        
        services.AddMediatR(options => { options.RegisterServicesFromAssembly(typeof(EventTeste).Assembly);});
        services.AddRabbitEventBusUseUri(option => 
        {
            option.QueueName = "queue-name";
            option.ExchangeName = "exchange-name";
            option.ConnectionUri = "amqp://guest:guest@localhost:5672/";
        });

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();

