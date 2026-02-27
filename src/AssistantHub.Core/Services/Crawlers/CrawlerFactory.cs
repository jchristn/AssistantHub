namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using System.Threading;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using SyslogLogging;

    /// <summary>
    /// Factory for creating crawler instances based on repository type.
    /// </summary>
    public static class CrawlerFactory
    {
        /// <summary>
        /// Create a crawler for the specified repository type.
        /// </summary>
        /// <param name="type">Repository type.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="database">Database driver.</param>
        /// <param name="crawlPlan">Crawl plan.</param>
        /// <param name="crawlOperation">Crawl operation.</param>
        /// <param name="ingestion">Ingestion service (nullable).</param>
        /// <param name="storage">Storage service (nullable).</param>
        /// <param name="processingLog">Processing log service (nullable).</param>
        /// <param name="enumerationDirectory">Enumeration directory.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Crawler instance.</returns>
        public static CrawlerBase Create(
            RepositoryTypeEnum type,
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            StorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token)
        {
            switch (type)
            {
                case RepositoryTypeEnum.Web:
                    return new WebRepositoryCrawler(
                        logging, database, crawlPlan, crawlOperation,
                        ingestion, storage, processingLog, enumerationDirectory, token);
                default:
                    throw new NotSupportedException("Repository type " + type + " is not supported.");
            }
        }
    }
}
