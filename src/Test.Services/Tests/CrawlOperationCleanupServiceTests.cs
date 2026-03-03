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

    public static class CrawlOperationCleanupServiceTests
    {
        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- CrawlOperationCleanupService Tests ---");

            await runner.RunTestAsync("CleanupService.Constructor_NullDatabase_Throws", async ct =>
            {
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                AssertHelper.ThrowsAsync<ArgumentNullException>(
                    () => Task.FromResult(new CrawlOperationCleanupService(null, logging, settings)),
                    "null database should throw");
            }, token);

            await runner.RunTestAsync("CleanupService.Constructor_NullLogging_Throws", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                AssistantHubSettings settings = new AssistantHubSettings();

                AssertHelper.ThrowsAsync<ArgumentNullException>(
                    () => Task.FromResult(new CrawlOperationCleanupService(db, null, settings)),
                    "null logging should throw");
            }, token);

            await runner.RunTestAsync("CleanupService.Constructor_NullSettings_Throws", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;

                AssertHelper.ThrowsAsync<ArgumentNullException>(
                    () => Task.FromResult(new CrawlOperationCleanupService(db, logging, null)),
                    "null settings should throw");
            }, token);

            await runner.RunTestAsync("CleanupService.Constructor_ValidParams_Succeeds", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                CrawlOperationCleanupService svc = new CrawlOperationCleanupService(db, logging, settings);
                AssertHelper.IsNotNull(svc, "service should be created");
            }, token);

            await runner.RunTestAsync("CleanupService.StartStop_EmptyDatabase", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                CrawlOperationCleanupService svc = new CrawlOperationCleanupService(db, logging, settings);

                using CancellationTokenSource cts = new CancellationTokenSource();
                await svc.StartAsync(cts.Token);

                // Give it a moment then stop
                await Task.Delay(100, ct);
                await svc.StopAsync();

                // Should complete without error on empty database
                AssertHelper.IsTrue(true, "start/stop completed without error");
            }, token);

            await runner.RunTestAsync("CleanupService.CleansExpiredOperations", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                // Create a tenant
                TenantMetadata tenant = await db.Tenant.CreateAsync(
                    new TenantMetadata { Name = "Cleanup Test Tenant" }, ct);

                // Create a crawl plan with 1-day retention
                CrawlPlan plan = await db.CrawlPlan.CreateAsync(
                    new CrawlPlan { TenantId = tenant.Id, Name = "Test Plan", RetentionDays = 1 }, ct);

                // Create an expired operation (created 5 days ago)
                CrawlOperation expiredOp = new CrawlOperation
                {
                    TenantId = tenant.Id,
                    CrawlPlanId = plan.Id,
                    State = CrawlOperationStateEnum.Success
                };
                expiredOp = await db.CrawlOperation.CreateAsync(expiredOp, ct);

                // Manually backdate CreatedUtc in the mock store
                var mockOpMethods = (MockDatabaseDriver.MockCrawlOperationMethods)db.CrawlOperation;
                if (mockOpMethods.Store.TryGetValue(expiredOp.Id, out CrawlOperation stored))
                {
                    stored.CreatedUtc = DateTime.UtcNow.AddDays(-5);
                }

                // Create a recent operation (should survive)
                CrawlOperation recentOp = await db.CrawlOperation.CreateAsync(
                    new CrawlOperation
                    {
                        TenantId = tenant.Id,
                        CrawlPlanId = plan.Id,
                        State = CrawlOperationStateEnum.Success
                    }, ct);

                // Start and immediately stop the service (it runs cleanup on startup)
                CrawlOperationCleanupService svc = new CrawlOperationCleanupService(db, logging, settings);
                using CancellationTokenSource cts = new CancellationTokenSource();
                await svc.StartAsync(cts.Token);
                await Task.Delay(200, ct);
                await svc.StopAsync();

                // The expired operation should have been cleaned up
                CrawlOperation readExpired = await db.CrawlOperation.ReadAsync(expiredOp.Id, ct);
                AssertHelper.IsNull(readExpired, "expired operation should be deleted");

                // The recent operation should still exist
                CrawlOperation readRecent = await db.CrawlOperation.ReadAsync(recentOp.Id, ct);
                AssertHelper.IsNotNull(readRecent, "recent operation should survive cleanup");
            }, token);

            await runner.RunTestAsync("CleanupService.StopAsync_Idempotent", async ct =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                LoggingModule logging = new LoggingModule();
                logging.Settings.EnableConsole = false;
                AssistantHubSettings settings = new AssistantHubSettings();

                CrawlOperationCleanupService svc = new CrawlOperationCleanupService(db, logging, settings);

                // StopAsync without StartAsync should not throw
                await svc.StopAsync();
                await svc.StopAsync();
                AssertHelper.IsTrue(true, "double stop should not throw");
            }, token);
        }
    }
}
