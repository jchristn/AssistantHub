namespace AssistantHub.Core.Services
{
    using System.Collections.Generic;

    /// <summary>
    /// Result of listing objects from S3-compatible storage.
    /// </summary>
    public class ObjectStorageListResult
    {
        /// <summary>
        /// Bucket that was listed.
        /// </summary>
        public string BucketName { get; set; } = null;

        /// <summary>
        /// Prefix used for listing.
        /// </summary>
        public string Prefix { get; set; } = null;

        /// <summary>
        /// Maximum requested object count.
        /// </summary>
        public int MaxResults { get; set; } = 0;

        /// <summary>
        /// Continuation token for the next page.
        /// </summary>
        public string ContinuationToken { get; set; } = null;

        /// <summary>
        /// Whether the object listing is complete.
        /// </summary>
        public bool EndOfResults { get; set; } = true;

        /// <summary>
        /// Listed objects.
        /// </summary>
        public List<ObjectStorageItem> Objects { get; set; } = new List<ObjectStorageItem>();
    }
}
