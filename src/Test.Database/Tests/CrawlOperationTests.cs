namespace Test.Database.Tests
{
    using System;
    using Test.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;

    public static class CrawlOperationTests
    {
        public static async Task RunAllAsync(DatabaseDriverBase driver, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- CrawlOperation Tests ---");

            string tenantId = TenantTests.TestTenantId;
            string crawlPlanId = CrawlPlanTests.TestCrawlPlanId;
            string createdId = null;

            await runner.RunTestAsync("CrawlOperation.Create", async ct =>
            {
                CrawlOperation op = new CrawlOperation
                {
                    TenantId = tenantId,
                    CrawlPlanId = crawlPlanId,
                    State = CrawlOperationStateEnum.NotStarted,
                    StatusMessage = "Created for testing"
                };

                CrawlOperation created = await driver.CrawlOperation.CreateAsync(op, ct);
                AssertHelper.IsNotNull(created, "created operation");
                AssertHelper.IsNotNull(created.Id, "Id");
                AssertHelper.StartsWith(created.Id, "cop_", "Id prefix");
                AssertHelper.AreEqual(tenantId, created.TenantId, "TenantId");
                AssertHelper.AreEqual(crawlPlanId, created.CrawlPlanId, "CrawlPlanId");
                AssertHelper.AreEqual(CrawlOperationStateEnum.NotStarted, created.State, "State");
                AssertHelper.AreEqual("Created for testing", created.StatusMessage, "StatusMessage");
                AssertHelper.DateTimeRecent(created.CreatedUtc, "CreatedUtc");
                createdId = created.Id;
            }, token);

            await runner.RunTestAsync("CrawlOperation.Read", async ct =>
            {
                CrawlOperation read = await driver.CrawlOperation.ReadAsync(createdId, ct);
                AssertHelper.IsNotNull(read, "read operation");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual(tenantId, read.TenantId, "TenantId");
                AssertHelper.AreEqual(crawlPlanId, read.CrawlPlanId, "CrawlPlanId");
                AssertHelper.AreEqual(CrawlOperationStateEnum.NotStarted, read.State, "State");
            }, token);

            await runner.RunTestAsync("CrawlOperation.Read_NotFound", async ct =>
            {
                CrawlOperation read = await driver.CrawlOperation.ReadAsync("cop_nonexistent", ct);
                AssertHelper.IsNull(read, "non-existent operation");
            }, token);

            await runner.RunTestAsync("CrawlOperation.Exists", async ct =>
            {
                bool exists = await driver.CrawlOperation.ExistsAsync(createdId, ct);
                AssertHelper.AreEqual(true, exists, "should exist");

                bool notExists = await driver.CrawlOperation.ExistsAsync("cop_nonexistent", ct);
                AssertHelper.AreEqual(false, notExists, "should not exist");
            }, token);

            await runner.RunTestAsync("CrawlOperation.Update", async ct =>
            {
                CrawlOperation read = await driver.CrawlOperation.ReadAsync(createdId, ct);
                read.State = CrawlOperationStateEnum.Enumerating;
                read.StatusMessage = "Enumerating pages";
                read.StartUtc = DateTime.UtcNow;

                CrawlOperation updated = await driver.CrawlOperation.UpdateAsync(read, ct);
                AssertHelper.AreEqual(CrawlOperationStateEnum.Enumerating, updated.State, "updated State");
                AssertHelper.AreEqual("Enumerating pages", updated.StatusMessage, "updated StatusMessage");
                AssertHelper.IsNotNull(updated.StartUtc, "StartUtc should be set");
            }, token);

            await runner.RunTestAsync("CrawlOperation.Enumerate", async ct =>
            {
                EnumerationResult<CrawlOperation> result = await driver.CrawlOperation.EnumerateAsync(tenantId, new EnumerationQuery(), ct);
                AssertHelper.IsNotNull(result, "enumeration result");
                AssertHelper.IsNotNull(result.Objects, "Objects list");
                AssertHelper.IsTrue(result.Objects.Count >= 1, "should have at least 1 operation");
            }, token);

            await runner.RunTestAsync("CrawlOperation.EnumerateByCrawlPlan", async ct =>
            {
                EnumerationResult<CrawlOperation> result = await driver.CrawlOperation.EnumerateByCrawlPlanAsync(crawlPlanId, new EnumerationQuery(), ct);
                AssertHelper.IsNotNull(result, "result");
                AssertHelper.IsTrue(result.Objects.Count >= 1, "should have at least 1 operation for plan");

                // Non-existent plan should return empty
                EnumerationResult<CrawlOperation> empty = await driver.CrawlOperation.EnumerateByCrawlPlanAsync("cplan_nonexistent", new EnumerationQuery(), ct);
                AssertHelper.AreEqual(0, empty.Objects.Count, "no operations for non-existent plan");
            }, token);

            await runner.RunTestAsync("CrawlOperation.Delete", async ct =>
            {
                CrawlOperation opDel = new CrawlOperation
                {
                    TenantId = tenantId,
                    CrawlPlanId = crawlPlanId,
                    State = CrawlOperationStateEnum.NotStarted
                };
                opDel = await driver.CrawlOperation.CreateAsync(opDel, ct);

                await driver.CrawlOperation.DeleteAsync(opDel.Id, ct);

                CrawlOperation read = await driver.CrawlOperation.ReadAsync(opDel.Id, ct);
                AssertHelper.IsNull(read, "deleted operation should not be found");
            }, token);

            await runner.RunTestAsync("CrawlOperation.DeleteByCrawlPlan", async ct =>
            {
                // Create a new plan with operations
                CrawlPlan tempPlan = new CrawlPlan { TenantId = tenantId, Name = "Temp Plan for Delete" };
                tempPlan = await driver.CrawlPlan.CreateAsync(tempPlan, ct);

                CrawlOperation op1 = await driver.CrawlOperation.CreateAsync(
                    new CrawlOperation { TenantId = tenantId, CrawlPlanId = tempPlan.Id }, ct);
                CrawlOperation op2 = await driver.CrawlOperation.CreateAsync(
                    new CrawlOperation { TenantId = tenantId, CrawlPlanId = tempPlan.Id }, ct);

                await driver.CrawlOperation.DeleteByCrawlPlanAsync(tempPlan.Id, ct);

                CrawlOperation r1 = await driver.CrawlOperation.ReadAsync(op1.Id, ct);
                CrawlOperation r2 = await driver.CrawlOperation.ReadAsync(op2.Id, ct);
                AssertHelper.IsNull(r1, "op1 should be deleted");
                AssertHelper.IsNull(r2, "op2 should be deleted");

                // Cleanup temp plan
                await driver.CrawlPlan.DeleteAsync(tempPlan.Id, ct);
            }, token);

            await runner.RunTestAsync("CrawlOperation.StateTransitions", async ct =>
            {
                CrawlOperation op = await driver.CrawlOperation.CreateAsync(
                    new CrawlOperation { TenantId = tenantId, CrawlPlanId = crawlPlanId, State = CrawlOperationStateEnum.NotStarted }, ct);

                // Walk through state machine
                CrawlOperationStateEnum[] states = new[]
                {
                    CrawlOperationStateEnum.Starting,
                    CrawlOperationStateEnum.Enumerating,
                    CrawlOperationStateEnum.Retrieving,
                    CrawlOperationStateEnum.Success
                };

                foreach (var state in states)
                {
                    op.State = state;
                    op = await driver.CrawlOperation.UpdateAsync(op, ct);
                    AssertHelper.AreEqual(state, op.State, "State should be " + state);
                }

                // Cleanup
                await driver.CrawlOperation.DeleteAsync(op.Id, ct);
            }, token);
        }
    }
}
