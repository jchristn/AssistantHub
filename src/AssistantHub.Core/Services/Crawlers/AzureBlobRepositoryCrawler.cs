#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using System.Threading;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using Azure.Storage;
    using Azure.Storage.Blobs;
    using Blobject.AzureBlob;
    using SyslogLogging;

    /// <summary>
    /// Crawler for Azure Blob Storage containers, using Blobject.AzureBlob.
    /// </summary>
    public class AzureBlobRepositoryCrawler : FileServerRepositoryCrawlerBase
    {
        #region Private-Members

        private readonly AzureBlobCrawlRepositorySettings _Settings;

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
        public AzureBlobRepositoryCrawler(
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

        private AzureBlobRepositoryCrawler(
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            IObjectStorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token,
            AzureBlobCrawlRepositorySettings settings)
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
                RepositoryLabel = "Azure Blob",
                NetworkProbe = false,
                RequireServerCheck = false,
                LocationLabel = "container",
                ShareName = _Settings.AzureContainer,
                Principal = _Settings.AzureAccountName,
                PrincipalLabel = "storage account",
                Guidance = "Verify the account name, access key, container name and endpoint (for Azurite, http://127.0.0.1:10000/devstoreaccount1/)."
            };
        }

        #endregion

        #region Private-Methods

        private static AzureBlobCrawlRepositorySettings GetSettings(CrawlPlan crawlPlan)
        {
            if (crawlPlan == null) throw new ArgumentNullException(nameof(crawlPlan));
            AzureBlobCrawlRepositorySettings settings = crawlPlan.RepositorySettings as AzureBlobCrawlRepositorySettings;
            if (settings == null) throw new ArgumentException("CrawlPlan must have AzureBlobCrawlRepositorySettings for a Azure Blob crawler.");
            return settings;
        }

        private static AzureBlobClient CreateBlobClient(AzureBlobCrawlRepositorySettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            Uri endpoint = new Uri(settings.GetEffectiveEndpoint());
            UriBuilder builder = new UriBuilder(endpoint) { Host = ResolveEffectiveHostname(endpoint.Host) };
            string serviceUrl = builder.Uri.ToString().TrimEnd('/');

            // The clients are built from explicit URLs rather than a connection string: from a connection string the
            // SDK guesses whether the account is in the host or the path, and guesses wrong for an emulator on a
            // host name and non-standard port (for example http://localhost:20000/devstoreaccount1).
            StorageSharedKeyCredential credential = new StorageSharedKeyCredential(settings.AzureAccountName.Trim(), settings.AzureAccessKey.Trim());
            BlobServiceClient service = new BlobServiceClient(new Uri(serviceUrl + "/"), credential);
            BlobContainerClient container = new BlobContainerClient(new Uri(serviceUrl + "/" + settings.AzureContainer.Trim()), credential);
            return new AzureBlobClient(service, container);
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
