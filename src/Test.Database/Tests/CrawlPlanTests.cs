namespace Test.Database.Tests
{
    using System;
    using Test.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;

    public static class CrawlPlanTests
    {
        public static string TestCrawlPlanId { get; private set; }

        public static async Task RunAllAsync(DatabaseDriverBase driver, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- CrawlPlan Tests ---");

            string tenantId = TenantTests.TestTenantId;
            string createdId = null;

            await runner.RunTestAsync("CrawlPlan.Create", async ct =>
            {
                CrawlPlan plan = new CrawlPlan
                {
                    TenantId = tenantId,
                    Name = "Test Crawl Plan",
                    RepositoryType = RepositoryTypeEnum.Web,
                    ProcessAdditions = true,
                    ProcessUpdates = true,
                    ProcessDeletions = false
                };

                CrawlPlan created = await driver.CrawlPlan.CreateAsync(plan, ct);
                AssertHelper.IsNotNull(created, "created plan");
                AssertHelper.IsNotNull(created.Id, "Id");
                AssertHelper.StartsWith(created.Id, "cplan_", "Id prefix");
                AssertHelper.AreEqual(tenantId, created.TenantId, "TenantId");
                AssertHelper.AreEqual("Test Crawl Plan", created.Name, "Name");
                AssertHelper.AreEqual(RepositoryTypeEnum.Web, created.RepositoryType, "RepositoryType");
                AssertHelper.AreEqual(true, created.ProcessAdditions, "ProcessAdditions");
                AssertHelper.AreEqual(true, created.ProcessUpdates, "ProcessUpdates");
                AssertHelper.AreEqual(false, created.ProcessDeletions, "ProcessDeletions");
                AssertHelper.AreEqual(CrawlPlanStateEnum.Stopped, created.State, "State");
                AssertHelper.DateTimeRecent(created.CreatedUtc, "CreatedUtc");
                createdId = created.Id;
                TestCrawlPlanId = createdId;
            }, token);

            await runner.RunTestAsync("CrawlPlan.Read", async ct =>
            {
                CrawlPlan read = await driver.CrawlPlan.ReadAsync(createdId, ct);
                AssertHelper.IsNotNull(read, "read plan");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual(tenantId, read.TenantId, "TenantId");
                AssertHelper.AreEqual("Test Crawl Plan", read.Name, "Name");
                AssertHelper.AreEqual(RepositoryTypeEnum.Web, read.RepositoryType, "RepositoryType");
            }, token);

            await runner.RunTestAsync("CrawlPlan.Read_NotFound", async ct =>
            {
                CrawlPlan read = await driver.CrawlPlan.ReadAsync("cplan_nonexistent", ct);
                AssertHelper.IsNull(read, "non-existent plan");
            }, token);

            await runner.RunTestAsync("CrawlPlan.Exists", async ct =>
            {
                bool exists = await driver.CrawlPlan.ExistsAsync(createdId, ct);
                AssertHelper.AreEqual(true, exists, "should exist");

                bool notExists = await driver.CrawlPlan.ExistsAsync("cplan_nonexistent", ct);
                AssertHelper.AreEqual(false, notExists, "should not exist");
            }, token);

            await runner.RunTestAsync("CrawlPlan.Update", async ct =>
            {
                CrawlPlan read = await driver.CrawlPlan.ReadAsync(createdId, ct);
                read.Name = "Updated Crawl Plan";
                read.ProcessDeletions = true;

                CrawlPlan updated = await driver.CrawlPlan.UpdateAsync(read, ct);
                AssertHelper.AreEqual("Updated Crawl Plan", updated.Name, "updated Name");
                AssertHelper.AreEqual(true, updated.ProcessDeletions, "updated ProcessDeletions");
            }, token);

            await runner.RunTestAsync("CrawlPlan.UpdateState", async ct =>
            {
                await driver.CrawlPlan.UpdateStateAsync(createdId, CrawlPlanStateEnum.Running, ct);
                CrawlPlan read = await driver.CrawlPlan.ReadAsync(createdId, ct);
                AssertHelper.AreEqual(CrawlPlanStateEnum.Running, read.State, "State after update");

                await driver.CrawlPlan.UpdateStateAsync(createdId, CrawlPlanStateEnum.Stopped, ct);
                read = await driver.CrawlPlan.ReadAsync(createdId, ct);
                AssertHelper.AreEqual(CrawlPlanStateEnum.Stopped, read.State, "State reset to Stopped");
            }, token);

            await runner.RunTestAsync("CrawlPlan.Enumerate", async ct =>
            {
                EnumerationResult<CrawlPlan> result = await driver.CrawlPlan.EnumerateAsync(tenantId, new EnumerationQuery(), ct);
                AssertHelper.IsNotNull(result, "enumeration result");
                AssertHelper.IsNotNull(result.Objects, "Objects list");
                AssertHelper.IsTrue(result.Objects.Count >= 1, "should have at least 1 plan");
            }, token);

            await runner.RunTestAsync("CrawlPlan.Enumerate_FilterByTenant", async ct =>
            {
                EnumerationResult<CrawlPlan> result = await driver.CrawlPlan.EnumerateAsync("ten_nonexistent", new EnumerationQuery(), ct);
                AssertHelper.IsNotNull(result, "result");
                AssertHelper.AreEqual(0, result.Objects.Count, "no plans for non-existent tenant");
            }, token);

            await runner.RunTestAsync("CrawlPlan.Enumerate_Pagination", async ct =>
            {
                // Create a second plan
                CrawlPlan plan2 = new CrawlPlan
                {
                    TenantId = tenantId,
                    Name = "Second Plan"
                };
                plan2 = await driver.CrawlPlan.CreateAsync(plan2, ct);

                EnumerationQuery query = new EnumerationQuery { MaxResults = 1 };
                EnumerationResult<CrawlPlan> page1 = await driver.CrawlPlan.EnumerateAsync(tenantId, query, ct);
                AssertHelper.AreEqual(1, page1.Objects.Count, "page 1 count");
                AssertHelper.IsNotNull(page1.ContinuationToken, "should have continuation token");

                query.ContinuationToken = page1.ContinuationToken;
                EnumerationResult<CrawlPlan> page2 = await driver.CrawlPlan.EnumerateAsync(tenantId, query, ct);
                AssertHelper.IsTrue(page2.Objects.Count >= 1, "page 2 should have results");

                // Cleanup
                await driver.CrawlPlan.DeleteAsync(plan2.Id, ct);
            }, token);

            await runner.RunTestAsync("CrawlPlan.Delete", async ct =>
            {
                CrawlPlan planDel = new CrawlPlan
                {
                    TenantId = tenantId,
                    Name = "To Delete"
                };
                planDel = await driver.CrawlPlan.CreateAsync(planDel, ct);

                await driver.CrawlPlan.DeleteAsync(planDel.Id, ct);

                CrawlPlan read = await driver.CrawlPlan.ReadAsync(planDel.Id, ct);
                AssertHelper.IsNull(read, "deleted plan should not be found");
            }, token);
        }
    }
}
