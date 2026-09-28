namespace AssistantHub.Server.OpenApi
{
    using System.Collections.Generic;
    using System.Text.Json;
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for assistant settings, tool policy, and retrieval diagnostic routes.
    /// </summary>
    public static class AssistantSettingsApiDocs
    {
        #region Private-Members

        private const string _Tag = "Assistant Settings";

        private const string _SettingsId = "aset_01JH3Z8F4G6H8J0K2M4N6P8Q0R";

        private const string _InferenceEndpointId = "cep_01JH3ZA1B3C5D7E9F1G3H5J7K9";

        private const string _ToolRoutingEndpointId = "cep_01JH3ZA2C4D6E8F0G2H4J6K8M0";

        private const string _EmbeddingEndpointId = "ep_01JH3ZA3D5E7F9G1H3J5K7M9N1";

        private static AssistantToolPolicy ExampleToolPolicy()
        {
            return new AssistantToolPolicy
            {
                EnableToolCalls = true,
                ToolChoiceMode = "Auto",
                MaxToolIterations = 6,
                MaxToolCallsPerTurn = 12,
                EnableCollectionSearchTool = true,
                EnableCollectionReadChunksTool = true,
                EnableWebSearchTool = false,
                AllowedToolNames = new List<string>(),
                EnableSlackToolProgressMessages = true
            };
        }

        private static AssistantSettings ExampleSettings()
        {
            return new AssistantSettings
            {
                Id = _SettingsId,
                AssistantId = ApiExamples.AssistantId,
                Temperature = 0.7,
                TopP = 1.0,
                SystemPrompt = "You are a helpful support assistant. Use the provided context to answer questions accurately.",
                MaxTokens = 4096,
                ContextWindow = 8192,
                EnableRag = true,
                EnableRetrievalGate = false,
                EnableQueryRewrite = false,
                EnableReranking = false,
                RerankerTopK = 5,
                RerankerScoreThreshold = 3.0,
                EnableCitations = true,
                CitationLinkMode = "Authenticated",
                EnableDocumentAttachments = false,
                DocumentAttachmentMaxCount = 10,
                CollectionId = ApiExamples.CollectionId,
                RetrievalTopK = 10,
                RetrievalScoreThreshold = 0.3,
                SearchMode = "Hybrid",
                TextWeight = 0.3,
                FusionStrategy = "Rrf",
                RrfK = 60,
                RecencyWeight = 0.0,
                ContextOrder = "Score",
                FullTextSearchType = "TsRank",
                FullTextLanguage = "english",
                FullTextNormalization = 32,
                RetrievalIncludeNeighbors = 1,
                InferenceEndpointId = _InferenceEndpointId,
                ToolRoutingInferenceEndpointId = _ToolRoutingEndpointId,
                EmbeddingEndpointId = _EmbeddingEndpointId,
                Title = "Acme Support",
                LogoUrl = "https://acme.example/logo.png",
                FaviconUrl = "https://acme.example/favicon.ico",
                Streaming = true,
                EnableSlack = true,
                SlackAppToken = "xapp-***",
                SlackBotToken = "xoxb-***",
                SlackChannelId = "C12345678",
                SlackMessagePrefix = "Hey bot,",
                ToolPolicy = ExampleToolPolicy(),
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Updated
            };
        }

        private static List<AssistantToolDescriptor> ExampleTools()
        {
            return new List<AssistantToolDescriptor>
            {
                new AssistantToolDescriptor
                {
                    ToolName = "collection_search",
                    DisplayName = "Collection Search",
                    Category = "Collection",
                    EnabledByPolicy = true,
                    Available = true,
                    UnavailableReason = null
                },
                new AssistantToolDescriptor
                {
                    ToolName = "collection_read_chunks",
                    DisplayName = "Collection Chunk Read",
                    Category = "Collection",
                    EnabledByPolicy = true,
                    Available = true,
                    UnavailableReason = null
                },
                new AssistantToolDescriptor
                {
                    ToolName = "web_search",
                    DisplayName = "Web Search",
                    Category = "Web",
                    EnabledByPolicy = false,
                    Available = false,
                    UnavailableReason = "Disabled by assistant tool policy."
                }
            };
        }

        private static AssistantToolPolicyValidationRequest ExampleValidationRequest()
        {
            return new AssistantToolPolicyValidationRequest
            {
                ToolPolicyJson = null,
                ToolPolicy = ExampleToolPolicy()
            };
        }

        private static AssistantToolPolicyValidationResult ExampleValidationResult()
        {
            AssistantToolPolicy policy = ExampleToolPolicy();
            policy.Normalize();

            return new AssistantToolPolicyValidationResult
            {
                Success = true,
                Message = "Tool policy is valid.",
                ToolPolicy = policy,
                ToolPolicyJson = JsonSerializer.Serialize(policy),
                Tools = ExampleTools(),
                Errors = new List<string>(),
                ErrorCodes = new List<string>()
            };
        }

        private static AssistantToolPolicyTestResult ExampleTestResult()
        {
            return new AssistantToolPolicyTestResult
            {
                Success = false,
                Message = "Tool diagnostics found blocking issues.",
                AssistantId = ApiExamples.AssistantId,
                InferenceEndpointId = _InferenceEndpointId,
                ToolRoutingInferenceEndpointId = _ToolRoutingEndpointId,
                EffectiveToolRoutingInferenceEndpointId = _ToolRoutingEndpointId,
                EndpointResolved = true,
                EndpointModel = "qwen3:8b",
                EndpointApiFormat = "Ollama",
                EndpointActive = true,
                EndpointSupportsToolCalling = false,
                EndpointToolCallingApiFormat = null,
                EndpointSupportsParallelToolCalls = false,
                EndpointSupportsStreamingToolCalls = false,
                Validation = ExampleValidationResult(),
                Tools = ExampleTools(),
                Warnings = new List<string>(),
                Errors = new List<string>
                {
                    "The effective tool-routing completion endpoint does not explicitly support tool calling."
                },
                ErrorCodes = new List<string>
                {
                    "tool_routing_endpoint_not_tool_capable"
                }
            };
        }

        private static SlackVerificationRequest ExampleSlackRequest()
        {
            return new SlackVerificationRequest
            {
                EnableSlack = true,
                SlackAppToken = "xapp-***",
                SlackBotToken = "xoxb-***",
                SlackChannelId = "C12345678",
                SlackMessagePrefix = "Hey bot,"
            };
        }

        private static SlackVerificationResponse ExampleSlackResponse()
        {
            return new SlackVerificationResponse
            {
                Success = true,
                BotToken = new SlackVerificationCheck
                {
                    Success = true,
                    Message = "Bot token is valid.",
                    Details = new
                    {
                        TeamId = "T12345678",
                        TeamName = "Acme",
                        UserId = "U12345678",
                        UserName = "acme-support-bot",
                        BotId = "B12345678",
                        Error = (string)null
                    }
                },
                Channel = new SlackVerificationCheck
                {
                    Success = true,
                    Message = "Channel lookup succeeded.",
                    Details = new
                    {
                        ChannelId = "C12345678",
                        Name = "support",
                        IsChannel = true,
                        IsPrivate = false,
                        Error = (string)null
                    }
                },
                SocketMode = new SlackVerificationCheck
                {
                    Success = true,
                    Message = "Socket Mode connection succeeded.",
                    Details = null
                }
            };
        }

        private static AssistantRetrieveRequest ExampleRetrieveRequest()
        {
            return new AssistantRetrieveRequest
            {
                Query = "What is the per diem in Rotterdam?",
                Messages = null,
                MetadataFilter = new ChatMetadataFilter
                {
                    RequiredLabels = new List<string> { "policy" }
                },
                AttachedDocumentIds = null,
                IncludeStages = true,
                IncludeAnswerability = true
            };
        }

        private static RetrievalChunk ExampleChunk()
        {
            return new RetrievalChunk
            {
                DocumentId = ApiExamples.DocumentId,
                Score = 0.82,
                VectorScore = 0.71,
                TextScore = 0.08,
                VectorRank = 1,
                TextRank = 1,
                Content = "Rotterdam: EUR 185 per night for lodging and EUR 60 per day for meals and incidentals.",
                Position = 4
            };
        }

        private static AssistantRetrieveResponse ExampleRetrieveResponse()
        {
            return new AssistantRetrieveResponse
            {
                AssistantId = ApiExamples.AssistantId,
                CollectionId = ApiExamples.CollectionId,
                SearchMode = "Hybrid",
                Retrieved = true,
                GateDecision = null,
                GateDurationMs = 0,
                Queries = new List<string> { "What is the per diem in Rotterdam?" },
                QueryRewriteDurationMs = 0,
                RetrievalDurationMs = 41.2,
                HybridFallbackRan = false,
                EmbeddingFailed = false,
                KeywordFallbackRan = false,
                RerankDurationMs = 0,
                RerankInputCount = 0,
                RerankOutputCount = 0,
                RerankParseFailed = false,
                AnswerabilityDecision = "not_checked",
                QueryClass = null,
                AnswerabilityReason = null,
                AnswerabilityParseFailed = false,
                AnswerabilityDurationMs = 0,
                DroppedCandidates = new List<RetrievalCandidateDropSummary>(),
                Chunks = new List<RetrievalChunk> { ExampleChunk() },
                Stages = new List<RetrievalStageSnapshot>
                {
                    new RetrievalStageSnapshot
                    {
                        Stage = "search",
                        Query = "What is the per diem in Rotterdam?",
                        Chunks = new List<RetrievalChunk> { ExampleChunk() }
                    },
                    new RetrievalStageSnapshot
                    {
                        Stage = "fused",
                        Chunks = new List<RetrievalChunk> { ExampleChunk() }
                    }
                },
                TotalDurationMs = 44.9
            };
        }

        #endregion

        #region Public-Members

        /// <summary>GET /v1.0/assistants/{assistantId}/settings.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get assistant settings", _Tag)
            .Describe("Returns the settings for an assistant, including retrieval, inference endpoint, branding, Slack, and tool policy configuration. Owner or administrator only; assistants in another tenant return 404 unless the caller is a global administrator. Returns 404 when the assistant has no settings record.")
            .Returns(200, "Assistant settings.", ExampleSettings())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>PUT /v1.0/assistants/{assistantId}/settings.</summary>
        public static OpenApiRouteMetadata Update => ApiDoc.Create("Update assistant settings", _Tag)
            .Describe("Creates or replaces the settings for an assistant. Owner or administrator only. The body is validated: InferenceEndpointId is required; SearchMode must be Vector, FullText, or Hybrid; QueryRewritePrompt must contain {prompt}; RerankPrompt must contain {query} and {chunks}; "
                + "SlackAppToken must start with xapp- and SlackBotToken with xoxb-, and all four Slack fields are required when EnableSlack is true; ToolPolicyJson must be valid AssistantToolPolicy JSON. "
                + "Values are normalized: endpoint identifiers are trimmed (blank optional endpoints become null), DocumentAttachmentMaxCount is clamped to 1-100, TextWeight to 0.0-1.0, RetrievalIncludeNeighbors to 0-10, and an unknown FullTextSearchType falls back to TsRank. "
                + "Id, AssistantId, and CreatedUtc are assigned by the server. When the Slack configuration changes, the assistant's Slack connection is refreshed in the background.")
            .Body("Complete assistant settings. Omitted fields take their defaults.", ExampleSettings())
            .Returns(200, "Saved assistant settings.", ExampleSettings())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>POST /v1.0/assistants/{assistantId}/settings/slack/verify.</summary>
        public static OpenApiRouteMetadata VerifySlack => ApiDoc.Create("Verify Slack settings", _Tag)
            .Describe("Verifies draft Slack settings without saving them: validates the bot token, looks up the channel, and opens (then closes) a Socket Mode connection with a 10-second timeout. All four Slack fields are required; SlackAppToken must start with xapp- and SlackBotToken with xoxb-. "
                + "Individual check failures are reported with Success false in the 200 response rather than as an HTTP error. Owner or administrator only.")
            .Body("Draft Slack settings to verify.", ExampleSlackRequest())
            .Returns(200, "Verification results for each check.", ExampleSlackResponse())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>POST /v1.0/assistants/{assistantId}/settings/tools/validate.</summary>
        public static OpenApiRouteMetadata ValidateTools => ApiDoc.Create("Validate tool policy", _Tag)
            .Describe("Validates a draft assistant tool policy without saving it and returns the normalized policy with the effective tool descriptors it would produce. Supply either ToolPolicy or ToolPolicyJson; an empty body validates the default policy. "
                + "Policy problems are reported as 200 with Success false and entries in Errors and ErrorCodes (invalid_tool_policy_json, unknown_allowed_tool, no_tool_enabled, no_available_tools). Malformed request JSON returns 400. Owner or administrator only; returns 404 when the assistant or its settings do not exist.")
            .Body("Draft tool policy to validate.", ExampleValidationRequest(), false)
            .Returns(200, "Validation result.", ExampleValidationResult())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>POST /v1.0/assistants/{assistantId}/settings/tools/test.</summary>
        public static OpenApiRouteMetadata TestTools => ApiDoc.Create("Test tool policy", _Tag)
            .Describe("Administrator dry-run diagnostics for a draft tool policy. Validates the policy as the validate route does, then resolves the effective tool-routing completion endpoint (ToolRoutingInferenceEndpointId, falling back to InferenceEndpointId) and checks that it is active and supports a compatible tool-calling format. "
                + "Does not save the policy, call the model, or execute tools. Diagnostic error codes include completion_endpoint_missing, tool_routing_endpoint_missing, tool_routing_endpoint_unresolved, tool_routing_endpoint_inactive, tool_routing_endpoint_not_tool_capable, and unsupported_tool_call_format, plus any validation codes. "
                + "Global or tenant administrators only; an empty body tests the default policy.")
            .Body("Draft tool policy to test.", ExampleValidationRequest(), false)
            .Returns(200, "Diagnostic result.", ExampleTestResult())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>GET /v1.0/assistants/{assistantId}/tools.</summary>
        public static OpenApiRouteMetadata Tools => ApiDoc.Create("Get assistant tools", _Tag)
            .Describe("Returns every known model-callable tool with its effective availability for the assistant, including tools disabled by policy and tools enabled by policy but unavailable because server prerequisites are missing. Reasons are non-secret. Owner or administrator only; returns 404 when the assistant or its settings do not exist.")
            .Returns(200, "Effective tool descriptors.", ExampleTools())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>POST /v1.0/assistants/{assistantId}/retrieve.</summary>
        public static OpenApiRouteMetadata Retrieve => ApiDoc.Create("Run retrieval only", _Tag)
            .Describe("Runs the assistant's retrieval pipeline exactly as chat does (retrieval gate, query rewrite, search with multi-query fusion, attached-document filtering, re-ranking, and the answerability check when enabled) without final inference and without writing chat history. "
                + "Either query or messages is required; when both are given, query is appended as the last user message. Utility model calls still run when enabled in the assistant settings. "
                + "Global or tenant administrators only. Returns 404 when the assistant is missing or inactive or has no settings, and 400 when no query is supplied, the JSON is malformed, or attached documents are invalid.")
            .Body("Retrieval request.", ExampleRetrieveRequest())
            .Returns(200, "Retrieval result.", ExampleRetrieveResponse())
            .Errors(400, 401, 403, 404, 500);

        #endregion
    }
}
