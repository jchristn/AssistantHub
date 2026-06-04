namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Server.Services;
    using Test.Shared;

    public class ServiceSuite : SuiteBase
    {
        public async Task<IReadOnlyList<AutomatedTestResult>> RunAsync()
        {
            ClearResults();

            await ExecuteTestAsync("SlackAssistantUtilities.BuildThreadId: deterministic for same input", async () =>
            {
                string a = SlackAssistantUtilities.BuildThreadId("asst_1", "C123", "171234.567");
                string b = SlackAssistantUtilities.BuildThreadId("asst_1", "C123", "171234.567");
                AssertHelper.AreEqual(a, b, "deterministic thread id");
                AssertHelper.StartsWith(a, "thr_slack_", "thread id prefix");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.BuildThreadId: changes when coordinates change", async () =>
            {
                string a = SlackAssistantUtilities.BuildThreadId("asst_1", "C123", "171234.567");
                string b = SlackAssistantUtilities.BuildThreadId("asst_1", "C123", "171234.568");
                AssertHelper.AreNotEqual(a, b, "thread id uniqueness");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.StripSlackTrigger: removes configured prefix", async () =>
            {
                string result = SlackAssistantUtilities.StripSlackTrigger("Hey bot, summarize this", "Hey bot,", null);
                AssertHelper.AreEqual("summarize this", result, "prefix removed");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.StripSlackTrigger: removes bot mention", async () =>
            {
                string result = SlackAssistantUtilities.StripSlackTrigger("<@U123> summarize this", null, "U123");
                AssertHelper.AreEqual("summarize this", result, "mention removed");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.StripSlackTrigger: removes prefix and mention together", async () =>
            {
                string result = SlackAssistantUtilities.StripSlackTrigger("Hey bot, <@U123> summarize this", "Hey bot,", "U123");
                AssertHelper.AreEqual("summarize this", result, "prefix and mention removed");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.ShapeSlackText: flattens headers and links", async () =>
            {
                string input = "# Header\nSee [docs](https://example.com)";
                string shaped = SlackAssistantUtilities.ShapeSlackText(input);
                AssertHelper.IsFalse(shaped.Contains("# Header"), "header markers removed");
                AssertHelper.StringContains(shaped, "Header", "header text retained");
                AssertHelper.StringContains(shaped, "<https://example.com|docs>", "link converted");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.ShapeSlackText: preserves fenced code block content", async () =>
            {
                string input = "Before\n```csharp\n**literal**\n```\nAfter **bold**";
                string shaped = SlackAssistantUtilities.ShapeSlackText(input);
                AssertHelper.StringContains(shaped, "```csharp", "code fence retained");
                AssertHelper.StringContains(shaped, "**literal**", "code block content retained");
                AssertHelper.StringContains(shaped, "After *bold*", "non-code bold flattened");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.ChunkSlackMessage: returns single chunk for short message", async () =>
            {
                var chunks = SlackAssistantUtilities.ChunkSlackMessage("short message", 50);
                AssertHelper.HasCount(chunks, 1, "chunk count");
                AssertHelper.AreEqual("short message", chunks[0], "chunk content");
            });

            await ExecuteTestAsync("SlackAssistantUtilities.ChunkSlackMessage: splits long message on boundaries", async () =>
            {
                string longText = String.Join("\n", new[]
                {
                    new string('a', 40),
                    new string('b', 40),
                    new string('c', 40)
                });

                var chunks = SlackAssistantUtilities.ChunkSlackMessage(longText, 60);
                AssertHelper.IsTrue(chunks.Count >= 2, "multiple chunks expected");
                foreach (string chunk in chunks)
                {
                    AssertHelper.IsTrue(chunk.Length <= 60, "chunk should respect max length");
                }
            });

            await ExecuteTestAsync("SlackAssistantUtilities.ChunkSlackMessage: preserves combined content modulo trimming", async () =>
            {
                string input = "First paragraph\nSecond paragraph\nThird paragraph";
                var chunks = SlackAssistantUtilities.ChunkSlackMessage(input, 18);
                string recombined = String.Join(" ", chunks);
                AssertHelper.StringContains(recombined, "First paragraph", "first paragraph present");
                AssertHelper.StringContains(recombined, "Second paragraph", "second paragraph present");
                AssertHelper.StringContains(recombined, "Third paragraph", "third paragraph present");
            });

            await ExecuteTestAsync("EndpointConcurrencyLimiter: max one serializes same endpoint", async () =>
            {
                string key = "completion:test_" + Guid.NewGuid().ToString("N");
                IDisposable firstLease = await EndpointConcurrencyLimiter.AcquireAsync(key, 1).ConfigureAwait(false);

                try
                {
                    Task<IDisposable> secondAcquire = EndpointConcurrencyLimiter.AcquireAsync(key, 1);
                    await Task.Delay(50).ConfigureAwait(false);
                    AssertHelper.IsFalse(secondAcquire.IsCompleted, "second acquire should wait while first lease is held");

                    firstLease.Dispose();
                    firstLease = null;

                    IDisposable secondLease = await secondAcquire.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    secondLease.Dispose();
                }
                finally
                {
                    firstLease?.Dispose();
                }
            });

            await ExecuteTestAsync("AssistantPerformanceTelemetryBuilder: projects final inference metrics into event rows", async () =>
            {
                ChatHistory history = new ChatHistory
                {
                    Id = "chist_test",
                    TenantId = "ten_test",
                    TraceId = IdGenerator.NewTraceId(),
                    RequestHistoryId = "req_test",
                    RetrievalGateDecision = "RETRIEVE",
                    RetrievalGateDurationMs = 11,
                    QueryRewriteDurationMs = 22,
                    RetrievalStartUtc = DateTime.UtcNow.AddMilliseconds(-200),
                    RetrievalDurationMs = 33,
                    RerankDurationMs = 44,
                    RerankInputCount = 5,
                    RerankOutputCount = 2,
                    InferenceConnectionDurationMs = 100,
                    TimeToFirstTokenMs = 250,
                    TimeToLastTokenMs = 1000,
                    PromptTokens = 120,
                    CompletionTokens = 30
                };

                AssistantPerformanceStage finalStage = new AssistantPerformanceStage
                {
                    Name = "provider_call",
                    Kind = "inference",
                    DurationMs = 950,
                    EndpointId = "cep_test",
                    EndpointName = "local",
                    EndpointType = "inference",
                    Provider = "Ollama",
                    ApiFormat = "Ollama",
                    Model = "gemma3:4b",
                    ClientTimings = new AssistantPerformanceClientTimings
                    {
                        EndpointLimiterWaitMs = 7,
                        RequestToHeadersMs = 90,
                        HeadersToFirstTokenMs = 160,
                        FirstTokenToLastTokenMs = 700,
                        TotalMs = 950
                    },
                    ProviderMetrics = new AssistantProviderMetrics
                    {
                        LoadMs = 12,
                        PromptEvalMs = 140,
                        GenerationMs = 700,
                        TotalMs = 900,
                        TokensPerSecond = 42
                    }
                };

                AssistantPerformanceTelemetry telemetry = AssistantPerformanceTelemetryBuilder.Build(history, finalStage, 3, 8);
                string json = AssistantPerformanceTelemetryBuilder.Serialize(telemetry);
                List<ChatHistoryPerformanceEvent> events = AssistantPerformanceTelemetryBuilder.ToEvents(telemetry, history.TenantId);

                AssertHelper.StringContains(json, "final_inference", "telemetry JSON");
                AssertHelper.IsTrue(events.Count >= 5, "legacy and final stages projected");

                ChatHistoryPerformanceEvent finalEvent = events.Find(evt => evt.Stage == "final_inference");
                AssertHelper.IsNotNull(finalEvent, "final inference event");
                AssertHelper.AreEqual(history.TraceId, finalEvent.TraceId, "TraceId");
                AssertHelper.AreEqual("cep_test", finalEvent.EndpointId, "EndpointId");
                AssertHelper.AreEqual(7.0, finalEvent.EndpointLimiterWaitMs.Value, "EndpointLimiterWaitMs");
                AssertHelper.AreEqual(90.0, finalEvent.RequestToHeadersMs.Value, "RequestToHeadersMs");
                AssertHelper.AreEqual(12.0, finalEvent.ProviderLoadMs.Value, "ProviderLoadMs");
                AssertHelper.AreEqual(120, finalEvent.InputTokens.Value, "InputTokens");
                AssertHelper.AreEqual(30, finalEvent.OutputTokens.Value, "OutputTokens");

                ChatHistoryPerformanceEvent retrievalEvent = events.Find(evt => evt.Stage == "retrieval");
                AssertHelper.IsNotNull(retrievalEvent, "retrieval event");
                AssertHelper.AreEqual(3, retrievalEvent.RetrievalQueryCount.Value, "RetrievalQueryCount");
                AssertHelper.AreEqual(8, retrievalEvent.ChunksOutput.Value, "ChunksOutput");
            });

            await ExecuteTestAsync("MockDatabaseDriver: persists telemetry correlation and performance events", async () =>
            {
                MockDatabaseDriver db = new MockDatabaseDriver();
                ChatHistory history = await db.ChatHistory.CreateAsync(new ChatHistory
                {
                    TraceId = "trace_test",
                    RequestHistoryId = "req_test",
                    PerformanceJson = "{\"SchemaVersion\":1}"
                }).ConfigureAwait(false);

                RequestHistoryEntry request = await db.RequestHistory.CreateAsync(new RequestHistoryEntry
                {
                    Id = "req_test",
                    TraceId = history.TraceId,
                    ChatHistoryId = history.Id,
                    TenantId = history.TenantId
                }).ConfigureAwait(false);

                await db.ChatHistoryPerformanceEvent.CreateManyAsync(new[]
                {
                    new ChatHistoryPerformanceEvent
                    {
                        TenantId = history.TenantId,
                        ChatHistoryId = history.Id,
                        RequestHistoryId = request.Id,
                        TraceId = history.TraceId,
                        SequenceNumber = 70,
                        Stage = "final_inference",
                        DurationMs = 12
                    }
                }).ConfigureAwait(false);

                ChatHistory storedHistory = await db.ChatHistory.ReadAsync(history.Id).ConfigureAwait(false);
                RequestHistoryEntry storedRequest = await db.RequestHistory.ReadAsync(request.Id).ConfigureAwait(false);
                List<ChatHistoryPerformanceEvent> events = await db.ChatHistoryPerformanceEvent.ListByChatHistoryIdAsync(history.Id).ConfigureAwait(false);

                AssertHelper.AreEqual("trace_test", storedHistory.TraceId, "stored history TraceId");
                AssertHelper.AreEqual(history.Id, storedRequest.ChatHistoryId, "stored request ChatHistoryId");
                AssertHelper.HasCount(events, 1, "stored performance events");
                AssertHelper.AreEqual("final_inference", events[0].Stage, "stored event stage");
            });

            return GetResults();
        }
    }
}
