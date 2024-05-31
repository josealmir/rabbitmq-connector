using System.Text.Json.Serialization;

namespace RabbitMq.Connector.Model
{
    public interface IEventBasicPropertie
    {
        [JsonIgnore]
        public IDictionary<string, object> Headers { get; set; }

        [JsonIgnore]
        public IDictionary<BasicPorperties, string> BasicPropertie { get; set; }        
    }
}