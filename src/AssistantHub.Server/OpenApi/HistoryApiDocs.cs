namespace AssistantHub.Server.OpenApi
{
    using System;
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for chat history and thread routes.
    /// </summary>
    public static class HistoryApiDocs
    {
        #region Private-Members

        private const string _Tag = "History";

        private const string _HistoryId = "chist_01JH4B3C4D5E6F7G8H9J0K1M2N";
        private const string _TraceId = "trace_01JH4B3D5E7F9G1H3J5K7M9N1P";
        private const string _RequestHistoryId = "req_01JH4B3E6F8G0H2J4K6M8N0P2Q";

        private static ChatHistory ExampleHistory()
        {
            DateTime userMessageUtc = ApiExamples.Created;

            return new ChatHistory
            {
                Id = _HistoryId,
                TraceId = _TraceId,
                RequestHistoryId = _RequestHistoryId,
                PerformanceSchemaVersion = 1,
                PerformanceJson = "{\"SchemaVersion\":1,\"TraceId\":\"" + _TraceId + "\",\"Stages\":[{\"Name\":\"final_inference\",\"Kind\":\"inference\",\"DurationMs\":890.75}]}",
                TenantId = ApiExamples.TenantId,
                ThreadId = ApiExamples.ThreadId,
                AssistantId = ApiExamples.AssistantId,
                CollectionId = ApiExamples.CollectionId,
                UserMessageUtc = userMessageUtc,
                UserMessage = "How do I reset my password?",
                RetrievalStartUtc = userMessageUtc.AddMilliseconds(100),
                RetrievalDurationMs = 45.23,
                RetrievalGateDecision = "RETRIEVE",
                RetrievalGateDurationMs = 120.5,
                QueryRewriteDurationMs = 0,
                RerankDurationMs = 0,
                RerankInputCount = 0,
                RerankOutputCount = 0,
                QueryClass = "factual_lookup",
                AnswerabilityDecision = "answerable",
                AnswerabilityReason = "Retrieved context contains the password reset procedure.",
                DroppedCandidateCount = 2,
                DroppedCandidateSummaryJson = "[{\"Reason\":\"below_score_threshold\",\"Count\":2}]",
                FinalCitationCount = 1,
                RetrievalContext = "Chunk 1: To reset your password, open Settings > Security and select Reset password...",
                PromptSentUtc = userMessageUtc.AddMilliseconds(150),
                PromptTokens = 1250,
                EndpointResolutionDurationMs = 45.12,
                CompactionDurationMs = 0,
                InferenceConnectionDurationMs = 850.0,
                TimeToFirstTokenMs = 120.5,
                TimeToLastTokenMs = 890.75,
                CompletionTokens = 87,
                TokensPerSecondOverall = 97.65,
                TokensPerSecondGeneration = 112.95,
                Origin = "web",
                AssistantResponse = "To reset your password, navigate to Settings > Security and select Reset password [1].",
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Created
            };
        }

        #endregion

        #region Public-Members

        /// <summary>GET /v1.0/history.</summary>
        public static OpenApiRouteMetadata List => ApiDoc.Create("List chat history", _Tag)
            .Describe("Enumerates chat history turns in the caller's tenant. Each record is one user message and assistant response in a thread, with retrieval, inference timing, and telemetry details. Global and tenant administrators see all history in the tenant; other users only see history for assistants they own (filtering is applied to the returned page, so a page may contain fewer than maxResults records).")
            .Paged()
            .Query("assistantId", "string", "Only return history for this assistant.", false, ApiExamples.AssistantId)
            .Query("threadId", "string", "Only return history for this conversation thread.", false, ApiExamples.ThreadId)
            .Returns(200, "Page of chat history records.", ApiExamples.Page(ExampleHistory()))
            .Errors(401, 500);

        /// <summary>GET /v1.0/history/{historyId}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get chat history entry", _Tag)
            .Describe("Returns one chat history turn. The record must belong to the caller's tenant (global administrators can read any tenant). Non-administrators must own the assistant the history belongs to.")
            .Returns(200, "Chat history record.", ExampleHistory())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/history/{historyId}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete chat history entry", _Tag)
            .Describe("Permanently deletes one chat history turn. The record must belong to the caller's tenant (global administrators can delete any tenant's history). Non-administrators must own the assistant the history belongs to. Linked request-history entries are not deleted.")
            .ReturnsNoContent(204, "Chat history entry deleted.")
            .Errors(400, 401, 403, 404, 500);

        /// <summary>GET /v1.0/threads.</summary>
        public static OpenApiRouteMetadata Threads => ApiDoc.Create("List conversation threads", _Tag)
            .Describe("Groups one page of chat history records (selected by the standard enumeration parameters and filters) by thread and returns a summary per thread. The response is a plain array, not a paged envelope. Non-administrators only see threads for assistants they own.")
            .Paged()
            .Query("assistantId", "string", "Only include history for this assistant.", false, ApiExamples.AssistantId)
            .Query("threadId", "string", "Only include history for this conversation thread.", false, ApiExamples.ThreadId)
            .Returns(200, "Thread summaries.", new[]
            {
                new
                {
                    ThreadId = ApiExamples.ThreadId,
                    AssistantId = ApiExamples.AssistantId,
                    FirstMessageUtc = ApiExamples.Created,
                    LastMessageUtc = ApiExamples.Created.AddMinutes(5),
                    TurnCount = 5
                }
            })
            .Errors(401, 500);

        #endregion
    }
}
