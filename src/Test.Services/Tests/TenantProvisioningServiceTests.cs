namespace Test.Services.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    using Test.Common;

    public static class TenantProvisioningServiceTests
    {
        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("TenantProvisioningServiceTests");

            MockDatabaseDriver db = new MockDatabaseDriver();
            LoggingModule logging = new LoggingModule();
            logging.Settings.EnableConsole = false;

            // Settings without S3 or RecallDB (will skip those provisioning steps gracefully)
            AssistantHubSettings settings = new AssistantHubSettings();

            await runner.RunTestAsync("TenantProvisioning: constructor throws on null database", async ct =>
            {
                AssertHelper.ThrowsAsync<ArgumentNullException>(async () =>
                {
                    new TenantProvisioningService(null, logging, settings);
                }, "should throw on null database");
            }, token);

            await runner.RunTestAsync("TenantProvisioning: constructor throws on null logging", async ct =>
            {
                AssertHelper.ThrowsAsync<ArgumentNullException>(async () =>
                {
                    new TenantProvisioningService(db, null, settings);
                }, "should throw on null logging");
            }, token);

            await runner.RunTestAsync("TenantProvisioning: constructor throws on null settings", async ct =>
            {
                AssertHelper.ThrowsAsync<ArgumentNullException>(async () =>
                {
                    new TenantProvisioningService(db, logging, null);
                }, "should throw on null settings");
            }, token);

            await runner.RunTestAsync("TenantProvisioning: ProvisionAsync throws on null tenant", async ct =>
            {
                TenantProvisioningService svc = new TenantProvisioningService(db, logging, settings);
                AssertHelper.ThrowsAsync<ArgumentNullException>(async () =>
                {
                    await svc.ProvisionAsync(null);
                }, "should throw on null tenant");
            }, token);

            await runner.RunTestAsync("TenantProvisioning: ProvisionAsync creates admin user and credential", async ct =>
            {
                MockDatabaseDriver localDb = new MockDatabaseDriver();
                TenantProvisioningService svc = new TenantProvisioningService(localDb, logging, settings);

                TenantMetadata tenant = new TenantMetadata { Name = "Acme Corp", Active = true };
                tenant = await localDb.Tenant.CreateAsync(tenant, ct);

                TenantProvisioningResult result = await svc.ProvisionAsync(tenant);

                AssertHelper.IsNotNull(result, "result should not be null");
                AssertHelper.AreEqual(tenant.Id, result.TenantId, "TenantId");
                AssertHelper.AreEqual("Acme Corp", result.TenantName, "TenantName");
                AssertHelper.IsNotNull(result.AdminUserId, "AdminUserId");
                AssertHelper.IsNotNull(result.AdminEmail, "AdminEmail");
                AssertHelper.StringContains(result.AdminEmail, "acmecorp", "AdminEmail should contain sanitized tenant name");
                AssertHelper.IsNotNull(result.AdminPassword, "AdminPassword");
                AssertHelper.IsNotNull(result.BearerToken, "BearerToken");
                AssertHelper.IsNotNull(result.User, "User");
                AssertHelper.IsNotNull(result.Credential, "Credential");
            }, token);

            await runner.RunTestAsync("TenantProvisioning: ProvisionAsync admin user has correct properties", async ct =>
            {
                MockDatabaseDriver localDb = new MockDatabaseDriver();
                TenantProvisioningService svc = new TenantProvisioningService(localDb, logging, settings);

                TenantMetadata tenant = new TenantMetadata { Name = "Test Org", Active = true };
                tenant = await localDb.Tenant.CreateAsync(tenant, ct);

                TenantProvisioningResult result = await svc.ProvisionAsync(tenant);

                AssertHelper.AreEqual(true, result.User.IsAdmin, "IsAdmin");
                AssertHelper.AreEqual(true, result.User.IsTenantAdmin, "IsTenantAdmin");
                AssertHelper.AreEqual(true, result.User.Active, "Active");
                AssertHelper.AreEqual(true, result.User.IsProtected, "IsProtected");
                AssertHelper.AreEqual(tenant.Id, result.User.TenantId, "User.TenantId");
                AssertHelper.AreEqual("Admin", result.User.FirstName, "FirstName");
                AssertHelper.AreEqual("User", result.User.LastName, "LastName");
            }, token);

            await runner.RunTestAsync("TenantProvisioning: ProvisionAsync credential is active and protected", async ct =>
            {
                MockDatabaseDriver localDb = new MockDatabaseDriver();
                TenantProvisioningService svc = new TenantProvisioningService(localDb, logging, settings);

                TenantMetadata tenant = new TenantMetadata { Name = "Cred Test", Active = true };
                tenant = await localDb.Tenant.CreateAsync(tenant, ct);

                TenantProvisioningResult result = await svc.ProvisionAsync(tenant);

                AssertHelper.AreEqual(true, result.Credential.Active, "Credential.Active");
                AssertHelper.AreEqual(true, result.Credential.IsProtected, "Credential.IsProtected");
                AssertHelper.AreEqual(tenant.Id, result.Credential.TenantId, "Credential.TenantId");
                AssertHelper.AreEqual(result.User.Id, result.Credential.UserId, "Credential.UserId");
            }, token);

            await runner.RunTestAsync("TenantProvisioning: ProvisionAsync creates ingestion rule", async ct =>
            {
                MockDatabaseDriver localDb = new MockDatabaseDriver();
                TenantProvisioningService svc = new TenantProvisioningService(localDb, logging, settings);

                TenantMetadata tenant = new TenantMetadata { Name = "Rule Test", Active = true };
                tenant = await localDb.Tenant.CreateAsync(tenant, ct);

                await svc.ProvisionAsync(tenant);

                // Verify ingestion rule was created
                var enumResult = await localDb.IngestionRule.EnumerateAsync(tenant.Id, new EnumerationQuery(), ct);
                AssertHelper.IsNotNull(enumResult, "enumResult should not be null");
                AssertHelper.IsTrue(enumResult.Objects.Count > 0, "should have at least one rule");
                AssertHelper.AreEqual("Default", enumResult.Objects[0].Name, "rule Name");
                AssertHelper.AreEqual(tenant.Id, enumResult.Objects[0].TenantId, "rule TenantId");
                AssertHelper.StringContains(enumResult.Objects[0].Bucket, tenant.Id, "bucket should contain tenant id");
            }, token);

            await runner.RunTestAsync("TenantProvisioning: DeprovisionAsync throws on null tenantId", async ct =>
            {
                TenantProvisioningService svc = new TenantProvisioningService(db, logging, settings);
                AssertHelper.ThrowsAsync<ArgumentNullException>(async () =>
                {
                    await svc.DeprovisionAsync(null);
                }, "should throw on null tenantId");
            }, token);

            await runner.RunTestAsync("TenantProvisioning: DeprovisionAsync throws on protected tenant", async ct =>
            {
                MockDatabaseDriver localDb = new MockDatabaseDriver();
                TenantProvisioningService svc = new TenantProvisioningService(localDb, logging, settings);

                TenantMetadata protectedTenant = new TenantMetadata
                {
                    Name = "Protected Tenant",
                    Active = true,
                    IsProtected = true
                };
                protectedTenant = await localDb.Tenant.CreateAsync(protectedTenant, ct);

                AssertHelper.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await svc.DeprovisionAsync(protectedTenant.Id);
                }, "should throw on protected tenant");
            }, token);
        }
    }
}
