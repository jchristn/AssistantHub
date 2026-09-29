namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Google Cloud Storage crawl repository settings.
    /// </summary>
    public class GoogleCloudCrawlRepositorySettings : CrawlRepositorySettings
    {
        /// <summary>
        /// Google Cloud project ID, for example contoso-docs-123456.
        /// </summary>
        [JsonPropertyName("GcpProjectId")]
        public string GcpProjectId { get; set; }

        /// <summary>
        /// Bucket name only, for example contoso-documents (not gs://contoso-documents).
        /// </summary>
        [JsonPropertyName("GcpBucketName")]
        public string GcpBucketName { get; set; }

        /// <summary>
        /// Full JSON key of a service account with read access to the bucket (Storage Object Viewer). Sensitive: stored
        /// with the crawl plan and returned by the API; treat it as a secret.
        /// </summary>
        [JsonPropertyName("GcpJsonCredentials")]
        public string GcpJsonCredentials { get; set; }

        /// <summary>
        /// Custom endpoint, for example for a private endpoint or an emulator. Null or empty uses the standard endpoint.
        /// </summary>
        [JsonPropertyName("GcpEndpoint")]
        public string GcpEndpoint { get; set; }

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GoogleCloudCrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.GoogleCloud;
        }
    }
}
