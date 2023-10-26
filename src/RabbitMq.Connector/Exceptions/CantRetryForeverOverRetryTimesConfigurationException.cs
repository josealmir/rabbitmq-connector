
namespace RabbitMq.Connector.Exceptions;

public class CantRetryForeverOverRetryTimesConfigurationException : Exception
{
    public CantRetryForeverOverRetryTimesConfigurationException() : base("Can't retry forever over Retry Times configuration.")
    {

    }
}