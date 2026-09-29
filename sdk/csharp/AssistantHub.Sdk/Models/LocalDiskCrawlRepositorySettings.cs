namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// Local disk crawl repository settings: a folder on the AssistantHub server's own file system. The folder must be
    /// inside one of the roots the operator lists in Crawl.AllowedLocalPaths; with none listed, local disk crawling is disabled.
    /// </summary>
    public class LocalDiskCrawlRepositorySettings : CrawlRepositorySettings
    {
        /// <summary>
        /// Absolute path of the folder to crawl on the AssistantHub server, for example /data/documents or
        /// D:\Shared\Documents. In Docker this is a path inside the container, so the host folder must be mounted.
        /// </summary>
        [JsonPropertyName("DiskPath")]
        public string DiskPath { get; set; }

        /// <summary>
        /// Include files in subfolders while crawling.
        /// Default: true.
        /// </summary>
        [JsonPropertyName("IncludeSubdirectories")]
        public bool IncludeSubdirectories { get; set; } = true;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public LocalDiskCrawlRepositorySettings()
        {
            RepositoryType = RepositoryTypeEnum.LocalDisk;
        }
    }
}
