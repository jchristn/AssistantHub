namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services.Crawlers;
    using OpenCIFS.Server;
    using OpenNFS.Server;
    using OpenNFS.Server.FileHandles;
    using Test.Shared;

    /// <summary>
    /// CIFS and NFS crawls against in-process OpenCIFS and OpenNFS servers (Blobject 6), on non-standard ports.
    /// </summary>
    public partial class ServiceSuite
    {
        private const string _CifsUser = "crawler";
        private const string _CifsPassword = "Crawl-Test-123!";

        private async Task RunFileShareCrawlTestsAsync()
        {
            await ExecuteTestAsync("CIFS crawler: connectivity, enumeration and a rejected logon against an OpenCIFS server", async () =>
            {
                // SMB 3 signing and encryption use AES-CCM, which .NET does not provide on macOS.
                if (!System.Security.Cryptography.AesCcm.IsSupported)
                {
                    Console.WriteLine("  AES-CCM unavailable on this platform; skipping the OpenCIFS crawl");
                    return;
                }

                string root = CreateSampleShare("ah-cifs");
                int port = GetFreePort();
                OpenCifsServerApplication server = new OpenCifsServerBuilder()
                    .WithServerName("assistanthub-test")
                    .WithBindAddress("127.0.0.1")
                    .WithBindPort(port)
                    .AddAccount(new OpenCifsServerAccount { UserName = _CifsUser, UserDomain = "WORKGROUP", Password = _CifsPassword })
                    .AddSrvsvcShareEnumerationEndpoint()
                    .AddFileSystemShare("share", root)
                    .BuildApplication(e => { });
                await server.StartAsync(CancellationToken.None).ConfigureAwait(false);

                try
                {
                    CifsCrawlRepositorySettings settings = new CifsCrawlRepositorySettings
                    {
                        CifsHostname = "127.0.0.1",
                        CifsPort = port,
                        CifsUsername = _CifsUser,
                        CifsPassword = _CifsPassword,
                        CifsDomain = "WORKGROUP",
                        CifsShareName = "share"
                    };

                    (CrawlConnectivityResult connectivity, List<CrawledObject> objects) = await CrawlShareAsync(RepositoryTypeEnum.CIFS, settings).ConfigureAwait(false);
                    AssertHelper.IsTrue(connectivity.Success, "connectivity verified: " + connectivity.Message);
                    AssertHelper.StringContains(connectivity.Message, "port " + port, "diagnostics use the configured port");
                    AssertSampleShareCrawled(objects);

                    settings.CifsPassword = "wrong";
                    CrawlConnectivityResult rejected = await GetConnectivityAsync(RepositoryTypeEnum.CIFS, settings).ConfigureAwait(false);
                    AssertHelper.IsFalse(rejected.Success, "wrong password rejected");
                    AssertHelper.StringContains(rejected.Message, "SessionSetup failed", "the logon failure reason is reported");
                }
                finally
                {
                    try { await server.StopAsync(CancellationToken.None).ConfigureAwait(false); } catch { }
                    try { await server.DisposeAsync().ConfigureAwait(false); } catch { }
                    DeleteQuietly(root);
                }
            });

            await ExecuteTestAsync("NFS crawler: connectivity and enumeration against an OpenNFS server (NFSv3)", async () =>
            {
                string baseDirectory = Path.Combine(Path.GetTempPath(), "ah-nfs-" + Guid.NewGuid().ToString("N"));
                string root = CreateSampleShare(Path.Combine(baseDirectory, "export"));
                string state = Path.Combine(baseDirectory, "state");
                Directory.CreateDirectory(state);

                OpenNfsServerApplication server = new OpenNfsServerBuilder()
                    .WithServerName("assistanthub-test")
                    .UseLocalFileSystem()
                    .UseFileHandleProvider(new PersistentMappingHandleProvider(Path.Combine(state, "handles.json")))
                    .AddExport("/export", root)
                    .BuildApplication(new OpenNfsServerApplicationOptions
                    {
                        ListenerAddress = "127.0.0.1",
                        EnableNfsV3 = true,
                        EnableNfs40 = false,
                        EnableNfs41 = false,
                        EnableNfs42 = false,
                        NfsPort = 0,
                        MountPort = 0
                    });
                await server.StartAsync(CancellationToken.None).ConfigureAwait(false);

                try
                {
                    NfsCrawlRepositorySettings settings = new NfsCrawlRepositorySettings
                    {
                        NfsHostname = "127.0.0.1",
                        NfsPort = server.NfsPort,
                        NfsMountPort = server.MountPort,
                        NfsUserId = 0,
                        NfsGroupId = 0,
                        NfsShareName = "/export",
                        NfsVersion = NfsVersionEnum.V3
                    };

                    (CrawlConnectivityResult connectivity, List<CrawledObject> objects) = await CrawlShareAsync(RepositoryTypeEnum.NFS, settings).ConfigureAwait(false);
                    AssertHelper.IsTrue(connectivity.Success, "connectivity verified: " + connectivity.Message);
                    AssertHelper.StringContains(connectivity.Message, "port " + server.NfsPort, "diagnostics use the configured port");
                    AssertSampleShareCrawled(objects);
                }
                finally
                {
                    try { await server.StopAsync(CancellationToken.None).ConfigureAwait(false); } catch { }
                    try { await server.DisposeAsync().ConfigureAwait(false); } catch { }
                    DeleteQuietly(baseDirectory);
                }
            });
        }

        private static string CreateSampleShare(string pathOrPrefix)
        {
            string root = Path.IsPathRooted(pathOrPrefix)
                ? pathOrPrefix
                : Path.Combine(Path.GetTempPath(), pathOrPrefix + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "policies"));
            File.WriteAllText(Path.Combine(root, "travel.txt"), "Travel policy: the nightly hotel cap is 320 dollars.\n");
            File.WriteAllText(Path.Combine(root, "policies", "warranty.md"), "# Warranty\n\nThe TS-4 gateway has a 24 month warranty.\n");
            return root;
        }

        private static void AssertSampleShareCrawled(List<CrawledObject> objects)
        {
            List<string> keys = objects.Where(o => !o.IsFolder).Select(o => o.Key.Replace('\\', '/')).ToList();
            AssertHelper.IsTrue(keys.Any(k => k.EndsWith("travel.txt", StringComparison.Ordinal)), "top-level file crawled: " + String.Join(", ", keys));
            AssertHelper.IsTrue(keys.Any(k => k.EndsWith("policies/warranty.md", StringComparison.Ordinal)), "nested file crawled: " + String.Join(", ", keys));
            CrawledObject warranty = objects.First(o => o.Key.Replace('\\', '/').EndsWith("policies/warranty.md", StringComparison.Ordinal));
            AssertHelper.IsTrue(warranty.ContentLength > 0, "file size reported");
        }

        private static async Task<CrawlConnectivityResult> GetConnectivityAsync(RepositoryTypeEnum type, CrawlRepositorySettings settings)
        {
            CrawlPlan plan = new CrawlPlan { Id = "cplan_share", TenantId = "tenant_share", RepositoryType = type, RepositorySettings = settings };
            using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
            using (CrawlerBase crawler = CrawlerFactory.Create(type, CreateSilentLogging(), new MockDatabaseDriver(), plan, new CrawlOperation(), null, null, null, Path.Combine(Path.GetTempPath(), "ah-share-enum"), timeout.Token))
            {
                return await crawler.GetConnectivityStatusAsync(timeout.Token).ConfigureAwait(false);
            }
        }

        private static async Task<(CrawlConnectivityResult Connectivity, List<CrawledObject> Objects)> CrawlShareAsync(RepositoryTypeEnum type, CrawlRepositorySettings settings)
        {
            CrawlPlan plan = new CrawlPlan { Id = "cplan_share", TenantId = "tenant_share", RepositoryType = type, RepositorySettings = settings };
            AssertHelper.HasCount(plan.ValidateRepositorySettings(), 0, "plan settings valid");

            using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
            using (CrawlerBase crawler = CrawlerFactory.Create(type, CreateSilentLogging(), new MockDatabaseDriver(), plan, new CrawlOperation(), null, null, null, Path.Combine(Path.GetTempPath(), "ah-share-enum"), timeout.Token))
            {
                CrawlConnectivityResult connectivity = await crawler.GetConnectivityStatusAsync(timeout.Token).ConfigureAwait(false);
                List<CrawledObject> objects = new List<CrawledObject>();
                await foreach (CrawledObject obj in crawler.EnumerateAsync(timeout.Token).ConfigureAwait(false))
                    objects.Add(obj);
                return (connectivity, objects);
            }
        }

        private static int GetFreePort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static void DeleteQuietly(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
        }
    }
}
