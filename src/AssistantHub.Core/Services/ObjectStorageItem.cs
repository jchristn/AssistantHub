namespace AssistantHub.Core.Services
{
    using System;

    /// <summary>
    /// Safe metadata for an object listed from S3-compatible storage.
    /// </summary>
    public class ObjectStorageItem
    {
        /// <summary>
        /// Object key.
        /// </summary>
        public string Key { get; set; } = null;

        /// <summary>
        /// Object size in bytes.
        /// </summary>
        public long SizeBytes { get; set; } = 0;

        /// <summary>
        /// Content type when available.
        /// </summary>
        public string ContentType { get; set; } = null;

        /// <summary>
        /// Object ETag when safe to expose.
        /// </summary>
        public string ETag { get; set; } = null;

        /// <summary>
        /// Last modified UTC timestamp when available.
        /// </summary>
        public DateTime? LastModifiedUtc { get; set; } = null;
    }
}
