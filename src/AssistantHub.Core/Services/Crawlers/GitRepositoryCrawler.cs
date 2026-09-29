#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using GitHubCrawler;
    using SyslogLogging;

    /// <summary>
    /// Git repository crawler for github.com, using GitHubCrawler (the GitHub REST contents API). Reads the default
    /// branch. Object keys are repository-relative paths such as docs/setup.md, so Filter.ObjectPrefix selects a folder.
    /// </summary>
    public class GitRepositoryCrawler : CrawlerBase
    {
        #region Private-Members

        private readonly string _Header = "[GitRepositoryCrawler] ";
        private readonly GitCrawlRepositorySettings _Settings;
        private readonly Func<HttpMessageHandler> _HandlerFactory;
        private readonly ConcurrentDictionary<string, string> _DownloadUrls = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
        public GitRepositoryCrawler(
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            IObjectStorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token)
            : this(logging, database, crawlPlan, crawlOperation, ingestion, storage, processingLog, enumerationDirectory, token, null)
        {
        }

        /// <summary>
        /// Instantiate with a custom HTTP handler, for example a proxy configuration or a fake GitHub API in tests.
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
        /// <param name="handlerFactory">Creates a handler for each GitHub client; null uses the default handler.</param>
        public GitRepositoryCrawler(
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            IObjectStorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token,
            Func<HttpMessageHandler> handlerFactory)
            : base(logging, database, crawlPlan, crawlOperation, ingestion, storage, processingLog, enumerationDirectory, token)
        {
            _Settings = crawlPlan.RepositorySettings as GitCrawlRepositorySettings;
            if (_Settings == null) throw new ArgumentException("CrawlPlan must have GitCrawlRepositorySettings for a Git crawler.");
            _HandlerFactory = handlerFactory;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override async IAsyncEnumerable<CrawledObject> EnumerateAsync([EnumeratorCancellation] CancellationToken token = default)
        {
            string url = GetRepositoryUrl();
            _Logging.Info(_Header + "enumerating " + url);

            using (GitHubRepoCrawler github = CreateClient())
            {
                await foreach (string downloadUrl in github.GetRepositoryContentsAsync(url, token).ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested) yield break;

                    string key = GetKey(downloadUrl);
                    if (String.IsNullOrEmpty(key) || !MatchesPathFilter(key)) continue;

                    // The contents API does not return sizes or hashes for listings, so each file is downloaded here
                    // to detect changes; the data is kept for ingestion, as the web crawler does.
                    GitHubFileResponse file;
                    try
                    {
                        file = await github.GetFileContentsAsync(downloadUrl, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        _Logging.Warn(_Header + "skipping " + key + ": " + e.Message);
                        continue;
                    }

                    if ((int)file.StatusCode < 200 || (int)file.StatusCode >= 300)
                    {
                        _Logging.Warn(_Header + "skipping " + key + " (HTTP " + (int)file.StatusCode + ")");
                        continue;
                    }

                    _DownloadUrls[key] = downloadUrl;
                    yield return ToCrawledObject(key, file, true);
                }
            }
        }

        /// <inheritdoc />
        public override async Task<bool> ValidateConnectivityAsync(CancellationToken token = default)
        {
            CrawlConnectivityResult result = await GetConnectivityStatusAsync(token).ConfigureAwait(false);
            return result.Success;
        }

        /// <inheritdoc />
        public override async Task<CrawlConnectivityResult> GetConnectivityStatusAsync(CancellationToken token = default)
        {
            if (!_Settings.TryParseRepository(out string owner, out string repository))
                return Result(false, "GitRepositoryUrl must be a GitHub repository URL such as https://github.com/owner/repo.");

            string name = owner + "/" + repository;
            try
            {
                using (GitHubRepoCrawler github = CreateClient())
                {
                    await foreach (string downloadUrl in github.GetRepositoryContentsAsync(GetRepositoryUrl(), token).ConfigureAwait(false))
                    {
                        return Result(true, "Git repository connectivity verified. GitHub repository '" + name + "' is readable"
                            + (String.IsNullOrWhiteSpace(_Settings.GitAccessToken) ? " without an access token (GitHub allows 60 unauthenticated API requests an hour; add a token for larger repositories)." : " with the access token."));
                    }

                    return Result(true, "Git repository connectivity verified, but GitHub repository '" + name + "' has no files on its default branch.");
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                return Result(false, BuildFailureMessage(name, e));
            }
        }

        /// <inheritdoc />
        public override async Task<List<CrawledObject>> EnumerateContentsAsync(int maxKeys = 100, int skip = 0, CancellationToken token = default)
        {
            // Lists paths only; nothing is downloaded, so sizes are not known.
            List<CrawledObject> results = new List<CrawledObject>();
            int current = 0;

            using (GitHubRepoCrawler github = CreateClient())
            {
                await foreach (string downloadUrl in github.GetRepositoryContentsAsync(GetRepositoryUrl(), token).ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested) break;
                    string key = GetKey(downloadUrl);
                    if (String.IsNullOrEmpty(key)) continue;

                    if (current++ < skip) continue;
                    if (results.Count >= maxKeys) break;
                    results.Add(new CrawledObject { Key = key, ContentType = GuessContentType(key) });
                }
            }

            return results;
        }

        /// <summary>
        /// Repository-relative path for a raw download URL (https://raw.githubusercontent.com/owner/repo/ref/path).
        /// </summary>
        /// <param name="downloadUrl">Download URL.</param>
        /// <returns>Path, or null when the URL is not a raw GitHub URL.</returns>
        public static string GetKey(string downloadUrl)
        {
            if (String.IsNullOrWhiteSpace(downloadUrl)) return null;
            if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri uri)) return null;

            string[] segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // owner / repo / ref / path..., where ref may be spelled refs/heads/<branch>.
            int skip = 3;
            if (segments.Length > 5 && segments[2] == "refs" && (segments[3] == "heads" || segments[3] == "tags")) skip = 5;
            if (segments.Length <= skip) return null;

            return String.Join("/", segments.Skip(skip).Select(Uri.UnescapeDataString));
        }

        #endregion

        #region Protected-Methods

        /// <inheritdoc />
        protected override async Task<byte[]> RetrieveDataAsync(CrawledObject obj, CancellationToken token = default)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (obj.Data != null) return obj.Data;

            if (!_DownloadUrls.TryGetValue(obj.Key, out string downloadUrl))
                throw new InvalidOperationException("No download URL is known for '" + obj.Key + "'.");

            using (GitHubRepoCrawler github = CreateClient())
            {
                GitHubFileResponse file = await github.GetFileContentsAsync(downloadUrl, token).ConfigureAwait(false);
                if ((int)file.StatusCode < 200 || (int)file.StatusCode >= 300)
                    throw new InvalidOperationException("GitHub returned HTTP " + (int)file.StatusCode + " for '" + obj.Key + "'.");
                return file.Content ?? Array.Empty<byte>();
            }
        }

        #endregion

        #region Private-Methods

        private GitHubRepoCrawler CreateClient()
        {
            string token = String.IsNullOrWhiteSpace(_Settings.GitAccessToken) ? null : _Settings.GitAccessToken.Trim();
            HttpMessageHandler handler = _HandlerFactory?.Invoke();
            return handler != null ? new GitHubRepoCrawler(handler, token) : new GitHubRepoCrawler(token);
        }

        private string GetRepositoryUrl()
        {
            if (!_Settings.TryParseRepository(out string owner, out string repository))
                throw new InvalidOperationException("GitRepositoryUrl must be a GitHub repository URL such as https://github.com/owner/repo.");
            return "https://github.com/" + owner + "/" + repository;
        }

        private bool MatchesPathFilter(string key)
        {
            // Checked before downloading, to avoid fetching files the crawl would discard. The base class applies the
            // full filter (content types and sizes) afterwards.
            CrawlFilterSettings filter = _CrawlPlan.Filter;
            if (filter == null) return true;
            if (!String.IsNullOrEmpty(filter.ObjectPrefix) && !key.StartsWith(filter.ObjectPrefix.TrimStart('/'), StringComparison.OrdinalIgnoreCase)) return false;
            if (!String.IsNullOrEmpty(filter.ObjectSuffix) && !key.EndsWith(filter.ObjectSuffix, StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        private static CrawledObject ToCrawledObject(string key, GitHubFileResponse file, bool includeData)
        {
            byte[] data = file.Content ?? Array.Empty<byte>();
            CrawledObject obj = new CrawledObject();
            obj.Key = key;
            obj.ContentLength = data.LongLength;
            obj.Data = includeData ? data : null;
            obj.SHA256Hash = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
            obj.ContentType = PickContentType(file.ContentType, key);
            if (file.Headers != null && file.Headers.TryGetValue("ETag", out IEnumerable<string> etags))
                obj.ETag = etags?.FirstOrDefault();
            obj.IsFolder = false;
            return obj;
        }

        private static string PickContentType(string reported, string key)
        {
            // raw.githubusercontent.com serves nearly everything as text/plain or application/octet-stream, so the
            // file extension is the better guide.
            string guessed = GuessContentType(key);
            if (guessed != null) return guessed;
            if (String.IsNullOrWhiteSpace(reported)) return "application/octet-stream";
            int semicolon = reported.IndexOf(';');
            return (semicolon >= 0 ? reported.Substring(0, semicolon) : reported).Trim();
        }

        private static string GuessContentType(string key)
        {
            string extension = System.IO.Path.GetExtension(key ?? "").ToLowerInvariant();
            switch (extension)
            {
                case ".md":
                case ".markdown": return "text/markdown";
                case ".txt":
                case ".rst": return "text/plain";
                case ".htm":
                case ".html": return "text/html";
                case ".json": return "application/json";
                case ".xml": return "application/xml";
                case ".csv": return "text/csv";
                case ".yml":
                case ".yaml": return "application/yaml";
                case ".pdf": return "application/pdf";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".pptx": return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                case ".png": return "image/png";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                default: return null;
            }
        }

        private string BuildFailureMessage(string name, Exception e)
        {
            string message = "Could not read GitHub repository '" + name + "': " + e.Message;
            string text = e.Message ?? "";
            if (text.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                message += String.IsNullOrWhiteSpace(_Settings.GitAccessToken)
                    ? ". Check the owner and repository name. A private repository needs an access token with read access to its contents."
                    : ". Check the owner and repository name, and that the access token can read this repository (for a fine-grained token, Contents: Read-only on this repository).";
            }
            else if (text.IndexOf("rate limit", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                message += String.IsNullOrWhiteSpace(_Settings.GitAccessToken)
                    ? " Add an access token: GitHub allows 60 unauthenticated API requests an hour from this server's address."
                    : " The token's hourly limit (5,000 requests) is used up, or the token was refused; wait an hour or use another token.";
            }
            else if (text.IndexOf("unauthorized", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("401", StringComparison.Ordinal) >= 0)
            {
                message += ". The access token was refused; it may be expired or revoked.";
            }
            else
            {
                message += ". Check that the AssistantHub server can reach api.github.com and raw.githubusercontent.com.";
            }

            _Logging.Warn(_Header + message);
            return message;
        }

        private static CrawlConnectivityResult Result(bool success, string message)
        {
            return new CrawlConnectivityResult { Success = success, Message = message };
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
