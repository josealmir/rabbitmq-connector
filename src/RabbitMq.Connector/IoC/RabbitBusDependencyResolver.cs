﻿using MediatR;
using RabbitMQ.Client;
using System.Reflection;
using RabbitMq.Connector.Rabbit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Diagnostics;

namespace RabbitMq.Connector.IoC
{
    public static class RabbitBusDependencyResolver
    {
        private static RabbitMqEventBusOptions _options = new RabbitMqEventBusOptions();
        public static void AddRabbitEventBus(this IServiceCollection services, Action<RabbitMqEventBusOptions> option)
        {
            services.RabbitConfigureDependency();
            services.RabbitConfigureOption(option);                        
            services.AddSingleton<IConnectionFactory>(sp =>
            {
                return new ConnectionFactory
                {
                    VirtualHost = _options.VirtualHost,
                    HostName = _options.HostName,
                    UserName = _options.Username,
                    Password = _options.Password,
                    Port = _options.Port,
                    ContinuationTimeout = TimeSpan.FromSeconds(30),
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
                    HandshakeContinuationTimeout = TimeSpan.FromSeconds(30),
                    SocketReadTimeout = TimeSpan.FromSeconds(30),
                    SocketWriteTimeout = TimeSpan.FromSeconds(30),
                };
            });
            services.AddSingleton<IRabbitMqPersistentConnection, RabbitMqPersistentConnection>();
        }

        public static void AddRabbitEventBusUseUri(this IServiceCollection services, Action<RabbitMqEventBusOptions> option)
        {
            services.RabbitConfigureDependency();
            services.RabbitConfigureOption(option);
            services.AddSingleton<IConnectionFactory>(sp => 
            {
                return new ConnectionFactory
                {
                    Uri = new Uri(_options.ConnectionUri),
                    ContinuationTimeout = TimeSpan.FromSeconds(30),
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
                    HandshakeContinuationTimeout = TimeSpan.FromSeconds(30),
                    SocketReadTimeout = TimeSpan.FromSeconds(30),
                    SocketWriteTimeout = TimeSpan.FromSeconds(30),
                };
            });
            services.AddSingleton<IRabbitMqPersistentConnection, RabbitMqPersistentConnection>();
        }

        internal static void RabbitConfigureOption(this IServiceCollection services, Action<RabbitMqEventBusOptions> option)
        {
            if (option is null)
                throw new ArgumentNullException(nameof(option));

            option.Invoke(_options);
            services.Configure(option);
        }

        internal static void RabbitConfigureDependency(this IServiceCollection services)
        {
            if (!services.Any(s => s.ServiceType == typeof(IMediator)))
                services.AddMediatR(options => options.RegisterServicesFromAssembly(Assembly.GetCallingAssembly()));

            services.AddScoped<IFailureEventService, FailureEventService>();
            services.AddSingleton<IEventPublisher, RabbitEventPublisher>();
            services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
            services.AddSingleton<IEventSubscriber, RabbitMqEventSubscriber>();
            services.AddSingleton<IExchangeQueueCreator, ExchangeQueueCreator>();
            services.AddSingleton<IRabbitConsumerHandler, RabbitConsumerHandler>();
            services.AddSingleton<IRabbitConsumerInitializer, RabbitConsumerInitializer>();
            services.AddSingleton(sp => sp.GetRequiredService<IServiceScopeFactory>());    
        }
    }
}