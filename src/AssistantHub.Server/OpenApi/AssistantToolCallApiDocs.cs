namespace AssistantHub.Server.OpenApi
{
    using System;
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for assistant tool-call trace routes.
    /// </summary>
    public static class AssistantToolCallApiDocs
    {
        #region Private-Members

        private const string _Tag = "Assistant Tool Calls";

        private static readonly DateTime _Started = new DateTime(2026, 1, 16, 9, 44, 58, DateTimeKind.Utc);

        private static AssistantToolCallRecord ExampleRecord()
        {
            return new AssistantToolCallRecord
            {
                Id = "atc_01JH3ZC1A3B5C7D9E1F3G5H7J9",
                TenantId = ApiExamples.TenantId,
                AssistantId = ApiExamples.AssistantId,
                ChatHistoryId = "chist_01JH3ZB5F7G9H1J3K5M7N9P1Q3",
                RequestHistoryId = "req_01JH3ZB4E6F8G0H2J4K6M8N0P2",
                TraceId = "trace_01JH3ZB6G8H0J2K4M6N8P0Q2R4",
                ThreadId = ApiExamples.ThreadId,
                Origin = "web",
                TurnIndex = 0,
                Iteration = 1,
                SequenceNumber = 1,
                ProviderToolCallId = "call_01JH3ZC2B4C6D8E0",
                ToolName = "collection_search",
                ArgumentsJson = "{\"query\":\"password reset procedure\",\"top_k\":5}",
                OutputJson = "{\"success\":true,\"output_characters\":512,\"truncated\":false}",
                ResultSummaryJson = "{\"Success\":true,\"Tool\":\"collection_search\",\"Denied\":false,\"Truncated\":false,\"OutputCharacters\":512,\"DurationMs\":42.5,\"ErrorCode\":null,\"Error\":null}",
                Success = true,
                Denied = false,
                Truncated = false,
                OutputCharacters = 512,
                InputBytes = 48,
                OutputBytes = 58,
                DurationMs = 42.5,
                ErrorType = null,
                ErrorMessage = null,
                Provider = "Ollama",
                Model = "qwen3:8b",
                Active = true,
                StartedUtc = _Started,
                FinishedUtc = _Started.AddMilliseconds(42.5),
                CreatedUtc = ApiExamples.Updated,
                LastUpdateUtc = ApiExamples.Updated
            };
        }

        private static ApiDoc Filters(ApiDoc doc)
        {
            return doc
                .Query("toolName", "string", "Only include tool calls with this tool name.", false, "collection_search")
                .Query("traceId", "string", "Only include tool calls with this trace identifier.", false, "trace_01JH3ZB6G8H0J2K4M6N8P0Q2R4")
                .Query("requestHistoryId", "string", "Only include tool calls linked to this request-history record.", false, "req_01JH3ZB4E6F8G0H2J4K6M8N0P2")
                .Query("chatHistoryId", "string", "Only include tool calls linked to this chat-history record.", false, "chist_01JH3ZB5F7G9H1J3K5M7N9P1Q3")
                .Query("threadId", "string", "Only include tool calls from this conversation thread.", false, ApiExamples.ThreadId)
                .Query("success", "boolean", "Only include successful (true) or failed (false) tool calls. Accepts true, false, 1, or 0.", false, true)
                .Query("denied", "boolean", "Only include tool calls that were (true) or were not (false) denied by policy. Accepts true, false, 1, or 0.", false, false)
                .Query("startUtc", "string", "Only include tool calls created at or after this UTC timestamp (ISO 8601).", false, "2026-01-15T00:00:00Z")
                .Query("endUtc", "string", "Only include tool calls created at or before this UTC timestamp (ISO 8601).", false, "2026-01-16T23:59:59Z");
        }

        #endregion

        #region Public-Members

        /// <summary>GET /v1.0/assistants/{assistantId}/tool-calls.</summary>
        public static OpenApiRouteMetadata List => Filters(ApiDoc.Create("List assistant tool calls", _Tag)
                .Describe("Enumerates redacted traces of model-directed tool calls made by an assistant. Arguments and outputs are redacted summaries. Global or tenant administrators only (403 otherwise); assistants in another tenant return 404 unless the caller is a global administrator.")
                .Paged())
            .Returns(200, "Page of tool-call records.", ApiExamples.Page(ExampleRecord()))
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/assistants/{assistantId}/tool-calls.</summary>
        public static OpenApiRouteMetadata DeleteMany => Filters(ApiDoc.Create("Delete assistant tool calls", _Tag)
                .Describe("Deletes every tool-call record of the assistant that matches the supplied filters; with no filters, all of the assistant's tool-call records are deleted. Paging parameters are ignored. Returns the number of records deleted. Global or tenant administrators only."))
            .Returns(200, "Number of records deleted.", new { DeletedCount = 42 })
            .Errors(400, 401, 403, 404, 500);

        /// <summary>GET /v1.0/assistants/{assistantId}/tool-calls/{toolCallRecordId}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get assistant tool call", _Tag)
            .Describe("Returns one redacted tool-call record. The record must belong to the assistant in the path and to the caller's tenant (unless the caller is a global administrator), otherwise 404. Global or tenant administrators only.")
            .Returns(200, "Tool-call record.", ExampleRecord())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/assistants/{assistantId}/tool-calls/{toolCallRecordId}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete assistant tool call", _Tag)
            .Describe("Deletes one tool-call record. The record must belong to the assistant in the path and to the caller's tenant (unless the caller is a global administrator), otherwise 404. Global or tenant administrators only.")
            .ReturnsNoContent(204, "Tool-call record deleted.")
            .Errors(400, 401, 403, 404, 500);

        #endregion
    }
}
