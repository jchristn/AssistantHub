namespace Test.Sdk.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk;
    using AssistantHub.Sdk.Models;
    using Test.Shared;

    /// <summary>
    /// Local SDK contract tests that do not require a live AssistantHub server.
    /// </summary>
    public static class SdkContractTests
    {
        /// <summary>
        /// Runs local serialization and route-shape checks.
        /// </summary>
        /// <param name="runner">Test runner instance.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task RunAsync(TestRunner runner, CancellationToken token)
        {
            await runner.RunTestAsync("SDK contract: ChatCompletionRequest serializes attached_document_ids", async (CancellationToken ct) =>
            {
                using SerializationProbeClient probe = new SerializationProbeClient();

                ChatCompletionRequest request = new ChatCompletionRequest
                {
                    Messages = new List<ChatCompletionMessage>
                    {
                        new ChatCompletionMessage { Role = "user", Content = "Summarize this document." }
                    },
                    AttachedDocumentIds = new List<string> { "adoc_one", "adoc_two" },
                    TopP = 0.8,
                    MaxTokens = 512
                };

                string json = probe.Serialize(request);
                AssertHelper.StringContains(json, "\"attached_document_ids\"", "serialized request JSON");
                AssertHelper.StringContains(json, "\"top_p\"", "serialized request JSON");
                AssertHelper.StringContains(json, "\"max_tokens\"", "serialized request JSON");
                AssertHelper.IsFalse(json.Contains("AttachedDocumentIds"), "serialized request should not use CLR property names");

                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement ids = doc.RootElement.GetProperty("attached_document_ids");
                AssertHelper.AreEqual(2, ids.GetArrayLength(), "attached_document_ids count");
                AssertHelper.AreEqual("adoc_one", ids[0].GetString(), "first attached document ID");
                AssertHelper.AreEqual("adoc_two", ids[1].GetString(), "second attached document ID");

                ChatCompletionRequest roundTrip = probe.Deserialize<ChatCompletionRequest>(json);
                AssertHelper.IsNotNull(roundTrip.AttachedDocumentIds, "round-trip attached document IDs");
                AssertHelper.HasCount(roundTrip.AttachedDocumentIds, 2, "round-trip attached document IDs");
                AssertHelper.AreEqual("adoc_one", roundTrip.AttachedDocumentIds[0], "round-trip first document ID");

                await Task.CompletedTask.ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("SDK contract: ChatCompletionRequest serializes local_attachments", async (CancellationToken ct) =>
            {
                using SerializationProbeClient probe = new SerializationProbeClient();

                ChatCompletionRequest request = new ChatCompletionRequest
                {
                    Messages = new List<ChatCompletionMessage>
                    {
                        new ChatCompletionMessage { Role = "user", Content = "Summarize this local file." }
                    },
                    LocalAttachments = new List<ChatLocalAttachment>
                    {
                        new ChatLocalAttachment
                        {
                            Name = "notes.txt",
                            ContentType = "text/plain",
                            Base64Content = "SGVsbG8="
                        }
                    }
                };

                string json = probe.Serialize(request);
                AssertHelper.StringContains(json, "\"local_attachments\"", "serialized request JSON");
                AssertHelper.StringContains(json, "\"base64_content\"", "serialized request JSON");
                AssertHelper.IsFalse(json.Contains("LocalAttachments"), "serialized request should not use CLR property names");

                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement attachments = doc.RootElement.GetProperty("local_attachments");
                AssertHelper.AreEqual(1, attachments.GetArrayLength(), "local_attachments count");
                AssertHelper.AreEqual("notes.txt", attachments[0].GetProperty("name").GetString(), "local attachment name");

                ChatCompletionRequest roundTrip = probe.Deserialize<ChatCompletionRequest>(json);
                AssertHelper.IsNotNull(roundTrip.LocalAttachments, "round-trip local attachments");
                AssertHelper.HasCount(roundTrip.LocalAttachments, 1, "round-trip local attachments");
                AssertHelper.AreEqual("notes.txt", roundTrip.LocalAttachments[0].Name, "round-trip local attachment name");

                await Task.CompletedTask.ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("SDK contract: AssistantToolPolicy serializes with settings", async (CancellationToken ct) =>
            {
                using SerializationProbeClient probe = new SerializationProbeClient();

                AssistantSettings settings = new AssistantSettings
                {
                    AssistantId = "asst_local",
                    ExposeThinking = true,
                    ToolPolicy = new AssistantToolPolicy
                    {
                        EnableToolCalls = true,
                        EnableCollectionSearchTool = true,
                        EnableDocumentAtomExtractionTool = true,
                        EnableWebSearchTool = true,
                        ToolChoiceMode = "Required",
                        MaxToolIterations = 4,
                        MaxToolCallsPerTurn = 6,
                        MaxToolResultItems = 9,
                        AllowedToolNames = new List<string> { "collection_search" },
                        MaxSearchTopK = 7,
                        MaxDocumentsConsideredPerSearch = 25,
                        MaxResultsConsideredPerSearch = 50,
                        MaxAtomExtractionBytes = 2097152,
                        MaxAtomExtractionCharacters = 24000,
                        AllowedSearchModes = new List<string> { "FullText" },
                        ReturnFullSearchContent = true,
                        MaxWebResults = 3,
                        TavilyEndpoint = "https://assistant.tavily.test/search",
                        TavilyApiKey = "assistant-key",
                        AllowUngovernedWebAccess = true,
                        AllowedWebDomains = new List<string> { "example.com" },
                        BlockedWebDomains = new List<string> { "blocked.example" }
                    }
                };

                string json = probe.Serialize(settings);
                AssertHelper.StringContains(json, "\"ToolPolicy\"", "serialized settings JSON");
                AssertHelper.StringContains(json, "\"ExposeThinking\"", "serialized settings JSON");
                AssertHelper.StringContains(json, "\"EnableToolCalls\"", "serialized policy JSON");
                AssertHelper.StringContains(json, "\"EnableCollectionSearchTool\"", "serialized policy JSON");
                AssertHelper.StringContains(json, "\"EnableDocumentAtomExtractionTool\"", "serialized policy JSON");
                AssertHelper.StringContains(json, "\"ToolChoiceMode\"", "serialized policy JSON");
                AssertHelper.StringContains(json, "\"AllowedToolNames\"", "serialized policy JSON");
                AssertHelper.StringContains(json, "\"AllowedSearchModes\"", "serialized policy JSON");
                AssertHelper.StringContains(json, "\"MaxDocumentsConsideredPerSearch\"", "serialized policy JSON");
                AssertHelper.StringContains(json, "\"MaxResultsConsideredPerSearch\"", "serialized policy JSON");
                AssertHelper.StringContains(json, "\"MaxAtomExtractionBytes\"", "serialized policy JSON");
                AssertHelper.StringContains(json, "\"ReturnFullSearchContent\"", "serialized policy JSON");
                AssertHelper.StringContains(json, "\"TavilyEndpoint\"", "serialized policy JSON");
                AssertHelper.StringContains(json, "\"AllowUngovernedWebAccess\"", "serialized policy JSON");
                AssertHelper.StringContains(json, "\"AllowedWebDomains\"", "serialized policy JSON");
                AssertHelper.IsFalse(json.Contains("enableToolCalls"), "serialized policy should use server property names");

                AssistantSettings roundTrip = probe.Deserialize<AssistantSettings>(json);
                AssertHelper.IsTrue(roundTrip.ExposeThinking, "round-trip ExposeThinking");
                AssertHelper.IsNotNull(roundTrip.ToolPolicy, "round-trip tool policy");
                AssertHelper.IsTrue(roundTrip.ToolPolicy.EnableToolCalls, "round-trip EnableToolCalls");
                AssertHelper.IsTrue(roundTrip.ToolPolicy.EnableCollectionSearchTool, "round-trip EnableCollectionSearchTool");
                AssertHelper.IsTrue(roundTrip.ToolPolicy.EnableDocumentAtomExtractionTool, "round-trip EnableDocumentAtomExtractionTool");
                AssertHelper.IsTrue(roundTrip.ToolPolicy.EnableWebSearchTool, "round-trip EnableWebSearchTool");
                AssertHelper.AreEqual("Required", roundTrip.ToolPolicy.ToolChoiceMode, "round-trip ToolChoiceMode");
                AssertHelper.AreEqual(4, roundTrip.ToolPolicy.MaxToolIterations, "round-trip MaxToolIterations");
                AssertHelper.AreEqual(9, roundTrip.ToolPolicy.MaxToolResultItems, "round-trip MaxToolResultItems");
                AssertHelper.AreEqual(7, roundTrip.ToolPolicy.MaxSearchTopK, "round-trip MaxSearchTopK");
                AssertHelper.AreEqual(25, roundTrip.ToolPolicy.MaxDocumentsConsideredPerSearch, "round-trip MaxDocumentsConsideredPerSearch");
                AssertHelper.AreEqual(50, roundTrip.ToolPolicy.MaxResultsConsideredPerSearch, "round-trip MaxResultsConsideredPerSearch");
                AssertHelper.AreEqual(2097152, roundTrip.ToolPolicy.MaxAtomExtractionBytes, "round-trip MaxAtomExtractionBytes");
                AssertHelper.AreEqual(24000, roundTrip.ToolPolicy.MaxAtomExtractionCharacters, "round-trip MaxAtomExtractionCharacters");
                AssertHelper.IsTrue(roundTrip.ToolPolicy.ReturnFullSearchContent, "round-trip ReturnFullSearchContent");
                AssertHelper.HasCount(roundTrip.ToolPolicy.AllowedToolNames, 1, "round-trip AllowedToolNames");
                AssertHelper.HasCount(roundTrip.ToolPolicy.AllowedSearchModes, 1, "round-trip AllowedSearchModes");
                AssertHelper.AreEqual(3, roundTrip.ToolPolicy.MaxWebResults, "round-trip MaxWebResults");
                AssertHelper.AreEqual("https://assistant.tavily.test/search", roundTrip.ToolPolicy.TavilyEndpoint, "round-trip TavilyEndpoint");
                AssertHelper.AreEqual("assistant-key", roundTrip.ToolPolicy.TavilyApiKey, "round-trip TavilyApiKey");
                AssertHelper.IsTrue(roundTrip.ToolPolicy.AllowUngovernedWebAccess, "round-trip AllowUngovernedWebAccess");
                AssertHelper.HasCount(roundTrip.ToolPolicy.AllowedWebDomains, 1, "round-trip AllowedWebDomains");
                AssertHelper.AreEqual("example.com", roundTrip.ToolPolicy.AllowedWebDomains[0], "round-trip AllowedWebDomains value");

                string validationJson = @"{
  ""Success"": false,
  ""Message"": ""Policy invalid."",
  ""ToolPolicyJson"": ""{}"",
  ""ToolPolicy"": {
    ""EnableToolCalls"": true,
    ""EnableCollectionSearchTool"": true,
    ""TavilyEndpoint"": ""https://assistant.tavily.test/search"",
    ""AllowedWebDomains"": [""example.com""]
  },
  ""Tools"": [],
  ""Errors"": [],
  ""ErrorCodes"": [""no_available_tools""]
}";
                AssistantToolPolicyValidationResult validation = probe.Deserialize<AssistantToolPolicyValidationResult>(validationJson);
                AssertHelper.IsFalse(validation.Success, "validation success");
                AssertHelper.IsNotNull(validation.ToolPolicy, "validation tool policy");
                AssertHelper.IsTrue(validation.ToolPolicy.EnableToolCalls, "validation EnableToolCalls");
                AssertHelper.AreEqual("https://assistant.tavily.test/search", validation.ToolPolicy.TavilyEndpoint, "validation TavilyEndpoint");
                AssertHelper.HasCount(validation.ToolPolicy.AllowedWebDomains, 1, "validation AllowedWebDomains");
                AssertHelper.HasCount(validation.ErrorCodes, 1, "validation ErrorCodes");
                AssertHelper.AreEqual("no_available_tools", validation.ErrorCodes[0], "validation ErrorCodes value");

                string diagnosticsJson = @"{
  ""Success"": false,
  ""Message"": ""Tool diagnostics found blocking issues."",
  ""AssistantId"": ""asst_local"",
  ""InferenceEndpointId"": ""cep_local"",
  ""ToolRoutingInferenceEndpointId"": ""cep_router"",
  ""EffectiveToolRoutingInferenceEndpointId"": ""cep_router"",
  ""EndpointResolved"": true,
  ""EndpointModel"": ""qwen3-tool"",
  ""EndpointApiFormat"": ""OpenAI"",
  ""EndpointActive"": true,
  ""EndpointSupportsToolCalling"": false,
  ""EndpointToolCallingApiFormat"": null,
  ""EndpointSupportsParallelToolCalls"": false,
  ""EndpointSupportsStreamingToolCalls"": false,
  ""Validation"": {
    ""Success"": true,
    ""Errors"": [],
    ""ErrorCodes"": []
  },
  ""Tools"": [],
  ""Warnings"": [],
  ""Errors"": [""The effective tool-routing completion endpoint does not explicitly support tool calling.""],
  ""ErrorCodes"": [""tool_routing_endpoint_not_tool_capable""]
}";
                AssistantToolPolicyTestResult diagnostics = probe.Deserialize<AssistantToolPolicyTestResult>(diagnosticsJson);
                AssertHelper.IsFalse(diagnostics.Success, "diagnostics success");
                AssertHelper.AreEqual("cep_router", diagnostics.ToolRoutingInferenceEndpointId, "diagnostics configured tool routing endpoint");
                AssertHelper.AreEqual("cep_router", diagnostics.EffectiveToolRoutingInferenceEndpointId, "diagnostics effective tool routing endpoint");
                AssertHelper.IsTrue(diagnostics.EndpointResolved, "diagnostics endpoint resolved");
                AssertHelper.AreEqual("qwen3-tool", diagnostics.EndpointModel, "diagnostics endpoint model");
                AssertHelper.HasCount(diagnostics.ErrorCodes, 1, "diagnostics ErrorCodes");
                AssertHelper.AreEqual("tool_routing_endpoint_not_tool_capable", diagnostics.ErrorCodes[0], "diagnostics ErrorCodes value");

                await Task.CompletedTask.ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("SDK contract: ChatCompletionUsage parses provider detail tokens", async (CancellationToken ct) =>
            {
                using SerializationProbeClient probe = new SerializationProbeClient();

                string usageJson = @"{
  ""prompt_tokens"": 12,
  ""completion_tokens"": 4,
  ""total_tokens"": 16,
  ""tool_definition_tokens"": 5,
  ""completion_tokens_details"": {
    ""reasoning_tokens"": 7
  },
  ""prompt_tokens_details"": {
    ""cached_tokens"": 3,
    ""tool_tokens"": 5
  }
}";

                ChatCompletionUsage usage = probe.Deserialize<ChatCompletionUsage>(usageJson);
                AssertHelper.AreEqual(12, usage.PromptTokens, "usage PromptTokens");
                AssertHelper.AreEqual(16, usage.TotalTokens, "usage TotalTokens");
                AssertHelper.AreEqual(5, usage.ToolDefinitionTokens, "usage ToolDefinitionTokens");
                AssertHelper.IsNotNull(usage.CompletionTokensDetails, "usage CompletionTokensDetails");
                AssertHelper.AreEqual(7, usage.CompletionTokensDetails.ReasoningTokens, "usage detail ReasoningTokens");
                AssertHelper.IsNotNull(usage.PromptTokensDetails, "usage PromptTokensDetails");
                AssertHelper.AreEqual(3, usage.PromptTokensDetails.CachedTokens, "usage detail CachedTokens");

                AssistantTokenUsageTelemetry tokens = probe.Deserialize<AssistantTokenUsageTelemetry>(
                    @"{""Input"":12,""Output"":4,""Total"":16,""Reasoning"":7,""ToolDefinitions"":5}");
                AssertHelper.AreEqual(7, tokens.Reasoning.Value, "telemetry Reasoning");
                AssertHelper.AreEqual(5, tokens.ToolDefinitions.Value, "telemetry ToolDefinitions");

                await Task.CompletedTask.ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("SDK contract: ExternalSearch status serializes without secrets", async (CancellationToken ct) =>
            {
                using SerializationProbeClient probe = new SerializationProbeClient();

                string json = @"{
  ""Enabled"": true,
  ""EnabledProviders"": 1,
  ""ConfiguredProviders"": 1,
  ""MisconfiguredProviders"": 0
}";
                ExternalSearchConfigurationStatus status = probe.Deserialize<ExternalSearchConfigurationStatus>(json);
                AssertHelper.IsTrue(status.Enabled, "status Enabled");
                AssertHelper.AreEqual(1, status.EnabledProviders, "status EnabledProviders");
                AssertHelper.AreEqual(1, status.ConfiguredProviders, "status ConfiguredProviders");
                AssertHelper.AreEqual(0, status.MisconfiguredProviders, "status MisconfiguredProviders");

                string serialized = probe.Serialize(status);
                AssertHelper.StringContains(serialized, "\"ConfiguredProviders\":1", "serialized external-search status JSON");
                AssertHelper.IsFalse(serialized.Contains("ApiKey"), "external-search status must not expose secrets");

                await Task.CompletedTask.ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("SDK contract: CompletionEndpoint serializes tool capability metadata", async (CancellationToken ct) =>
            {
                using SerializationProbeClient probe = new SerializationProbeClient();

                CompletionEndpoint endpoint = new CompletionEndpoint
                {
                    Id = "ep_tool",
                    Name = "Tool endpoint",
                    Model = "qwen3",
                    Endpoint = "http://localhost:11434",
                    ApiFormat = "OpenAI",
                    Active = true,
                    MaxConcurrentRequests = 2,
                    SupportsToolCalling = true,
                    ToolCallingApiFormat = "OpenAIChatCompletions",
                    SupportsParallelToolCalls = true,
                    SupportsStreamingToolCalls = true
                };

                string json = probe.Serialize(endpoint);
                AssertHelper.StringContains(json, "\"SupportsToolCalling\":true", "serialized endpoint JSON");
                AssertHelper.StringContains(json, "\"ToolCallingApiFormat\":\"OpenAIChatCompletions\"", "serialized endpoint JSON");

                CompletionEndpoint roundTrip = probe.Deserialize<CompletionEndpoint>(json);
                AssertHelper.IsTrue(roundTrip.SupportsToolCalling, "round-trip SupportsToolCalling");
                AssertHelper.AreEqual("OpenAIChatCompletions", roundTrip.ToolCallingApiFormat, "round-trip ToolCallingApiFormat");
                AssertHelper.IsTrue(roundTrip.SupportsParallelToolCalls, "round-trip SupportsParallelToolCalls");
                AssertHelper.IsTrue(roundTrip.SupportsStreamingToolCalls, "round-trip SupportsStreamingToolCalls");

                await Task.CompletedTask.ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("SDK contract: ChatCompletionRetrieval parses attached document metadata", async (CancellationToken ct) =>
            {
                using SerializationProbeClient probe = new SerializationProbeClient();

                string json = @"{
  ""collection_id"": ""col_abc123"",
  ""duration_ms"": 42.7,
  ""chunks_returned"": 3,
  ""attached_document_ids"": [""adoc_one""],
  ""attached_documents"": [
    {
      ""Id"": ""adoc_one"",
      ""Name"": ""Policy Handbook"",
      ""OriginalFilename"": ""policy.pdf"",
      ""ContentType"": ""application/pdf"",
      ""SizeBytes"": 12345,
      ""CreatedUtc"": ""2026-01-01T00:00:00Z"",
      ""LastUpdateUtc"": ""2026-01-02T00:00:00Z""
    }
  ],
  ""document_filter_applied"": true
}";

                ChatCompletionRetrieval retrieval = probe.Deserialize<ChatCompletionRetrieval>(json);
                AssertHelper.AreEqual("col_abc123", retrieval.CollectionId, "retrieval collection ID");
                AssertHelper.AreEqual(42.7, retrieval.DurationMs, "retrieval duration");
                AssertHelper.AreEqual(3, retrieval.ChunksReturned, "retrieval chunks returned");
                AssertHelper.IsTrue(retrieval.DocumentFilterApplied, "retrieval document filter applied");
                AssertHelper.HasCount(retrieval.AttachedDocumentIds, 1, "retrieval attached document IDs");
                AssertHelper.HasCount(retrieval.AttachedDocuments, 1, "retrieval attached document metadata");
                AssertHelper.AreEqual("adoc_one", retrieval.AttachedDocuments[0].Id, "attached document metadata ID");
                AssertHelper.AreEqual("policy.pdf", retrieval.AttachedDocuments[0].OriginalFilename, "attached document filename");

                string serialized = probe.Serialize(retrieval);
                AssertHelper.StringContains(serialized, "\"attached_document_ids\"", "serialized retrieval JSON");
                AssertHelper.StringContains(serialized, "\"attached_documents\"", "serialized retrieval JSON");
                AssertHelper.StringContains(serialized, "\"document_filter_applied\"", "serialized retrieval JSON");
                AssertHelper.IsFalse(serialized.Contains("S3Key"), "selection metadata should not expose S3 key");
                AssertHelper.IsFalse(serialized.Contains("BucketName"), "selection metadata should not expose bucket name");

                await Task.CompletedTask.ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("SDK contract: ChatCompletionResponse parses safe tool trace metadata", async (CancellationToken ct) =>
            {
                using SerializationProbeClient probe = new SerializationProbeClient();

                string json = @"{
  ""id"": ""chatcmpl_tool"",
  ""object"": ""chat.completion"",
  ""created"": 1760000000,
  ""model"": ""qwen3-tool"",
  ""choices"": [
    {
      ""index"": 0,
      ""message"": { ""role"": ""assistant"", ""content"": ""done"", ""thinking"": ""hidden reasoning"" },
      ""finish_reason"": ""stop""
    }
  ],
  ""tool_calls"": [
    {
      ""tool_call_id"": ""call_search"",
      ""tool_name"": ""collection_search"",
      ""display_label"": ""Searching collection"",
      ""iteration"": 1,
      ""sequence_number"": 1,
      ""success"": true,
      ""denied"": false,
      ""truncated"": false,
      ""output_characters"": 128,
      ""result_count"": 3,
      ""credits_used"": 2,
      ""provider_latency_ms"": 45.5,
      ""duration_ms"": 12.5,
      ""summary"": ""Searching collection completed.""
    }
  ]
}";

                ChatCompletionResponse response = probe.Deserialize<ChatCompletionResponse>(json);
                AssertHelper.AreEqual("hidden reasoning", response.Choices[0].Message.Thinking, "response message thinking");
                AssertHelper.HasCount(response.ToolCalls, 1, "response tool trace count");
                AssertHelper.AreEqual("collection_search", response.ToolCalls[0].ToolName, "tool trace name");
                AssertHelper.AreEqual("Searching collection", response.ToolCalls[0].DisplayLabel, "tool trace label");
                AssertHelper.IsTrue(response.ToolCalls[0].Success, "tool trace success");
                AssertHelper.AreEqual(3, response.ToolCalls[0].ResultCount.Value, "tool trace result count");
                AssertHelper.AreEqual(2, response.ToolCalls[0].CreditsUsed.Value, "tool trace credits used");
                AssertHelper.AreEqual(45.5, response.ToolCalls[0].ProviderLatencyMs.Value, "tool trace provider latency");

                string serialized = probe.Serialize(response);
                AssertHelper.StringContains(serialized, "\"tool_calls\"", "serialized response tool trace JSON");
                AssertHelper.StringContains(serialized, "\"result_count\"", "serialized response tool trace result count JSON");
                AssertHelper.StringContains(serialized, "\"credits_used\"", "serialized response tool trace credits JSON");
                AssertHelper.StringContains(serialized, "\"provider_latency_ms\"", "serialized response tool trace provider latency JSON");
                AssertHelper.IsFalse(serialized.Contains("ArgumentsJson"), "response tool trace should not expose arguments");
                AssertHelper.IsFalse(serialized.Contains("OutputJson"), "response tool trace should not expose raw output");

                await Task.CompletedTask.ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("SDK contract: assistant tool-call trace routes", async (CancellationToken ct) =>
            {
                RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(request =>
                {
                    string path = request.RequestUri.PathAndQuery;
                    if (request.Method == HttpMethod.Get && path.StartsWith("/v1.0/assistants/asst_local/tool-calls?", StringComparison.Ordinal))
                    {
                        return JsonResponse(@"{
  ""Success"": true,
  ""MaxResults"": 5,
  ""TotalRecords"": 1,
  ""RecordsRemaining"": 0,
  ""EndOfResults"": true,
  ""Objects"": [
    {
      ""Id"": ""atc_local"",
      ""AssistantId"": ""asst_local"",
      ""TraceId"": ""trace_local"",
      ""ToolName"": ""collection_search"",
      ""ArgumentsJson"": ""[redacted]"",
      ""Success"": true,
      ""Denied"": false
    }
  ]
}");
                    }

                    if (request.Method == HttpMethod.Get && path == "/v1.0/assistants/asst_local/tool-calls/atc_local")
                    {
                        return JsonResponse(@"{""Id"":""atc_local"",""AssistantId"":""asst_local"",""ToolName"":""collection_search"",""Success"":true}");
                    }

                    if (request.Method == HttpMethod.Delete && path.StartsWith("/v1.0/assistants/asst_local/tool-calls?", StringComparison.Ordinal))
                    {
                        return JsonResponse(@"{""DeletedCount"":1}");
                    }

                    if (request.Method == HttpMethod.Delete && path == "/v1.0/assistants/asst_local/tool-calls/atc_local")
                    {
                        return new HttpResponseMessage(HttpStatusCode.NoContent);
                    }

                    return new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("{}")
                    };
                });

                using HttpClient httpClient = new HttpClient(handler);
                using AssistantHubClient client = new AssistantHubClient("http://localhost", httpClient, "test-key");

                EnumerationResult<AssistantToolCallRecord> list = await client.ListAssistantToolCallsAsync(
                    "asst_local",
                    new EnumerationQuery
                    {
                        MaxResults = 5,
                        TraceIdFilter = "trace_local",
                        ToolNameFilter = "collection_search",
                        SuccessFilter = true
                    },
                    ct).ConfigureAwait(false);
                AssertHelper.HasCount(list.Objects, 1, "tool-call list count");
                AssertHelper.AreEqual("atc_local", list.Objects[0].Id, "tool-call list id");
                AssertHelper.AreEqual("collection_search", list.Objects[0].ToolName, "tool-call list tool name");
                AssertHelper.IsFalse(list.Objects[0].ArgumentsJson.Contains("secret", StringComparison.OrdinalIgnoreCase), "tool-call list arguments redacted");

                AssistantToolCallRecord record = await client.GetAssistantToolCallAsync("asst_local", "atc_local", ct).ConfigureAwait(false);
                AssertHelper.AreEqual("atc_local", record.Id, "tool-call get id");

                RequestHistoryDeleteResult deleted = await client.DeleteAssistantToolCallsAsync(
                    "asst_local",
                    new EnumerationQuery { ToolNameFilter = "collection_search" },
                    ct).ConfigureAwait(false);
                AssertHelper.AreEqual(1, deleted.DeletedCount, "tool-call bulk delete count");

                await client.DeleteAssistantToolCallAsync("asst_local", "atc_local", ct).ConfigureAwait(false);

                AssertHelper.AreEqual(HttpMethod.Get, handler.Requests[0].Method, "tool-call list method");
                AssertHelper.StringContains(handler.Requests[0].PathAndQuery, "/v1.0/assistants/asst_local/tool-calls?", "tool-call list path");
                AssertHelper.StringContains(handler.Requests[0].PathAndQuery, "traceId=trace_local", "tool-call list trace query");
                AssertHelper.StringContains(handler.Requests[0].PathAndQuery, "toolName=collection_search", "tool-call list tool query");
                AssertHelper.StringContains(handler.Requests[0].PathAndQuery, "success=true", "tool-call list success query");
                AssertHelper.AreEqual(HttpMethod.Get, handler.Requests[1].Method, "tool-call get method");
                AssertHelper.AreEqual("/v1.0/assistants/asst_local/tool-calls/atc_local", handler.Requests[1].PathAndQuery, "tool-call get path");
                AssertHelper.AreEqual(HttpMethod.Delete, handler.Requests[2].Method, "tool-call bulk delete method");
                AssertHelper.StringContains(handler.Requests[2].PathAndQuery, "toolName=collection_search", "tool-call bulk delete query");
                AssertHelper.AreEqual(HttpMethod.Delete, handler.Requests[3].Method, "tool-call delete method");
                AssertHelper.AreEqual("/v1.0/assistants/asst_local/tool-calls/atc_local", handler.Requests[3].PathAndQuery, "tool-call delete path");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("SDK contract: ChatHistory parses attached document metadata", async (CancellationToken ct) =>
            {
                using SerializationProbeClient probe = new SerializationProbeClient();

                string json = """
{
  "Id": "chist_local",
  "TenantId": "default",
  "ThreadId": "thr_local",
  "AssistantId": "asst_local",
  "AttachedDocumentIdsJson": "[\"adoc_one\"]",
  "AttachedDocumentsJson": "[{\"Id\":\"adoc_one\",\"Name\":\"Policy Handbook\"}]",
  "CreatedUtc": "2026-01-01T00:00:00Z",
  "LastUpdateUtc": "2026-01-01T00:00:00Z"
}
""";

                ChatHistory history = probe.Deserialize<ChatHistory>(json);
                AssertHelper.AreEqual("chist_local", history.Id, "history ID");
                AssertHelper.StringContains(history.AttachedDocumentIdsJson, "adoc_one", "history attached document IDs JSON");
                AssertHelper.StringContains(history.AttachedDocumentsJson, "Policy Handbook", "history attached documents JSON");

                string serialized = probe.Serialize(history);
                AssertHelper.StringContains(serialized, "\"AttachedDocumentIdsJson\"", "serialized history JSON");
                AssertHelper.StringContains(serialized, "\"AttachedDocumentsJson\"", "serialized history JSON");

                await Task.CompletedTask.ConfigureAwait(false);
            }, token).ConfigureAwait(false);
        }

        private sealed class SerializationProbeClient : AssistantHubClientBase
        {
            public SerializationProbeClient() : base("http://localhost")
            {
            }

            public string Serialize(object value)
            {
                return SerializeJson(value);
            }

            public T Deserialize<T>(string json)
            {
                return DeserializeJson<T>(json);
            }
        }

        private sealed class RecordingHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _Responder;

            public RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _Responder = responder;
            }

            public List<(HttpMethod Method, string PathAndQuery)> Requests { get; } = new List<(HttpMethod Method, string PathAndQuery)>();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add((request.Method, request.RequestUri.PathAndQuery));
                return Task.FromResult(_Responder(request));
            }
        }

        private static HttpResponseMessage JsonResponse(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
