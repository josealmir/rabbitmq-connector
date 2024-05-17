using RabbitMQ.Client.Events;

namespace RabbitMq.Connector.Rabbit
{
    internal static class BasicDeliverEventArgsExtensions
    {
        /// <summary>
        /// Get Correlation from BasicProperties or Headers.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns>Correlation Id from BasicProperties or Headers</returns>
        /// <exception cref="ArgumentNullException"/>
        public static string Correlation(this BasicDeliverEventArgs eventArgs)
        {
            if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs));  
            
            return  eventArgs.BasicProperties.MessageId;                
        }
    }
}