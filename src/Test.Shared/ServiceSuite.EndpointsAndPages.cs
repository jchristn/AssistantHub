namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Services;
    using Test.Shared;

    /// <summary>
    /// Tests for per-endpoint timeouts and page filters.
    /// </summary>
    public partial class ServiceSuite
    {
        private async Task RunEndpointAndPageTestsAsync()
        {
            #region Endpoint timeouts

            await ExecuteTestAsync("PartioEndpointMerge: timeout fields are stored as tags, cleared with 0, and never sent top-level", async () =>
            {
                string created = PartioEndpointMerge.BuildCreateBody("{\"Name\":\"slow\",\"Model\":\"m\",\"RequestTimeoutMs\":120000,\"UtilityTimeoutMs\":5000}");
                JsonObject body = JsonNode.Parse(created).AsObject();
                AssertHelper.IsFalse(body.ContainsKey("RequestTimeoutMs"), "RequestTimeoutMs not sent to Partio");
                AssertHelper.IsFalse(body.ContainsKey("UtilityTimeoutMs"), "UtilityTimeoutMs not sent to Partio");
                AssertHelper.AreEqual("120000", body["Tags"]?[PartioEndpointTimeouts.RequestTimeoutTag]?.GetValue<string>(), "request timeout tag");
                AssertHelper.AreEqual("5000", body["Tags"]?[PartioEndpointTimeouts.UtilityTimeoutTag]?.GetValue<string>(), "utility timeout tag");

                string updated = PartioEndpointMerge.BuildUpdateBody(created, "{\"RequestTimeoutMs\":0}");
                JsonObject after = JsonNode.Parse(updated).AsObject();
                AssertHelper.IsNull(after["Tags"]?[PartioEndpointTimeouts.RequestTimeoutTag], "0 clears the request timeout");
                AssertHelper.AreEqual("5000", after["Tags"]?[PartioEndpointTimeouts.UtilityTimeoutTag]?.GetValue<string>(), "an absent field keeps its tag");

                string untouched = PartioEndpointMerge.BuildUpdateBody(created, "{\"Name\":\"renamed\"}");
                AssertHelper.AreEqual("120000", JsonNode.Parse(untouched)["Tags"]?[PartioEndpointTimeouts.RequestTimeoutTag]?.GetValue<string>(), "other updates keep the timeout");

                string serialized = PartioEndpointToolMetadata.SerializePartioRequest(new PartioEndpointRequest { Model = "m", RequestTimeoutMs = 9000 });
                AssertHelper.IsFalse(serialized.Contains("RequestTimeoutMs", StringComparison.Ordinal), "serialized Partio request has no top-level timeout");
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("PartioEndpointTimeouts: validation, exposure in responses and the endpoint registry", async () =>
            {
                PartioEndpointTimeouts.Reset();
                AssertHelper.IsNull(PartioEndpointTimeouts.ValidateRequestJson("{\"RequestTimeoutMs\":0,\"UtilityTimeoutMs\":null}"), "0 and null are valid");
                AssertHelper.IsNull(PartioEndpointTimeouts.ValidateRequestJson("{\"requesttimeoutms\":60000}"), "field names are case-insensitive");
                AssertHelper.IsNotNull(PartioEndpointTimeouts.ValidateRequestJson("{\"RequestTimeoutMs\":500}"), "below the minimum");
                AssertHelper.IsNotNull(PartioEndpointTimeouts.ValidateRequestJson("{\"UtilityTimeoutMs\":3600001}"), "above the maximum");
                AssertHelper.IsNotNull(PartioEndpointTimeouts.ValidateRequestJson("{\"RequestTimeoutMs\":\"soon\"}"), "not a number");

                string listing = PartioEndpointTimeouts.ExposeInJson(
                    "{\"Objects\":[{\"Id\":\"eep_a\",\"Tags\":{\"AssistantHub.RequestTimeoutMs\":\"45000\"}},{\"Id\":\"eep_b\",\"Tags\":{}}]}");
                JsonNode exposed = JsonNode.Parse(listing);
                AssertHelper.AreEqual(45000, exposed["Objects"][0]["RequestTimeoutMs"].GetValue<int>(), "field exposed from the tag");
                AssertHelper.IsNull(exposed["Objects"][1]["RequestTimeoutMs"], "no tag, no value");
                AssertHelper.AreEqual(45000, PartioEndpointTimeouts.GetRequestTimeoutMs("eep_a"), "registry remembers the endpoint");

                PartioEndpointConfig completion = new PartioEndpointConfig
                {
                    Id = "cep_t",
                    Tags = new Dictionary<string, string> { [PartioEndpointTimeouts.RequestTimeoutTag] = "90000", [PartioEndpointTimeouts.UtilityTimeoutTag] = "garbage" }
                };
                PartioEndpointToolMetadata.ReadTagsToToolFields(completion);
                AssertHelper.AreEqual(90000, completion.RequestTimeoutMs, "completion request timeout read");
                AssertHelper.IsNull(completion.UtilityTimeoutMs, "invalid tag ignored");
                AssertHelper.AreEqual(90000, PartioEndpointTimeouts.GetRequestTimeoutMs("cep_t"), "completion endpoint remembered");
                PartioEndpointTimeouts.Reset();
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("InferenceService.UseRequestTimeout: a per-endpoint timeout bounds the call and is restored after", async () =>
            {
                DelayingChatHandler slow = new DelayingChatHandler(5000);
                using HttpClient httpClient = new HttpClient(slow);
                InferenceService inference = new InferenceService(
                    new InferenceSettings { Provider = InferenceProviderEnum.OpenAI, Endpoint = "https://slow.test/v1", ApiKey = "k", DefaultModel = "m", RequestTimeoutMs = 300000 },
                    CreateSilentLogging(),
                    httpClient);
                List<ChatCompletionMessage> messages = new List<ChatCompletionMessage> { new ChatCompletionMessage { Role = "user", Content = "hi" } };

                Stopwatch sw = Stopwatch.StartNew();
                InferenceResult result;
                using (InferenceService.UseRequestTimeout(1000))
                {
                    AssertHelper.AreEqual(1000, inference.EffectiveRequestTimeoutMs, "scope applies the endpoint timeout");
                    result = await inference.GenerateResponseAsync(messages, "m", 16, 0, 1, InferenceProviderEnum.OpenAI, "https://slow.test/v1", "k").ConfigureAwait(false);
                }

                sw.Stop();
                AssertHelper.IsFalse(result.Success, "slow endpoint times out");
                AssertHelper.IsTrue(sw.ElapsedMilliseconds < 4000, "timed out near 1 s, took " + sw.ElapsedMilliseconds + " ms");
                AssertHelper.AreEqual(300000, inference.EffectiveRequestTimeoutMs, "server default restored after the scope");
            });

            await ExecuteTestAsync("Chat rail: a completion endpoint's UtilityTimeoutMs bounds utility steps", async () =>
            {
                UtilityCircuitBreaker.Reset();
                PartioEndpointTimeouts.Reset();
                try
                {
                    (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                        await CreateRetrievalFixtureAsync(s => s.EnableQueryRewrite = true, null, null, null,
                            new Dictionary<string, string> { [PartioEndpointTimeouts.UtilityTimeoutTag] = "1000" },
                            new DelayingChatHandler(6000)).ConfigureAwait(false);
                    vectorStore.Enqueue(HttpStatusCode.OK, ThreeChunkSearchResponse());

                    Stopwatch sw = Stopwatch.StartNew();
                    AssistantRetrievalExecutionResult result = await service.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest { Query = "engine speed" }).ConfigureAwait(false);
                    sw.Stop();

                    AssertHelper.IsTrue(result.Success, "retrieval succeeds without the rewrite");
                    AssertHelper.HasCount(result.Response.Queries, 1, "rewrite abandoned");
                    AssertHelper.IsTrue(sw.ElapsedMilliseconds < 4000, "utility step cut at the endpoint's 1 s, took " + sw.ElapsedMilliseconds + " ms");
                }
                finally
                {
                    UtilityCircuitBreaker.Reset();
                    PartioEndpointTimeouts.Reset();
                }
            });

            await ExecuteTestAsync("Chat rail: a completion endpoint's RequestTimeoutMs bounds the answer call", async () =>
            {
                PartioEndpointTimeouts.Reset();
                try
                {
                    (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                        await CreateRetrievalFixtureAsync(s => s.EnableRag = false, null, null, null,
                            new Dictionary<string, string> { [PartioEndpointTimeouts.RequestTimeoutTag] = "1000" },
                            new DelayingChatHandler(6000)).ConfigureAwait(false);

                    Stopwatch sw = Stopwatch.StartNew();
                    AssistantChatExecutionResult result = await service.ExecuteNonStreamingAsync(new AssistantChatExecutionRequest
                    {
                        AssistantId = assistant.Id,
                        Assistant = assistant,
                        AssistantSettings = await ReadFixtureSettingsAsync(service, assistant).ConfigureAwait(false),
                        Messages = new List<ChatCompletionMessage> { new ChatCompletionMessage { Role = "user", Content = "hello" } }
                    }).ConfigureAwait(false);
                    sw.Stop();

                    AssertHelper.IsFalse(result.Success, "answer times out");
                    AssertHelper.IsTrue(sw.ElapsedMilliseconds < 4000, "answer cut at the endpoint's 1 s, took " + sw.ElapsedMilliseconds + " ms");
                }
                finally
                {
                    PartioEndpointTimeouts.Reset();
                }
            });

            await ExecuteTestAsync("RetrievalService: an embedding endpoint's RequestTimeoutMs bounds the query embedding", async () =>
            {
                RecordingChunkingService chunking = new RecordingChunkingService
                {
                    EndpointResponse = "{\"Id\":\"eep_slow\",\"Model\":\"all-minilm\",\"Tags\":{\"AssistantHub.RequestTimeoutMs\":\"1000\"}}",
                    EmbedDelayMs = 6000
                };
                RecordingVectorStoreService vectorStore = new RecordingVectorStoreService();
                RetrievalService retrieval = new RetrievalService(new ChunkingSettings(), new RecallDbSettings(), CreateSilentLogging(), vectorStore, chunking);
                RetrievalSearchOptions options = new RetrievalSearchOptions { SearchMode = "Vector" };

                Stopwatch sw = Stopwatch.StartNew();
                List<RetrievalChunk> chunks = await retrieval.RetrieveAsync("tenant_t", "col_t", "slow query", 5, 0, default, "eep_slow", options).ConfigureAwait(false);
                sw.Stop();

                AssertHelper.IsTrue(options.EmbeddingFailed, "embedding reported as failed");
                AssertHelper.IsTrue(sw.ElapsedMilliseconds < 4000, "query embedding cut at the endpoint's 1 s, took " + sw.ElapsedMilliseconds + " ms");
            });

            #endregion

            await ExecuteTestAsync("Inference logging: API keys are described, never written to logs", async () =>
            {
                AssertHelper.AreEqual("(not set)", AssistantHub.Core.Helpers.InferenceProviderHelper.DescribeSecret(null), "no key");
                AssertHelper.AreEqual("(set, 6 characters)", AssistantHub.Core.Helpers.InferenceProviderHelper.DescribeSecret("secret"), "key described by length only");

                string root = GetRepositoryRoot();
                foreach (string file in new[] { "InferenceServiceProviderBase.cs", "InferenceServiceResponseBase.cs", "InferenceService.cs" })
                {
                    string source = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "src", "AssistantHub.Core", "Services", file));
                    AssertHelper.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(source, @":\s*""\s*\+\s*apiKey\s*\+"), file + " does not log a raw API key");
                }

                string server = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "src", "AssistantHub.Server", "AssistantHubServer.cs"));
                AssertHelper.IsFalse(server.Contains("_Logging.Info(_Header + \"  password: \" + adminPassword)", StringComparison.Ordinal), "first-run password not logged");
                await Task.CompletedTask;
            });

            #region Page filters

            await ExecuteTestAsync("ChatMetadataFilter: page ranges validate, become tag conditions and intersect when merged", async () =>
            {
                ChatMetadataFilter range = new ChatMetadataFilter { PageStart = 3, PageEnd = 5 };
                AssertHelper.IsNull(range.Validate(), "valid range");
                AssertHelper.IsFalse(range.IsEmpty, "a page range is a filter");
                List<ChatTagCondition> conditions = range.PageTagConditions();
                AssertHelper.HasCount(conditions, 2, "two conditions");
                AssertHelper.IsTrue(conditions.Any(c => c.Key == ProvenanceTags.PageStart && c.Condition == "LessThan" && c.Value == "00006"), "chunk starts on or before page 5");
                AssertHelper.IsTrue(conditions.Any(c => c.Key == ProvenanceTags.PageEnd && c.Condition == "GreaterThan" && c.Value == "00002"), "chunk ends on or after page 3");

                AssertHelper.HasCount(new ChatMetadataFilter { PageStart = 7 }.PageTagConditions(), 1, "open-ended range");
                AssertHelper.IsNotNull(new ChatMetadataFilter { PageStart = 5, PageEnd = 3 }.Validate(), "reversed range rejected");
                AssertHelper.IsNotNull(new ChatMetadataFilter { PageStart = 0 }.Validate(), "page 0 rejected");
                AssertHelper.IsNotNull(new ChatMetadataFilter { PageEnd = 100000 }.Validate(), "page above 99999 rejected");

                ChatMetadataFilter merged = new ChatMetadataFilter { PageStart = 2, PageEnd = 10 };
                merged.Merge(new ChatMetadataFilter { PageStart = 4, PageEnd = 20 });
                AssertHelper.AreEqual(4, merged.PageStart, "later start wins");
                AssertHelper.AreEqual(10, merged.PageEnd, "earlier end wins");

                ChatMetadataFilter json = JsonSerializer.Deserialize<ChatMetadataFilter>("{\"page_start\":12,\"page_end\":13}");
                AssertHelper.AreEqual(12, json.PageStart, "page_start from JSON");
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("RetrievalService: a page range is sent to RecallDB as tag conditions", async () =>
            {
                RecordingVectorStoreService vectorStore = new RecordingVectorStoreService();
                vectorStore.Enqueue(HttpStatusCode.OK, "{\"Documents\":[]}");
                RetrievalService retrieval = new RetrievalService(new ChunkingSettings(), new RecallDbSettings(), CreateSilentLogging(), vectorStore, new RecordingChunkingService());
                RetrievalSearchOptions options = new RetrievalSearchOptions
                {
                    SearchMode = "FullText",
                    MetadataFilter = new ChatMetadataFilter { PageStart = 12, PageEnd = 13, RequiredLabels = new List<string> { "manual" } }
                };

                await retrieval.RetrieveAsync("tenant_p", "col_p", "torque", 5, 0, default, null, options).ConfigureAwait(false);

                string body = vectorStore.Calls[0].Body;
                AssertHelper.StringContains(body, "\"Key\":\"ah_page_start\",\"Condition\":\"LessThan\",\"Value\":\"00014\"", "upper bound");
                AssertHelper.StringContains(body, "\"Key\":\"ah_page_end\",\"Condition\":\"GreaterThan\",\"Value\":\"00011\"", "lower bound");
                AssertHelper.StringContains(body, "\"manual\"", "other filters kept");
            });

            await ExecuteTestAsync("AssistantChatService.ExecuteRetrievalOnlyAsync: an invalid page range returns 400", async () =>
            {
                (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                    await CreateRetrievalFixtureAsync(null).ConfigureAwait(false);
                AssistantRetrievalExecutionResult result = await service.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest
                {
                    Query = "q",
                    MetadataFilter = new ChatMetadataFilter { PageStart = 9, PageEnd = 2 }
                }).ConfigureAwait(false);
                AssertHelper.AreEqual(400, result.StatusCode, "invalid range rejected");
                AssertHelper.HasCount(vectorStore.Calls, 0, "no search ran");
            });

            await ExecuteTestAsync("ProvenanceTags.MapFlatChunks: flat chunks get the pages and sheet of the blocks they cover", async () =>
            {
                List<ExtractedBlock> blocks = new List<ExtractedBlock>
                {
                    new ExtractedBlock { Kind = "Heading", Text = "Installation", HeaderLevel = 1, PageNumber = 1 },
                    new ExtractedBlock { Kind = "Text", Text = "Unpack the pump and check the seals before use.", PageNumber = 1 },
                    new ExtractedBlock { Kind = "Text", Text = "Mount the pump on a level surface   with four bolts.", PageNumber = 2 },
                    new ExtractedBlock { Kind = "Text", Text = "Torque the bolts to 40 Nm and connect the inlet hose.", PageNumber = 3 }
                };
                string content = "# Installation\n\nUnpack the pump and check the seals before use.\n\nMount the pump on a level surface   with four bolts.\n\nTorque the bolts to 40 Nm and connect the inlet hose.";
                List<string> chunks = new List<string>
                {
                    "# Installation Unpack the pump and check the seals before use.",
                    "Mount the pump on a level surface with four bolts. Torque the bolts to 40 Nm and connect",
                    "a chunk whose text drifted and cannot be found"
                };

                List<Dictionary<string, string>> tags = ProvenanceTags.MapFlatChunks(content, blocks, chunks);
                AssertHelper.HasCount(tags, 3, "one entry per chunk");
                AssertHelper.AreEqual("00001", tags[0][ProvenanceTags.PageStart], "first chunk starts on page 1");
                AssertHelper.AreEqual("00001", tags[0][ProvenanceTags.PageEnd], "first chunk ends on page 1");
                AssertHelper.AreEqual("00002", tags[1][ProvenanceTags.PageStart], "second chunk starts on page 2");
                AssertHelper.AreEqual("00003", tags[1][ProvenanceTags.PageEnd], "second chunk spans into page 3");
                AssertHelper.AreEqual(0, tags[2].Count, "unlocated chunk gets no tags");

                List<Dictionary<string, string>> noPages = ProvenanceTags.MapFlatChunks("plain text", new List<ExtractedBlock> { new ExtractedBlock { Text = "plain text" } }, new List<string> { "plain text" });
                AssertHelper.AreEqual(0, noPages[0].Count, "documents without pages get no tags");

                List<Dictionary<string, string>> sheets = ProvenanceTags.MapFlatChunks(
                    "Q1 revenue 10\n\nQ2 revenue 12",
                    new List<ExtractedBlock> { new ExtractedBlock { Text = "Q1 revenue 10", SheetName = "Summary" }, new ExtractedBlock { Text = "Q2 revenue 12", SheetName = "Summary" } },
                    new List<string> { "Q1 revenue 10 Q2 revenue 12" });
                AssertHelper.AreEqual("Summary", sheets[0][ProvenanceTags.Sheet], "sheet recorded");
                await Task.CompletedTask;
            });

            #endregion
        }

        /// <summary>
        /// Answers every request with a chat reply after a delay, honoring cancellation.
        /// </summary>
        private sealed class DelayingChatHandler : HttpMessageHandler
        {
            private readonly int _DelayMs;

            public DelayingChatHandler(int delayMs)
            {
                _DelayMs = delayMs;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(_DelayMs, cancellationToken).ConfigureAwait(false);
                return ChatReply("late answer");
            }
        }
    }
}
