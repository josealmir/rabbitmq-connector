using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace RabbitMq.Connector.Extensions
{
    internal static class JsonSerializerOptionsExtension
    {
        public static JsonSerializerOptions Configure(this JsonSerializerOptions config)
        {
            config.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            config.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            config.WriteIndented = true;
            config.IncludeFields = true;
            config.Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic);
            return config;
        }
    }
}