namespace Test.Sdk.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk;
    using AssistantHub.Sdk.Models;
    using Test.Common;

    /// <summary>
    /// Tests for tenant, user, and credential management.
    /// </summary>
    public static class TenantTests
    {
        /// <summary>
        /// Runs all tenant management tests.
        /// </summary>
        /// <param name="runner">Test runner instance.</param>
        /// <param name="client">AssistantHub client.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task RunAsync(TestRunner runner, AssistantHubClient client, CancellationToken token)
        {
            string createdTenantId = null;
            string createdUserId = null;
            string createdCredentialId = null;
            string uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);

            await runner.RunTestAsync("Tenant: List tenants returns results", async (CancellationToken ct) =>
            {
                EnumerationResult<TenantMetadata> result = await client.ListTenantsAsync(ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListTenants result");
                AssertHelper.IsNotNull(result.Objects, "ListTenants result.Objects");
                AssertHelper.IsGreaterThanOrEqual(result.Objects.Count, 1, "ListTenants count");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Tenant: Create tenant with unique name", async (CancellationToken ct) =>
            {
                TenantMetadata tenant = new TenantMetadata
                {
                    Name = "test-tenant-" + uniqueSuffix,
                    Active = true
                };

                System.Text.Json.JsonElement response = await client.CreateTenantAsync(tenant, ct).ConfigureAwait(false);
                AssertHelper.IsTrue(
                    response.ValueKind == System.Text.Json.JsonValueKind.Object,
                    "CreateTenant should return a JSON object");

                if (response.TryGetProperty("Tenant", out System.Text.Json.JsonElement tenantElement))
                {
                    if (tenantElement.TryGetProperty("Id", out System.Text.Json.JsonElement idElement))
                    {
                        createdTenantId = idElement.GetString();
                    }
                }

                AssertHelper.IsNotNull(createdTenantId, "Created tenant ID");
                AssertHelper.StartsWith(createdTenantId, "ten_", "Created tenant ID prefix");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Tenant: Get tenant by ID", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdTenantId, "createdTenantId from previous test");
                TenantMetadata tenant = await client.GetTenantAsync(createdTenantId, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(tenant, "GetTenant result");
                AssertHelper.AreEqual(createdTenantId, tenant.Id, "Tenant ID");
                AssertHelper.AreEqual("test-tenant-" + uniqueSuffix, tenant.Name, "Tenant Name");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Tenant: Update tenant name", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdTenantId, "createdTenantId from previous test");
                TenantMetadata tenant = new TenantMetadata
                {
                    Name = "test-tenant-updated-" + uniqueSuffix,
                    Active = true
                };

                TenantMetadata updated = await client.UpdateTenantAsync(createdTenantId, tenant, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(updated, "UpdateTenant result");
                AssertHelper.AreEqual("test-tenant-updated-" + uniqueSuffix, updated.Name, "Updated tenant name");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Tenant: List users in tenant", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdTenantId, "createdTenantId from previous test");
                EnumerationResult<UserMaster> result = await client.ListUsersAsync(createdTenantId, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "ListUsers result");
                AssertHelper.IsNotNull(result.Objects, "ListUsers result.Objects");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Tenant: Create user in tenant", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdTenantId, "createdTenantId from previous test");
                UserMaster user = new UserMaster
                {
                    FirstName = "Test",
                    LastName = "User",
                    Email = "testuser-" + uniqueSuffix + "@example.com",
                    Active = true
                };

                UserMaster created = await client.CreateUserAsync(createdTenantId, user, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(created, "CreateUser result");
                AssertHelper.IsNotNull(created.Id, "Created user ID");
                AssertHelper.StartsWith(created.Id, "usr_", "Created user ID prefix");
                createdUserId = created.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Tenant: Create credential in tenant", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdTenantId, "createdTenantId from previous test");
                Credential credential = new Credential
                {
                    Name = "test-cred-" + uniqueSuffix,
                    Active = true
                };

                Credential created = await client.CreateCredentialAsync(createdTenantId, credential, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(created, "CreateCredential result");
                AssertHelper.IsNotNull(created.Id, "Created credential ID");
                AssertHelper.StartsWith(created.Id, "cred_", "Created credential ID prefix");
                createdCredentialId = created.Id;
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Tenant: Delete credential", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdTenantId, "createdTenantId from previous test");
                AssertHelper.IsNotNull(createdCredentialId, "createdCredentialId from previous test");
                await client.DeleteCredentialAsync(createdTenantId, createdCredentialId, ct).ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Tenant: Delete user", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdTenantId, "createdTenantId from previous test");
                AssertHelper.IsNotNull(createdUserId, "createdUserId from previous test");
                await client.DeleteUserAsync(createdTenantId, createdUserId, ct).ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("Tenant: Delete tenant", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(createdTenantId, "createdTenantId from previous test");
                await client.DeleteTenantAsync(createdTenantId, ct).ConfigureAwait(false);
            }, token).ConfigureAwait(false);
        }
    }
}
