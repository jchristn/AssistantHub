namespace Test.Database.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Enums;

    public static class AssistantTests
    {
        public static async Task RunAllAsync(DatabaseDriverBase driver, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- Assistant Tests ---");

            UserMaster user = await driver.User.CreateAsync(new UserMaster { Email = "asst-owner@example.com" }, token);
            string userId = user.Id;
            string createdId = null;

            await runner.RunTestAsync("Assistant.Create", async ct =>
            {
                Assistant asst = new Assistant
                {
                    UserId = userId,
                    Name = "Test Assistant",
                    Description = "A test assistant for unit testing",
                    Active = true
                };

                Assistant created = await driver.Assistant.CreateAsync(asst, ct);
                AssertHelper.IsNotNull(created, "created assistant");
                AssertHelper.IsNotNull(created.Id, "Id");
                AssertHelper.StartsWith(created.Id, "asst_", "Id prefix");
                AssertHelper.AreEqual(userId, created.UserId, "UserId");
                AssertHelper.AreEqual("Test Assistant", created.Name, "Name");
                AssertHelper.AreEqual("A test assistant for unit testing", created.Description, "Description");
                AssertHelper.AreEqual(true, created.Active, "Active");
                AssertHelper.DateTimeRecent(created.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(created.LastUpdateUtc, "LastUpdateUtc");
                createdId = created.Id;
            }, token);

            await runner.RunTestAsync("Assistant.Create_NullDescription", async ct =>
            {
                Assistant asst = new Assistant
                {
                    UserId = userId,
                    Name = "No Description Assistant"
                };

                Assistant created = await driver.Assistant.CreateAsync(asst, ct);
                AssertHelper.IsNotNull(created, "created assistant");
                AssertHelper.IsNull(created.Description, "Description should be null");
                AssertHelper.AreEqual(true, created.Active, "Active default");
            }, token);

            await runner.RunTestAsync("Assistant.Read", async ct =>
            {
                Assistant read = await driver.Assistant.ReadAsync(createdId, ct);
                AssertHelper.IsNotNull(read, "read assistant");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual(userId, read.UserId, "UserId");
                AssertHelper.AreEqual("Test Assistant", read.Name, "Name");
                AssertHelper.AreEqual("A test assistant for unit testing", read.Description, "Description");
                AssertHelper.AreEqual(true, read.Active, "Active");
                AssertHelper.DateTimeRecent(read.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(read.LastUpdateUtc, "LastUpdateUtc");
            }, token);

            await runner.RunTestAsync("Assistant.Read_NotFound", async ct =>
            {
                Assistant read = await driver.Assistant.ReadAsync("asst_nonexistent", ct);
                AssertHelper.IsNull(read, "non-existent assistant");
            }, token);

            await runner.RunTestAsync("Assistant.Exists_True", async ct =>
            {
                bool exists = await driver.Assistant.ExistsAsync(createdId, ct);
                AssertHelper.IsTrue(exists, "assistant should exist");
            }, token);

            await runner.RunTestAsync("Assistant.Exists_False", async ct =>
            {
                bool exists = await driver.Assistant.ExistsAsync("asst_nonexistent", ct);
                AssertHelper.IsFalse(exists, "non-existent assistant");
            }, token);

            await runner.RunTestAsync("Assistant.Update", async ct =>
            {
                Assistant read = await driver.Assistant.ReadAsync(createdId, ct);
                read.Name = "Updated Assistant";
                read.Description = "Updated description";
                read.Active = false;

                Assistant updated = await driver.Assistant.UpdateAsync(read, ct);
                AssertHelper.IsNotNull(updated, "updated assistant");
                AssertHelper.AreEqual(createdId, updated.Id, "Id");
                AssertHelper.AreEqual(userId, updated.UserId, "UserId");
                AssertHelper.AreEqual("Updated Assistant", updated.Name, "Name");
                AssertHelper.AreEqual("Updated description", updated.Description, "Description");
                AssertHelper.AreEqual(false, updated.Active, "Active");
                AssertHelper.DateTimeRecent(updated.LastUpdateUtc, "LastUpdateUtc");
            }, token);

            await runner.RunTestAsync("Assistant.Update_VerifyPersistence", async ct =>
            {
                Assistant read = await driver.Assistant.ReadAsync(createdId, ct);
                AssertHelper.AreEqual("Updated Assistant", read.Name, "Name after re-read");
                AssertHelper.AreEqual("Updated description", read.Description, "Description after re-read");
                AssertHelper.AreEqual(false, read.Active, "Active after re-read");
            }, token);

            await runner.RunTestAsync("Assistant.GetCount", async ct =>
            {
                long count = await driver.Assistant.GetCountAsync(ct);
                AssertHelper.IsGreaterThanOrEqual(count, 2, "assistant count");
            }, token);

            await runner.RunTestAsync("Assistant.Enumerate_Default", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery { MaxResults = 100 };
                EnumerationResult<Assistant> result = await driver.Assistant.EnumerateAsync(query, ct);
                AssertHelper.IsNotNull(result, "enumeration result");
                AssertHelper.IsTrue(result.Success, "success");
                AssertHelper.IsGreaterThanOrEqual(result.Objects.Count, 2, "objects count");
                AssertHelper.IsGreaterThanOrEqual(result.TotalRecords, 2, "total records");
            }, token);

            await runner.RunTestAsync("Assistant.Enumerate_Pagination", async ct =>
            {
                EnumerationQuery q1 = new EnumerationQuery { MaxResults = 1 };
                EnumerationResult<Assistant> r1 = await driver.Assistant.EnumerateAsync(q1, ct);
                AssertHelper.AreEqual(1, r1.Objects.Count, "page 1 count");
                AssertHelper.IsFalse(r1.EndOfResults, "page 1 not end");

                EnumerationQuery q2 = new EnumerationQuery { MaxResults = 1, ContinuationToken = r1.ContinuationToken };
                EnumerationResult<Assistant> r2 = await driver.Assistant.EnumerateAsync(q2, ct);
                AssertHelper.AreEqual(1, r2.Objects.Count, "page 2 count");
                AssertHelper.AreNotEqual(r1.Objects[0].Id, r2.Objects[0].Id, "different pages different items");
            }, token);

            await runner.RunTestAsync("Assistant.Enumerate_Ascending", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery { MaxResults = 100, Ordering = EnumerationOrderEnum.CreatedAscending };
                EnumerationResult<Assistant> result = await driver.Assistant.EnumerateAsync(query, ct);
                for (int i = 1; i < result.Objects.Count; i++)
                    AssertHelper.IsTrue(result.Objects[i].CreatedUtc >= result.Objects[i - 1].CreatedUtc, $"ascending order at {i}");
            }, token);

            await runner.RunTestAsync("Assistant.Enumerate_Descending", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery { MaxResults = 100, Ordering = EnumerationOrderEnum.CreatedDescending };
                EnumerationResult<Assistant> result = await driver.Assistant.EnumerateAsync(query, ct);
                for (int i = 1; i < result.Objects.Count; i++)
                    AssertHelper.IsTrue(result.Objects[i].CreatedUtc <= result.Objects[i - 1].CreatedUtc, $"descending order at {i}");
            }, token);

            await runner.RunTestAsync("Assistant.Delete", async ct =>
            {
                Assistant asst = new Assistant { UserId = userId, Name = "Delete Me" };
                Assistant created = await driver.Assistant.CreateAsync(asst, ct);

                await driver.Assistant.DeleteAsync(created.Id, ct);

                Assistant read = await driver.Assistant.ReadAsync(created.Id, ct);
                AssertHelper.IsNull(read, "deleted assistant");
                bool exists = await driver.Assistant.ExistsAsync(created.Id, ct);
                AssertHelper.IsFalse(exists, "deleted assistant should not exist");
            }, token);

            await runner.RunTestAsync("Assistant.Delete_NonExistent", async ct =>
            {
                await driver.Assistant.DeleteAsync("asst_nonexistent_delete", ct);
            }, token);
        }
    }
}
