#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using CrawlSharp.Web;
    using SyslogLogging;

    /// <summary>
    /// Web repository crawler implementation using CrawlSharp.
    /// </summary>
    public class WebRepositoryCrawler : CrawlerBase
    {
        #region Private-Members

        private readonly string _Header = "[WebRepositoryCrawler] ";
        private WebCrawlRepositorySettings _WebSettings = null;

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
        public WebRepositoryCrawler(
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            StorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token)
            : base(logging, database, crawlPlan, crawlOperation, ingestion, storage, processingLog, enumerationDirectory, token)
        {
            _WebSettings = crawlPlan.RepositorySettings as WebCrawlRepositorySettings;
            if (_WebSettings == null) throw new ArgumentException("CrawlPlan must have WebCrawlRepositorySettings for a web crawler.");
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override async IAsyncEnumerable<CrawledObject> EnumerateAsync([EnumeratorCancellation] CancellationToken token = default)
        {
            _Logging.Info(_Header + "building settings for crawl of " + _WebSettings.StartUrl);
            Settings settings = BuildSettings();
            _Logging.Info(_Header + "creating WebCrawler instance (UseHeadlessBrowser=" + settings.Crawl.UseHeadlessBrowser + ")");
            WebCrawler crawler = new WebCrawler(settings, token);
            crawler.Logger = (msg) => _Logging.Info(_Header + msg);
            crawler.Exception = (msg, ex) => _Logging.Warn(_Header + "crawler exception: " + msg + " " + ex.Message);

            _Logging.Info(_Header + "starting CrawlAsync for " + _WebSettings.StartUrl);
            await foreach (WebResource resource in crawler.CrawlAsync(token))
            {
                if (token.IsCancellationRequested) yield break;

                if (resource.Status < 200 || resource.Status >= 400)
                {
                    _Logging.Warn(_Header + "skipping " + resource.Url + " (HTTP " + resource.Status + ")");
                    continue;
                }

                CrawledObject obj = new CrawledObject();
                obj.Key = resource.Url;
                obj.ContentType = resource.ContentType;
                obj.ContentLength = resource.ContentLength;
                obj.Data = resource.Data;
                obj.MD5Hash = resource.MD5Hash;
                obj.SHA1Hash = resource.SHA1Hash;
                obj.SHA256Hash = resource.SHA256Hash;
                obj.ETag = resource.ETag;
                obj.LastModifiedUtc = resource.LastModified;
                obj.IsFolder = false;

                yield return obj;
            }
        }

        /// <inheritdoc />
        public override async Task<bool> ValidateConnectivityAsync(CancellationToken token = default)
        {
            try
            {
                Settings settings = BuildSettings();
                settings.Crawl.FollowLinks = false;
                settings.Crawl.MaxCrawlDepth = 0;

                WebCrawler crawler = new WebCrawler(settings, token);
                crawler.Logger = (msg) => _Logging.Debug(_Header + msg);
                crawler.Exception = (msg, ex) => _Logging.Warn(_Header + "connectivity test exception: " + msg + " " + ex.Message);

                await foreach (WebResource resource in crawler.CrawlAsync(token))
                {
                    if (resource.Status >= 200 && resource.Status < 400)
                        return true;
                    else
                        return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "connectivity test failed: " + ex.Message);
                return false;
            }
        }

        /// <inheritdoc />
        public override async Task<List<CrawledObject>> EnumerateContentsAsync(int maxKeys = 100, int skip = 0, CancellationToken token = default)
        {
            List<CrawledObject> results = new List<CrawledObject>();
            int current = 0;
            int collected = 0;

            Settings settings = BuildSettings();
            WebCrawler crawler = new WebCrawler(settings, token);
            crawler.Logger = (msg) => _Logging.Debug(_Header + msg);
            crawler.Exception = (msg, ex) => _Logging.Warn(_Header + "enumeration exception: " + msg + " " + ex.Message);

            await foreach (WebResource resource in crawler.CrawlAsync(token))
            {
                if (token.IsCancellationRequested) break;
                if (resource.Status < 200 || resource.Status >= 400) continue;

                if (current < skip)
                {
                    current++;
                    continue;
                }

                if (collected >= maxKeys) break;

                CrawledObject obj = new CrawledObject();
                obj.Key = resource.Url;
                obj.ContentType = resource.ContentType;
                obj.ContentLength = resource.ContentLength;
                obj.MD5Hash = resource.MD5Hash;
                obj.SHA1Hash = resource.SHA1Hash;
                obj.SHA256Hash = resource.SHA256Hash;
                obj.ETag = resource.ETag;
                obj.LastModifiedUtc = resource.LastModified;

                results.Add(obj);
                collected++;
                current++;
            }

            return results;
        }

        #endregion

        #region Protected-Methods

        /// <summary>
        /// Web crawlers do not skip files by filename.
        /// </summary>
        /// <param name="filename">Filename.</param>
        /// <returns>Always false.</returns>
        protected override bool IsSkipFile(string filename)
        {
            return false;
        }

        #endregion

        #region Private-Methods

        private Settings BuildSettings()
        {
            Settings settings = new Settings();
            settings.Crawl.StartUrl = _WebSettings.StartUrl;
            settings.Crawl.UserAgent = _WebSettings.UserAgent;
            settings.Crawl.FollowLinks = _WebSettings.FollowLinks;
            settings.Crawl.FollowRedirects = _WebSettings.FollowRedirects;
            settings.Crawl.IncludeSitemap = _WebSettings.ExtractSitemapLinks;
            settings.Crawl.RestrictToChildUrls = _WebSettings.RestrictToChildUrls;
            settings.Crawl.RestrictToSameSubdomain = _WebSettings.RestrictToSubdomain;
            settings.Crawl.RestrictToSameRootDomain = _WebSettings.RestrictToRootDomain;
            settings.Crawl.IgnoreRobotsText = _WebSettings.IgnoreRobotsTxt;
            settings.Crawl.MaxCrawlDepth = _WebSettings.MaxDepth;
            settings.Crawl.MaxParallelTasks = _WebSettings.MaxParallelTasks;
            settings.Crawl.UseHeadlessBrowser = _WebSettings.UseHeadlessBrowser;
            settings.Crawl.ThrottleMs = _WebSettings.CrawlDelayMs;

            // Authentication
            switch (_WebSettings.AuthenticationType)
            {
                case Enums.WebAuthTypeEnum.Basic:
                    settings.Authentication.Username = _WebSettings.Username;
                    settings.Authentication.Password = _WebSettings.Password;
                    break;
                case Enums.WebAuthTypeEnum.ApiKey:
                    settings.Authentication.ApiKeyHeader = _WebSettings.ApiKeyHeader;
                    settings.Authentication.ApiKey = _WebSettings.ApiKeyValue;
                    break;
                case Enums.WebAuthTypeEnum.BearerToken:
                    settings.Authentication.BearerToken = _WebSettings.BearerToken;
                    break;
            }

            return settings;
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
