#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using System.Threading;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using Blobject.CIFS;
    using SyslogLogging;

    /// <summary>
    /// CIFS repository crawler.
    /// </summary>
    public class CifsRepositoryCrawler : FileServerRepositoryCrawlerBase
    {
        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="logging">Logging module.</param>
        /// <param name="database">Database driver.</param>
        /// <param name="crawlPlan">Crawl plan.</param>
        /// <param name="crawlOperation">Crawl operation.</param>
        /// <param name="ingestion">Ingestion service (nullable).</param>
        /// <param name="storage">Storage service (nullable).</param>
        /// <param name="processingLog">Processing log service (nullable).</param>
        /// <param name="enumerationDirectory">Enumeration directory.</param>
        /// <param name="token">Cancellation token.</param>
        public CifsRepositoryCrawler(
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            IObjectStorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token)
            : this(
                  logging,
                  database,
                  crawlPlan,
                  crawlOperation,
                  ingestion,
                  storage,
                  processingLog,
                  enumerationDirectory,
                  token,
                  GetSettings(crawlPlan))
        {
        }

        private CifsRepositoryCrawler(
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            IObjectStorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token,
            CifsCrawlRepositorySettings settings)
            : base(
                  logging,
                  database,
                  crawlPlan,
                  crawlOperation,
                  ingestion,
                  storage,
                  processingLog,
                  enumerationDirectory,
                  token,
                  () => CreateBlobClient(settings),
                  settings.IncludeSubdirectories)
        {
        }

        #endregion

        #region Private-Methods

        private static CifsCrawlRepositorySettings GetSettings(CrawlPlan crawlPlan)
        {
            if (crawlPlan == null) throw new ArgumentNullException(nameof(crawlPlan));

            CifsCrawlRepositorySettings settings = crawlPlan.RepositorySettings as CifsCrawlRepositorySettings;
            if (settings == null) throw new ArgumentException("CrawlPlan must have CifsCrawlRepositorySettings for a CIFS crawler.");
            return settings;
        }

        private static CifsBlobClient CreateBlobClient(CifsCrawlRepositorySettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            CifsSettings cifs = new CifsSettings(
                ResolveEffectiveHostname(settings.CifsHostname),
                settings.CifsUsername,
                settings.CifsPassword,
                settings.CifsShareName);

            return new CifsBlobClient(cifs);
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
