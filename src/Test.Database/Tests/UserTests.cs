namespace Test.Database.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Enums;

    public static class UserTests
    {
        public static async Task RunAllAsync(DatabaseDriverBase driver, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- User Tests ---");

            string createdId = null;

            await runner.RunTestAsync("User.Create", async ct =>
            {
                UserMaster user = new UserMaster
                {
                    Email = "testuser@example.com",
                    FirstName = "Test",
                    LastName = "User",
                    IsAdmin = false,
                    Active = true
                };
                user.SetPassword("TestPassword123!");

                UserMaster created = await driver.User.CreateAsync(user, ct);
                AssertHelper.IsNotNull(created, "created user");
                AssertHelper.IsNotNull(created.Id, "created user Id");
                AssertHelper.StartsWith(created.Id, "usr_", "created user Id prefix");
                AssertHelper.AreEqual("testuser@example.com", created.Email, "Email");
                AssertHelper.AreEqual("Test", created.FirstName, "FirstName");
                AssertHelper.AreEqual("User", created.LastName, "LastName");
                AssertHelper.AreEqual(false, created.IsAdmin, "IsAdmin");
                AssertHelper.AreEqual(true, created.Active, "Active");
                AssertHelper.IsNotNull(created.PasswordSha256, "PasswordSha256");
                AssertHelper.IsTrue(created.VerifyPassword("TestPassword123!"), "VerifyPassword should succeed");
                AssertHelper.DateTimeRecent(created.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(created.LastUpdateUtc, "LastUpdateUtc");
                createdId = created.Id;
            }, token);

            await runner.RunTestAsync("User.Create_AdminUser", async ct =>
            {
                UserMaster admin = new UserMaster
                {
                    Email = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "Boss",
                    IsAdmin = true,
                    Active = true
                };
                admin.SetPassword("AdminPass!");

                UserMaster created = await driver.User.CreateAsync(admin, ct);
                AssertHelper.IsNotNull(created, "created admin");
                AssertHelper.AreEqual(true, created.IsAdmin, "IsAdmin");
                AssertHelper.AreEqual("admin@example.com", created.Email, "Email");
                AssertHelper.AreEqual("Admin", created.FirstName, "FirstName");
                AssertHelper.AreEqual("Boss", created.LastName, "LastName");
            }, token);

            await runner.RunTestAsync("User.Create_NullableFields", async ct =>
            {
                UserMaster user = new UserMaster
                {
                    Email = "minimal@example.com"
                };

                UserMaster created = await driver.User.CreateAsync(user, ct);
                AssertHelper.IsNotNull(created, "created user");
                AssertHelper.AreEqual("minimal@example.com", created.Email, "Email");
                AssertHelper.IsNull(created.FirstName, "FirstName");
                AssertHelper.IsNull(created.LastName, "LastName");
                AssertHelper.IsNull(created.PasswordSha256, "PasswordSha256");
                AssertHelper.AreEqual(false, created.IsAdmin, "IsAdmin");
                AssertHelper.AreEqual(true, created.Active, "Active");
            }, token);

            await runner.RunTestAsync("User.Read", async ct =>
            {
                AssertHelper.IsNotNull(createdId, "createdId from prior test");
                UserMaster read = await driver.User.ReadAsync(createdId, ct);
                AssertHelper.IsNotNull(read, "read user");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual("testuser@example.com", read.Email, "Email");
                AssertHelper.AreEqual("Test", read.FirstName, "FirstName");
                AssertHelper.AreEqual("User", read.LastName, "LastName");
                AssertHelper.AreEqual(false, read.IsAdmin, "IsAdmin");
                AssertHelper.AreEqual(true, read.Active, "Active");
                AssertHelper.IsNotNull(read.PasswordSha256, "PasswordSha256");
                AssertHelper.IsTrue(read.VerifyPassword("TestPassword123!"), "VerifyPassword after read");
                AssertHelper.DateTimeRecent(read.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(read.LastUpdateUtc, "LastUpdateUtc");
            }, token);

            await runner.RunTestAsync("User.ReadByEmail", async ct =>
            {
                UserMaster read = await driver.User.ReadByEmailAsync("testuser@example.com", ct);
                AssertHelper.IsNotNull(read, "read user by email");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual("testuser@example.com", read.Email, "Email");
            }, token);

            await runner.RunTestAsync("User.ReadByEmail_NotFound", async ct =>
            {
                UserMaster read = await driver.User.ReadByEmailAsync("nonexistent@example.com", ct);
                AssertHelper.IsNull(read, "non-existent user by email");
            }, token);

            await runner.RunTestAsync("User.Read_NotFound", async ct =>
            {
                UserMaster read = await driver.User.ReadAsync("usr_nonexistent", ct);
                AssertHelper.IsNull(read, "non-existent user");
            }, token);

            await runner.RunTestAsync("User.Exists_True", async ct =>
            {
                bool exists = await driver.User.ExistsAsync(createdId, ct);
                AssertHelper.IsTrue(exists, "user should exist");
            }, token);

            await runner.RunTestAsync("User.Exists_False", async ct =>
            {
                bool exists = await driver.User.ExistsAsync("usr_nonexistent", ct);
                AssertHelper.IsFalse(exists, "non-existent user should not exist");
            }, token);

            await runner.RunTestAsync("User.Update", async ct =>
            {
                UserMaster read = await driver.User.ReadAsync(createdId, ct);
                read.FirstName = "Updated";
                read.LastName = "Name";
                read.IsAdmin = true;
                read.Active = false;
                read.SetPassword("NewPassword456!");

                UserMaster updated = await driver.User.UpdateAsync(read, ct);
                AssertHelper.IsNotNull(updated, "updated user");
                AssertHelper.AreEqual(createdId, updated.Id, "Id");
                AssertHelper.AreEqual("testuser@example.com", updated.Email, "Email");
                AssertHelper.AreEqual("Updated", updated.FirstName, "FirstName");
                AssertHelper.AreEqual("Name", updated.LastName, "LastName");
                AssertHelper.AreEqual(true, updated.IsAdmin, "IsAdmin");
                AssertHelper.AreEqual(false, updated.Active, "Active");
                AssertHelper.IsTrue(updated.VerifyPassword("NewPassword456!"), "VerifyPassword after update");
                AssertHelper.IsFalse(updated.VerifyPassword("TestPassword123!"), "old password should fail");
                AssertHelper.DateTimeRecent(updated.LastUpdateUtc, "LastUpdateUtc after update");
            }, token);

            await runner.RunTestAsync("User.Update_VerifyPersistence", async ct =>
            {
                UserMaster read = await driver.User.ReadAsync(createdId, ct);
                AssertHelper.AreEqual("Updated", read.FirstName, "FirstName after re-read");
                AssertHelper.AreEqual("Name", read.LastName, "LastName after re-read");
                AssertHelper.AreEqual(true, read.IsAdmin, "IsAdmin after re-read");
                AssertHelper.AreEqual(false, read.Active, "Active after re-read");
            }, token);

            await runner.RunTestAsync("User.GetCount", async ct =>
            {
                long count = await driver.User.GetCountAsync(ct);
                AssertHelper.IsGreaterThanOrEqual(count, 3, "user count");
            }, token);

            await runner.RunTestAsync("User.Enumerate_Default", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery { MaxResults = 10 };
                EnumerationResult<UserMaster> result = await driver.User.EnumerateAsync(query, ct);
                AssertHelper.IsNotNull(result, "enumeration result");
                AssertHelper.IsTrue(result.Success, "enumeration success");
                AssertHelper.IsGreaterThanOrEqual(result.Objects.Count, 3, "objects count");
                AssertHelper.IsGreaterThanOrEqual(result.TotalRecords, 3, "total records");
            }, token);

            await runner.RunTestAsync("User.Enumerate_Pagination_Page1", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery { MaxResults = 1 };
                EnumerationResult<UserMaster> result = await driver.User.EnumerateAsync(query, ct);
                AssertHelper.IsNotNull(result, "page 1 result");
                AssertHelper.AreEqual(1, result.Objects.Count, "page 1 count");
                AssertHelper.IsFalse(result.EndOfResults, "page 1 should not be end");
                AssertHelper.IsNotNull(result.ContinuationToken, "page 1 continuation token");
                AssertHelper.IsGreaterThan(result.RecordsRemaining, 0, "records remaining");
            }, token);

            await runner.RunTestAsync("User.Enumerate_Pagination_Page2", async ct =>
            {
                EnumerationQuery q1 = new EnumerationQuery { MaxResults = 1 };
                EnumerationResult<UserMaster> r1 = await driver.User.EnumerateAsync(q1, ct);

                EnumerationQuery q2 = new EnumerationQuery
                {
                    MaxResults = 1,
                    ContinuationToken = r1.ContinuationToken
                };
                EnumerationResult<UserMaster> r2 = await driver.User.EnumerateAsync(q2, ct);
                AssertHelper.IsNotNull(r2, "page 2 result");
                AssertHelper.AreEqual(1, r2.Objects.Count, "page 2 count");
                AssertHelper.AreNotEqual(r1.Objects[0].Id, r2.Objects[0].Id, "page 2 should have different user");
            }, token);

            await runner.RunTestAsync("User.Enumerate_Ordering_Ascending", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery
                {
                    MaxResults = 100,
                    Ordering = EnumerationOrderEnum.CreatedAscending
                };
                EnumerationResult<UserMaster> result = await driver.User.EnumerateAsync(query, ct);
                AssertHelper.IsGreaterThanOrEqual(result.Objects.Count, 2, "ascending result count");
                for (int i = 1; i < result.Objects.Count; i++)
                {
                    AssertHelper.IsTrue(
                        result.Objects[i].CreatedUtc >= result.Objects[i - 1].CreatedUtc,
                        $"ascending order at index {i}");
                }
            }, token);

            await runner.RunTestAsync("User.Enumerate_Ordering_Descending", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery
                {
                    MaxResults = 100,
                    Ordering = EnumerationOrderEnum.CreatedDescending
                };
                EnumerationResult<UserMaster> result = await driver.User.EnumerateAsync(query, ct);
                AssertHelper.IsGreaterThanOrEqual(result.Objects.Count, 2, "descending result count");
                for (int i = 1; i < result.Objects.Count; i++)
                {
                    AssertHelper.IsTrue(
                        result.Objects[i].CreatedUtc <= result.Objects[i - 1].CreatedUtc,
                        $"descending order at index {i}");
                }
            }, token);

            await runner.RunTestAsync("User.Enumerate_AllPages", async ct =>
            {
                List<UserMaster> allUsers = new List<UserMaster>();
                string continuationToken = null;

                while (true)
                {
                    EnumerationQuery query = new EnumerationQuery
                    {
                        MaxResults = 1,
                        ContinuationToken = continuationToken
                    };
                    EnumerationResult<UserMaster> result = await driver.User.EnumerateAsync(query, ct);
                    allUsers.AddRange(result.Objects);
                    if (result.EndOfResults) break;
                    continuationToken = result.ContinuationToken;
                }

                AssertHelper.IsGreaterThanOrEqual(allUsers.Count, 3, "all users via pagination");
            }, token);

            await runner.RunTestAsync("User.Delete", async ct =>
            {
                // create a user specifically for deletion
                UserMaster user = new UserMaster { Email = "delete-me@example.com" };
                UserMaster created = await driver.User.CreateAsync(user, ct);
                string deleteId = created.Id;

                await driver.User.DeleteAsync(deleteId, ct);

                UserMaster read = await driver.User.ReadAsync(deleteId, ct);
                AssertHelper.IsNull(read, "deleted user should be null");

                bool exists = await driver.User.ExistsAsync(deleteId, ct);
                AssertHelper.IsFalse(exists, "deleted user should not exist");
            }, token);

            await runner.RunTestAsync("User.Delete_NonExistent", async ct =>
            {
                // should not throw
                await driver.User.DeleteAsync("usr_nonexistent_delete", ct);
            }, token);
        }
    }
}
