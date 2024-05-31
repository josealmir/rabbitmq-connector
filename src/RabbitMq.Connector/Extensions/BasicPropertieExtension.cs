using RabbitMq.Connector.Model;
using RabbitMQ.Client;

namespace RabbitMq.Connector.Extensions
{
    public static class BasicPropertieExtension
    {
        public static IBasicProperties MapTo(this IEventBasicPropertie @event, IModel model)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            var props = model.CreateBasicProperties();
            props.Headers = @event.Headers;

            var content_encoding = string.Empty;
            @event.BasicPropertie.TryGetValue(BasicPorperties.content_encoding, out content_encoding);
            props.ContentEncoding = content_encoding;

            var content_type = string.Empty;
            @event.BasicPropertie.TryGetValue(BasicPorperties.content_type, out content_type);
            props.ContentEncoding = content_type;

            var correlation_id = string.Empty;
            @event.BasicPropertie.TryGetValue(BasicPorperties.correlation_id, out correlation_id);
            props.CorrelationId = correlation_id;

            var message_id = string.Empty;
            @event.BasicPropertie.TryGetValue(BasicPorperties.message_id, out message_id);
            props.MessageId = message_id;

            var cluster_id = string.Empty;
            @event.BasicPropertie.TryGetValue(BasicPorperties.cluster_id, out cluster_id);
            props.ClusterId = cluster_id;

            var expiration = string.Empty;
            @event.BasicPropertie.TryGetValue(BasicPorperties.expiration, out expiration);
            props.Expiration = expiration;

            ExtractTimeStamp(@event, props);
            
            ExtractPriority(@event, props);

            var reply_to = string.Empty;
            @event.BasicPropertie.TryGetValue(BasicPorperties.priority, out reply_to);
            props.ReplyTo = reply_to;

            var app_id = string.Empty;
            @event.BasicPropertie.TryGetValue(BasicPorperties.app_id, out app_id);
            props.AppId = app_id;

            var user_id = string.Empty;
            @event.BasicPropertie.TryGetValue(BasicPorperties.user_id, out user_id);
            props.UserId = user_id;

            var type = string.Empty;
            @event.BasicPropertie.TryGetValue(BasicPorperties.user_id, out type);
            props.Type = type;

            return props;
        }

        internal static void ExtractTimeStamp(IEventBasicPropertie @event, IBasicProperties props)
        {
            var timestamp = string.Empty;
            @event.BasicPropertie.TryGetValue(BasicPorperties.timestamp, out timestamp);
            
            if(!long.TryParse(timestamp, out var timeLong))
                return;

            props.Timestamp = timestamp == string.Empty ? default : new AmqpTimestamp(timeLong);            
        }

        internal static void ExtractPriority(IEventBasicPropertie @event, IBasicProperties props)
        {
            var priority = string.Empty;
            @event.BasicPropertie.TryGetValue(BasicPorperties.priority, out priority);

            if (!byte.TryParse(priority, out var priorityBytes))
                return;

            props.Priority = byte.Parse(priority);
        }
    }
}