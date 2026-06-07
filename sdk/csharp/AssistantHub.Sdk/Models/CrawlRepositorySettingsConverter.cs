namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// JSON converter for crawl repository settings polymorphic deserialization.
    /// </summary>
    public class CrawlRepositorySettingsConverter : JsonConverter<CrawlRepositorySettings>
    {
        /// <inheritdoc />
        public override CrawlRepositorySettings Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument document = JsonDocument.ParseValue(ref reader))
            {
                JsonElement root = document.RootElement;
                RepositoryTypeEnum repositoryType = DetectRepositoryType(root);
                string json = root.GetRawText();

                switch (repositoryType)
                {
                    case RepositoryTypeEnum.CIFS:
                        return JsonSerializer.Deserialize<CifsCrawlRepositorySettings>(json, options);
                    case RepositoryTypeEnum.NFS:
                        return JsonSerializer.Deserialize<NfsCrawlRepositorySettings>(json, options);
                    case RepositoryTypeEnum.Web:
                    default:
                        return JsonSerializer.Deserialize<WebCrawlRepositorySettings>(json, options);
                }
            }
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, CrawlRepositorySettings value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        private static RepositoryTypeEnum DetectRepositoryType(JsonElement root)
        {
            if (TryGetProperty(root, "RepositoryType", out JsonElement repositoryTypeElement) &&
                repositoryTypeElement.ValueKind == JsonValueKind.String)
            {
                string repositoryTypeString = repositoryTypeElement.GetString();
                if (Enum.TryParse(repositoryTypeString, true, out RepositoryTypeEnum repositoryType))
                {
                    return repositoryType;
                }

                throw new JsonException("Unknown crawl repository type '" + repositoryTypeString + "'.");
            }

            if (TryGetProperty(root, "CifsHostname", out _)) return RepositoryTypeEnum.CIFS;
            if (TryGetProperty(root, "NfsHostname", out _)) return RepositoryTypeEnum.NFS;
            return RepositoryTypeEnum.Web;
        }

        private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement value)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
