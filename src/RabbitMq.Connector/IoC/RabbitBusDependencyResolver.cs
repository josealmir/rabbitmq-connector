using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMq.Connector.Rabbit;
using RabbitMQ.Client;
using System.Reflection;

namespace RabbitMq.Connector.IoC
{
    public static class RabbitBusDependencyResolver
    {
        private static readonly RabbitMqEventBusOptions _options = new();
        public static void AddRabbitEventBus(this IServiceCollection services, Action<RabbitMqEventBusOptions> config)
        {
            ArgumentNullException.ThrowIfNull(config);

            config.Invoke(_options);
                  
            if (!services.Any(s => s.ServiceType == typeof(IMediator)))
                services.AddMediatR(options => options.RegisterServicesFromAssembly(Assembly.GetCallingAssembly()));

            services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
            services.AddScoped<IFailureEventService, FailureEventService>();
            services.AddSingleton<IEventPublisher, RabbitEventPublisher>();
            services.AddSingleton<IRabbitConsumerInitializer, RabbitConsumerInitializer>();
            services.AddSingleton<IRabbitConsumerHandler, RabbitConsumerHandler>();
            services.AddSingleton<IEventSubscriber, RabbitMqEventSubscriber>();
            services.AddSingleton<IExchangeQueueCreator, ExchangeQueueCreator>();
            services.AddSingleton<IServiceScopeFactory>(sp => sp.GetRequiredService<IServiceScopeFactory>());
            services.AddSingleton<IConnectionFactory>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<RabbitMqEventBusOptions>>().Value;

                return new ConnectionFactory()
                {
                    VirtualHost = options.VirtualHost,
                    HostName = options.HostName,
                    UserName = options.Username,
                    Password = options.Password,
                    Port = options.Port,
                    ContinuationTimeout = TimeSpan.FromSeconds(30),
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
                    HandshakeContinuationTimeout = TimeSpan.FromSeconds(30),
                    SocketReadTimeout = TimeSpan.FromSeconds(30),
                    SocketWriteTimeout = TimeSpan.FromSeconds(30),
                };
            });
            services.AddSingleton<IRabbitMqPersistentConnection, RabbitMqPersistentConnection>();
        }
    }
}