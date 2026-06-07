#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using Blobject.Core;
    using SyslogLogging;

    /// <summary>
    /// Base class for Blobject-backed file-server repository crawlers.
    /// </summary>
    public abstract class FileServerRepositoryCrawlerBase : CrawlerBase
    {
        #region Private-Members

        private readonly Func<BlobClientBase> _BlobFactory;
        private BlobClientBase _Blob = null;
        private readonly bool _IncludeSubdirectories = true;
        private readonly bool _EnableNetworkDiagnostics = true;

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
        /// <param name="blob">Blobject client.</param>
        /// <param name="includeSubdirectories">Include files in subdirectories.</param>
        /// <param name="enableNetworkDiagnostics">Enable DNS and TCP port diagnostics.</param>
        protected FileServerRepositoryCrawlerBase(
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            IObjectStorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token,
            BlobClientBase blob,
            bool includeSubdirectories,
            bool enableNetworkDiagnostics = true)
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
                  CreateBlobFactory(blob),
                  includeSubdirectories,
                  enableNetworkDiagnostics)
        {
        }

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
        /// <param name="blobFactory">Factory for creating the Blobject client.</param>
        /// <param name="includeSubdirectories">Include files in subdirectories.</param>
        /// <param name="enableNetworkDiagnostics">Enable DNS and TCP port diagnostics.</param>
        protected FileServerRepositoryCrawlerBase(
            LoggingModule logging,
            DatabaseDriverBase database,
            CrawlPlan crawlPlan,
            CrawlOperation crawlOperation,
            IngestionService ingestion,
            IObjectStorageService storage,
            ProcessingLogService processingLog,
            string enumerationDirectory,
            CancellationToken token,
            Func<BlobClientBase> blobFactory,
            bool includeSubdirectories,
            bool enableNetworkDiagnostics = true)
            : base(logging, database, crawlPlan, crawlOperation, ingestion, storage, processingLog, enumerationDirectory, token)
        {
            _BlobFactory = blobFactory ?? throw new ArgumentNullException(nameof(blobFactory));
            _IncludeSubdirectories = includeSubdirectories;
            _EnableNetworkDiagnostics = enableNetworkDiagnostics;
        }

        #endregion

        #region Protected-Methods

        /// <summary>
        /// Resolve the effective file-server hostname used from inside containerized deployments.
        /// </summary>
        /// <param name="hostname">Configured hostname.</param>
        /// <returns>Effective hostname.</returns>
        protected static string ResolveEffectiveHostname(string hostname)
        {
            return ResolveEffectiveHostname(hostname, IsRunningInContainer(), CanResolveHostname("host.docker.internal"));
        }

        /// <summary>
        /// Resolve the effective file-server hostname using supplied container context.
        /// </summary>
        /// <param name="hostname">Configured hostname.</param>
        /// <param name="runningInContainer">True if AssistantHub is running inside a container.</param>
        /// <param name="hostDockerInternalAvailable">True if host.docker.internal is resolvable.</param>
        /// <returns>Effective hostname.</returns>
        protected static string ResolveEffectiveHostname(string hostname, bool runningInContainer, bool hostDockerInternalAvailable)
        {
            if (!IsLoopbackHostname(hostname)) return hostname;
            if (!runningInContainer) return hostname;
            if (!hostDockerInternalAvailable) return hostname;
            return "host.docker.internal";
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override async IAsyncEnumerable<CrawledObject> EnumerateAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token = default)
        {
            EnumerationFilter filter = BuildEnumerationFilter();

            await foreach (BlobMetadata blob in GetBlobClient().EnumerateAsync(filter, token).ConfigureAwait(false))
            {
                if (token.IsCancellationRequested) yield break;
                if (blob == null) continue;
                if (!ShouldIncludeObject(blob.Key)) continue;

                yield return FromBlobMetadata(blob);
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
            FileServerDiagnosticInfo info = GetDiagnosticInfo();
            CrawlConnectivityResult settingsResult = ValidateDiagnosticInfo(info);
            if (settingsResult != null) return settingsResult;

            if (_EnableNetworkDiagnostics)
            {
                CrawlConnectivityResult networkResult = await ValidateNetworkAsync(info, token).ConfigureAwait(false);
                if (networkResult != null) return networkResult;
            }

            BlobClientBase blob = null;
            try
            {
                blob = GetBlobClient();
            }
            catch (Exception e)
            {
                return CreateResult(false, BuildRepositoryAccessFailureMessage(info, e));
            }

            bool serverConnectivity = false;
            Exception serverConnectivityException = null;

            try
            {
                serverConnectivity = await blob.ValidateConnectivity(token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                serverConnectivityException = e;
            }

            try
            {
                BlobMetadata metadata = await blob.GetMetadataAsync(String.Empty, token).ConfigureAwait(false);
                if (metadata != null) return CreateResult(true, BuildRepositoryAccessSuccessMessage(info));
            }
            catch (Exception e)
            {
                return CreateResult(false, BuildRepositoryAccessFailureMessage(info, e, serverConnectivity, serverConnectivityException));
            }

            return CreateResult(false, BuildRepositoryAccessFailureMessage(info, null, serverConnectivity, serverConnectivityException));
        }

        /// <inheritdoc />
        public override async Task<List<CrawledObject>> EnumerateContentsAsync(int maxKeys = 100, int skip = 0, CancellationToken token = default)
        {
            List<CrawledObject> results = new List<CrawledObject>();
            int current = 0;

            await foreach (CrawledObject obj in EnumerateAsync(token))
            {
                if (token.IsCancellationRequested) break;
                if (current < skip)
                {
                    current++;
                    continue;
                }

                if (results.Count >= maxKeys) break;
                results.Add(obj);
                current++;
            }

            return results;
        }

        #endregion

        #region Protected-Methods

        /// <inheritdoc />
        protected override async Task<byte[]> RetrieveDataAsync(CrawledObject obj, CancellationToken token = default)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return await GetBlobClient().GetAsync(obj.Key, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_Blob is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Private-Methods

        private FileServerDiagnosticInfo GetDiagnosticInfo()
        {
            if (_CrawlPlan.RepositoryType == RepositoryTypeEnum.CIFS || _CrawlPlan.RepositorySettings is CifsCrawlRepositorySettings)
            {
                CifsCrawlRepositorySettings settings = _CrawlPlan.RepositorySettings as CifsCrawlRepositorySettings;
                return new FileServerDiagnosticInfo
                {
                    RepositoryLabel = "CIFS",
                    ConfiguredHostname = settings?.CifsHostname,
                    Hostname = ResolveEffectiveHostname(settings?.CifsHostname),
                    Port = 445,
                    ShareName = settings?.CifsShareName,
                    Principal = settings?.CifsUsername,
                    PrincipalLabel = "user"
                };
            }

            if (_CrawlPlan.RepositoryType == RepositoryTypeEnum.NFS || _CrawlPlan.RepositorySettings is NfsCrawlRepositorySettings)
            {
                NfsCrawlRepositorySettings settings = _CrawlPlan.RepositorySettings as NfsCrawlRepositorySettings;
                return new FileServerDiagnosticInfo
                {
                    RepositoryLabel = "NFS",
                    ConfiguredHostname = settings?.NfsHostname,
                    Hostname = ResolveEffectiveHostname(settings?.NfsHostname),
                    Port = 2049,
                    ShareName = settings?.NfsShareName,
                    Principal = settings != null ? "UID " + settings.NfsUserId + "/GID " + settings.NfsGroupId + ", " + settings.NfsVersion : null,
                    PrincipalLabel = "identity"
                };
            }

            return new FileServerDiagnosticInfo
            {
                RepositoryLabel = "file-server",
                ConfiguredHostname = null,
                Hostname = null,
                Port = 0,
                ShareName = null,
                Principal = null,
                PrincipalLabel = "identity"
            };
        }

        private CrawlConnectivityResult ValidateDiagnosticInfo(FileServerDiagnosticInfo info)
        {
            if (info == null) return CreateResult(false, "Unable to determine file-server repository settings.");
            if (String.IsNullOrWhiteSpace(info.Hostname)) return CreateResult(false, info.RepositoryLabel + " hostname is missing.");
            if (String.IsNullOrWhiteSpace(info.ShareName)) return CreateResult(false, info.RepositoryLabel + " share/export name is missing.");
            if (info.Port <= 0) return CreateResult(false, info.RepositoryLabel + " connectivity port is not configured.");
            return null;
        }

        private async Task<CrawlConnectivityResult> ValidateNetworkAsync(FileServerDiagnosticInfo info, CancellationToken token)
        {
            IPAddress[] addresses = null;

            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(5));

                try
                {
                    addresses = await Dns.GetHostAddressesAsync(info.Hostname).WaitAsync(timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    return CreateResult(false, "Timed out while resolving " + info.RepositoryLabel + " hostname '" + info.Hostname + "'. Verify DNS or host-name resolution from the AssistantHub server.");
                }
                catch (SocketException e)
                {
                    return CreateResult(false, "Could not resolve " + info.RepositoryLabel + " hostname '" + info.Hostname + "'. Verify the hostname or DNS settings. Detail: " + DescribeException(e));
                }
                catch (Exception e)
                {
                    return CreateResult(false, "Could not resolve " + info.RepositoryLabel + " hostname '" + info.Hostname + "'. Verify the hostname or DNS settings. Detail: " + DescribeException(e));
                }
            }

            if (addresses == null || addresses.Length < 1)
                return CreateResult(false, "Could not resolve " + info.RepositoryLabel + " hostname '" + info.Hostname + "'. DNS returned no addresses.");

            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(5));

                try
                {
                    using (TcpClient client = new TcpClient())
                    {
                        await client.ConnectAsync(info.Hostname, info.Port).WaitAsync(timeout.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    return CreateResult(false, "Resolved " + info.RepositoryLabel + " hostname '" + info.Hostname + "', but timed out connecting to port " + info.Port + ". Verify firewall rules, routing, and that the file-server service is listening.");
                }
                catch (SocketException e)
                {
                    return CreateResult(false, "Resolved " + info.RepositoryLabel + " hostname '" + info.Hostname + "', but could not connect to port " + info.Port + ". Verify firewall rules, routing, and that the file-server service is listening. Detail: " + DescribeException(e));
                }
                catch (Exception e)
                {
                    return CreateResult(false, "Resolved " + info.RepositoryLabel + " hostname '" + info.Hostname + "', but could not connect to port " + info.Port + ". Verify firewall rules, routing, and that the file-server service is listening. Detail: " + DescribeException(e));
                }
            }

            return null;
        }

        private string BuildRepositoryAccessSuccessMessage(FileServerDiagnosticInfo info)
        {
            string target = info.RepositoryLabel + " repository connectivity verified. " + BuildHostnameDescription(info) + " resolved, port " + info.Port + " is reachable, and share/export '" + info.ShareName + "' is accessible";
            if (!String.IsNullOrWhiteSpace(info.Principal))
                target += " with " + info.PrincipalLabel + " '" + info.Principal + "'";

            return target + ".";
        }

        private string BuildRepositoryAccessFailureMessage(FileServerDiagnosticInfo info, Exception exception, bool serverConnectivity = false, Exception serverConnectivityException = null)
        {
            string message = "Resolved " + info.RepositoryLabel + " " + BuildHostnameDescription(info) + " and reached port " + info.Port + ", but could not access share/export '" + info.ShareName + "'";

            if (!String.IsNullOrWhiteSpace(info.Principal))
                message += " with " + info.PrincipalLabel + " '" + info.Principal + "'";

            if (String.Equals(info.RepositoryLabel, "CIFS", StringComparison.OrdinalIgnoreCase))
                message += ". Verify the share name, username, password, and share permissions.";
            else if (String.Equals(info.RepositoryLabel, "NFS", StringComparison.OrdinalIgnoreCase))
                message += ". Verify the export path, UID/GID permissions, NFS version, and export ACLs.";
            else
                message += ". Verify repository settings and permissions.";

            string detail = DescribeException(exception);
            if (!String.IsNullOrWhiteSpace(detail)) message += " Detail: " + detail;

            if (!serverConnectivity)
            {
                string serverDetail = DescribeException(serverConnectivityException);
                if (!String.IsNullOrWhiteSpace(serverDetail))
                    message += " Server-level connectivity check also failed: " + serverDetail;
                else
                    message += " Server-level connectivity check also failed.";
            }

            return message;
        }

        private static string BuildHostnameDescription(FileServerDiagnosticInfo info)
        {
            if (info == null) return "hostname";

            if (!String.IsNullOrWhiteSpace(info.ConfiguredHostname)
                && !String.Equals(info.ConfiguredHostname, info.Hostname, StringComparison.OrdinalIgnoreCase))
            {
                return "hostname '" + info.ConfiguredHostname + "' as '" + info.Hostname + "'";
            }

            return "hostname '" + info.Hostname + "'";
        }

        private static string DescribeException(Exception exception)
        {
            if (exception == null) return null;

            AggregateException aggregate = exception as AggregateException;
            if (aggregate != null && aggregate.InnerException != null) return DescribeException(aggregate.InnerException);

            SocketException socket = exception as SocketException;
            string message = socket != null ? socket.SocketErrorCode + " - " + socket.Message : exception.Message;

            if (String.IsNullOrWhiteSpace(message)) message = exception.GetType().Name;
            message = message.Replace("\r", " ").Replace("\n", " ").Trim();
            return message;
        }

        private static bool IsLoopbackHostname(string hostname)
        {
            if (String.IsNullOrWhiteSpace(hostname)) return false;

            return String.Equals(hostname, "localhost", StringComparison.OrdinalIgnoreCase)
                   || String.Equals(hostname, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                   || String.Equals(hostname, "::1", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRunningInContainer()
        {
            string dotnetContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            if (String.Equals(dotnetContainer, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (String.Equals(dotnetContainer, "1", StringComparison.OrdinalIgnoreCase)) return true;

            return File.Exists("/.dockerenv");
        }

        private static bool CanResolveHostname(string hostname)
        {
            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(hostname);
                return addresses != null && addresses.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static CrawlConnectivityResult CreateResult(bool success, string message)
        {
            return new CrawlConnectivityResult
            {
                Success = success,
                Message = message
            };
        }

        private EnumerationFilter BuildEnumerationFilter()
        {
            CrawlFilterSettings filter = _CrawlPlan.Filter;

            return new EnumerationFilter
            {
                MinimumSize = filter != null ? filter.MinimumSize : 0,
                MaximumSize = filter != null && filter.MaximumSize != null ? filter.MaximumSize.Value : Int64.MaxValue,
                Prefix = filter != null && !String.IsNullOrEmpty(filter.ObjectPrefix) ? filter.ObjectPrefix : String.Empty,
                Suffix = filter != null && !String.IsNullOrEmpty(filter.ObjectSuffix) ? filter.ObjectSuffix : String.Empty
            };
        }

        private bool ShouldIncludeObject(string key)
        {
            if (_IncludeSubdirectories) return true;
            if (String.IsNullOrEmpty(key)) return true;

            string normalized = key.Trim('/', '\\');
            return !normalized.Contains("/") && !normalized.Contains("\\");
        }

        private BlobClientBase GetBlobClient()
        {
            if (_Blob == null)
            {
                _Blob = _BlobFactory();
                if (_Blob == null) throw new InvalidOperationException("The file-server crawler Blobject client factory returned null.");
            }

            return _Blob;
        }

        private static Func<BlobClientBase> CreateBlobFactory(BlobClientBase blob)
        {
            if (blob == null) throw new ArgumentNullException(nameof(blob));
            return () => blob;
        }

        private static CrawledObject FromBlobMetadata(BlobMetadata blob)
        {
            DateTime? lastModified = blob.LastUpdateUtc;
            if (lastModified == null) lastModified = blob.CreatedUtc;
            if (lastModified == null) lastModified = blob.LastAccessUtc;
            if (lastModified != null) lastModified = lastModified.Value.ToUniversalTime();

            CrawledObject obj = new CrawledObject();
            obj.Key = blob.Key;
            obj.IsFolder = blob.IsFolder;
            obj.ContentType = blob.ContentType;
            obj.ContentLength = blob.ContentLength;
            obj.ETag = blob.ETag;
            obj.LastModifiedUtc = lastModified;
            obj.Data = null;
            return obj;
        }

        private class FileServerDiagnosticInfo
        {
            public string RepositoryLabel { get; set; }
            public string ConfiguredHostname { get; set; }
            public string Hostname { get; set; }
            public int Port { get; set; }
            public string ShareName { get; set; }
            public string Principal { get; set; }
            public string PrincipalLabel { get; set; }
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
