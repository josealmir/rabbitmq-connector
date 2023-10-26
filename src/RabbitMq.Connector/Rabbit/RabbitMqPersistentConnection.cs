using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMq.Connector.Rabbit;

public sealed class RabbitMqPersistentConnection : IRabbitMqPersistentConnection
{
    private readonly ILogger<RabbitMqEventBusOptions> _logger;
    private readonly IConnectionFactory _connectionFactory;
    readonly object sync_root = new object();
    private IConnection _connection;
    private bool _disposed;

    public RabbitMqPersistentConnection(ILogger<RabbitMqEventBusOptions> logger, IConnectionFactory connectionFactory)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
    }

    public bool IsConnected => (_connection?.IsOpen ?? false) && !_disposed;

    public void TryConnect()
    {
        _logger.LogDebug("Trying to connect to RabbitMq");
        lock (sync_root)
        {
            try
            {
                _connection = _connectionFactory.CreateConnection();

                if (IsConnected)
                {
                    _logger.LogDebug("RabbitMq Connected");
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;
                }
                else
                {
                    _logger.LogError("Falied to connect to RabbitMq");
                    throw new FailedToConnectToRabbitMqException();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falied to connect to RabbitMq");
                throw new FailedToConnectToRabbitMqException(ex);
            }
        }
    }

    public IModel CreateModel()
    {
        _logger.LogDebug("Creating RabbitMq model");
        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
        }


        var model = _connection.CreateModel();
        _logger.LogDebug("Model created");

        return model;
    }

    public EventingBasicConsumer CreateConsumer(IModel channel) 
        => new(channel);

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            _connection.Dispose();
        }
        catch (IOException ex)
        {
            _logger.LogCritical(ex, "Falied to dispose RabbitMq connection");
        }
    }

    private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

        TryConnect();
    }

    private void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

        TryConnect();
    }

    private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

        TryConnect();
    }

}