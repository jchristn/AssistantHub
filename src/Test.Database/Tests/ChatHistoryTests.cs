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

    public static class ChatHistoryTests
    {
        public static async Task RunAllAsync(DatabaseDriverBase driver, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- ChatHistory Tests ---");

            string tenantId = TenantTests.TestTenantId;
            UserMaster user = await driver.User.CreateAsync(new UserMaster { TenantId = tenantId, Email = "chat-owner@example.com" }, token);
            Assistant asst = await driver.Assistant.CreateAsync(new Assistant { TenantId = tenantId, UserId = user.Id, Name = "Chat Test Asst" }, token);
            string assistantId = asst.Id;
            string threadId = IdGenerator.NewThreadId();
            string createdId = null;

            await runner.RunTestAsync("ChatHistory.Create", async ct =>
            {
                ChatHistory history = new ChatHistory
                {
                    TenantId = tenantId,
                    ThreadId = threadId,
                    AssistantId = assistantId,
                    CollectionId = "col_chat_123",
                    UserMessageUtc = DateTime.UtcNow,
                    UserMessage = "Hello, how are you?",
                    RetrievalStartUtc = DateTime.UtcNow,
                    RetrievalDurationMs = 45.5,
                    RetrievalGateDecision = "RETRIEVE",
                    RetrievalGateDurationMs = 12.3,
                    RetrievalContext = "Relevant context from documents...",
                    PromptSentUtc = DateTime.UtcNow,
                    PromptTokens = 150,
                    EndpointResolutionDurationMs = 5.2,
                    CompactionDurationMs = 3.1,
                    InferenceConnectionDurationMs = 8.7,
                    TimeToFirstTokenMs = 200.5,
                    TimeToLastTokenMs = 1500.3,
                    CompletionTokens = 85,
                    TokensPerSecondOverall = 56.7,
                    TokensPerSecondGeneration = 65.4,
                    AssistantResponse = "I'm doing well, thank you!"
                };

                ChatHistory created = await driver.ChatHistory.CreateAsync(history, ct);
                AssertHelper.IsNotNull(created, "created chat history");
                AssertHelper.IsNotNull(created.Id, "Id");
                AssertHelper.StartsWith(created.Id, "chist_", "Id prefix");
                AssertHelper.AreEqual(threadId, created.ThreadId, "ThreadId");
                AssertHelper.StartsWith(created.ThreadId, "thr_", "ThreadId prefix");
                AssertHelper.AreEqual(assistantId, created.AssistantId, "AssistantId");
                AssertHelper.AreEqual("col_chat_123", created.CollectionId, "CollectionId");
                AssertHelper.DateTimeRecent(created.UserMessageUtc, "UserMessageUtc");
                AssertHelper.AreEqual("Hello, how are you?", created.UserMessage, "UserMessage");
                AssertHelper.DateTimeNullableRecent(created.RetrievalStartUtc, "RetrievalStartUtc");
                AssertHelper.AreEqual(45.5, created.RetrievalDurationMs, "RetrievalDurationMs");
                AssertHelper.AreEqual("RETRIEVE", created.RetrievalGateDecision, "RetrievalGateDecision");
                AssertHelper.AreEqual(12.3, created.RetrievalGateDurationMs, "RetrievalGateDurationMs");
                AssertHelper.AreEqual("Relevant context from documents...", created.RetrievalContext, "RetrievalContext");
                AssertHelper.DateTimeNullableRecent(created.PromptSentUtc, "PromptSentUtc");
                AssertHelper.AreEqual(150, created.PromptTokens, "PromptTokens");
                AssertHelper.AreEqual(5.2, created.EndpointResolutionDurationMs, "EndpointResolutionDurationMs");
                AssertHelper.AreEqual(3.1, created.CompactionDurationMs, "CompactionDurationMs");
                AssertHelper.AreEqual(8.7, created.InferenceConnectionDurationMs, "InferenceConnectionDurationMs");
                AssertHelper.AreEqual(200.5, created.TimeToFirstTokenMs, "TimeToFirstTokenMs");
                AssertHelper.AreEqual(1500.3, created.TimeToLastTokenMs, "TimeToLastTokenMs");
                AssertHelper.AreEqual(85, created.CompletionTokens, "CompletionTokens");
                AssertHelper.AreEqual(56.7, created.TokensPerSecondOverall, "TokensPerSecondOverall");
                AssertHelper.AreEqual(65.4, created.TokensPerSecondGeneration, "TokensPerSecondGeneration");
                AssertHelper.AreEqual("I'm doing well, thank you!", created.AssistantResponse, "AssistantResponse");
                AssertHelper.DateTimeRecent(created.CreatedUtc, "CreatedUtc");
                AssertHelper.DateTimeRecent(created.LastUpdateUtc, "LastUpdateUtc");
                createdId = created.Id;
            }, token);

            await runner.RunTestAsync("ChatHistory.Create_Minimal", async ct =>
            {
                ChatHistory history = new ChatHistory
                {
                    TenantId = tenantId,
                    ThreadId = IdGenerator.NewThreadId(),
                    AssistantId = assistantId
                };

                ChatHistory created = await driver.ChatHistory.CreateAsync(history, ct);
                AssertHelper.IsNotNull(created, "created minimal");
                AssertHelper.IsNull(created.CollectionId, "CollectionId");
                AssertHelper.IsNull(created.UserMessage, "UserMessage");
                AssertHelper.IsNull(created.RetrievalStartUtc, "RetrievalStartUtc");
                AssertHelper.AreEqual(0.0, created.RetrievalDurationMs, "RetrievalDurationMs default");
                AssertHelper.IsNull(created.RetrievalGateDecision, "RetrievalGateDecision");
                AssertHelper.AreEqual(0.0, created.RetrievalGateDurationMs, "RetrievalGateDurationMs default");
                AssertHelper.IsNull(created.RetrievalContext, "RetrievalContext");
                AssertHelper.IsNull(created.PromptSentUtc, "PromptSentUtc");
                AssertHelper.AreEqual(0, created.PromptTokens, "PromptTokens default");
                AssertHelper.AreEqual(0.0, created.EndpointResolutionDurationMs, "EndpointResolutionDurationMs default");
                AssertHelper.AreEqual(0.0, created.CompactionDurationMs, "CompactionDurationMs default");
                AssertHelper.AreEqual(0.0, created.InferenceConnectionDurationMs, "InferenceConnectionDurationMs default");
                AssertHelper.AreEqual(0.0, created.TimeToFirstTokenMs, "TimeToFirstTokenMs default");
                AssertHelper.AreEqual(0.0, created.TimeToLastTokenMs, "TimeToLastTokenMs default");
                AssertHelper.AreEqual(0, created.CompletionTokens, "CompletionTokens default");
                AssertHelper.AreEqual(0.0, created.TokensPerSecondOverall, "TokensPerSecondOverall default");
                AssertHelper.AreEqual(0.0, created.TokensPerSecondGeneration, "TokensPerSecondGeneration default");
                AssertHelper.IsNull(created.AssistantResponse, "AssistantResponse");
            }, token);

            await runner.RunTestAsync("ChatHistory.Create_SkipGate", async ct =>
            {
                ChatHistory history = new ChatHistory
                {
                    TenantId = tenantId,
                    ThreadId = IdGenerator.NewThreadId(),
                    AssistantId = assistantId,
                    RetrievalGateDecision = "SKIP",
                    UserMessage = "Simple greeting"
                };

                ChatHistory created = await driver.ChatHistory.CreateAsync(history, ct);
                AssertHelper.AreEqual("SKIP", created.RetrievalGateDecision, "RetrievalGateDecision SKIP");
            }, token);

            await runner.RunTestAsync("ChatHistory.Read", async ct =>
            {
                ChatHistory read = await driver.ChatHistory.ReadAsync(createdId, ct);
                AssertHelper.IsNotNull(read, "read chat history");
                AssertHelper.AreEqual(createdId, read.Id, "Id");
                AssertHelper.AreEqual(threadId, read.ThreadId, "ThreadId");
                AssertHelper.AreEqual(assistantId, read.AssistantId, "AssistantId");
                AssertHelper.AreEqual("col_chat_123", read.CollectionId, "CollectionId");
                AssertHelper.AreEqual("Hello, how are you?", read.UserMessage, "UserMessage");
                AssertHelper.AreEqual(45.5, read.RetrievalDurationMs, "RetrievalDurationMs");
                AssertHelper.AreEqual("RETRIEVE", read.RetrievalGateDecision, "RetrievalGateDecision");
                AssertHelper.AreEqual(12.3, read.RetrievalGateDurationMs, "RetrievalGateDurationMs");
                AssertHelper.AreEqual("Relevant context from documents...", read.RetrievalContext, "RetrievalContext");
                AssertHelper.AreEqual(150, read.PromptTokens, "PromptTokens");
                AssertHelper.AreEqual(5.2, read.EndpointResolutionDurationMs, "EndpointResolutionDurationMs");
                AssertHelper.AreEqual(3.1, read.CompactionDurationMs, "CompactionDurationMs");
                AssertHelper.AreEqual(8.7, read.InferenceConnectionDurationMs, "InferenceConnectionDurationMs");
                AssertHelper.AreEqual(200.5, read.TimeToFirstTokenMs, "TimeToFirstTokenMs");
                AssertHelper.AreEqual(1500.3, read.TimeToLastTokenMs, "TimeToLastTokenMs");
                AssertHelper.AreEqual(85, read.CompletionTokens, "CompletionTokens");
                AssertHelper.AreEqual(56.7, read.TokensPerSecondOverall, "TokensPerSecondOverall");
                AssertHelper.AreEqual(65.4, read.TokensPerSecondGeneration, "TokensPerSecondGeneration");
                AssertHelper.AreEqual("I'm doing well, thank you!", read.AssistantResponse, "AssistantResponse");
            }, token);

            await runner.RunTestAsync("ChatHistory.Read_NotFound", async ct =>
            {
                ChatHistory read = await driver.ChatHistory.ReadAsync("chist_nonexistent", ct);
                AssertHelper.IsNull(read, "non-existent chat history");
            }, token);

            await runner.RunTestAsync("ChatHistory.Enumerate_Default", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery { MaxResults = 100 };
                EnumerationResult<ChatHistory> result = await driver.ChatHistory.EnumerateAsync(tenantId, query, ct);
                AssertHelper.IsNotNull(result, "enumeration result");
                AssertHelper.IsTrue(result.Success, "success");
                AssertHelper.IsGreaterThanOrEqual(result.Objects.Count, 3, "objects count");
            }, token);

            await runner.RunTestAsync("ChatHistory.Enumerate_Pagination", async ct =>
            {
                EnumerationQuery q1 = new EnumerationQuery { MaxResults = 1 };
                EnumerationResult<ChatHistory> r1 = await driver.ChatHistory.EnumerateAsync(tenantId, q1, ct);
                AssertHelper.AreEqual(1, r1.Objects.Count, "page 1 count");
                AssertHelper.IsFalse(r1.EndOfResults, "page 1 not end");

                EnumerationQuery q2 = new EnumerationQuery { MaxResults = 1, ContinuationToken = r1.ContinuationToken };
                EnumerationResult<ChatHistory> r2 = await driver.ChatHistory.EnumerateAsync(tenantId, q2, ct);
                AssertHelper.AreEqual(1, r2.Objects.Count, "page 2 count");
                AssertHelper.AreNotEqual(r1.Objects[0].Id, r2.Objects[0].Id, "different items on pages");
            }, token);

            await runner.RunTestAsync("ChatHistory.Enumerate_AssistantIdFilter", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery
                {
                    MaxResults = 100,
                    AssistantIdFilter = assistantId
                };
                EnumerationResult<ChatHistory> result = await driver.ChatHistory.EnumerateAsync(tenantId, query, ct);
                AssertHelper.IsGreaterThanOrEqual(result.Objects.Count, 3, "filtered count");
                foreach (ChatHistory h in result.Objects)
                    AssertHelper.AreEqual(assistantId, h.AssistantId, "AssistantId filter");
            }, token);

            await runner.RunTestAsync("ChatHistory.Enumerate_ThreadIdFilter", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery
                {
                    MaxResults = 100,
                    ThreadIdFilter = threadId
                };
                EnumerationResult<ChatHistory> result = await driver.ChatHistory.EnumerateAsync(tenantId, query, ct);
                AssertHelper.IsGreaterThanOrEqual(result.Objects.Count, 1, "thread-filtered count");
                foreach (ChatHistory h in result.Objects)
                    AssertHelper.AreEqual(threadId, h.ThreadId, "ThreadId filter");
            }, token);

            await runner.RunTestAsync("ChatHistory.Enumerate_Ascending", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery
                {
                    MaxResults = 100,
                    Ordering = EnumerationOrderEnum.CreatedAscending
                };
                EnumerationResult<ChatHistory> result = await driver.ChatHistory.EnumerateAsync(tenantId, query, ct);
                for (int i = 1; i < result.Objects.Count; i++)
                    AssertHelper.IsTrue(result.Objects[i].CreatedUtc >= result.Objects[i - 1].CreatedUtc, $"ascending at {i}");
            }, token);

            await runner.RunTestAsync("ChatHistory.Enumerate_Descending", async ct =>
            {
                EnumerationQuery query = new EnumerationQuery
                {
                    MaxResults = 100,
                    Ordering = EnumerationOrderEnum.CreatedDescending
                };
                EnumerationResult<ChatHistory> result = await driver.ChatHistory.EnumerateAsync(tenantId, query, ct);
                for (int i = 1; i < result.Objects.Count; i++)
                    AssertHelper.IsTrue(result.Objects[i].CreatedUtc <= result.Objects[i - 1].CreatedUtc, $"descending at {i}");
            }, token);

            await runner.RunTestAsync("ChatHistory.Enumerate_AllPages", async ct =>
            {
                List<ChatHistory> all = new List<ChatHistory>();
                string contToken = null;

                while (true)
                {
                    EnumerationQuery query = new EnumerationQuery
                    {
                        MaxResults = 1,
                        ContinuationToken = contToken,
                        AssistantIdFilter = assistantId
                    };
                    EnumerationResult<ChatHistory> result = await driver.ChatHistory.EnumerateAsync(tenantId, query, ct);
                    all.AddRange(result.Objects);
                    if (result.EndOfResults) break;
                    contToken = result.ContinuationToken;
                }

                AssertHelper.IsGreaterThanOrEqual(all.Count, 3, "all chat history via full pagination");
            }, token);

            await runner.RunTestAsync("ChatHistory.Delete", async ct =>
            {
                ChatHistory history = await driver.ChatHistory.CreateAsync(new ChatHistory
                {
                    TenantId = tenantId,
                    ThreadId = IdGenerator.NewThreadId(),
                    AssistantId = assistantId,
                    UserMessage = "Delete me"
                }, ct);

                await driver.ChatHistory.DeleteAsync(history.Id, ct);

                ChatHistory read = await driver.ChatHistory.ReadAsync(history.Id, ct);
                AssertHelper.IsNull(read, "deleted chat history");
            }, token);

            await runner.RunTestAsync("ChatHistory.DeleteByAssistantId", async ct =>
            {
                Assistant tempAsst = await driver.Assistant.CreateAsync(new Assistant { TenantId = tenantId, UserId = user.Id, Name = "Chat Del Asst" }, ct);
                await driver.ChatHistory.CreateAsync(new ChatHistory { TenantId = tenantId, ThreadId = IdGenerator.NewThreadId(), AssistantId = tempAsst.Id, UserMessage = "M1" }, ct);
                await driver.ChatHistory.CreateAsync(new ChatHistory { TenantId = tenantId, ThreadId = IdGenerator.NewThreadId(), AssistantId = tempAsst.Id, UserMessage = "M2" }, ct);

                await driver.ChatHistory.DeleteByAssistantIdAsync(tempAsst.Id, ct);

                EnumerationQuery query = new EnumerationQuery { MaxResults = 100, AssistantIdFilter = tempAsst.Id };
                EnumerationResult<ChatHistory> result = await driver.ChatHistory.EnumerateAsync(tenantId, query, ct);
                AssertHelper.AreEqual(0, result.Objects.Count, "all chat history deleted by assistant id");
            }, token);

            await runner.RunTestAsync("ChatHistory.DeleteExpired", async ct =>
            {
                // create entries, then try to delete with a very large retention (should keep all)
                long countBefore = 0;
                EnumerationQuery q = new EnumerationQuery { MaxResults = 1, AssistantIdFilter = assistantId };
                EnumerationResult<ChatHistory> r = await driver.ChatHistory.EnumerateAsync(tenantId, q, ct);
                countBefore = r.TotalRecords;

                await driver.ChatHistory.DeleteExpiredAsync(9999, ct);

                EnumerationResult<ChatHistory> rAfter = await driver.ChatHistory.EnumerateAsync(tenantId, q, ct);
                AssertHelper.AreEqual(countBefore, rAfter.TotalRecords, "no records deleted with large retention");
            }, token);

            await runner.RunTestAsync("ChatHistory.DeleteExpired_ActualDeletion", async ct =>
            {
                // delete with 0-day retention should delete everything
                Assistant tempAsst2 = await driver.Assistant.CreateAsync(new Assistant { TenantId = tenantId, UserId = user.Id, Name = "Expire Asst" }, ct);
                await driver.ChatHistory.CreateAsync(new ChatHistory { TenantId = tenantId, ThreadId = IdGenerator.NewThreadId(), AssistantId = tempAsst2.Id, UserMessage = "Old msg" }, ct);

                // wait briefly to ensure timestamp is in the past
                await Task.Delay(100, ct);
                await driver.ChatHistory.DeleteExpiredAsync(0, ct);

                EnumerationQuery query = new EnumerationQuery { MaxResults = 100, AssistantIdFilter = tempAsst2.Id };
                EnumerationResult<ChatHistory> result = await driver.ChatHistory.EnumerateAsync(tenantId, query, ct);
                AssertHelper.AreEqual(0, result.Objects.Count, "expired records deleted with 0-day retention");
            }, token);
        }
    }
}
