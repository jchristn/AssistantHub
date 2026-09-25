namespace AssistantHub.Server.OpenApi
{
    using System.Collections.Generic;
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for request-history (HTTP audit log) routes.
    /// </summary>
    public static class RequestHistoryApiDocs
    {
        #region Private-Members

        private const string _Tag = "Request History";

        private const string _RequestId = "req_01JH4C4D5E6F7G8H9J0K1M2N3P";
        private const string _TraceId = "trace_01JH4B3D5E7F9G1H3J5K7M9N1P";
        private const string _ChatHistoryId = "chist_01JH4B3C4D5E6F7G8H9J0K1M2N";

        private static RequestHistoryEntry ExampleEntrySummary()
        {
            return new RequestHistoryEntry
            {
                Id = _RequestId,
                TraceId = _TraceId,
                ChatHistoryId = _ChatHistoryId,
                TenantId = ApiExamples.TenantId,
                UserId = ApiExamples.UserId,
                CredentialId = ApiExamples.CredentialId,
                AssistantId = ApiExamples.AssistantId,
                ThreadId = ApiExamples.ThreadId,
                PrincipalName = "admin@acme.example",
                RequestType = "AssistantApi",
                SourceType = "public-assistant",
                HttpMethod = "POST",
                RouteTemplate = "/v1.0/assistants/{assistantId}/chat",
                RequestPath = "/v1.0/assistants/" + ApiExamples.AssistantId + "/chat",
                RequestUrl = "/v1.0/assistants/" + ApiExamples.AssistantId + "/chat",
                SourceIp = "203.0.113.24",
                StatusCode = 200,
                Success = true,
                DurationMs = 842.6,
                RequestContentType = "application/json",
                ResponseContentType = "text/event-stream",
                RequestSizeBytes = 58,
                ResponseSizeBytes = 4096,
                RequestBodyTruncated = false,
                ResponseBodyTruncated = false,
                RequestBodyIsBinary = false,
                ResponseBodyIsBinary = false,
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Created
            };
        }

        private static RequestHistoryEntry ExampleEntryDetail()
        {
            RequestHistoryEntry entry = ExampleEntrySummary();
            entry.RouteParameters = new Dictionary<string, string> { { "assistantId", ApiExamples.AssistantId } };
            entry.QueryParameters = new Dictionary<string, string>();
            entry.RequestHeaders = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Authorization", "[redacted]" },
                { "X-Thread-ID", ApiExamples.ThreadId }
            };
            entry.ResponseHeaders = new Dictionary<string, string> { { "Content-Type", "text/event-stream" } };
            entry.RequestBody = "{\"messages\":[{\"role\":\"user\",\"content\":\"Hello\"}]}";
            entry.ResponseBody = "[server-sent events stream omitted]";
            return entry;
        }

        private static RequestHistorySummaryResult ExampleSummary()
        {
            return new RequestHistorySummaryResult
            {
                TotalCount = 42,
                TotalSuccess = 38,
                TotalFailure = 4,
                AverageDurationMs = 128.4,
                Buckets = new List<RequestHistorySummaryBucket>
                {
                    new RequestHistorySummaryBucket
                    {
                        BucketStartUtc = ApiExamples.Created,
                        BucketEndUtc = ApiExamples.Created.AddMinutes(15),
                        RequestCount = 12,
                        SuccessCount = 11,
                        FailureCount = 1,
                        AverageDurationMs = 110.2
                    },
                    new RequestHistorySummaryBucket
                    {
                        BucketStartUtc = ApiExamples.Created.AddMinutes(15),
                        BucketEndUtc = ApiExamples.Created.AddMinutes(30),
                        RequestCount = 30,
                        SuccessCount = 27,
                        FailureCount = 3,
                        AverageDurationMs = 135.6
                    }
                }
            };
        }

        private static ApiDoc Filters(ApiDoc doc)
        {
            return doc
                .Query("startUtc", "string", "Inclusive lower bound on CreatedUtc (ISO 8601 UTC timestamp).", false, "2026-01-15T00:00:00Z")
                .Query("endUtc", "string", "Inclusive upper bound on CreatedUtc (ISO 8601 UTC timestamp).", false, "2026-01-16T00:00:00Z")
                .Query("method", "string", "HTTP method filter (case-insensitive), for example GET or POST.", false, "POST")
                .Query("path", "string", "Request-path substring filter.", false, "/chat")
                .Query("statusCode", "integer", "Exact HTTP response status code filter.", false, 200)
                .Query("success", "boolean", "Filter to successful (true or 1) or failed (false or 0) requests.", false, true)
                .Query("tenantId", "string", "Tenant filter. Honored for global administrators only; tenant administrators are always restricted to their own tenant.", false, ApiExamples.TenantId)
                .Query("userId", "string", "User identifier filter.", false, ApiExamples.UserId)
                .Query("credentialId", "string", "Credential identifier filter.", false, ApiExamples.CredentialId)
                .Query("assistantId", "string", "Assistant identifier filter.", false, ApiExamples.AssistantId)
                .Query("threadId", "string", "Thread identifier filter.", false, ApiExamples.ThreadId)
                .Query("requestType", "string", "Request type filter: SystemApi or AssistantApi.", false, "AssistantApi")
                .Query("sourceType", "string", "Source classification filter: dashboard, api, public, or public-assistant.", false, "public-assistant")
                .Query("search", "string", "Free-text search across request path, request URL, principal name, request body, and response body.", false, "password");
        }

        private static ApiDoc Paging(ApiDoc doc)
        {
            return doc
                .Query("maxResults", "integer", "Maximum number of records to return (clamped to 1 through 1000, default 100).", false, 100)
                .Query("continuationToken", "string", "Continuation token (record offset) returned by the previous page.")
                .Query("ordering", "string", "Result ordering: CreatedAscending or CreatedDescending (default).", false, "CreatedDescending");
        }

        #endregion

        #region Public-Members

        /// <summary>GET /v1.0/requesthistory.</summary>
        public static OpenApiRouteMetadata List => Filters(Paging(ApiDoc.Create("List request history", _Tag)
            .Describe("Enumerates captured HTTP request/response entries. Global administrators can view every tenant; tenant administrators are restricted to their own tenant. Other callers receive 403. List entries are lightweight: route parameters, query parameters, headers, and bodies are not hydrated (use GET /v1.0/requesthistory/{requestId} for the full entry). When request-history capture is disabled in server settings an empty page is returned.")))
            .Returns(200, "Page of request-history entries.", ApiExamples.Page(ExampleEntrySummary()))
            .Errors(401, 403, 500);

        /// <summary>GET /v1.0/requesthistory/summary.</summary>
        public static OpenApiRouteMetadata Summary => Filters(ApiDoc.Create("Summarize request history", _Tag)
            .Describe("Aggregates request-history entries matching the filters into fixed-width time buckets for charts. When startUtc or endUtc are omitted the window defaults to the last 24 hours. Global administrators or tenant administrators only (tenant administrators are restricted to their own tenant). When request-history capture is disabled, empty buckets are returned."))
            .Query("bucketSeconds", "integer", "Bucket width in seconds (1 to 86400, default 900). Takes precedence over bucketMinutes.", false, 900)
            .Query("bucketMinutes", "integer", "Legacy bucket width in minutes (1 to 1440). Used only when bucketSeconds is not supplied.", false, 15)
            .Returns(200, "Bucketed request-history summary.", ExampleSummary())
            .Errors(401, 403, 500);

        /// <summary>GET /v1.0/requesthistory/{requestId}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get request-history entry", _Tag)
            .Describe("Returns one fully hydrated request-history entry including route parameters, query parameters, headers, and captured (possibly truncated or redacted) request and response bodies. Global administrators or tenant administrators only; the entry must belong to the caller's tenant unless the caller is a global administrator. Returns 404 when request-history capture is disabled.")
            .Returns(200, "Request-history entry.", ExampleEntryDetail())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>GET /v1.0/requesthistory/{requestId}/detail.</summary>
        public static OpenApiRouteMetadata ReadDetail => ApiDoc.Create("Get request-history entry detail", _Tag)
            .Describe("Alias of GET /v1.0/requesthistory/{requestId}; returns the fully hydrated request-history entry. Global administrators or tenant administrators only.")
            .Returns(200, "Request-history entry.", ExampleEntryDetail())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/requesthistory/{requestId}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete request-history entry", _Tag)
            .Describe("Permanently deletes one request-history entry. Global administrators or tenant administrators only; the entry must belong to the caller's tenant unless the caller is a global administrator. When request-history capture is disabled the call is a no-op that returns 204.")
            .ReturnsNoContent(204, "Entry deleted.")
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/requesthistory/bulk.</summary>
        public static OpenApiRouteMetadata DeleteBulk => Filters(ApiDoc.Create("Bulk delete request history", _Tag)
            .Describe("Deletes every request-history entry matching the supplied filters and returns the number removed. With no filters, all entries visible to the caller are deleted (all tenants for a global administrator, the caller's tenant for a tenant administrator). Global administrators or tenant administrators only. Returns a count of 0 when request-history capture is disabled."))
            .Returns(200, "Number of deleted entries.", new { DeletedCount = 17 })
            .Errors(401, 403, 500);

        #endregion
    }
}
