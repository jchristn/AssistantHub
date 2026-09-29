#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using System.Threading;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using System.IO;
    using Blobject.Disk;
    using SyslogLogging;

    /// <summary>
    /// Crawler for a folder on the AssistantHub server, using Blobject.Disk. Only folders inside Crawl.AllowedLocalPaths may be crawled, and files reached through a symbolic link or junction are skipped so a crawl cannot leave the allowed folder.
    /// </summary>
    public class LocalDiskRepositoryCrawler : FileServerRepositoryCrawlerBase
    {
        #region Private-Members

        private readonly LocalDiskCrawlRepositorySettings _Settings;

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
        public LocalDiskRepositoryCrawler(
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

        private LocalDiskRepositoryCrawler(
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            IObjectStorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token,
            LocalDiskCrawlRepositorySettings settings)
            : base(logging, database, crawlPlan, crawlOperation, ingestion, storage, processingLog, enumerationDirectory, token,
                  () => CreateBlobClient(settings), settings.IncludeSubdirectories, false)
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
                RepositoryLabel = "Local disk",
                NetworkProbe = false,
                LocationLabel = "folder",
                ShareName = _Settings.DiskPath,
                Principal = null,
                PrincipalLabel = "identity",
                Guidance = "Verify that the folder exists on the AssistantHub server (in Docker, inside the container, so the host folder must be mounted), that it is inside Crawl.AllowedLocalPaths, and that the server process can read it."
            };
        }

        /// <inheritdoc />
        protected override bool IncludeBlob(string key)
        {
            // Skip anything reached through a symbolic link or junction: it may point outside the allowed folder.
            if (String.IsNullOrEmpty(key)) return true;
            string current = Path.GetFullPath(_Settings.DiskPath.Trim());
            foreach (string part in key.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, part);
                FileSystemInfo info = File.Exists(current) ? new FileInfo(current) : new DirectoryInfo(current);
                if (info.Exists && info.LinkTarget != null) return false;
            }

            return true;
        }

        #endregion

        #region Private-Methods

        private static LocalDiskCrawlRepositorySettings GetSettings(CrawlPlan crawlPlan)
        {
            if (crawlPlan == null) throw new ArgumentNullException(nameof(crawlPlan));
            LocalDiskCrawlRepositorySettings settings = crawlPlan.RepositorySettings as LocalDiskCrawlRepositorySettings;
            if (settings == null) throw new ArgumentException("CrawlPlan must have LocalDiskCrawlRepositorySettings for a Local disk crawler.");
            return settings;
        }

        private static DiskBlobClient CreateBlobClient(LocalDiskCrawlRepositorySettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            // Checked on every crawl, not only when the plan is saved, because the allowed roots can change.
            string policyError = LocalDiskCrawlPolicy.Check(settings.DiskPath);
            if (policyError != null) throw new UnauthorizedAccessException(policyError);

            string path = Path.GetFullPath(settings.DiskPath.Trim());

            // Blobject.Disk creates a missing directory; a crawler must never create folders on the server.
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException("Folder '" + path + "' does not exist on the AssistantHub server.");

            DirectoryInfo directory = new DirectoryInfo(path);
            if (directory.LinkTarget != null)
            {
                FileSystemInfo target = directory.ResolveLinkTarget(true);
                string targetError = target != null ? LocalDiskCrawlPolicy.Check(target.FullName) : null;
                if (targetError != null) throw new UnauthorizedAccessException("Folder '" + path + "' is a link to '" + target.FullName + "'. " + targetError);
            }

            return new DiskBlobClient(new DiskSettings(path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar));
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
