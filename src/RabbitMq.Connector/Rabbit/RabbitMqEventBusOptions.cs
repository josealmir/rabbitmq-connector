using System.ComponentModel.DataAnnotations;

namespace RabbitMq.Connector.Rabbit
{
    public sealed class RabbitMqEventBusOptions
    {
        [Required]
        public string Username { get; set; } = "guest";
    
        [Required]
        public string Password { get; set; } = "guest";
    
        [Required]
        public int Port { get; set; } = 5672;
    
        [Required]
        public string HostName { get; set; } = "127.0.0.1";

        [Required]
        public string ExchangeName { get; set; } = string.Empty;
    
        [Required]
        public string QueueName { get; set; } = string.Empty;
    
        [Required]    
        public string VirtualHost { get; set; } = "/";
    
        public string ConnectionUri { get; set; } = "amqp://guest:guest@localhost:5672/";    

        public string DeadLetterName => $"{QueueName}.error";
    
        public int ConsumersCount { get; set; } = 5;

    }
}