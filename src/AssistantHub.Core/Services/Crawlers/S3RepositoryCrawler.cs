#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using System.Threading;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using Blobject.AmazonS3;
    using SyslogLogging;

    /// <summary>
    /// Crawler for Amazon S3 buckets and S3-compatible stores (MinIO, Less3, Ceph, Wasabi, Cloudflare R2), using Blobject.AmazonS3.
    /// </summary>
    public class S3RepositoryCrawler : FileServerRepositoryCrawlerBase
    {
        #region Private-Members

        private readonly S3CrawlRepositorySettings _Settings;

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
        public S3RepositoryCrawler(
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

        private S3RepositoryCrawler(
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            IObjectStorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token,
            S3CrawlRepositorySettings settings)
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
                RepositoryLabel = "S3",
                NetworkProbe = false,
                RequireServerCheck = false,
                LocationLabel = "bucket",
                ShareName = _Settings.S3BucketName,
                Principal = String.IsNullOrWhiteSpace(_Settings.S3AccessKey) ? "anonymous" : _Settings.S3AccessKey.Trim(),
                PrincipalLabel = "access key",
                Guidance = "Verify the bucket name, region and endpoint, and that the access key may list and read the bucket (s3:ListBucket and s3:GetObject)."
            };
        }

        #endregion

        #region Private-Methods

        private static S3CrawlRepositorySettings GetSettings(CrawlPlan crawlPlan)
        {
            if (crawlPlan == null) throw new ArgumentNullException(nameof(crawlPlan));
            S3CrawlRepositorySettings settings = crawlPlan.RepositorySettings as S3CrawlRepositorySettings;
            if (settings == null) throw new ArgumentException("CrawlPlan must have S3CrawlRepositorySettings for a S3 crawler.");
            return settings;
        }

        private static AmazonS3BlobClient CreateBlobClient(S3CrawlRepositorySettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            string accessKey = String.IsNullOrWhiteSpace(settings.S3AccessKey) ? null : settings.S3AccessKey.Trim();
            string secretKey = String.IsNullOrWhiteSpace(settings.S3SecretKey) ? null : settings.S3SecretKey.Trim();
            string region = String.IsNullOrWhiteSpace(settings.S3Region) ? "us-east-1" : settings.S3Region.Trim();
            string bucket = settings.S3BucketName.Trim();

            if (String.IsNullOrWhiteSpace(settings.S3Endpoint))
                return new AmazonS3BlobClient(new AwsSettings(accessKey, secretKey, region, bucket));

            // S3-compatible store: path-style requests against the given service URL. A loopback host is sent to the
            // Docker host when AssistantHub runs in a container.
            Uri endpoint = new Uri(settings.S3Endpoint.Trim());
            UriBuilder builder = new UriBuilder(endpoint) { Host = ResolveEffectiveHostname(endpoint.Host) };
            string serviceUrl = builder.Uri.ToString();
            if (!serviceUrl.EndsWith("/", StringComparison.Ordinal)) serviceUrl += "/";
            bool ssl = String.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            return new AmazonS3BlobClient(new AwsSettings(serviceUrl, ssl, accessKey, secretKey, region, bucket, serviceUrl + "{bucket}/{key}"));
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
