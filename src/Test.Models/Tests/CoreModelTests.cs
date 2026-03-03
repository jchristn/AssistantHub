namespace Test.Models.Tests
{
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using Test.Common;

    public static class CoreModelTests
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("CoreModelTests");

            // TenantMetadata
            await runner.RunTestAsync("Model.TenantMetadata: defaults and JSON round-trip", async ct =>
            {
                var t = new TenantMetadata();
                AssertHelper.IsNotNull(t.Id, "Id");
                AssertHelper.StartsWith(t.Id, "ten_", "Id prefix");
                AssertHelper.AreEqual("My Tenant", t.Name, "default Name");
                AssertHelper.AreEqual(true, t.Active, "default Active");
                AssertHelper.DateTimeRecent(t.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(t.LastUpdateUtc, "LastUpdateUtc");

                // JSON round-trip
                t.Name = "Test Tenant";
                string json = JsonSerializer.Serialize(t, _jsonOptions);
                var deserialized = JsonSerializer.Deserialize<TenantMetadata>(json, _jsonOptions);
                AssertHelper.AreEqual(t.Id, deserialized.Id, "round-trip Id");
                AssertHelper.AreEqual("Test Tenant", deserialized.Name, "round-trip Name");
            }, token);

            // UserMaster
            await runner.RunTestAsync("Model.UserMaster: defaults and password", async ct =>
            {
                var u = new UserMaster();
                AssertHelper.IsNotNull(u.Id, "Id");
                AssertHelper.StartsWith(u.Id, "usr_", "Id prefix");
                AssertHelper.AreEqual(Constants.DefaultTenantId, u.TenantId, "default TenantId");
                AssertHelper.AreEqual("user@example.com", u.Email, "default Email");
                AssertHelper.AreEqual(false, u.IsAdmin, "default IsAdmin");
                AssertHelper.AreEqual(true, u.Active, "default Active");
            }, token);

            await runner.RunTestAsync("Model.UserMaster: SetPassword and VerifyPassword", async ct =>
            {
                var u = new UserMaster();
                u.SetPassword("mysecret");
                AssertHelper.IsNotNull(u.PasswordSha256, "PasswordSha256 set");
                AssertHelper.IsTrue(u.VerifyPassword("mysecret"), "correct password matches");
                AssertHelper.IsFalse(u.VerifyPassword("wrongpassword"), "wrong password does not match");
                AssertHelper.IsFalse(u.VerifyPassword(""), "empty password does not match");
            }, token);

            await runner.RunTestAsync("Model.UserMaster: password not serialized as plain text", async ct =>
            {
                var u = new UserMaster();
                u.SetPassword("mysecret");
                string json = JsonSerializer.Serialize(u, _jsonOptions);
                AssertHelper.IsFalse(json.Contains("mysecret"), "plain text password should not appear in JSON");
            }, token);

            // Credential
            await runner.RunTestAsync("Model.Credential: defaults and bearer token", async ct =>
            {
                var c = new Credential();
                AssertHelper.IsNotNull(c.Id, "Id");
                AssertHelper.StartsWith(c.Id, "cred_", "Id prefix");
                AssertHelper.IsNotNull(c.BearerToken, "BearerToken");
                AssertHelper.IsTrue(c.BearerToken.Length > 0, "BearerToken has length");
                AssertHelper.AreEqual(true, c.Active, "default Active");

                // Each credential gets unique bearer token
                var c2 = new Credential();
                AssertHelper.AreNotEqual(c.BearerToken, c2.BearerToken, "unique bearer tokens");
            }, token);

            // Assistant
            await runner.RunTestAsync("Model.Assistant: defaults and JSON round-trip", async ct =>
            {
                var a = new Assistant();
                AssertHelper.IsNotNull(a.Id, "Id");
                AssertHelper.StartsWith(a.Id, "asst_", "Id prefix");
                AssertHelper.AreEqual("My Assistant", a.Name, "default Name");
                AssertHelper.AreEqual(true, a.Active, "default Active");

                a.Name = "Test Assistant";
                a.Description = "A test assistant";
                string json = JsonSerializer.Serialize(a, _jsonOptions);
                var d = JsonSerializer.Deserialize<Assistant>(json, _jsonOptions);
                AssertHelper.AreEqual("Test Assistant", d.Name, "round-trip Name");
                AssertHelper.AreEqual("A test assistant", d.Description, "round-trip Description");
            }, token);

            // AssistantDocument
            await runner.RunTestAsync("Model.AssistantDocument: defaults and status", async ct =>
            {
                var doc = new AssistantDocument();
                AssertHelper.IsNotNull(doc.Id, "Id");
                AssertHelper.StartsWith(doc.Id, "adoc_", "Id prefix");
                AssertHelper.AreEqual("Untitled Document", doc.Name, "default Name");
                AssertHelper.AreEqual(DocumentStatusEnum.Pending, doc.Status, "default Status");
                AssertHelper.AreEqual("application/octet-stream", doc.ContentType, "default ContentType");
                AssertHelper.AreEqual(0L, doc.SizeBytes, "default SizeBytes");

                // Status transitions
                doc.Status = DocumentStatusEnum.Processing;
                AssertHelper.AreEqual(DocumentStatusEnum.Processing, doc.Status, "Status after set");
                doc.Status = DocumentStatusEnum.Completed;
                AssertHelper.AreEqual(DocumentStatusEnum.Completed, doc.Status, "Status completed");
            }, token);

            // AssistantFeedback
            await runner.RunTestAsync("Model.AssistantFeedback: defaults and rating", async ct =>
            {
                var fb = new AssistantFeedback();
                AssertHelper.IsNotNull(fb.Id, "Id");
                AssertHelper.StartsWith(fb.Id, "afb_", "Id prefix");
                AssertHelper.AreEqual(FeedbackRatingEnum.ThumbsUp, fb.Rating, "default Rating");

                fb.Rating = FeedbackRatingEnum.ThumbsDown;
                string json = JsonSerializer.Serialize(fb, _jsonOptions);
                var d = JsonSerializer.Deserialize<AssistantFeedback>(json, _jsonOptions);
                AssertHelper.AreEqual(FeedbackRatingEnum.ThumbsDown, d.Rating, "round-trip Rating");
            }, token);

            // ChatHistory
            await runner.RunTestAsync("Model.ChatHistory: defaults", async ct =>
            {
                var ch = new ChatHistory();
                AssertHelper.IsNotNull(ch.Id, "Id");
                AssertHelper.StartsWith(ch.Id, "chist_", "Id prefix");
                AssertHelper.AreEqual(Constants.DefaultTenantId, ch.TenantId, "default TenantId");
                AssertHelper.DateTimeRecent(ch.CreatedUtc, "CreatedUtc");
            }, token);

            // IngestionRule
            await runner.RunTestAsync("Model.IngestionRule: defaults", async ct =>
            {
                var rule = new IngestionRule();
                AssertHelper.IsNotNull(rule.Id, "Id");
                AssertHelper.StartsWith(rule.Id, "irule_", "Id prefix");
                AssertHelper.AreEqual("Untitled Rule", rule.Name, "default Name");
                AssertHelper.AreEqual("default", rule.Bucket, "default Bucket");
                AssertHelper.AreEqual("default", rule.CollectionName, "default CollectionName");
            }, token);

            // CrawlPlan
            await runner.RunTestAsync("Model.CrawlPlan: defaults and nested objects", async ct =>
            {
                var plan = new CrawlPlan();
                AssertHelper.IsNotNull(plan.Id, "Id");
                AssertHelper.StartsWith(plan.Id, "cplan_", "Id prefix");
                AssertHelper.AreEqual("My crawl plan", plan.Name, "default Name");
                AssertHelper.AreEqual(RepositoryTypeEnum.Web, plan.RepositoryType, "default RepositoryType");
                AssertHelper.AreEqual(CrawlPlanStateEnum.Stopped, plan.State, "default State");
                AssertHelper.IsNotNull(plan.IngestionSettings, "IngestionSettings");
                AssertHelper.IsNotNull(plan.RepositorySettings, "RepositorySettings");
                AssertHelper.IsNotNull(plan.Schedule, "Schedule");
                AssertHelper.IsNotNull(plan.Filter, "Filter");
                AssertHelper.AreEqual(8, plan.MaxDrainTasks, "default MaxDrainTasks");
                AssertHelper.AreEqual(7, plan.RetentionDays, "default RetentionDays");
            }, token);

            // CrawlOperation
            await runner.RunTestAsync("Model.CrawlOperation: defaults and state", async ct =>
            {
                var op = new CrawlOperation();
                AssertHelper.IsNotNull(op.Id, "Id");
                AssertHelper.StartsWith(op.Id, "cop_", "Id prefix");
                AssertHelper.AreEqual(CrawlOperationStateEnum.NotStarted, op.State, "default State");
                AssertHelper.AreEqual(0L, op.ObjectsEnumerated, "default ObjectsEnumerated");
                AssertHelper.AreEqual(0L, op.BytesEnumerated, "default BytesEnumerated");
            }, token);

            // RetrievalChunk
            await runner.RunTestAsync("Model.RetrievalChunk: defaults and rerank_score JSON", async ct =>
            {
                var chunk = new RetrievalChunk();
                AssertHelper.IsNull(chunk.RerankScore, "default RerankScore");
                AssertHelper.AreEqual(0.0, chunk.Score, "default Score");
                AssertHelper.IsNull(chunk.DocumentId, "default DocumentId");
            }, token);
        }
    }
}
