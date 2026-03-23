namespace Test.Sdk.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk;
    using AssistantHub.Sdk.Enums;
    using AssistantHub.Sdk.Models;
    using Test.Common;

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
                        RespectRobotsTxt = true,
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

            await runner.RunTestAsync("CrawlPlan: List crawl plans includes created one", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdPlanId, "createdPlanId from previous test");
                EnumerationResult<CrawlPlan> result = await client.ListCrawlPlansAsync(ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListCrawlPlans result");
                AssertHelper.IsNotNull(result.Objects, "ListCrawlPlans result.Objects");
                bool found = result.Objects.Any(p => p.Id == createdPlanId);
                AssertHelper.IsTrue(found, "Created crawl plan should appear in list");
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
                        RespectRobotsTxt = true,
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
            }, token).ConfigureAwait(false);
        }
    }
}
