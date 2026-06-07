#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using System.Threading;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using Blobject.NFS;
    using SyslogLogging;

    /// <summary>
    /// NFS repository crawler.
    /// </summary>
    public class NfsRepositoryCrawler : FileServerRepositoryCrawlerBase
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
        public NfsRepositoryCrawler(
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

        private NfsRepositoryCrawler(
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            IObjectStorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token,
            NfsCrawlRepositorySettings settings)
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

        private static NfsCrawlRepositorySettings GetSettings(CrawlPlan crawlPlan)
        {
            if (crawlPlan == null) throw new ArgumentNullException(nameof(crawlPlan));

            NfsCrawlRepositorySettings settings = crawlPlan.RepositorySettings as NfsCrawlRepositorySettings;
            if (settings == null) throw new ArgumentException("CrawlPlan must have NfsCrawlRepositorySettings for an NFS crawler.");
            if (settings.NfsUserId == null) throw new ArgumentException("NfsUserId is required for an NFS crawler.");
            if (settings.NfsGroupId == null) throw new ArgumentException("NfsGroupId is required for an NFS crawler.");
            return settings;
        }

        private static NfsBlobClient CreateBlobClient(NfsCrawlRepositorySettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.NfsUserId == null) throw new ArgumentException("NfsUserId is required for an NFS crawler.");
            if (settings.NfsGroupId == null) throw new ArgumentException("NfsGroupId is required for an NFS crawler.");

            NfsSettings nfs = new NfsSettings(
                ResolveEffectiveHostname(settings.NfsHostname),
                settings.NfsUserId.Value,
                settings.NfsGroupId.Value,
                settings.NfsShareName,
                NfsVersionConverter.ToBlobjectNfsVersion(settings.NfsVersion));

            return new NfsBlobClient(nfs);
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
