namespace AssistantHub.Sdk.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Repository type enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RepositoryTypeEnum
    {
        /// <summary>
        /// Web.
        /// </summary>
        [EnumMember(Value = "Web")]
        Web,

        /// <summary>
        /// Common Internet File System file server.
        /// </summary>
        [EnumMember(Value = "CIFS")]
        CIFS,

        /// <summary>
        /// Network File System file server.
        /// </summary>
        [EnumMember(Value = "NFS")]
        NFS,

        /// <summary>
        /// Amazon S3 or an S3-compatible object store.
        /// </summary>
        [EnumMember(Value = "S3")]
        S3,

        /// <summary>
        /// Azure Blob Storage container.
        /// </summary>
        [EnumMember(Value = "AzureBlob")]
        AzureBlob,

        /// <summary>
        /// Google Cloud Storage bucket.
        /// </summary>
        [EnumMember(Value = "GoogleCloud")]
        GoogleCloud,

        /// <summary>
        /// Folder on the AssistantHub server (must be inside Crawl.AllowedLocalPaths).
        /// </summary>
        [EnumMember(Value = "LocalDisk")]
        LocalDisk,

        /// <summary>
        /// Git repository on github.com.
        /// </summary>
        [EnumMember(Value = "Git")]
        Git
    }
}
