namespace Test.Services.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Services;
    using SyslogLogging;
    using Test.Common;

    public static class CrawlSchedulerServiceTests
    {
        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- CrawlSchedulerService Tests ---");

            await runner.RunTestAsync("Scheduler.Constructor_NullDatabase_Throws", async ct =>
            {
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                AssertHelper.ThrowsAsync<ArgumentNullException>(
                    () => Task.FromResult(new CrawlSchedulerService(null, logging, settings, null, null, null)),
                    "null database should throw");
            }, token);

            await runner.RunTestAsync("Scheduler.Constructor_NullLogging_Throws", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                AssistantHubSettings settings = new AssistantHubSettings();

                AssertHelper.ThrowsAsync<ArgumentNullException>(
                    () => Task.FromResult(new CrawlSchedulerService(db, null, settings, null, null, null)),
                    "null logging should throw");
            }, token);

            await runner.RunTestAsync("Scheduler.Constructor_NullSettings_Throws", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;

                AssertHelper.ThrowsAsync<ArgumentNullException>(
                    () => Task.FromResult(new CrawlSchedulerService(db, logging, null, null, null, null)),
                    "null settings should throw");
            }, token);

            await runner.RunTestAsync("Scheduler.Constructor_ValidParams_Succeeds", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                CrawlSchedulerService svc = new CrawlSchedulerService(db, logging, settings, null, null, null);
                AssertHelper.IsNotNull(svc, "scheduler should be created");
            }, token);

            await runner.RunTestAsync("Scheduler.IsRunning_NoPlan_ReturnsFalse", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                CrawlSchedulerService svc = new CrawlSchedulerService(db, logging, settings, null, null, null);

                bool running = svc.IsRunning("cplan_nonexistent");
                AssertHelper.AreEqual(false, running, "non-existent plan should not be running");
            }, token);

            await runner.RunTestAsync("Scheduler.IsRunning_NullId_ReturnsFalse", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                CrawlSchedulerService svc = new CrawlSchedulerService(db, logging, settings, null, null, null);

                bool running = svc.IsRunning(null);
                AssertHelper.AreEqual(false, running, "null id should return false");

                bool emptyRunning = svc.IsRunning("");
                AssertHelper.AreEqual(false, emptyRunning, "empty id should return false");
            }, token);

            await runner.RunTestAsync("Scheduler.StartCrawlAsync_NullId_Throws", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                CrawlSchedulerService svc = new CrawlSchedulerService(db, logging, settings, null, null, null);

                AssertHelper.ThrowsAsync<ArgumentNullException>(
                    () => svc.StartCrawlAsync(null),
                    "null crawl plan id should throw");
            }, token);

            await runner.RunTestAsync("Scheduler.StartCrawlAsync_NonExistentPlan_ReturnsNull", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                CrawlSchedulerService svc = new CrawlSchedulerService(db, logging, settings, null, null, null);

                CrawlOperation result = await svc.StartCrawlAsync("cplan_nonexistent");
                AssertHelper.IsNull(result, "non-existent plan should return null");
            }, token);

            await runner.RunTestAsync("Scheduler.StopCrawlAsync_NullId_Throws", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                CrawlSchedulerService svc = new CrawlSchedulerService(db, logging, settings, null, null, null);

                AssertHelper.ThrowsAsync<ArgumentNullException>(
                    () => svc.StopCrawlAsync(null),
                    "null crawl plan id should throw");
            }, token);

            await runner.RunTestAsync("Scheduler.StartStop_EmptyDatabase", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                CrawlSchedulerService svc = new CrawlSchedulerService(db, logging, settings, null, null, null);

                using CancellationTokenSource cts = new CancellationTokenSource();
                await svc.StartAsync(cts.Token);
                await Task.Delay(100, ct);
                await svc.StopAsync();

                AssertHelper.IsTrue(true, "start/stop completed without error on empty database");
            }, token);

            await runner.RunTestAsync("Scheduler.StartupRecovery_ResetsRunningPlans", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                // Create a tenant and a plan that's stuck in Running state
                TenantMetadata tenant = await db.Tenant.CreateAsync(
                    new TenantMetadata { Name = "Recovery Tenant" }, ct);

                CrawlPlan plan = await db.CrawlPlan.CreateAsync(
                    new CrawlPlan
                    {
                        TenantId = tenant.Id,
                        Name = "Stuck Plan",
                        State = CrawlPlanStateEnum.Running
                    }, ct);

                // Manually set state to Running (CreateAsync may default to Stopped)
                plan.State = CrawlPlanStateEnum.Running;
                await db.CrawlPlan.UpdateAsync(plan, ct);

                // Create an in-progress operation
                CrawlOperation op = await db.CrawlOperation.CreateAsync(
                    new CrawlOperation
                    {
                        TenantId = tenant.Id,
                        CrawlPlanId = plan.Id,
                        State = CrawlOperationStateEnum.Enumerating
                    }, ct);

                // Start the scheduler — startup recovery should reset the stuck plan
                CrawlSchedulerService svc = new CrawlSchedulerService(db, logging, settings, null, null, null);
                using CancellationTokenSource cts = new CancellationTokenSource();
                await svc.StartAsync(cts.Token);
                await Task.Delay(100, ct);
                await svc.StopAsync();

                // Verify plan was reset to Stopped
                CrawlPlan readPlan = await db.CrawlPlan.ReadAsync(plan.Id, ct);
                AssertHelper.AreEqual(CrawlPlanStateEnum.Stopped, readPlan.State, "plan should be reset to Stopped");

                // Verify operation was marked as Failed
                CrawlOperation readOp = await db.CrawlOperation.ReadAsync(op.Id, ct);
                AssertHelper.AreEqual(CrawlOperationStateEnum.Failed, readOp.State, "operation should be marked Failed");
                AssertHelper.AreEqual("Recovered during startup", readOp.StatusMessage, "status message should indicate recovery");
            }, token);

            await runner.RunTestAsync("Scheduler.StopAsync_Idempotent", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                CrawlSchedulerService svc = new CrawlSchedulerService(db, logging, settings, null, null, null);

                // StopAsync without StartAsync should not throw
                await svc.StopAsync();
                await svc.StopAsync();
                AssertHelper.IsTrue(true, "double stop should not throw");
            }, token);
        }
    }
}
