namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Services;
    using SyslogLogging;
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
                List<string> chunks = SlackAssistantUtilities.ChunkSlackMessage("short message", 50);
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

                List<string> chunks = SlackAssistantUtilities.ChunkSlackMessage(longText, 60);
                AssertHelper.IsTrue(chunks.Count >= 2, "multiple chunks expected");
                foreach (string chunk in chunks)
                {
                    AssertHelper.IsTrue(chunk.Length <= 60, "chunk should respect max length");
                }
            });

            await ExecuteTestAsync("SlackAssistantUtilities.ChunkSlackMessage: preserves combined content modulo trimming", async () =>
            {
                string input = "First paragraph\nSecond paragraph\nThird paragraph";
                List<string> chunks = SlackAssistantUtilities.ChunkSlackMessage(input, 18);
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
                    AssistantId = "asst_test",
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

                AssistantPerformanceStage rewriteStage = new AssistantPerformanceStage
                {
                    Name = "inference",
                    Kind = "inference",
                    DurationMs = 22,
                    EndpointId = "cep_rewrite",
                    EndpointName = "rewrite-local",
                    EndpointType = "completion",
                    Provider = "Ollama",
                    ApiFormat = "Ollama",
                    Model = "gemma3:4b",
                    ClientTimings = new AssistantPerformanceClientTimings
                    {
                        EndpointLimiterWaitMs = 2,
                        RequestToHeadersMs = 15,
                        TotalMs = 22
                    }
                };

                AssistantPerformanceStage rerankStage = new AssistantPerformanceStage
                {
                    Name = "inference",
                    Kind = "inference",
                    DurationMs = 44,
                    EndpointId = "cep_rerank",
                    EndpointName = "rerank-local",
                    EndpointType = "completion",
                    Provider = "Ollama",
                    ApiFormat = "Ollama",
                    Model = "gemma3:4b",
                    ClientTimings = new AssistantPerformanceClientTimings
                    {
                        EndpointLimiterWaitMs = 3,
                        RequestToHeadersMs = 25,
                        TotalMs = 44
                    }
                };

                AssistantPerformanceTelemetry telemetry = AssistantPerformanceTelemetryBuilder.Build(
                    history,
                    finalStage,
                    3,
                    8,
                    null,
                    rewriteStage,
                    rerankStage);
                string json = AssistantPerformanceTelemetryBuilder.Serialize(telemetry);
                List<ChatHistoryPerformanceEvent> events = AssistantPerformanceTelemetryBuilder.ToEvents(telemetry, history.TenantId);

                AssertHelper.StringContains(json, "final_inference", "telemetry JSON");
                AssertHelper.IsTrue(events.Count >= 5, "legacy and final stages projected");

                ChatHistoryPerformanceEvent finalEvent = events.Find(evt => evt.Stage == "final_inference");
                AssertHelper.IsNotNull(finalEvent, "final inference event");
                AssertHelper.AreEqual("asst_test", finalEvent.AssistantId, "AssistantId");
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

                ChatHistoryPerformanceEvent rewriteEvent = events.Find(evt => evt.Stage == "query_rewrite");
                AssertHelper.IsNotNull(rewriteEvent, "query rewrite event");
                AssertHelper.AreEqual("cep_rewrite", rewriteEvent.EndpointId, "QueryRewrite EndpointId");
                AssertHelper.AreEqual("Ollama", rewriteEvent.Provider, "QueryRewrite Provider");
                AssertHelper.AreEqual("gemma3:4b", rewriteEvent.Model, "QueryRewrite Model");
                AssertHelper.AreEqual(2.0, rewriteEvent.EndpointLimiterWaitMs.Value, "QueryRewrite EndpointLimiterWaitMs");

                ChatHistoryPerformanceEvent rerankEvent = events.Find(evt => evt.Stage == "rerank");
                AssertHelper.IsNotNull(rerankEvent, "rerank event");
                AssertHelper.AreEqual("cep_rerank", rerankEvent.EndpointId, "Rerank EndpointId");
                AssertHelper.AreEqual("Ollama", rerankEvent.Provider, "Rerank Provider");
                AssertHelper.AreEqual("gemma3:4b", rerankEvent.Model, "Rerank Model");
                AssertHelper.AreEqual(3.0, rerankEvent.EndpointLimiterWaitMs.Value, "Rerank EndpointLimiterWaitMs");
                AssertHelper.AreEqual(5, rerankEvent.ChunksInput.Value, "Rerank ChunksInput");
                AssertHelper.AreEqual(2, rerankEvent.ChunksOutput.Value, "Rerank ChunksOutput");
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

            await ExecuteTestAsync("SQLite telemetry backfill: materializes analytics event rows", async () =>
            {
                string dbPath = Path.Combine(Path.GetTempPath(), "assistanthub_telemetry_" + Guid.NewGuid().ToString("N") + ".db");
                DatabaseDriverBase database = null;
                try
                {
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;
                    database = await DatabaseDriverFactory.CreateAndInitializeAsync(new DatabaseSettings
                    {
                        Type = DatabaseTypeEnum.Sqlite,
                        Filename = dbPath
                    }, logging).ConfigureAwait(false);

                    string tenantId = "ten_telemetry";
                    string assistantId = "asst_telemetry";
                    DateTime createdUtc = DateTime.UtcNow.AddMinutes(-5);

                    await database.Tenant.CreateAsync(new TenantMetadata
                    {
                        Id = tenantId,
                        Name = "Telemetry Tenant"
                    }).ConfigureAwait(false);

                    ChatHistory history = new ChatHistory
                    {
                        TenantId = tenantId,
                        AssistantId = assistantId,
                        ThreadId = "thr_telemetry",
                        TraceId = "trace_telemetry",
                        RequestHistoryId = "req_telemetry",
                        UserMessage = "hello",
                        AssistantResponse = "world",
                        CreatedUtc = createdUtc,
                        LastUpdateUtc = createdUtc,
                        PerformanceJson = AssistantPerformanceTelemetryBuilder.Serialize(new AssistantPerformanceTelemetry
                        {
                            TraceId = "trace_telemetry",
                            ChatHistoryId = null,
                            RequestHistoryId = "req_telemetry",
                            AssistantId = assistantId,
                            CreatedUtc = createdUtc,
                            Stages = new List<AssistantPerformanceStage>
                            {
                                new AssistantPerformanceStage
                                {
                                    Name = "retrieval",
                                    Kind = "retrieval",
                                    Sequence = 30,
                                    DurationMs = 50,
                                    Success = true,
                                    Metadata = new Dictionary<string, object>
                                    {
                                        ["retrieval_query_count"] = 2,
                                        ["chunks_output"] = 3
                                    }
                                },
                                new AssistantPerformanceStage
                                {
                                    Name = "final_inference",
                                    Kind = "inference",
                                    Sequence = 70,
                                    EndpointId = "cep_telemetry",
                                    EndpointName = "Test endpoint",
                                    EndpointType = "completion",
                                    Provider = "Ollama",
                                    ApiFormat = "Ollama",
                                    Model = "gemma3:4b",
                                    DurationMs = 250,
                                    Success = true
                                }
                            }
                        })
                    };

                    history = await database.ChatHistory.CreateAsync(history).ConfigureAwait(false);
                    AssistantPerformanceTelemetryBackfillService backfill = new AssistantPerformanceTelemetryBackfillService(database, logging);
                    int inserted = await backfill.BackfillMissingEventsAsync().ConfigureAwait(false);
                    List<ChatHistoryPerformanceEvent> events = await database.ChatHistoryPerformanceEvent.ListByChatHistoryIdAsync(history.Id).ConfigureAwait(false);

                    AssertHelper.AreEqual(2, inserted, "backfilled event count");
                    AssertHelper.HasCount(events, 2, "sqlite persisted events");
                    AssertHelper.AreEqual(assistantId, events[0].AssistantId, "event AssistantId");
                    AssertHelper.AreEqual(history.Id, events[0].ChatHistoryId, "event ChatHistoryId");

                    AssistantAnalyticsService analytics = new AssistantAnalyticsService(database);
                    AssistantAnalyticsStageResult stages = await analytics.GetStagesAsync(new AssistantAnalyticsFilter
                    {
                        TenantId = tenantId,
                        AssistantId = assistantId,
                        StartUtc = createdUtc.AddMinutes(-1),
                        EndUtc = createdUtc.AddMinutes(10),
                        BucketSeconds = 600
                    }).ConfigureAwait(false);

                    AssistantAnalyticsStageBucket finalStage = stages.Buckets.Find(bucket => bucket.Stage == "final_inference");
                    AssertHelper.IsNotNull(finalStage, "analytics final stage bucket");
                    AssertHelper.AreEqual(1, finalStage.Calls, "analytics final stage calls");
                    AssertHelper.AreEqual(250.0, finalStage.AverageDurationMs.Value, "analytics final stage duration");
                }
                finally
                {
                    IDisposable disposable = database as IDisposable;
                    disposable?.Dispose();
                    TryDeleteFile(dbPath);
                    TryDeleteFile(dbPath + "-wal");
                    TryDeleteFile(dbPath + "-shm");
                }
            });

            await ExecuteTestAsync("AssistantAnalyticsService.ResolveRange: caps explicit bucket count", async () =>
            {
                AssistantAnalyticsService service = new AssistantAnalyticsService(new MockDatabaseDriver());
                AssistantAnalyticsRange range = service.ResolveRange(new AssistantAnalyticsFilter
                {
                    TenantId = "ten_test",
                    AssistantId = "asst_test",
                    StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    BucketSeconds = 1
                });

                AssertHelper.AreEqual("custom", range.RangeId, "custom range id");
                AssertHelper.AreEqual(360, range.BucketSeconds, "capped bucket seconds");
                AssertHelper.AreEqual(240, range.BucketCount, "capped bucket count");
            });

            await ExecuteTestAsync("AssistantAnalyticsService: aggregates requests, stages, endpoints, slowest rows, and feedback", async () =>
            {
                DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                AssistantAnalyticsFilter filter = new AssistantAnalyticsFilter
                {
                    TenantId = "ten_test",
                    AssistantId = "asst_test",
                    StartUtc = start,
                    EndUtc = start.AddMinutes(10),
                    BucketSeconds = 300,
                    Metrics = new List<string> { "request_count", "final_inference_calls" },
                    Limit = 10
                };

                AssistantAnalyticsService service = new AssistantAnalyticsService(new AnalyticsDatabaseDriver(start));

                AssistantAnalyticsOverviewResult overview = await service.GetOverviewAsync(filter).ConfigureAwait(false);
                AssertHelper.AreEqual(2, overview.RequestCount, "overview RequestCount");
                AssertHelper.AreEqual(1, overview.SuccessCount, "overview SuccessCount");
                AssertHelper.AreEqual(1, overview.FailureCount, "overview FailureCount");
                AssertHelper.AreEqual(1500.0, overview.AverageDurationMs.Value, "overview AverageDurationMs");
                AssertHelper.AreEqual(1.0, overview.TelemetryCoverageRate.Value, "overview TelemetryCoverageRate");
                AssertHelper.AreEqual("final_inference", overview.DominantStage, "overview DominantStage");
                AssertHelper.AreEqual("cep_final", overview.TopEndpointId, "overview TopEndpointId");
                AssertHelper.AreEqual(0.5, overview.NegativeFeedbackRate.Value, "overview NegativeFeedbackRate");

                AssistantAnalyticsTimeSeriesResult timeSeries = await service.GetTimeSeriesAsync(filter).ConfigureAwait(false);
                AssertHelper.HasCount(timeSeries.Series, 2, "filtered analytics series");
                AssistantAnalyticsSeries requestCount = timeSeries.Series.Find(series => series.Metric == "request_count");
                AssistantAnalyticsSeries finalCalls = timeSeries.Series.Find(series => series.Metric == "final_inference_calls");
                AssertHelper.IsNotNull(requestCount, "request_count series");
                AssertHelper.IsNotNull(finalCalls, "final_inference_calls series");
                AssertHelper.AreEqual(1.0, requestCount.Points[0].Value.Value, "first bucket request count");
                AssertHelper.AreEqual(1.0, requestCount.Points[1].Value.Value, "second bucket request count");
                AssertHelper.AreEqual(1.0, finalCalls.Points[0].Value.Value, "first bucket final inference calls");
                AssertHelper.AreEqual(1.0, finalCalls.Points[1].Value.Value, "second bucket final inference calls");

                AssistantAnalyticsStageResult stages = await service.GetStagesAsync(filter).ConfigureAwait(false);
                AssistantAnalyticsStageBucket firstFinalStage = stages.Buckets.Find(bucket => bucket.Stage == "final_inference" && bucket.Calls == 1);
                AssertHelper.IsNotNull(firstFinalStage, "final inference stage bucket");
                AssertHelper.AreEqual("inference", firstFinalStage.Kind, "final inference stage kind");

                AssistantAnalyticsEndpointResult endpoints = await service.GetEndpointsAsync(filter).ConfigureAwait(false);
                AssertHelper.HasCount(endpoints.Endpoints, 1, "endpoint summaries");
                AssertHelper.AreEqual(2, endpoints.Endpoints[0].Calls, "endpoint calls");
                AssertHelper.AreEqual(15.0, endpoints.Endpoints[0].AverageLimiterWaitMs.Value, "endpoint average limiter wait");
                AssertHelper.AreEqual(30, endpoints.Endpoints[0].InputTokens, "endpoint input tokens");
                AssertHelper.AreEqual(15, endpoints.Endpoints[0].OutputTokens, "endpoint output tokens");

                AssistantAnalyticsSlowestResult slowest = await service.GetSlowestAsync(filter).ConfigureAwait(false);
                AssertHelper.HasCount(slowest.Requests, 2, "slowest requests");
                AssertHelper.AreEqual("req_2", slowest.Requests[0].RequestHistoryId, "slowest request id");
                AssertHelper.AreEqual(2000.0, slowest.Requests[0].DurationMs, "slowest duration");
                AssertHelper.AreEqual("final_inference", slowest.Requests[0].DominantStage, "slowest dominant stage");

                AssistantAnalyticsFeedbackResult feedback = await service.GetFeedbackAsync(filter).ConfigureAwait(false);
                AssertHelper.AreEqual(2, feedback.TotalCount, "feedback total");
                AssertHelper.AreEqual(1, feedback.ThumbsUpCount, "feedback thumbs up");
                AssertHelper.AreEqual(1, feedback.ThumbsDownCount, "feedback thumbs down");
                AssertHelper.AreEqual(0.5, feedback.NegativeRate.Value, "feedback negative rate");
                AssertHelper.HasCount(feedback.Buckets, 2, "feedback buckets");
            });

            await ExecuteTestAsync("AssistantAnalyticsService: slowest requests recover zero request durations", async () =>
            {
                DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                AssistantAnalyticsFilter filter = new AssistantAnalyticsFilter
                {
                    TenantId = "ten_test",
                    AssistantId = "asst_test",
                    StartUtc = start,
                    EndUtc = start.AddMinutes(10),
                    BucketSeconds = 300,
                    Limit = 10
                };

                AssistantAnalyticsService service = new AssistantAnalyticsService(new AnalyticsDatabaseDriver(start, "1", "0", true));

                AssistantAnalyticsSlowestResult slowest = await service.GetSlowestAsync(filter).ConfigureAwait(false);
                AssertHelper.HasCount(slowest.Requests, 2, "slowest fallback requests");
                AssertHelper.AreEqual("req_2", slowest.Requests[0].RequestHistoryId, "fallback slowest request id");
                AssertHelper.AreEqual(1900.0, slowest.Requests[0].DurationMs, "fallback slowest duration");
                AssertHelper.AreEqual("final_inference", slowest.Requests[0].DominantStage, "fallback dominant stage");
            });

            await ExecuteTestAsync("AssistantAnalyticsService: uses database boolean formatting for retained-chat fallback", async () =>
            {
                DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                AssistantAnalyticsFilter filter = new AssistantAnalyticsFilter
                {
                    TenantId = "ten_test",
                    AssistantId = "asst_test",
                    StartUtc = start,
                    EndUtc = start.AddMinutes(10),
                    BucketSeconds = 300,
                    Metrics = new List<string> { "request_count" },
                    Limit = 10
                };

                Dictionary<string, string> providerTrueLiterals = new Dictionary<string, string>
                {
                    ["sqlite"] = "1",
                    ["mysql"] = "1",
                    ["postgresql"] = "1",
                    ["sqlserver"] = "1"
                };

                foreach (KeyValuePair<string, string> provider in providerTrueLiterals)
                {
                    AnalyticsDatabaseDriver driver = new AnalyticsDatabaseDriver(start, provider.Value, "0");
                    AssistantAnalyticsService service = new AssistantAnalyticsService(driver);
                    AssistantAnalyticsOverviewResult overview = await service.GetOverviewAsync(filter).ConfigureAwait(false);
                    AssertHelper.AreEqual(2, overview.RequestCount, provider.Key + " request count");

                    string requestQuery = driver.Queries.FirstOrDefault(query => query.Contains("LEFT JOIN request_history r", StringComparison.OrdinalIgnoreCase));
                    AssertHelper.IsNotNull(requestQuery, provider.Key + " request analytics query");
                    AssertHelper.StringContains(requestQuery, "CASE WHEN r.id IS NULL THEN " + provider.Value + " ELSE r.success END AS success", provider.Key + " success fallback");
                    AssertHelper.StringContains(requestQuery, "CASE WHEN r.duration_ms IS NULL OR r.duration_ms <= 0 THEN", provider.Key + " duration fallback case");
                    AssertHelper.StringContains(requestQuery, "COALESCE(h.time_to_last_token_ms, 0)", provider.Key + " chat duration null guard");
                }
            });

            return GetResults();
        }

        private static void TryDeleteFile(string filename)
        {
            try
            {
                if (File.Exists(filename)) File.Delete(filename);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
