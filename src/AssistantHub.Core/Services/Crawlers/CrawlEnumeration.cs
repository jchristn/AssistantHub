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
}
