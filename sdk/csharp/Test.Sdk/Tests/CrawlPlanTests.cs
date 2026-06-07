namespace Test.Sdk.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk;
    using AssistantHub.Sdk.Enums;
    using AssistantHub.Sdk.Models;
    using Test.Shared;

    /// <summary>
    /// Tests for crawl plan CRUD lifecycle.
    /// </summary>
    public static class CrawlPlanTests
    {
        /// <summary>
        /// Runs all crawl plan tests.
        /// </summary>
        /// <param name="runner">Test runner instance.</param>
        /// <param name="client">AssistantHub client.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task RunAsync(TestRunner runner, AssistantHubClient client, CancellationToken token)
        {
            string createdPlanId = null;
            string createdCifsPlanId = null;
            string createdNfsPlanId = null;
            string uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);

            await runner.RunTestAsync("CrawlPlan: Create crawl plan", async (CancellationToken ct) =>
            {
                CrawlPlan plan = new CrawlPlan
                {
                    Name = "test-crawlplan-" + uniqueSuffix,
                    RepositoryType = RepositoryTypeEnum.Web,
                    RepositorySettings = new WebCrawlRepositorySettings
                    {
                        AuthType = WebAuthTypeEnum.None,
                        StartUrl = "https://example.com",
                        MaxDepth = 1,
                        MaxParallelTasks = 1,
                        CrawlDelayMs = 1000,
                        FollowLinks = false,
                        FollowRedirects = true,
                        ExtractSitemapLinks = true,
                        IgnoreRobotsTxt = false,
                        RestrictToChildUrls = true
                    },
                    Schedule = new CrawlScheduleSettings
                    {
                        IntervalType = ScheduleIntervalEnum.OneTime,
                        IntervalValue = 1
                    },
                    ProcessAdditions = true,
                    ProcessUpdates = true,
                    ProcessDeletions = false,
                    MaxDrainTasks = 1,
                    RetentionDays = 7
                };

                CrawlPlan created = await client.CreateCrawlPlanAsync(plan, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(created, "CreateCrawlPlan result");
                AssertHelper.IsNotNull(created.Id, "Created crawl plan ID");
                AssertHelper.StartsWith(created.Id, "cplan_", "Created crawl plan ID prefix");
                AssertHelper.AreEqual("test-crawlplan-" + uniqueSuffix, created.Name, "Created crawl plan name");
                createdPlanId = created.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("CrawlPlan: Create CIFS crawl plan", async (CancellationToken ct) =>
            {
                CrawlPlan plan = new CrawlPlan
                {
                    Name = "test-cifs-crawlplan-" + uniqueSuffix,
                    RepositoryType = RepositoryTypeEnum.CIFS,
                    RepositorySettings = new CifsCrawlRepositorySettings
                    {
                        CifsHostname = "fileserver.example.com",
                        CifsUsername = "crawler",
                        CifsPassword = "secret",
                        CifsShareName = "content",
                        IncludeSubdirectories = true
                    },
                    Schedule = new CrawlScheduleSettings
                    {
                        IntervalType = ScheduleIntervalEnum.OneTime,
                        IntervalValue = 1
                    },
                    ProcessAdditions = true,
                    ProcessUpdates = true,
                    ProcessDeletions = false,
                    MaxDrainTasks = 1,
                    RetentionDays = 7
                };

                CrawlPlan created = await client.CreateCrawlPlanAsync(plan, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(created, "Create CIFS crawl plan result");
                AssertHelper.IsNotNull(created.Id, "Created CIFS crawl plan ID");
                AssertHelper.AreEqual(RepositoryTypeEnum.CIFS, created.RepositoryType, "Created CIFS repository type");
                AssertHelper.IsTrue(created.RepositorySettings is CifsCrawlRepositorySettings, "Created CIFS repository settings type");
                createdCifsPlanId = created.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("CrawlPlan: Create NFS crawl plan", async (CancellationToken ct) =>
            {
                CrawlPlan plan = new CrawlPlan
                {
                    Name = "test-nfs-crawlplan-" + uniqueSuffix,
                    RepositoryType = RepositoryTypeEnum.NFS,
                    RepositorySettings = new NfsCrawlRepositorySettings
                    {
                        NfsHostname = "nfs.example.com",
                        NfsUserId = 1000,
                        NfsGroupId = 1000,
                        NfsShareName = "/exports/content",
                        NfsVersion = NfsVersionEnum.V3,
                        IncludeSubdirectories = true
                    },
                    Schedule = new CrawlScheduleSettings
                    {
                        IntervalType = ScheduleIntervalEnum.OneTime,
                        IntervalValue = 1
                    },
                    ProcessAdditions = true,
                    ProcessUpdates = true,
                    ProcessDeletions = false,
                    MaxDrainTasks = 1,
                    RetentionDays = 7
                };

                CrawlPlan created = await client.CreateCrawlPlanAsync(plan, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(created, "Create NFS crawl plan result");
                AssertHelper.IsNotNull(created.Id, "Created NFS crawl plan ID");
                AssertHelper.AreEqual(RepositoryTypeEnum.NFS, created.RepositoryType, "Created NFS repository type");
                AssertHelper.IsTrue(created.RepositorySettings is NfsCrawlRepositorySettings, "Created NFS repository settings type");
                createdNfsPlanId = created.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("CrawlPlan: List crawl plans includes created one", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdPlanId, "createdPlanId from previous test");
                AssertHelper.IsNotNull(createdCifsPlanId, "createdCifsPlanId from previous test");
                AssertHelper.IsNotNull(createdNfsPlanId, "createdNfsPlanId from previous test");
                EnumerationResult<CrawlPlan> result = await client.ListCrawlPlansAsync(ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListCrawlPlans result");
                AssertHelper.IsNotNull(result.Objects, "ListCrawlPlans result.Objects");
                AssertHelper.IsTrue(result.Objects.Any(p => p.Id == createdPlanId), "Created web crawl plan should appear in list");
                AssertHelper.IsTrue(result.Objects.Any(p => p.Id == createdCifsPlanId), "Created CIFS crawl plan should appear in list");
                AssertHelper.IsTrue(result.Objects.Any(p => p.Id == createdNfsPlanId), "Created NFS crawl plan should appear in list");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("CrawlPlan: Get crawl plan by ID", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdPlanId, "createdPlanId from previous test");
                CrawlPlan plan = await client.GetCrawlPlanAsync(createdPlanId, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(plan, "GetCrawlPlan result");
                AssertHelper.AreEqual(createdPlanId, plan.Id, "Crawl plan ID");
                AssertHelper.AreEqual("test-crawlplan-" + uniqueSuffix, plan.Name, "Crawl plan name");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("CrawlPlan: Update crawl plan", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdPlanId, "createdPlanId from previous test");
                CrawlPlan plan = new CrawlPlan
                {
                    Name = "test-crawlplan-updated-" + uniqueSuffix,
                    RepositoryType = RepositoryTypeEnum.Web,
                    RepositorySettings = new WebCrawlRepositorySettings
                    {
                        AuthType = WebAuthTypeEnum.None,
                        StartUrl = "https://example.com/updated",
                        MaxDepth = 2,
                        MaxParallelTasks = 1,
                        CrawlDelayMs = 500,
                        FollowLinks = true,
                        FollowRedirects = true,
                        ExtractSitemapLinks = true,
                        IgnoreRobotsTxt = false,
                        RestrictToChildUrls = true
                    },
                    Schedule = new CrawlScheduleSettings
                    {
                        IntervalType = ScheduleIntervalEnum.OneTime,
                        IntervalValue = 1
                    },
                    ProcessAdditions = true,
                    ProcessUpdates = true,
                    ProcessDeletions = true,
                    MaxDrainTasks = 2,
                    RetentionDays = 14
                };

                CrawlPlan updated = await client.UpdateCrawlPlanAsync(createdPlanId, plan, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(updated, "UpdateCrawlPlan result");
                AssertHelper.AreEqual("test-crawlplan-updated-" + uniqueSuffix, updated.Name, "Updated crawl plan name");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("CrawlPlan: Delete crawl plan", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdPlanId, "createdPlanId from previous test");
                await client.DeleteCrawlPlanAsync(createdPlanId, ct).ConfigureAwait(false);
                if (!String.IsNullOrEmpty(createdCifsPlanId))
                {
                    await client.DeleteCrawlPlanAsync(createdCifsPlanId, ct).ConfigureAwait(false);
                }
                if (!String.IsNullOrEmpty(createdNfsPlanId))
                {
                    await client.DeleteCrawlPlanAsync(createdNfsPlanId, ct).ConfigureAwait(false);
                }
            }, token).ConfigureAwait(false);
        }
    }
}
