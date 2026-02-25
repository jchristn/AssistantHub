namespace Test.Database.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;

    public static class TenantTests
    {
        /// <summary>
        /// The ID of the primary test tenant, available after RunAllAsync completes.
        /// Other test classes use this to set TenantId on their objects.
        /// </summary>
        public static string TestTenantId { get; private set; }

        public static async Task RunAllAsync(DatabaseDriverBase driver, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- Tenant Tests ---");

            string createdId = null;

            await runner.RunTestAsync("Tenant.Create", async ct =>
            {
                TenantMetadata tenant = new TenantMetadata
                {
                    Name = "Test Tenant",
                    Active = true,
                    Labels = new List<string> { "test", "unit" },
                    Tags = new Dictionary<string, string> { { "env", "test" } }
                };

                TenantMetadata created = await driver.Tenant.CreateAsync(tenant, ct);
                AssertHelper.IsNotNull(created, "created tenant");
                AssertHelper.IsNotNull(created.Id, "Id");
                AssertHelper.StartsWith(created.Id, "ten_", "Id prefix");
                AssertHelper.AreEqual("Test Tenant", created.Name, "Name");
                AssertHelper.AreEqual(true, created.Active, "Active");
                AssertHelper.IsNotNull(created.Labels, "Labels");
                AssertHelper.AreEqual(2, created.Labels.Count, "Labels count");
                AssertHelper.IsNotNull(created.Tags, "Tags");
                AssertHelper.AreEqual("test", created.Tags["env"], "Tags[env]");
                AssertHelper.DateTimeRecent(created.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(created.LastUpdateUtc, "LastUpdateUtc");
                createdId = created.Id;
                TestTenantId = created.Id;
            }, token);

            await runner.RunTestAsync("Tenant.Create_Defaults", async ct =>
            {
                TenantMetadata tenant = new TenantMetadata { Name = "Defaults Tenant" };
                TenantMetadata created = await driver.Tenant.CreateAsync(tenant, ct);
                AssertHelper.IsNotNull(created, "created tenant");
                AssertHelper.AreEqual(true, created.Active, "Active defaults to true");
            }, token);

            await runner.RunTestAsync("Tenant.Read", async ct =>
            {
                TenantMetadata read = await driver.Tenant.ReadByIdAsync(createdId, ct);
                AssertHelper.IsNotNull(read, "read tenant");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual("Test Tenant", read.Name, "Name");
                AssertHelper.AreEqual(true, read.Active, "Active");
                AssertHelper.DateTimeRecent(read.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(read.LastUpdateUtc, "LastUpdateUtc");
            }, token);

            await runner.RunTestAsync("Tenant.ReadByName", async ct =>
            {
                TenantMetadata read = await driver.Tenant.ReadByNameAsync("Test Tenant", ct);
                AssertHelper.IsNotNull(read, "read by name");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual("Test Tenant", read.Name, "Name");
            }, token);

            await runner.RunTestAsync("Tenant.ReadByName_NotFound", async ct =>
            {
                TenantMetadata read = await driver.Tenant.ReadByNameAsync("Nonexistent Tenant", ct);
                AssertHelper.IsNull(read, "non-existent tenant name");
            }, token);

            await runner.RunTestAsync("Tenant.Read_NotFound", async ct =>
            {
                TenantMetadata read = await driver.Tenant.ReadByIdAsync("ten_nonexistent", ct);
                AssertHelper.IsNull(read, "non-existent tenant");
            }, token);

            await runner.RunTestAsync("Tenant.Exists_True", async ct =>
            {
                bool exists = await driver.Tenant.ExistsByIdAsync(createdId, ct);
                AssertHelper.IsTrue(exists, "tenant should exist");
            }, token);

            await runner.RunTestAsync("Tenant.Exists_False", async ct =>
            {
                bool exists = await driver.Tenant.ExistsByIdAsync("ten_nonexistent", ct);
                AssertHelper.IsFalse(exists, "non-existent tenant should not exist");
            }, token);

            await runner.RunTestAsync("Tenant.Update", async ct =>
            {
                TenantMetadata read = await driver.Tenant.ReadByIdAsync(createdId, ct);
                read.Name = "Updated Tenant";
                read.Active = false;
                read.Labels = new List<string> { "updated" };
                read.Tags = new Dictionary<string, string> { { "env", "prod" } };

                TenantMetadata updated = await driver.Tenant.UpdateAsync(read, ct);
                AssertHelper.IsNotNull(updated, "updated tenant");
                AssertHelper.AreEqual(createdId, updated.Id, "Id");
                AssertHelper.AreEqual("Updated Tenant", updated.Name, "Name");
                AssertHelper.AreEqual(false, updated.Active, "Active");
                AssertHelper.DateTimeRecent(updated.LastUpdateUtc, "LastUpdateUtc");
            }, token);

            await runner.RunTestAsync("Tenant.Update_VerifyPersistence", async ct =>
            {
                TenantMetadata read = await driver.Tenant.ReadByIdAsync(createdId, ct);
                AssertHelper.AreEqual("Updated Tenant", read.Name, "Name after re-read");
                AssertHelper.AreEqual(false, read.Active, "Active after re-read");
            }, token);

            await runner.RunTestAsync("Tenant.GetCount", async ct =>
            {
                long count = await driver.Tenant.GetCountAsync(ct);
                AssertHelper.IsGreaterThanOrEqual(count, 2, "tenant count");
            }, token);

            await runner.RunTestAsync("Tenant.Enumerate_Default", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery { MaxResults = 100 };
                EnumerationResult<TenantMetadata> result = await driver.Tenant.EnumerateAsync(query, ct);
                AssertHelper.IsNotNull(result, "enumeration result");
                AssertHelper.IsTrue(result.Success, "enumeration success");
                AssertHelper.IsGreaterThanOrEqual(result.Objects.Count, 2, "objects count");
            }, token);

            await runner.RunTestAsync("Tenant.Enumerate_Pagination_Page1", async ct =>
            {
                EnumerationQuery q1 = new EnumerationQuery { MaxResults = 1 };
                EnumerationResult<TenantMetadata> r1 = await driver.Tenant.EnumerateAsync(q1, ct);
                AssertHelper.AreEqual(1, r1.Objects.Count, "page 1 count");
                AssertHelper.IsFalse(r1.EndOfResults, "page 1 not end");
                AssertHelper.IsNotNull(r1.ContinuationToken, "continuation token");
            }, token);

            await runner.RunTestAsync("Tenant.Enumerate_Pagination_Page2", async ct =>
            {
                EnumerationQuery q1 = new EnumerationQuery { MaxResults = 1 };
                EnumerationResult<TenantMetadata> r1 = await driver.Tenant.EnumerateAsync(q1, ct);

                EnumerationQuery q2 = new EnumerationQuery { MaxResults = 1, ContinuationToken = r1.ContinuationToken };
                EnumerationResult<TenantMetadata> r2 = await driver.Tenant.EnumerateAsync(q2, ct);
                AssertHelper.AreEqual(1, r2.Objects.Count, "page 2 count");
                AssertHelper.AreNotEqual(r1.Objects[0].Id, r2.Objects[0].Id, "different tenants on different pages");
            }, token);

            await runner.RunTestAsync("Tenant.Enumerate_Ordering_Ascending", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery { MaxResults = 100, Ordering = AssistantHub.Core.Enums.EnumerationOrderEnum.CreatedAscending };
                EnumerationResult<TenantMetadata> result = await driver.Tenant.EnumerateAsync(query, ct);
                AssertHelper.IsTrue(result.Success, "ascending enumeration success");
                if (result.Objects.Count >= 2)
                {
                    AssertHelper.IsTrue(result.Objects[0].CreatedUtc <= result.Objects[1].CreatedUtc, "ascending order");
                }
            }, token);

            await runner.RunTestAsync("Tenant.Enumerate_Ordering_Descending", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery { MaxResults = 100, Ordering = AssistantHub.Core.Enums.EnumerationOrderEnum.CreatedDescending };
                EnumerationResult<TenantMetadata> result = await driver.Tenant.EnumerateAsync(query, ct);
                AssertHelper.IsTrue(result.Success, "descending enumeration success");
                if (result.Objects.Count >= 2)
                {
                    AssertHelper.IsTrue(result.Objects[0].CreatedUtc >= result.Objects[1].CreatedUtc, "descending order");
                }
            }, token);

            await runner.RunTestAsync("Tenant.Delete", async ct =>
            {
                TenantMetadata tenant = new TenantMetadata { Name = "To Delete" };
                TenantMetadata created = await driver.Tenant.CreateAsync(tenant, ct);

                await driver.Tenant.DeleteByIdAsync(created.Id, ct);

                TenantMetadata read = await driver.Tenant.ReadByIdAsync(created.Id, ct);
                AssertHelper.IsNull(read, "deleted tenant");
            }, token);

            await runner.RunTestAsync("Tenant.Delete_NonExistent", async ct =>
            {
                await driver.Tenant.DeleteByIdAsync("ten_nonexistent", ct);
                // should not throw
            }, token);
        }
    }
}
