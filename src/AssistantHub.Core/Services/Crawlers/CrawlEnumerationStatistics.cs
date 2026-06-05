namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using System.Collections.Generic;

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
