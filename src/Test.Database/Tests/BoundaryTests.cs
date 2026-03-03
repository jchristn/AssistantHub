namespace Test.Database.Tests
{
    using System;
    using System.Collections.Generic;
    using Test.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Boundary and edge case tests for database drivers.
    /// Tests max string lengths, special characters, empty strings, and enumeration edge cases.
    /// </summary>
    public static class BoundaryTests
    {
        public static async Task RunAllAsync(DatabaseDriverBase driver, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- Boundary Tests ---");

            string tenantId = TenantTests.TestTenantId;

            // --- String boundary tests ---

            await runner.RunTestAsync("Boundary.EmptyStringName_Rejected", async ct =>
            {
                // TenantMetadata.Name property setter rejects empty/null values
                bool threw = false;
                try
                {
                    TenantMetadata tenant = new TenantMetadata { Name = "" };
                }
                catch (ArgumentNullException)
                {
                    threw = true;
                }
                AssertHelper.AreEqual(true, threw, "empty name should throw ArgumentNullException in setter");
            }, token);

            await runner.RunTestAsync("Boundary.NullName_Rejected", async ct =>
            {
                bool threw = false;
                try
                {
                    TenantMetadata tenant = new TenantMetadata { Name = null };
                }
                catch (ArgumentNullException)
                {
                    threw = true;
                }
                AssertHelper.AreEqual(true, threw, "null name should throw ArgumentNullException in setter");
            }, token);

            await runner.RunTestAsync("Boundary.UnicodeCharacters", async ct =>
            {
                TenantMetadata tenant = new TenantMetadata
                {
                    Name = "测试テスト한국어 Ünïcödé émojis 🎉🚀"
                };
                TenantMetadata created = await driver.Tenant.CreateAsync(tenant, ct);
                AssertHelper.IsNotNull(created, "unicode tenant should be created");
                AssertHelper.AreEqual("测试テスト한국어 Ünïcödé émojis 🎉🚀", created.Name, "unicode Name");

                TenantMetadata read = await driver.Tenant.ReadByIdAsync(created.Id, ct);
                AssertHelper.AreEqual(created.Name, read.Name, "unicode round-trip");
                await driver.Tenant.DeleteByIdAsync(created.Id, ct);
            }, token);

            await runner.RunTestAsync("Boundary.SpecialCharacters_SQLInjection", async ct =>
            {
                // SQL injection attempt should be safely stored as a literal string
                string malicious = "'; DROP TABLE tenants; --";
                TenantMetadata tenant = new TenantMetadata { Name = malicious };
                TenantMetadata created = await driver.Tenant.CreateAsync(tenant, ct);
                AssertHelper.IsNotNull(created, "SQL injection string should be safely stored");
                AssertHelper.AreEqual(malicious, created.Name, "malicious string should be preserved as-is");

                TenantMetadata read = await driver.Tenant.ReadByIdAsync(created.Id, ct);
                AssertHelper.AreEqual(malicious, read.Name, "malicious string round-trip");
                await driver.Tenant.DeleteByIdAsync(created.Id, ct);
            }, token);

            await runner.RunTestAsync("Boundary.LongString", async ct =>
            {
                // Create a tenant with a very long name
                string longName = new string('A', 1000);
                TenantMetadata tenant = new TenantMetadata { Name = longName };
                TenantMetadata created = await driver.Tenant.CreateAsync(tenant, ct);
                AssertHelper.IsNotNull(created, "long name tenant should be created");
                AssertHelper.AreEqual(longName, created.Name, "long name preserved");
                await driver.Tenant.DeleteByIdAsync(created.Id, ct);
            }, token);

            await runner.RunTestAsync("Boundary.NewlinesAndTabs", async ct =>
            {
                string name = "Line1\nLine2\tTabbed\r\nCRLF";
                TenantMetadata tenant = new TenantMetadata { Name = name };
                TenantMetadata created = await driver.Tenant.CreateAsync(tenant, ct);
                AssertHelper.IsNotNull(created, "tenant with newlines should be created");
                AssertHelper.AreEqual(name, created.Name, "newlines/tabs preserved");
                await driver.Tenant.DeleteByIdAsync(created.Id, ct);
            }, token);

            // --- Enumeration edge cases ---

            await runner.RunTestAsync("Boundary.Enumerate_MaxResults1", async ct =>
            {
                // Enumerate with MaxResults=1 should return exactly 1 result
                EnumerationQuery query = new EnumerationQuery { MaxResults = 1 };
                EnumerationResult<TenantMetadata> result = await driver.Tenant.EnumerateAsync(query, ct);
                AssertHelper.IsNotNull(result, "result should not be null");
                AssertHelper.AreEqual(1, result.Objects.Count, "should return exactly 1 result");
            }, token);

            await runner.RunTestAsync("Boundary.Enumerate_FullPagination", async ct =>
            {
                // Create several tenants and paginate through all of them one by one
                List<string> createdIds = new List<string>();
                for (int i = 0; i < 3; i++)
                {
                    TenantMetadata t = await driver.Tenant.CreateAsync(
                        new TenantMetadata { Name = $"Pagination Test {i}" }, ct);
                    createdIds.Add(t.Id);
                }

                int totalSeen = 0;
                string continuationToken = null;
                int pages = 0;

                do
                {
                    EnumerationQuery query = new EnumerationQuery
                    {
                        MaxResults = 1,
                        ContinuationToken = continuationToken
                    };
                    EnumerationResult<TenantMetadata> page = await driver.Tenant.EnumerateAsync(query, ct);
                    totalSeen += page.Objects.Count;
                    continuationToken = page.ContinuationToken;
                    pages++;

                    // Safety: break if we've iterated too many times
                    if (pages > 100) break;
                }
                while (!string.IsNullOrEmpty(continuationToken));

                // We should have seen at least 4 tenants (1 original + 3 new)
                AssertHelper.IsTrue(totalSeen >= 4, "should see at least 4 tenants through pagination, saw " + totalSeen);

                // Cleanup
                foreach (string id in createdIds)
                    await driver.Tenant.DeleteByIdAsync(id, ct);
            }, token);

            // --- Tenant isolation tests ---

            await runner.RunTestAsync("Boundary.TenantIsolation_Users", async ct =>
            {
                // Create two tenants
                TenantMetadata tenantA = await driver.Tenant.CreateAsync(
                    new TenantMetadata { Name = "Tenant A" }, ct);
                TenantMetadata tenantB = await driver.Tenant.CreateAsync(
                    new TenantMetadata { Name = "Tenant B" }, ct);

                // Create a user in each tenant
                UserMaster userA = await driver.User.CreateAsync(
                    new UserMaster { TenantId = tenantA.Id, FirstName = "UserA", Email = "usera@isolation.test" }, ct);
                UserMaster userB = await driver.User.CreateAsync(
                    new UserMaster { TenantId = tenantB.Id, FirstName = "UserB", Email = "userb@isolation.test" }, ct);

                // Enumerate users for tenant A — should not include tenant B's user
                EnumerationResult<UserMaster> usersA = await driver.User.EnumerateAsync(
                    tenantA.Id, new EnumerationQuery(), ct);
                bool foundB = false;
                foreach (UserMaster u in usersA.Objects)
                {
                    if (u.Id == userB.Id) foundB = true;
                }
                AssertHelper.AreEqual(false, foundB, "tenant A's enumerate should not include tenant B's user");

                // Enumerate users for tenant B — should not include tenant A's user
                EnumerationResult<UserMaster> usersB = await driver.User.EnumerateAsync(
                    tenantB.Id, new EnumerationQuery(), ct);
                bool foundA = false;
                foreach (UserMaster u in usersB.Objects)
                {
                    if (u.Id == userA.Id) foundA = true;
                }
                AssertHelper.AreEqual(false, foundA, "tenant B's enumerate should not include tenant A's user");

                // Cleanup
                await driver.User.DeleteAsync(userA.Id, ct);
                await driver.User.DeleteAsync(userB.Id, ct);
                await driver.Tenant.DeleteByIdAsync(tenantA.Id, ct);
                await driver.Tenant.DeleteByIdAsync(tenantB.Id, ct);
            }, token);

            await runner.RunTestAsync("Boundary.TenantIsolation_Assistants", async ct =>
            {
                TenantMetadata tenantA = await driver.Tenant.CreateAsync(
                    new TenantMetadata { Name = "Isolation Tenant A" }, ct);
                TenantMetadata tenantB = await driver.Tenant.CreateAsync(
                    new TenantMetadata { Name = "Isolation Tenant B" }, ct);

                UserMaster userA = await driver.User.CreateAsync(
                    new UserMaster { TenantId = tenantA.Id, FirstName = "UA", Email = "ua@iso.test" }, ct);
                UserMaster userB = await driver.User.CreateAsync(
                    new UserMaster { TenantId = tenantB.Id, FirstName = "UB", Email = "ub@iso.test" }, ct);

                Assistant asstA = await driver.Assistant.CreateAsync(
                    new Assistant { TenantId = tenantA.Id, UserId = userA.Id, Name = "Assistant A" }, ct);
                Assistant asstB = await driver.Assistant.CreateAsync(
                    new Assistant { TenantId = tenantB.Id, UserId = userB.Id, Name = "Assistant B" }, ct);

                // Enumerate assistants for tenant A
                EnumerationResult<Assistant> resultA = await driver.Assistant.EnumerateAsync(
                    tenantA.Id, new EnumerationQuery(), ct);
                bool foundBAsst = false;
                foreach (Assistant a in resultA.Objects)
                {
                    if (a.Id == asstB.Id) foundBAsst = true;
                }
                AssertHelper.AreEqual(false, foundBAsst, "tenant A should not see tenant B's assistant");

                // Cleanup
                await driver.Assistant.DeleteAsync(asstA.Id, ct);
                await driver.Assistant.DeleteAsync(asstB.Id, ct);
                await driver.User.DeleteAsync(userA.Id, ct);
                await driver.User.DeleteAsync(userB.Id, ct);
                await driver.Tenant.DeleteByIdAsync(tenantA.Id, ct);
                await driver.Tenant.DeleteByIdAsync(tenantB.Id, ct);
            }, token);

            // --- ExistsAsync for all entity types ---

            await runner.RunTestAsync("Boundary.ExistsAsync_AllTypes", async ct =>
            {
                // Tenant
                bool tenantExists = await driver.Tenant.ExistsByIdAsync(tenantId, ct);
                AssertHelper.AreEqual(true, tenantExists, "test tenant should exist");
                bool tenantNotExists = await driver.Tenant.ExistsByIdAsync("ten_fake", ct);
                AssertHelper.AreEqual(false, tenantNotExists, "fake tenant should not exist");

                // User
                UserMaster user = await driver.User.CreateAsync(
                    new UserMaster { TenantId = tenantId, FirstName = "ExistsTest", Email = "exists@test.com" }, ct);
                bool userExists = await driver.User.ExistsAsync(user.Id, ct);
                AssertHelper.AreEqual(true, userExists, "user should exist");
                bool userNotExists = await driver.User.ExistsAsync("usr_fake", ct);
                AssertHelper.AreEqual(false, userNotExists, "fake user should not exist");

                // Credential
                Credential cred = await driver.Credential.CreateAsync(
                    new Credential { TenantId = tenantId, UserId = user.Id }, ct);
                bool credExists = await driver.Credential.ExistsAsync(cred.Id, ct);
                AssertHelper.AreEqual(true, credExists, "credential should exist");
                bool credNotExists = await driver.Credential.ExistsAsync("cred_fake", ct);
                AssertHelper.AreEqual(false, credNotExists, "fake credential should not exist");

                // Assistant
                Assistant asst = await driver.Assistant.CreateAsync(
                    new Assistant { TenantId = tenantId, UserId = user.Id, Name = "Exists Test" }, ct);
                bool asstExists = await driver.Assistant.ExistsAsync(asst.Id, ct);
                AssertHelper.AreEqual(true, asstExists, "assistant should exist");
                bool asstNotExists = await driver.Assistant.ExistsAsync("asst_fake", ct);
                AssertHelper.AreEqual(false, asstNotExists, "fake assistant should not exist");

                // IngestionRule
                IngestionRule rule = await driver.IngestionRule.CreateAsync(
                    new IngestionRule { TenantId = tenantId }, ct);
                bool ruleExists = await driver.IngestionRule.ExistsAsync(rule.Id, ct);
                AssertHelper.AreEqual(true, ruleExists, "ingestion rule should exist");
                bool ruleNotExists = await driver.IngestionRule.ExistsAsync("irule_fake", ct);
                AssertHelper.AreEqual(false, ruleNotExists, "fake ingestion rule should not exist");

                // Cleanup
                await driver.IngestionRule.DeleteAsync(rule.Id, ct);
                await driver.Assistant.DeleteAsync(asst.Id, ct);
                await driver.Credential.DeleteAsync(cred.Id, ct);
                await driver.User.DeleteAsync(user.Id, ct);
            }, token);

            // --- ReadByName / ReadByEmail with non-existent values ---

            await runner.RunTestAsync("Boundary.ReadByEmail_NonExistent_ReturnsNull", async ct =>
            {
                UserMaster user = await driver.User.ReadByEmailAsync(tenantId, "nonexistent@email.com", ct);
                AssertHelper.IsNull(user, "non-existent email should return null");
            }, token);

            await runner.RunTestAsync("Boundary.ReadByName_NonExistent_ReturnsNull", async ct =>
            {
                // Test tenant ReadByName
                TenantMetadata tenant = await driver.Tenant.ReadByNameAsync("NonExistentTenantName_XYZ_999", ct);
                AssertHelper.IsNull(tenant, "non-existent tenant name should return null");

                // Test ingestion rule ReadByName
                IngestionRule rule = await driver.IngestionRule.ReadByNameAsync(tenantId, "NonExistentRule_XYZ_999", ct);
                AssertHelper.IsNull(rule, "non-existent ingestion rule name should return null");
            }, token);
        }
    }
}
