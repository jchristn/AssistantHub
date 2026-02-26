namespace AssistantHub.Core.Models
{
    using System;

    /// <summary>
    /// Crawl ingestion settings sub-object.
    /// </summary>
    public class CrawlIngestionSettings
    {
        #region Public-Members

        /// <summary>
        /// Ingestion rule identifier.
        /// </summary>
        public string IngestionRuleId { get; set; } = null;

        /// <summary>
        /// Store crawled documents in S3.
        /// Default: true.
        /// </summary>
        public bool StoreInS3 { get; set; } = true;

        /// <summary>
        /// S3 bucket name for storing crawled documents.
        /// </summary>
        public string S3BucketName { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CrawlIngestionSettings()
        {
        }

        #endregion
    }
}
