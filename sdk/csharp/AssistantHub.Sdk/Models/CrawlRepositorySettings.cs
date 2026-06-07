namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Base crawl repository settings.
    /// </summary>
    public class CrawlRepositorySettings
    {
        /// <summary>
        /// Repository type.
        /// </summary>
        [JsonPropertyName("RepositoryType")]
        public RepositoryTypeEnum RepositoryType { get; set; } = RepositoryTypeEnum.Web;
    }
}
