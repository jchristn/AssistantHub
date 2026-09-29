#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using System.Threading;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using Blobject.GoogleCloud;
    using SyslogLogging;

    /// <summary>
    /// Crawler for Google Cloud Storage buckets, using Blobject.GoogleCloud.
    /// </summary>
    public class GoogleCloudRepositoryCrawler : FileServerRepositoryCrawlerBase
    {
        #region Private-Members

        private readonly GoogleCloudCrawlRepositorySettings _Settings;

        #endregion

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
        public GoogleCloudRepositoryCrawler(
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            IObjectStorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token)
            : this(logging, database, crawlPlan, crawlOperation, ingestion, storage, processingLog, enumerationDirectory, token, GetSettings(crawlPlan))
        {
        }

        private GoogleCloudRepositoryCrawler(
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            IObjectStorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token,
            GoogleCloudCrawlRepositorySettings settings)
            : base(logging, database, crawlPlan, crawlOperation, ingestion, storage, processingLog, enumerationDirectory, token,
                  () => CreateBlobClient(settings), true, false)
        {
            _Settings = settings;
        }

        #endregion

        #region Protected-Methods

        /// <inheritdoc />
        protected override RepositoryDiagnosticInfo GetDiagnosticInfo()
        {
            return new RepositoryDiagnosticInfo
            {
                RepositoryLabel = "Google Cloud Storage",
                NetworkProbe = false,
                RequireServerCheck = false,
                LocationLabel = "bucket",
                ShareName = _Settings.GcpBucketName,
                Principal = _Settings.GcpProjectId,
                PrincipalLabel = "project",
                Guidance = "Verify the project ID and bucket name, and that the service account key is valid and has the Storage Object Viewer role on the bucket."
            };
        }

        #endregion

        #region Private-Methods

        private static GoogleCloudCrawlRepositorySettings GetSettings(CrawlPlan crawlPlan)
        {
            if (crawlPlan == null) throw new ArgumentNullException(nameof(crawlPlan));
            GoogleCloudCrawlRepositorySettings settings = crawlPlan.RepositorySettings as GoogleCloudCrawlRepositorySettings;
            if (settings == null) throw new ArgumentException("CrawlPlan must have GoogleCloudCrawlRepositorySettings for a Google Cloud Storage crawler.");
            return settings;
        }

        private static GcpBlobClient CreateBlobClient(GoogleCloudCrawlRepositorySettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            string endpoint = String.IsNullOrWhiteSpace(settings.GcpEndpoint) ? null : settings.GcpEndpoint.Trim();
            return new GcpBlobClient(new GcpBlobSettings(settings.GcpProjectId.Trim(), settings.GcpBucketName.Trim(), settings.GcpJsonCredentials, endpoint));
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
