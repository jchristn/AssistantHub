namespace AssistantHub.Core.Models
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// JSON converter for CrawlRepositorySettings polymorphic deserialization.
    /// Uses the RepositoryType property to determine the derived type.
    /// </summary>
    public class CrawlRepositorySettingsConverter : JsonConverter<CrawlRepositorySettings>
    {
        /// <inheritdoc />
        public override CrawlRepositorySettings Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<WebCrawlRepositorySettings>(ref reader, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, CrawlRepositorySettings value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
