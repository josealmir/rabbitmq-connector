using System.ComponentModel.DataAnnotations;

namespace RabbitMq.Connector.Rabbit;

public class RabbitMqEventBusOptions
{
    public const string SectionName = "RabbitMq";
    [Required]
    public virtual string Username { get; set; } = "guest";
    [Required]
    public virtual string Password { get; set; } = "guest";
    [Required]
    public virtual int Port { get; set; } = 5672;
    [Required]
    public virtual string HostName { get; set; } = "127.0.0.1";
    [Required]
    public virtual string ExchangeName { get; set; } = string.Empty;
    [Required]
    public virtual string QueueName { get; set; } = string.Empty;
    [Required]
    public virtual string VirtualHost { get; set; } = "/";
    public virtual string DeadLetterName => $"{QueueName}.error";
    public virtual int ConsumersCount { get; set; } = 5;      
}