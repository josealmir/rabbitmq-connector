using System;
using System.Text;
using RabbitMQ.Client.Events;

namespace RabbitMq.Connector.Rabbit
{
    internal static class BasicDeliverEventArgsExtensions
    {
        /// <summary>
        /// Get Correlation from BasicProperties or Headers.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns>Correlation Id from BasicProperties or Headers with name X-Correlation-Id.</returns>
        /// <exception cref="ArgumentNullException"/>
        public static string Correlation(this BasicDeliverEventArgs eventArgs)
        {
            if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs));

            if (eventArgs.BasicProperties.Headers != null && eventArgs.BasicProperties.Headers.TryGetValue("X-Correlation-Id", out var correlationId) && correlationId is byte[] correlation && correlation.Length > 0)
                return  Encoding.UTF8.GetString(correlation);
            else 
                return eventArgs.BasicProperties.CorrelationId ?? string.Empty;
        }
    }
}