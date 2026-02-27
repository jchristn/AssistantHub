namespace AssistantHub.Core.Services.Crawlers
{
    using System;

    /// <summary>
    /// Represents a single crawled object (URL, file, etc.).
    /// </summary>
    public class CrawledObject
    {
        #region Public-Members

        /// <summary>
        /// Object key (URL or path).
        /// </summary>
        public string Key { get; set; } = null;

        /// <summary>
        /// MIME content type.
        /// </summary>
        public string ContentType { get; set; } = null;

        /// <summary>
        /// Content length in bytes.
        /// </summary>
        public long ContentLength { get; set; } = 0;

        /// <summary>
        /// Raw data bytes.
        /// </summary>
        public byte[] Data { get; set; } = null;

        /// <summary>
        /// MD5 hash of the content.
        /// </summary>
        public string MD5Hash { get; set; } = null;

        /// <summary>
        /// SHA1 hash of the content.
        /// </summary>
        public string SHA1Hash { get; set; } = null;

        /// <summary>
        /// SHA256 hash of the content.
        /// </summary>
        public string SHA256Hash { get; set; } = null;

        /// <summary>
        /// ETag from the server.
        /// </summary>
        public string ETag { get; set; } = null;

        /// <summary>
        /// Last modified timestamp in UTC.
        /// </summary>
        public DateTime? LastModifiedUtc { get; set; } = null;

        /// <summary>
        /// Document identifier populated after ingestion.
        /// </summary>
        public string DocumentId { get; set; } = null;

        /// <summary>
        /// Whether this object represents a folder/directory.
        /// </summary>
        public bool IsFolder { get; set; } = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CrawledObject()
        {
        }

        /// <summary>
        /// Create a copy without the Data field (for enumeration file storage).
        /// </summary>
        /// <returns>Copy without data.</returns>
        public CrawledObject CopyWithoutData()
        {
            CrawledObject copy = new CrawledObject();
            copy.Key = Key;
            copy.ContentType = ContentType;
            copy.ContentLength = ContentLength;
            copy.Data = null;
            copy.MD5Hash = MD5Hash;
            copy.SHA1Hash = SHA1Hash;
            copy.SHA256Hash = SHA256Hash;
            copy.ETag = ETag;
            copy.LastModifiedUtc = LastModifiedUtc;
            copy.DocumentId = DocumentId;
            copy.IsFolder = IsFolder;
            return copy;
        }

        #endregion
    }
}
