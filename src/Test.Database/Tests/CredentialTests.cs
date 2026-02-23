namespace Test.Database.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;

    public static class CredentialTests
    {
        public static async Task RunAllAsync(DatabaseDriverBase driver, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- Credential Tests ---");

            // setup: create a user to own credentials
            UserMaster user = await driver.User.CreateAsync(new UserMaster { Email = "cred-owner@example.com", FirstName = "Cred", LastName = "Owner" }, token);
            string userId = user.Id;
            string createdId = null;
            string bearerTokenValue = null;

            await runner.RunTestAsync("Credential.Create", async ct =>
            {
                Credential cred = new Credential
                {
                    UserId = userId,
                    Name = "Test Credential",
                    Active = true
                };

                Credential created = await driver.Credential.CreateAsync(cred, ct);
                AssertHelper.IsNotNull(created, "created credential");
                AssertHelper.IsNotNull(created.Id, "Id");
                AssertHelper.StartsWith(created.Id, "cred_", "Id prefix");
                AssertHelper.AreEqual(userId, created.UserId, "UserId");
                AssertHelper.AreEqual("Test Credential", created.Name, "Name");
                AssertHelper.IsNotNull(created.BearerToken, "BearerToken");
                AssertHelper.AreEqual(true, created.Active, "Active");
                AssertHelper.DateTimeRecent(created.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(created.LastUpdateUtc, "LastUpdateUtc");
                createdId = created.Id;
                bearerTokenValue = created.BearerToken;
            }, token);

            await runner.RunTestAsync("Credential.Create_DefaultName", async ct =>
            {
                Credential cred = new Credential
                {
                    UserId = userId
                };

                Credential created = await driver.Credential.CreateAsync(cred, ct);
                AssertHelper.IsNotNull(created, "created credential");
                AssertHelper.AreEqual("Default credential", created.Name, "default Name");
            }, token);

            await runner.RunTestAsync("Credential.Read", async ct =>
            {
                Credential read = await driver.Credential.ReadAsync(createdId, ct);
                AssertHelper.IsNotNull(read, "read credential");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual(userId, read.UserId, "UserId");
                AssertHelper.AreEqual("Test Credential", read.Name, "Name");
                AssertHelper.AreEqual(bearerTokenValue, read.BearerToken, "BearerToken");
                AssertHelper.AreEqual(true, read.Active, "Active");
                AssertHelper.DateTimeRecent(read.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(read.LastUpdateUtc, "LastUpdateUtc");
            }, token);

            await runner.RunTestAsync("Credential.ReadByBearerToken", async ct =>
            {
                Credential read = await driver.Credential.ReadByBearerTokenAsync(bearerTokenValue, ct);
                AssertHelper.IsNotNull(read, "read by bearer token");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual(bearerTokenValue, read.BearerToken, "BearerToken");
            }, token);

            await runner.RunTestAsync("Credential.ReadByBearerToken_NotFound", async ct =>
            {
                Credential read = await driver.Credential.ReadByBearerTokenAsync("nonexistent_token_value", ct);
                AssertHelper.IsNull(read, "non-existent bearer token");
            }, token);

            await runner.RunTestAsync("Credential.Read_NotFound", async ct =>
            {
                Credential read = await driver.Credential.ReadAsync("cred_nonexistent", ct);
                AssertHelper.IsNull(read, "non-existent credential");
            }, token);

            await runner.RunTestAsync("Credential.Exists_True", async ct =>
            {
                bool exists = await driver.Credential.ExistsAsync(createdId, ct);
                AssertHelper.IsTrue(exists, "credential should exist");
            }, token);

            await runner.RunTestAsync("Credential.Exists_False", async ct =>
            {
                bool exists = await driver.Credential.ExistsAsync("cred_nonexistent", ct);
                AssertHelper.IsFalse(exists, "non-existent credential should not exist");
            }, token);

            await runner.RunTestAsync("Credential.Update", async ct =>
            {
                Credential read = await driver.Credential.ReadAsync(createdId, ct);
                read.Name = "Updated Credential";
                read.Active = false;

                Credential updated = await driver.Credential.UpdateAsync(read, ct);
                AssertHelper.IsNotNull(updated, "updated credential");
                AssertHelper.AreEqual(createdId, updated.Id, "Id");
                AssertHelper.AreEqual("Updated Credential", updated.Name, "Name");
                AssertHelper.AreEqual(false, updated.Active, "Active");
                AssertHelper.AreEqual(userId, updated.UserId, "UserId");
                AssertHelper.AreEqual(bearerTokenValue, updated.BearerToken, "BearerToken preserved");
                AssertHelper.DateTimeRecent(updated.LastUpdateUtc, "LastUpdateUtc");
            }, token);

            await runner.RunTestAsync("Credential.Update_VerifyPersistence", async ct =>
            {
                Credential read = await driver.Credential.ReadAsync(createdId, ct);
                AssertHelper.AreEqual("Updated Credential", read.Name, "Name after re-read");
                AssertHelper.AreEqual(false, read.Active, "Active after re-read");
            }, token);

            await runner.RunTestAsync("Credential.Enumerate_Default", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery { MaxResults = 100 };
                EnumerationResult<Credential> result = await driver.Credential.EnumerateAsync(query, ct);
                AssertHelper.IsNotNull(result, "enumeration result");
                AssertHelper.IsTrue(result.Success, "enumeration success");
                AssertHelper.IsGreaterThanOrEqual(result.Objects.Count, 2, "objects count");
            }, token);

            await runner.RunTestAsync("Credential.Enumerate_Pagination", async ct =>
            {
                EnumerationQuery q1 = new EnumerationQuery { MaxResults = 1 };
                EnumerationResult<Credential> r1 = await driver.Credential.EnumerateAsync(q1, ct);
                AssertHelper.AreEqual(1, r1.Objects.Count, "page 1 count");
                AssertHelper.IsFalse(r1.EndOfResults, "page 1 not end");

                EnumerationQuery q2 = new EnumerationQuery { MaxResults = 1, ContinuationToken = r1.ContinuationToken };
                EnumerationResult<Credential> r2 = await driver.Credential.EnumerateAsync(q2, ct);
                AssertHelper.AreEqual(1, r2.Objects.Count, "page 2 count");
                AssertHelper.AreNotEqual(r1.Objects[0].Id, r2.Objects[0].Id, "different credentials on different pages");
            }, token);

            await runner.RunTestAsync("Credential.Delete", async ct =>
            {
                Credential cred = new Credential { UserId = userId, Name = "To Delete" };
                Credential created = await driver.Credential.CreateAsync(cred, ct);

                await driver.Credential.DeleteAsync(created.Id, ct);

                Credential read = await driver.Credential.ReadAsync(created.Id, ct);
                AssertHelper.IsNull(read, "deleted credential");
            }, token);

            await runner.RunTestAsync("Credential.DeleteByUserId", async ct =>
            {
                UserMaster tempUser = await driver.User.CreateAsync(new UserMaster { Email = "cred-delete-test@example.com" }, token);
                Credential c1 = await driver.Credential.CreateAsync(new Credential { UserId = tempUser.Id, Name = "C1" }, ct);
                Credential c2 = await driver.Credential.CreateAsync(new Credential { UserId = tempUser.Id, Name = "C2" }, ct);

                await driver.Credential.DeleteByUserIdAsync(tempUser.Id, ct);

                Credential r1 = await driver.Credential.ReadAsync(c1.Id, ct);
                Credential r2 = await driver.Credential.ReadAsync(c2.Id, ct);
                AssertHelper.IsNull(r1, "credential 1 after DeleteByUserId");
                AssertHelper.IsNull(r2, "credential 2 after DeleteByUserId");
            }, token);
        }
    }
}
