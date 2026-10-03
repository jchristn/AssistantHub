namespace Test.Automated
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services.Crawlers;
    using Test.Shared;

    /// <summary>
    /// S3, Azure Blob, Google Cloud Storage, local disk and Git crawl repository types: settings validation and JSON
    /// round-trips, local disk crawls with the allowlist, Git crawls against a fake GitHub API, and S3 crawls against
    /// an in-process S3 stub.
    /// </summary>
    public partial class ServiceSuite
    {
        private async Task RunRepositoryTypeTestsAsync()
        {
            await ExecuteTestAsync("Repository types: every type round-trips through JSON and is accepted by CrawlPlan and CrawlerFactory", async () =>
            {
                IReadOnlyList<string> previousRoots = LocalDiskCrawlPolicy.AllowedRoots;
                string root = Path.Combine(Path.GetTempPath(), "ah-types-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(root);
                LocalDiskCrawlPolicy.Configure(new[] { root });

                try
                {
                    List<CrawlRepositorySettings> all = new List<CrawlRepositorySettings>
                    {
                        new S3CrawlRepositorySettings { S3BucketName = "company-docs", S3Region = "eu-west-2", S3AccessKey = "AKIAEXAMPLE", S3SecretKey = "secret" },
                        new AzureBlobCrawlRepositorySettings { AzureAccountName = "contosodocs", AzureAccessKey = "a2V5", AzureContainer = "documents" },
                        new GoogleCloudCrawlRepositorySettings { GcpProjectId = "contoso-prod", GcpBucketName = "contoso-documents", GcpJsonCredentials = SampleGcpKey() },
                        new LocalDiskCrawlRepositorySettings { DiskPath = root, IncludeSubdirectories = false },
                        new GitCrawlRepositorySettings { GitRepositoryUrl = "https://github.com/acme/handbook", GitAccessToken = "ghp_example" }
                    };

                    foreach (CrawlRepositorySettings settings in all)
                    {
                        AssertHelper.HasCount(settings.Validate(), 0, settings.RepositoryType + " sample settings valid");

                        CrawlPlan plan = new CrawlPlan { Name = "types", RepositoryType = settings.RepositoryType, RepositorySettings = settings };
                        CrawlPlan copy = Serializer.DeserializeJson<CrawlPlan>(Serializer.SerializeJson(plan));
                        AssertHelper.AreEqual(settings.GetType(), copy.RepositorySettings.GetType(), settings.RepositoryType + " settings type after round-trip");
                        AssertHelper.AreEqual(settings.RepositoryType, copy.RepositoryType, settings.RepositoryType + " repository type after round-trip");
                        AssertHelper.HasCount(copy.ValidateRepositorySettings(), 0, settings.RepositoryType + " plan valid after round-trip");

                        // Without RepositoryType in the settings, the converter recognizes the type from its fields.
                        using (JsonDocument doc = JsonDocument.Parse(Serializer.SerializeJson(settings)))
                        {
                            Dictionary<string, JsonElement> fields = doc.RootElement.EnumerateObject()
                                .Where(p => p.Name != "RepositoryType")
                                .ToDictionary(p => p.Name, p => p.Value.Clone());
                            string planJson = "{\"Name\":\"types\",\"RepositorySettings\":" + JsonSerializer.Serialize(fields) + "}";
                            CrawlPlan detected = Serializer.DeserializeJson<CrawlPlan>(planJson);
                            AssertHelper.AreEqual(settings.GetType(), detected.RepositorySettings.GetType(), settings.RepositoryType + " detected without RepositoryType");
                        }

                        using (CrawlerBase crawler = CrawlerFactory.Create(settings.RepositoryType, CreateSilentLogging(), new MockDatabaseDriver(), plan, new CrawlOperation(), null, null, null, Path.Combine(Path.GetTempPath(), "ah-types-enum"), CancellationToken.None))
                        {
                            AssertHelper.IsNotNull(crawler, settings.RepositoryType + " crawler created");
                        }
                    }

                    CrawlPlan mismatched = new CrawlPlan { RepositoryType = RepositoryTypeEnum.S3, RepositorySettings = all[1] };
                    AssertHelper.IsTrue(mismatched.ValidateRepositorySettings().Any(e => e.Contains("S3CrawlRepositorySettings")), "settings of another type are rejected");
                }
                finally
                {
                    LocalDiskCrawlPolicy.Configure(previousRoots);
                    DeleteQuietly(root);
                }
            });

            await ExecuteTestAsync("Repository types: settings validation explains common mistakes", async () =>
            {
                AssertValidationMentions(new S3CrawlRepositorySettings { S3BucketName = "s3://company-docs" }, "bucket name only");
                AssertValidationMentions(new S3CrawlRepositorySettings { S3BucketName = "company-docs/policies" }, "Filter.ObjectPrefix");
                AssertValidationMentions(new S3CrawlRepositorySettings { S3BucketName = "docs", S3Endpoint = "minio:9000" }, "absolute http or https URL");
                AssertValidationMentions(new S3CrawlRepositorySettings { S3BucketName = "docs", S3Region = null }, "S3Region is required");
                AssertValidationMentions(new S3CrawlRepositorySettings { S3BucketName = "docs", S3AccessKey = "AKIA" }, "both S3AccessKey and S3SecretKey");
                AssertHelper.HasCount(new S3CrawlRepositorySettings { S3BucketName = "docs", S3Region = null, S3Endpoint = "http://127.0.0.1:9000/" }.Validate(), 0, "a custom endpoint needs no region, and a public bucket no keys");

                AssertValidationMentions(new AzureBlobCrawlRepositorySettings { AzureAccountName = "https://contosodocs.blob.core.windows.net", AzureAccessKey = "k", AzureContainer = "documents" }, "storage account name only");
                AssertValidationMentions(new AzureBlobCrawlRepositorySettings { AzureAccountName = "contosodocs", AzureAccessKey = "k", AzureContainer = "Documents" }, "lowercase");
                AssertValidationMentions(new AzureBlobCrawlRepositorySettings { AzureAccountName = "contosodocs", AzureAccessKey = "k", AzureContainer = "documents/policies" }, "Filter.ObjectPrefix");
                AssertValidationMentions(new AzureBlobCrawlRepositorySettings { AzureAccountName = "contosodocs", AzureContainer = "documents" }, "AzureAccessKey is required");
                AzureBlobCrawlRepositorySettings azure = new AzureBlobCrawlRepositorySettings { AzureAccountName = "contosodocs" };
                AssertHelper.AreEqual("https://contosodocs.blob.core.windows.net/", azure.GetEffectiveEndpoint(), "default Azure endpoint");
                azure.AzureEndpoint = "http://127.0.0.1:10000/devstoreaccount1";
                AssertHelper.AreEqual("http://127.0.0.1:10000/devstoreaccount1/", azure.GetEffectiveEndpoint(), "custom Azure endpoint gets a trailing slash");

                AssertValidationMentions(new GoogleCloudCrawlRepositorySettings { GcpProjectId = "p", GcpBucketName = "gs://docs", GcpJsonCredentials = SampleGcpKey() }, "bucket name only");
                AssertValidationMentions(new GoogleCloudCrawlRepositorySettings { GcpProjectId = "p", GcpBucketName = "docs", GcpJsonCredentials = "not json" }, "not valid JSON");
                AssertValidationMentions(new GoogleCloudCrawlRepositorySettings { GcpProjectId = "p", GcpBucketName = "docs", GcpJsonCredentials = "{\"type\":\"authorized_user\"}" }, "service account JSON key");

                AssertValidationMentions(new GitCrawlRepositorySettings { GitRepositoryUrl = "https://github.com/acme/handbook/tree/main/docs" }, "Filter.ObjectPrefix");
                AssertValidationMentions(new GitCrawlRepositorySettings { GitRepositoryUrl = "https://gitlab.com/acme/handbook" }, "GitHub repository URL");
                foreach (string url in new[] { "https://github.com/acme/handbook", "https://github.com/acme/handbook.git", "https://github.com/acme/handbook/", "git@github.com:acme/handbook.git" })
                {
                    GitCrawlRepositorySettings git = new GitCrawlRepositorySettings { GitRepositoryUrl = url };
                    AssertHelper.IsTrue(git.TryParseRepository(out string owner, out string repo), "parsed " + url);
                    AssertHelper.AreEqual("acme/handbook", owner + "/" + repo, "owner and repository from " + url);
                }

                AssertHelper.AreEqual("docs/getting started.md", GitRepositoryCrawler.GetKey("https://raw.githubusercontent.com/acme/handbook/main/docs/getting%20started.md"), "raw URL key is the decoded repository path");
                AssertHelper.AreEqual("README.md", GitRepositoryCrawler.GetKey("https://raw.githubusercontent.com/acme/handbook/refs/heads/main/README.md"), "refs/heads form");
                await Task.CompletedTask.ConfigureAwait(false);
            });

            await ExecuteTestAsync("Local disk: allowlist, crawl, subfolders, and no folder is created for a missing path", async () =>
            {
                IReadOnlyList<string> previousRoots = LocalDiskCrawlPolicy.AllowedRoots;
                string allowed = CreateSampleShare("ah-disk");
                string outside = CreateSampleShare("ah-disk-outside");

                try
                {
                    LocalDiskCrawlPolicy.Configure(Array.Empty<string>());
                    AssertValidationMentions(new LocalDiskCrawlRepositorySettings { DiskPath = allowed }, "disabled");

                    LocalDiskCrawlPolicy.Configure(new[] { allowed });
                    AssertValidationMentions(new LocalDiskCrawlRepositorySettings { DiskPath = outside }, "outside the folders");
                    AssertValidationMentions(new LocalDiskCrawlRepositorySettings { DiskPath = Path.Combine(allowed, "..", Path.GetFileName(outside)) }, "outside the folders");
                    AssertValidationMentions(new LocalDiskCrawlRepositorySettings { DiskPath = "relative/folder" }, "absolute path");
                    AssertValidationMentions(new LocalDiskCrawlRepositorySettings { DiskPath = allowed + "-sibling" }, "outside the folders");
                    AssertHelper.HasCount(new LocalDiskCrawlRepositorySettings { DiskPath = Path.Combine(allowed, "policies") }.Validate(), 0, "a folder inside an allowed root is valid");

                    (CrawlConnectivityResult connectivity, List<CrawledObject> objects) = await CrawlShareAsync(RepositoryTypeEnum.LocalDisk, new LocalDiskCrawlRepositorySettings { DiskPath = allowed }).ConfigureAwait(false);
                    AssertHelper.IsTrue(connectivity.Success, "connectivity verified: " + connectivity.Message);
                    AssertSampleShareCrawled(objects);
                    AssertHelper.IsTrue(objects.All(o => !Path.IsPathRooted(o.Key)), "keys are relative to the folder");

                    (_, List<CrawledObject> topLevel) = await CrawlShareAsync(RepositoryTypeEnum.LocalDisk, new LocalDiskCrawlRepositorySettings { DiskPath = allowed, IncludeSubdirectories = false }).ConfigureAwait(false);
                    AssertHelper.IsTrue(topLevel.Any(o => o.Key.EndsWith("travel.txt", StringComparison.Ordinal)), "top-level file crawled without subfolders");
                    AssertHelper.IsFalse(topLevel.Any(o => o.Key.Contains("warranty", StringComparison.Ordinal)), "nested file skipped without subfolders");

                    string missing = Path.Combine(allowed, "does-not-exist");
                    CrawlConnectivityResult missingResult = await GetConnectivityAsync(RepositoryTypeEnum.LocalDisk, new LocalDiskCrawlRepositorySettings { DiskPath = missing }).ConfigureAwait(false);
                    AssertHelper.IsFalse(missingResult.Success, "missing folder fails connectivity");
                    AssertHelper.StringContains(missingResult.Message, "does not exist", "missing folder is explained");
                    AssertHelper.IsFalse(Directory.Exists(missing), "the crawler did not create the missing folder");

                    // A plan saved while the folder was allowed is refused once the operator removes the root.
                    LocalDiskCrawlPolicy.Configure(new[] { outside });
                    CrawlConnectivityResult revoked = await GetConnectivityAsync(RepositoryTypeEnum.LocalDisk, new LocalDiskCrawlRepositorySettings { DiskPath = allowed }).ConfigureAwait(false);
                    AssertHelper.IsFalse(revoked.Success, "removed root is refused at crawl time");
                }
                finally
                {
                    LocalDiskCrawlPolicy.Configure(previousRoots);
                    DeleteQuietly(allowed);
                    DeleteQuietly(outside);
                }
            });

            await ExecuteTestAsync("Local disk: files and folders reached through symbolic links are skipped", async () =>
            {
                IReadOnlyList<string> previousRoots = LocalDiskCrawlPolicy.AllowedRoots;
                string allowed = CreateSampleShare("ah-disk-links");
                string outside = CreateSampleShare("ah-disk-secret");
                File.WriteAllText(Path.Combine(outside, "secret.txt"), "not for crawling\n");

                try
                {
                    try
                    {
                        Directory.CreateSymbolicLink(Path.Combine(allowed, "linked-folder"), outside);
                        File.CreateSymbolicLink(Path.Combine(allowed, "linked-secret.txt"), Path.Combine(outside, "secret.txt"));
                    }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is PlatformNotSupportedException)
                    {
                        // Windows without Developer Mode or elevation cannot create symbolic links, but any user can
                        // create a directory junction, which the crawler must skip the same way.
                        if (!OperatingSystem.IsWindows() || Directory.Exists(Path.Combine(allowed, "linked-folder")) || !CreateJunction(Path.Combine(allowed, "linked-folder"), outside))
                        {
                            Console.WriteLine("  symbolic links unavailable here (" + e.GetType().Name + "); skipping link checks");
                            return;
                        }
                    }

                    LocalDiskCrawlPolicy.Configure(new[] { allowed });
                    (CrawlConnectivityResult connectivity, List<CrawledObject> objects) = await CrawlShareAsync(RepositoryTypeEnum.LocalDisk, new LocalDiskCrawlRepositorySettings { DiskPath = allowed }).ConfigureAwait(false);
                    AssertHelper.IsTrue(connectivity.Success, "connectivity verified: " + connectivity.Message);
                    AssertSampleShareCrawled(objects);
                    List<string> keys = objects.Select(o => o.Key.Replace('\\', '/')).ToList();
                    AssertHelper.IsFalse(keys.Any(k => k.StartsWith("linked-", StringComparison.Ordinal)), "linked file and folder skipped: " + String.Join(", ", keys));

                    string linkedRoot = Path.Combine(allowed, "linked-folder");
                    CrawlConnectivityResult linkedResult = await GetConnectivityAsync(RepositoryTypeEnum.LocalDisk, new LocalDiskCrawlRepositorySettings { DiskPath = linkedRoot }).ConfigureAwait(false);
                    AssertHelper.IsFalse(linkedResult.Success, "a DiskPath that links outside the allowed roots is refused");
                }
                finally
                {
                    LocalDiskCrawlPolicy.Configure(previousRoots);

                    // Remove the links themselves first, so deleting the folder never reaches their targets.
                    try { Directory.Delete(Path.Combine(allowed, "linked-folder")); } catch { }
                    try { File.Delete(Path.Combine(allowed, "linked-secret.txt")); } catch { }
                    for (int attempt = 0; attempt < 5 && Directory.Exists(allowed); attempt++)
                    {
                        DeleteQuietly(allowed);
                        if (Directory.Exists(allowed)) await Task.Delay(200).ConfigureAwait(false);
                    }

                    DeleteQuietly(outside);
                }
            });

            await ExecuteTestAsync("Git crawler: repository paths, prefix filter before download, and GitHub errors against a fake GitHub API", async () =>
            {
                FakeGitHub github = new FakeGitHub();
                GitCrawlRepositorySettings settings = new GitCrawlRepositorySettings { GitRepositoryUrl = "https://github.com/acme/handbook.git", GitAccessToken = "ghp_test" };

                (CrawlConnectivityResult connectivity, List<CrawledObject> objects) = await CrawlGitAsync(settings, null, github).ConfigureAwait(false);
                AssertHelper.IsTrue(connectivity.Success, "connectivity verified: " + connectivity.Message);
                List<string> keys = objects.Select(o => o.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();
                AssertHelper.AreEqual("README.md|docs/faq.txt|docs/getting started.md", String.Join("|", keys), "keys are decoded repository paths");
                CrawledObject readme = objects.First(o => o.Key == "README.md");
                AssertHelper.AreEqual("text/markdown", readme.ContentType, "content type from the extension");
                AssertHelper.AreEqual((long)Encoding.UTF8.GetByteCount(FakeGitHub.Readme), readme.ContentLength, "size");
                AssertHelper.IsTrue(!String.IsNullOrEmpty(readme.SHA256Hash), "hash computed for change detection");
                AssertHelper.IsTrue(github.Authorizations.All(a => a == "token ghp_test"), "token sent on every request");

                github.Downloads.Clear();
                (_, List<CrawledObject> docs) = await CrawlGitAsync(settings, new CrawlFilterSettings { ObjectPrefix = "docs/", ObjectSuffix = ".md" }, github).ConfigureAwait(false);
                AssertHelper.AreEqual("docs/getting started.md", String.Join("|", docs.Select(o => o.Key)), "prefix and suffix select the folder and type");
                AssertHelper.AreEqual(1, github.Downloads.Count, "files outside the filter are not downloaded");

                CrawlConnectivityResult missing = await CrawlGitConnectivityAsync(new GitCrawlRepositorySettings { GitRepositoryUrl = "https://github.com/acme/missing" }, github).ConfigureAwait(false);
                AssertHelper.IsFalse(missing.Success, "missing repository fails");
                AssertHelper.StringContains(missing.Message, "private repository needs an access token", "missing repository guidance");

                CrawlConnectivityResult limited = await CrawlGitConnectivityAsync(new GitCrawlRepositorySettings { GitRepositoryUrl = "https://github.com/acme/limited" }, github).ConfigureAwait(false);
                AssertHelper.IsFalse(limited.Success, "rate-limited request fails");
                AssertHelper.StringContains(limited.Message, "Add an access token", "rate limit guidance");
                AssertHelper.StringContains(limited.Message, "The limit resets at 2026-01-01 00:00:00 UTC", "rate limit reset time from X-RateLimit-Reset");

                CrawlConnectivityResult revoked = await CrawlGitConnectivityAsync(new GitCrawlRepositorySettings { GitRepositoryUrl = "https://github.com/acme/revoked", GitAccessToken = "ghp_revoked" }, github).ConfigureAwait(false);
                AssertHelper.IsFalse(revoked.Success, "refused token fails");
                AssertHelper.StringContains(revoked.Message, "The access token was refused", "refused token guidance from the 401 status code");
            });

            await ExecuteTestAsync("S3 crawler: connectivity and enumeration against an in-process S3 endpoint", async () =>
            {
                using (S3StubServer s3 = new S3StubServer(GetFreePort(), "company-docs"))
                {
                    s3.Objects["travel.txt"] = Encoding.UTF8.GetBytes("Travel policy: the nightly hotel cap is 320 dollars.\n");
                    s3.Objects["policies/warranty.md"] = Encoding.UTF8.GetBytes("# Warranty\n\nThe TS-4 gateway has a 24 month warranty.\n");
                    s3.Start();

                    S3CrawlRepositorySettings settings = new S3CrawlRepositorySettings
                    {
                        S3Endpoint = s3.BaseUrl,
                        S3BucketName = "company-docs",
                        S3AccessKey = "AKIDEXAMPLE",
                        S3SecretKey = "secret"
                    };

                    (CrawlConnectivityResult connectivity, List<CrawledObject> objects) = await CrawlShareAsync(RepositoryTypeEnum.S3, settings).ConfigureAwait(false);
                    AssertHelper.IsTrue(connectivity.Success, "connectivity verified: " + connectivity.Message + " | requests: " + String.Join(", ", s3.Requests));
                    AssertHelper.StringContains(connectivity.Message, "Bucket 'company-docs' is accessible", "message names the bucket");
                    AssertSampleShareCrawled(objects);

                    // A key scoped to one bucket cannot list the account's buckets but can crawl the bucket.
                    s3.DenyListBuckets = true;
                    CrawlConnectivityResult scoped = await GetConnectivityAsync(RepositoryTypeEnum.S3, settings).ConfigureAwait(false);
                    AssertHelper.IsTrue(scoped.Success, "a bucket-scoped key passes connectivity: " + scoped.Message);

                    settings.S3BucketName = "no-such-bucket";
                    CrawlConnectivityResult missing = await GetConnectivityAsync(RepositoryTypeEnum.S3, settings).ConfigureAwait(false);
                    AssertHelper.IsFalse(missing.Success, "missing bucket fails");
                    AssertHelper.StringContains(missing.Message, "s3:ListBucket", "S3 guidance");
                }
            });
        }

        private static void AssertValidationMentions(CrawlRepositorySettings settings, string expected)
        {
            List<string> errors = settings.Validate();
            AssertHelper.IsTrue(errors.Any(e => e.Contains(expected, StringComparison.OrdinalIgnoreCase)),
                settings.RepositoryType + " validation mentions '" + expected + "': " + String.Join(" | ", errors));
        }

        private static bool CreateJunction(string link, string target)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + target + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(start))
                {
                    process.WaitForExit(10000);
                    return process.ExitCode == 0 && new DirectoryInfo(link).LinkTarget != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string SampleGcpKey()
        {
            return "{\"type\":\"service_account\",\"project_id\":\"contoso-prod\",\"private_key_id\":\"1\",\"private_key\":\"-----BEGIN PRIVATE KEY-----\\nMIIB\\n-----END PRIVATE KEY-----\\n\",\"client_email\":\"crawler@contoso-prod.iam.gserviceaccount.com\"}";
        }

        private static GitRepositoryCrawler CreateGitCrawler(GitCrawlRepositorySettings settings, CrawlFilterSettings filter, FakeGitHub github, CancellationToken token)
        {
            CrawlPlan plan = new CrawlPlan { Id = "cplan_git", TenantId = "tenant_git", RepositoryType = RepositoryTypeEnum.Git, RepositorySettings = settings, Filter = filter ?? new CrawlFilterSettings() };
            return new GitRepositoryCrawler(CreateSilentLogging(), new MockDatabaseDriver(), plan, new CrawlOperation(), null, null, null, Path.Combine(Path.GetTempPath(), "ah-git-enum"), token, () => github);
        }

        private static async Task<(CrawlConnectivityResult Connectivity, List<CrawledObject> Objects)> CrawlGitAsync(GitCrawlRepositorySettings settings, CrawlFilterSettings filter, FakeGitHub github)
        {
            using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
            using (GitRepositoryCrawler crawler = CreateGitCrawler(settings, filter, github, timeout.Token))
            {
                CrawlConnectivityResult connectivity = await crawler.GetConnectivityStatusAsync(timeout.Token).ConfigureAwait(false);
                List<CrawledObject> objects = new List<CrawledObject>();
                await foreach (CrawledObject obj in crawler.EnumerateAsync(timeout.Token).ConfigureAwait(false))
                    objects.Add(obj);
                return (connectivity, objects);
            }
        }

        private static async Task<CrawlConnectivityResult> CrawlGitConnectivityAsync(GitCrawlRepositorySettings settings, FakeGitHub github)
        {
            using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
            using (GitRepositoryCrawler crawler = CreateGitCrawler(settings, null, github, timeout.Token))
            {
                return await crawler.GetConnectivityStatusAsync(timeout.Token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Emulates the GitHub contents API and raw.githubusercontent.com for acme/handbook; acme/missing returns 404
        /// acme/limited 403 (rate limit, with X-RateLimit-Reset)
        /// and acme/revoked 401 (refused token). GitHubRepoCrawler disposes its handler, so this one ignores Dispose.
        /// </summary>
        private sealed class FakeGitHub : HttpMessageHandler
        {
            public const string Readme = "# Handbook\n\nThe on-call rotation changes every Monday.\n";

            public ConcurrentBag<string> Authorizations { get; } = new ConcurrentBag<string>();

            public List<string> Downloads { get; } = new List<string>();

            protected override void Dispose(bool disposing)
            {
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Authorizations.Add(request.Headers.Authorization?.ToString() ?? "");
                string host = request.RequestUri.Host;
                string path = request.RequestUri.AbsolutePath;

                if (host == "api.github.com")
                {
                    if (path.StartsWith("/repos/acme/missing/", StringComparison.Ordinal)) return Respond(request, HttpStatusCode.NotFound, "{\"message\":\"Not Found\"}", "application/json");
                    if (path.StartsWith("/repos/acme/limited/", StringComparison.Ordinal)) return RespondRateLimited(request);
                    if (path.StartsWith("/repos/acme/revoked/", StringComparison.Ordinal)) return Respond(request, HttpStatusCode.Unauthorized, "{\"message\":\"Bad credentials\"}", "application/json");
                    if (path == "/repos/acme/handbook/contents/" || path == "/repos/acme/handbook/contents")
                        return Respond(request, HttpStatusCode.OK,
                            "[" + File("README.md", "README.md") + "," + Folder("docs") + "]", "application/json");
                    if (path == "/repos/acme/handbook/contents/docs")
                        return Respond(request, HttpStatusCode.OK,
                            "[" + File("getting started.md", "docs/getting%20started.md") + "," + File("faq.txt", "docs/faq.txt") + "]", "application/json");
                    return Respond(request, HttpStatusCode.NotFound, "{\"message\":\"Not Found\"}", "application/json");
                }

                if (host == "raw.githubusercontent.com")
                {
                    lock (Downloads) Downloads.Add(path);
                    string body = path.EndsWith("README.md", StringComparison.Ordinal) ? Readme
                        : path.Contains("getting", StringComparison.Ordinal) ? "# Getting started\n\nClone the repository.\n"
                        : "Q: Where are the docs? A: Here.\n";
                    return Respond(request, HttpStatusCode.OK, body, "text/plain; charset=utf-8");
                }

                return Respond(request, HttpStatusCode.NotFound, "", "text/plain");
            }

            private static string File(string name, string encodedPath)
            {
                return "{\"name\":\"" + name + "\",\"path\":\"" + Uri.UnescapeDataString(encodedPath) + "\",\"type\":\"file\",\"download_url\":\"https://raw.githubusercontent.com/acme/handbook/main/" + encodedPath + "\"}";
            }

            private static string Folder(string path)
            {
                return "{\"name\":\"" + path + "\",\"path\":\"" + path + "\",\"type\":\"dir\",\"download_url\":null}";
            }

            private static async Task<HttpResponseMessage> RespondRateLimited(HttpRequestMessage request)
            {
                HttpResponseMessage response = await Respond(request, HttpStatusCode.Forbidden, "{\"message\":\"API rate limit exceeded\"}", "application/json").ConfigureAwait(false);
                response.Headers.Add("X-RateLimit-Remaining", "0");
                response.Headers.Add("X-RateLimit-Reset", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds().ToString());
                return response;
            }

            private static Task<HttpResponseMessage> Respond(HttpRequestMessage request, HttpStatusCode status, string body, string contentType)
            {
                HttpResponseMessage response = new HttpResponseMessage(status) { RequestMessage = request, Content = new StringContent(body, Encoding.UTF8) };
                response.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
                response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"" + Math.Abs(body.GetHashCode()).ToString() + "\"");
                return Task.FromResult(response);
            }
        }

        /// <summary>
        /// A minimal path-style S3 endpoint for one bucket: ListBuckets, HeadBucket, GetBucketLocation, ListObjects (v1
        /// and v2, with prefix and delimiter), HeadObject and GetObject. Signatures are not checked.
        /// </summary>
        private sealed class S3StubServer : IDisposable
        {
            private readonly HttpListener _Listener = new HttpListener();
            private readonly CancellationTokenSource _TokenSource = new CancellationTokenSource();
            private readonly string _Bucket;
            private Task _ListenerTask;

            public S3StubServer(int port, string bucket)
            {
                BaseUrl = "http://127.0.0.1:" + port.ToString() + "/";
                _Bucket = bucket;
                _Listener.Prefixes.Add(BaseUrl);
            }

            public string BaseUrl { get; }

            public ConcurrentDictionary<string, byte[]> Objects { get; } = new ConcurrentDictionary<string, byte[]>(StringComparer.Ordinal);

            public ConcurrentQueue<string> Requests { get; } = new ConcurrentQueue<string>();

            public bool DenyListBuckets { get; set; }

            public void Start()
            {
                _Listener.Start();
                _ListenerTask = Task.Run(() => ListenAsync(_TokenSource.Token));
            }

            public void Dispose()
            {
                _TokenSource.Cancel();
                try { _Listener.Stop(); _Listener.Close(); } catch { }
                try { _ListenerTask?.Wait(TimeSpan.FromSeconds(2)); } catch { }
                _TokenSource.Dispose();
            }

            private async Task ListenAsync(CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await _Listener.GetContextAsync().ConfigureAwait(false);
                    }
                    catch (Exception) when (cancellationToken.IsCancellationRequested || !_Listener.IsListening)
                    {
                        break;
                    }

                    _ = Task.Run(() => Handle(context));
                }
            }

            private void Handle(HttpListenerContext context)
            {
                try
                {
                    HttpListenerRequest request = context.Request;
                    string rawPath = request.Url.AbsolutePath.TrimStart('/');
                    int slash = rawPath.IndexOf('/');
                    string bucket = Uri.UnescapeDataString(slash < 0 ? rawPath : rawPath.Substring(0, slash));
                    string key = slash < 0 ? "" : Uri.UnescapeDataString(rawPath.Substring(slash + 1));
                    Requests.Enqueue(request.HttpMethod + " " + request.Url.PathAndQuery);

                    if (String.IsNullOrEmpty(bucket))
                    {
                        if (DenyListBuckets)
                        {
                            Send(context, 403, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Error><Code>AccessDenied</Code><Message>Access Denied</Message></Error>");
                            return;
                        }

                        Send(context, 200, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ListAllMyBucketsResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\"><Owner><ID>stub</ID><DisplayName>stub</DisplayName></Owner><Buckets><Bucket><Name>" + _Bucket + "</Name><CreationDate>2026-01-01T00:00:00.000Z</CreationDate></Bucket></Buckets></ListAllMyBucketsResult>");
                        return;
                    }

                    if (!String.Equals(bucket, _Bucket, StringComparison.Ordinal))
                    {
                        Send(context, 404, request.HttpMethod == "HEAD" ? null : "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Error><Code>NoSuchBucket</Code><Message>The specified bucket does not exist</Message><BucketName>" + SecurityElement.Escape(bucket) + "</BucketName></Error>");
                        return;
                    }

                    if (String.IsNullOrEmpty(key))
                    {
                        if (request.HttpMethod == "HEAD") { Send(context, 200, null); return; }
                        if (request.QueryString.AllKeys.Contains("location"))
                        {
                            Send(context, 200, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><LocationConstraint xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\">us-east-1</LocationConstraint>");
                            return;
                        }

                        Send(context, 200, BuildListing(request));
                        return;
                    }

                    if (!Objects.TryGetValue(key, out byte[] data))
                    {
                        Send(context, 404, request.HttpMethod == "HEAD" ? null : "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Error><Code>NoSuchKey</Code><Message>The specified key does not exist.</Message><Key>" + SecurityElement.Escape(key) + "</Key></Error>");
                        return;
                    }

                    context.Response.StatusCode = 200;
                    context.Response.ContentType = key.EndsWith(".md", StringComparison.Ordinal) ? "text/markdown" : "text/plain";
                    context.Response.Headers["ETag"] = ETag(data);
                    context.Response.Headers["Last-Modified"] = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToString("R");
                    context.Response.ContentLength64 = data.Length;
                    if (request.HttpMethod != "HEAD") context.Response.OutputStream.Write(data, 0, data.Length);
                    context.Response.Close();
                }
                catch
                {
                    try { context.Response.StatusCode = 500; context.Response.Close(); } catch { }
                }
            }

            private string BuildListing(HttpListenerRequest request)
            {
                string prefix = request.QueryString["prefix"] ?? "";
                string delimiter = request.QueryString["delimiter"];
                bool v2 = request.QueryString["list-type"] == "2";

                List<string> keys = Objects.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(k => k, StringComparer.Ordinal).ToList();
                SortedSet<string> commonPrefixes = new SortedSet<string>(StringComparer.Ordinal);
                StringBuilder contents = new StringBuilder();
                foreach (string key in keys)
                {
                    if (!String.IsNullOrEmpty(delimiter))
                    {
                        int index = key.IndexOf(delimiter, prefix.Length, StringComparison.Ordinal);
                        if (index >= 0)
                        {
                            commonPrefixes.Add(key.Substring(0, index + delimiter.Length));
                            continue;
                        }
                    }

                    byte[] data = Objects[key];
                    contents.Append("<Contents><Key>").Append(SecurityElement.Escape(key)).Append("</Key><LastModified>2026-01-01T00:00:00.000Z</LastModified><ETag>")
                        .Append(SecurityElement.Escape(ETag(data))).Append("</ETag><Size>").Append(data.Length).Append("</Size><StorageClass>STANDARD</StorageClass></Contents>");
                }

                StringBuilder xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><ListBucketResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\">");
                xml.Append("<Name>").Append(_Bucket).Append("</Name><Prefix>").Append(SecurityElement.Escape(prefix)).Append("</Prefix>");
                if (v2) xml.Append("<KeyCount>").Append(keys.Count).Append("</KeyCount>");
                else xml.Append("<Marker></Marker>");
                xml.Append("<MaxKeys>1000</MaxKeys>");
                if (!String.IsNullOrEmpty(delimiter)) xml.Append("<Delimiter>").Append(SecurityElement.Escape(delimiter)).Append("</Delimiter>");
                xml.Append("<IsTruncated>false</IsTruncated>").Append(contents);
                foreach (string common in commonPrefixes) xml.Append("<CommonPrefixes><Prefix>").Append(SecurityElement.Escape(common)).Append("</Prefix></CommonPrefixes>");
                xml.Append("</ListBucketResult>");
                return xml.ToString();
            }

            private static string ETag(byte[] data)
            {
                return "\"" + Convert.ToHexString(System.Security.Cryptography.MD5.HashData(data)).ToLowerInvariant() + "\"";
            }

            private static void Send(HttpListenerContext context, int status, string xml)
            {
                context.Response.StatusCode = status;
                if (xml != null)
                {
                    byte[] body = Encoding.UTF8.GetBytes(xml);
                    context.Response.ContentType = "application/xml";
                    context.Response.ContentLength64 = body.Length;
                    context.Response.OutputStream.Write(body, 0, body.Length);
                }

                context.Response.Close();
            }
        }
    }
}
