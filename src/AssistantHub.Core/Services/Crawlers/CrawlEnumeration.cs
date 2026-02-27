namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Crawl enumeration containing all file lists and statistics from a crawl operation.
    /// </summary>
    public class CrawlEnumeration
    {
        #region Public-Members

        /// <summary>
        /// All files found during enumeration.
        /// </summary>
        public List<CrawledObject> AllFiles { get; set; } = new List<CrawledObject>();

        /// <summary>
        /// Files added (new since last crawl).
        /// </summary>
        public List<CrawledObject> Added { get; set; } = new List<CrawledObject>();

        /// <summary>
        /// Files changed since last crawl.
        /// </summary>
        public List<CrawledObject> Changed { get; set; } = new List<CrawledObject>();

        /// <summary>
        /// Files deleted since last crawl.
        /// </summary>
        public List<CrawledObject> Deleted { get; set; } = new List<CrawledObject>();

        /// <summary>
        /// Files unchanged since last crawl.
        /// </summary>
        public List<CrawledObject> Unchanged { get; set; } = new List<CrawledObject>();

        /// <summary>
        /// Files successfully processed.
        /// </summary>
        public List<CrawledObject> Success { get; set; } = new List<CrawledObject>();

        /// <summary>
        /// Files that failed processing.
        /// </summary>
        public List<CrawledObject> Failed { get; set; } = new List<CrawledObject>();

        /// <summary>
        /// Aggregate statistics.
        /// </summary>
        public CrawlEnumerationStatistics Statistics { get; set; } = new CrawlEnumerationStatistics();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CrawlEnumeration()
        {
        }

        /// <summary>
        /// Create a copy with Data fields stripped from all objects (for file storage).
        /// </summary>
        /// <returns>Copy without data.</returns>
        public CrawlEnumeration CopyWithoutData()
        {
            CrawlEnumeration copy = new CrawlEnumeration();
            copy.Statistics = Statistics;

            foreach (CrawledObject obj in AllFiles) copy.AllFiles.Add(obj.CopyWithoutData());
            foreach (CrawledObject obj in Added) copy.Added.Add(obj.CopyWithoutData());
            foreach (CrawledObject obj in Changed) copy.Changed.Add(obj.CopyWithoutData());
            foreach (CrawledObject obj in Deleted) copy.Deleted.Add(obj.CopyWithoutData());
            foreach (CrawledObject obj in Unchanged) copy.Unchanged.Add(obj.CopyWithoutData());
            foreach (CrawledObject obj in Success) copy.Success.Add(obj.CopyWithoutData());
            foreach (CrawledObject obj in Failed) copy.Failed.Add(obj.CopyWithoutData());

            return copy;
        }

        #endregion
    }

    /// <summary>
    /// Aggregate statistics for a crawl enumeration.
    /// </summary>
    public class CrawlEnumerationStatistics
    {
        /// <summary>
        /// Total object count.
        /// </summary>
        public long TotalCount { get; set; } = 0;

        /// <summary>
        /// Total bytes.
        /// </summary>
        public long TotalBytes { get; set; } = 0;

        /// <summary>
        /// Added object count.
        /// </summary>
        public long AddedCount { get; set; } = 0;

        /// <summary>
        /// Added bytes.
        /// </summary>
        public long AddedBytes { get; set; } = 0;

        /// <summary>
        /// Changed object count.
        /// </summary>
        public long ChangedCount { get; set; } = 0;

        /// <summary>
        /// Changed bytes.
        /// </summary>
        public long ChangedBytes { get; set; } = 0;

        /// <summary>
        /// Deleted object count.
        /// </summary>
        public long DeletedCount { get; set; } = 0;

        /// <summary>
        /// Deleted bytes.
        /// </summary>
        public long DeletedBytes { get; set; } = 0;

        /// <summary>
        /// Success object count.
        /// </summary>
        public long SuccessCount { get; set; } = 0;

        /// <summary>
        /// Success bytes.
        /// </summary>
        public long SuccessBytes { get; set; } = 0;

        /// <summary>
        /// Failed object count.
        /// </summary>
        public long FailedCount { get; set; } = 0;

        /// <summary>
        /// Failed bytes.
        /// </summary>
        public long FailedBytes { get; set; } = 0;
    }
}
