namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// NFS crawl repository settings.
    /// </summary>
    public class NfsCrawlRepositorySettings : CrawlRepositorySettings
    {
        /// <summary>
        /// NFS hostname or IP address.
        /// </summary>
        [JsonPropertyName("NfsHostname")]
        public string NfsHostname { get; set; }

        /// <summary>
        /// NFS user identifier.
        /// </summary>
        [JsonPropertyName("NfsUserId")]
        public int? NfsUserId { get; set; }

        /// <summary>
        /// NFS group identifier.
        /// </summary>
        [JsonPropertyName("NfsGroupId")]
        public int? NfsGroupId { get; set; }

        /// <summary>
        /// NFS share name.
        /// </summary>
        [JsonPropertyName("NfsShareName")]
        public string NfsShareName { get; set; }

        /// <summary>
        /// NFS protocol version.
        /// </summary>
        [JsonPropertyName("NfsVersion")]
        public NfsVersionEnum NfsVersion { get; set; } = NfsVersionEnum.V3;

        /// <summary>
        /// Include files in subdirectories while crawling.
        /// </summary>
        [JsonPropertyName("IncludeSubdirectories")]
        public bool IncludeSubdirectories { get; set; } = true;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public NfsCrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.NFS;
        }
    }
}
