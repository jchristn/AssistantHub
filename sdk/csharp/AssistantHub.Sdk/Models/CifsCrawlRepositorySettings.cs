namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// CIFS crawl repository settings.
    /// </summary>
    public class CifsCrawlRepositorySettings : CrawlRepositorySettings
    {
        /// <summary>
        /// CIFS hostname or IP address.
        /// </summary>
        [JsonPropertyName("CifsHostname")]
        public string CifsHostname { get; set; }

        /// <summary>
        /// CIFS username.
        /// </summary>
        [JsonPropertyName("CifsUsername")]
        public string CifsUsername { get; set; }

        /// <summary>
        /// CIFS password.
        /// </summary>
        [JsonPropertyName("CifsPassword")]
        public string CifsPassword { get; set; }

        /// <summary>
        /// CIFS share name.
        /// </summary>
        [JsonPropertyName("CifsShareName")]
        public string CifsShareName { get; set; }

        /// <summary>
        /// Include files in subdirectories while crawling.
        /// </summary>
        [JsonPropertyName("IncludeSubdirectories")]
        public bool IncludeSubdirectories { get; set; } = true;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CifsCrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.CIFS;
        }
    }
}
