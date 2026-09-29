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
        /// TCP port of the NFS service (null uses 2049).
        /// </summary>
        [JsonPropertyName("NfsPort")]
        public int? NfsPort { get; set; }

        /// <summary>
        /// TCP port of the MOUNT service (null or 0 discovers it through the portmapper).
        /// </summary>
        [JsonPropertyName("NfsMountPort")]
        public int? NfsMountPort { get; set; }

        /// <summary>
        /// TCP port of the portmapper (null uses 111).
        /// </summary>
        [JsonPropertyName("NfsPortmapperPort")]
        public int? NfsPortmapperPort { get; set; }

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
