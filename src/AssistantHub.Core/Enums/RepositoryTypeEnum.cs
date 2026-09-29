namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Repository type for crawl plans.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RepositoryTypeEnum
    {
        /// <summary>
        /// Web site, crawled with CrawlSharp.
        /// </summary>
        [EnumMember(Value = "Web")]
        Web,

        /// <summary>
        /// SMB/CIFS file share, crawled with Blobject.CIFS.
        /// </summary>
        [EnumMember(Value = "CIFS")]
        CIFS,

        /// <summary>
        /// NFSv3 export, crawled with Blobject.NFS.
        /// </summary>
        [EnumMember(Value = "NFS")]
        NFS,

        /// <summary>
        /// Amazon S3 or S3-compatible bucket, crawled with Blobject.AmazonS3.
        /// </summary>
        [EnumMember(Value = "S3")]
        S3,

        /// <summary>
        /// Azure Blob Storage container, crawled with Blobject.AzureBlob.
        /// </summary>
        [EnumMember(Value = "AzureBlob")]
        AzureBlob,

        /// <summary>
        /// Google Cloud Storage bucket, crawled with Blobject.GoogleCloud.
        /// </summary>
        [EnumMember(Value = "GoogleCloud")]
        GoogleCloud,

        /// <summary>
        /// Folder on the AssistantHub server's file system, crawled with Blobject.Disk.
        /// </summary>
        [EnumMember(Value = "LocalDisk")]
        LocalDisk,

        /// <summary>
        /// Git repository hosted on GitHub, crawled with GitHubCrawler.
        /// </summary>
        [EnumMember(Value = "Git")]
        Git
    }
}
