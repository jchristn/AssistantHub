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
                    case RepositoryTypeEnum.S3:
                        return JsonSerializer.Deserialize<S3CrawlRepositorySettings>(json, options);
                    case RepositoryTypeEnum.AzureBlob:
                        return JsonSerializer.Deserialize<AzureBlobCrawlRepositorySettings>(json, options);
                    case RepositoryTypeEnum.GoogleCloud:
                        return JsonSerializer.Deserialize<GoogleCloudCrawlRepositorySettings>(json, options);
                    case RepositoryTypeEnum.LocalDisk:
                        return JsonSerializer.Deserialize<LocalDiskCrawlRepositorySettings>(json, options);
                    case RepositoryTypeEnum.Git:
                        return JsonSerializer.Deserialize<GitCrawlRepositorySettings>(json, options);
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
            if (TryGetProperty(root, "S3BucketName", out _)) return RepositoryTypeEnum.S3;
            if (TryGetProperty(root, "AzureAccountName", out _)) return RepositoryTypeEnum.AzureBlob;
            if (TryGetProperty(root, "GcpBucketName", out _)) return RepositoryTypeEnum.GoogleCloud;
            if (TryGetProperty(root, "DiskPath", out _)) return RepositoryTypeEnum.LocalDisk;
            if (TryGetProperty(root, "GitRepositoryUrl", out _)) return RepositoryTypeEnum.Git;
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
