using System;

namespace RabbitMq.Connector.Rabbit
{
    internal class FailedToConnectToRabbitMqException : Exception
    {
        public FailedToConnectToRabbitMqException() : base("Failed to connect to RabbitMq")
        {

        }

        public FailedToConnectToRabbitMqException(Exception innerException) : base("Failed to connect to RabbitMq", innerException)
        {

        }
    }
}