namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Azure Blob Storage crawl repository settings.
    /// </summary>
    public class AzureBlobCrawlRepositorySettings : CrawlRepositorySettings
    {
        /// <summary>
        /// Storage account name, for example contosodocs (3-24 lowercase letters and digits).
        /// </summary>
        [JsonPropertyName("AzureAccountName")]
        public string AzureAccountName { get; set; }

        /// <summary>
        /// Storage account access key (key1 or key2 from Access keys in the Azure portal). Sensitive: stored with the
        /// crawl plan and returned by the API; treat it as a secret.
        /// </summary>
        [JsonPropertyName("AzureAccessKey")]
        public string AzureAccessKey { get; set; }

        /// <summary>
        /// Container name only, for example documents.
        /// </summary>
        [JsonPropertyName("AzureContainer")]
        public string AzureContainer { get; set; }

        /// <summary>
        /// Blob service endpoint. Null or empty uses https://{account}.blob.core.windows.net/. Set it for sovereign clouds,
        /// private endpoints or the Azurite emulator (for example http://127.0.0.1:10000/devstoreaccount1/).
        /// </summary>
        [JsonPropertyName("AzureEndpoint")]
        public string AzureEndpoint { get; set; }

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AzureBlobCrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.AzureBlob;
        }
    }
}
