using RabbitMQ.Client;

namespace RabbitMq.Connector.Model
{
    public class BasicPorperties : IBasicProperties
    {
        public string AppId { get; set; } = string.Empty;
        public string ClusterId { get; set; } = string.Empty;
        public string ContentEncoding { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public byte DeliveryMode { get; set; }
        public string Expiration { get; set; } = string.Empty;
        public IDictionary<string, object> Headers { get; set; } = new Dictionary<string, object>();
        public string MessageId { get; set; } = string.Empty;
        public bool Persistent { get; set; }
        public byte Priority { get; set; }
        public string ReplyTo { get; set; } = string.Empty;
        public PublicationAddress ReplyToAddress { get; set; }
        public AmqpTimestamp Timestamp { get; set; } = default;
        public string Type { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;

        public ushort ProtocolClassId => throw new NotImplementedException();

        public string ProtocolClassName => throw new NotImplementedException();

        public void ClearAppId()
            => AppId = string.Empty;

        public void ClearClusterId()
            => ClusterId = string.Empty;

        public void ClearContentEncoding()
            => ContentEncoding = string.Empty;

        public void ClearContentType()
            => ContentType = string.Empty;

        public void ClearCorrelationId()
            => CorrelationId = string.Empty;

        public void ClearDeliveryMode()
            => DeliveryMode = default;

        public void ClearExpiration()
            => Expiration = string.Empty;

        public void ClearHeaders()
            => Headers = new Dictionary<string, object>();

        public void ClearMessageId()
            => MessageId = string.Empty;

        public void ClearPriority()
            => Priority = default;

        public void ClearReplyTo()
            => ReplyTo = string.Empty;

        public void ClearTimestamp()
            => Timestamp = default;

        public void ClearType()
            => Type = string.Empty;

        public void ClearUserId()
            => UserId = string.Empty;

        public bool IsAppIdPresent()
            => !string.IsNullOrWhiteSpace(AppId);

        public bool IsClusterIdPresent()
            => !string.IsNullOrWhiteSpace(ClusterId);

        public bool IsContentEncodingPresent()
            => !string.IsNullOrWhiteSpace(ContentEncoding);

        public bool IsContentTypePresent()
            => !string.IsNullOrWhiteSpace(ContentType);

        public bool IsCorrelationIdPresent()
            => !string.IsNullOrWhiteSpace(CorrelationId);

        public bool IsDeliveryModePresent()
            => DeliveryMode != default(byte);

        public bool IsExpirationPresent()
            => !string.IsNullOrWhiteSpace(Expiration);

        public bool IsHeadersPresent()
            => Headers.Any();

        public bool IsMessageIdPresent()
            => !string.IsNullOrWhiteSpace(MessageId);

        public bool IsPriorityPresent()
            => Priority != default(byte); 
        public bool IsReplyToPresent()
            => !string.IsNullOrWhiteSpace(ReplyTo);

        public bool IsTimestampPresent()
            => !string.IsNullOrEmpty(Timestamp.ToString());

        public bool IsTypePresent()
            => !string.IsNullOrWhiteSpace(Type);

        public bool IsUserIdPresent()
            => !string.IsNullOrWhiteSpace(UserId);
    }
}