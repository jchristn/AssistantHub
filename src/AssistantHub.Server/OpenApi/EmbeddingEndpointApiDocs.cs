namespace AssistantHub.Server.OpenApi
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for embedding endpoint routes (proxied to Partio).
    /// </summary>
    public static class EmbeddingEndpointApiDocs
    {
        #region Private-Members

        private const string _Tag = "Embedding Endpoints";

        private const string _EndpointId = "ep_01JH3ZA3D5E7F9G1H3J5K7M9N1";

        private const string _Admin = "Global administrators only; other callers receive 403. ";

        private const string _Proxied = "The request is forwarded to Partio and Partio's HTTP status code and JSON body are relayed unchanged, so Partio validation (400) and not-found (404) errors pass through; Partio's endpoint object also carries EnableRequestHistory, Tokenization, CreatedUtc, and LastUpdateUtc. ";

        private static PartioEndpointRequest ExampleEndpointRequest()
        {
            return new PartioEndpointRequest
            {
                Name = "Ollama Embeddings",
                Model = "all-minilm",
                Endpoint = "http://ollama:11434",
                ApiFormat = "Ollama",
                ApiKey = null,
                Active = true,
                MaxConcurrentRequests = 2,
                MaxQueueDepth = 0,
                MaximumTimeoutMs = 60000,
                EnableRequestHistory = true,
                Labels = new List<string> { "production" },
                Tags = new Dictionary<string, string> { { "owner", "platform-team" } },
                HealthCheckEnabled = true,
                HealthCheckUrl = "http://ollama:11434/api/tags",
                HealthCheckMethod = "GET",
                HealthCheckIntervalMs = 30000,
                HealthCheckTimeoutMs = 10000,
                HealthCheckExpectedStatusCode = 200,
                HealthyThreshold = 2,
                UnhealthyThreshold = 2,
                HealthCheckUseAuth = false
            };
        }

        private static PartioEndpointConfig ExampleEndpoint()
        {
            return new PartioEndpointConfig
            {
                Id = _EndpointId,
                Name = "Ollama Embeddings",
                Model = "all-minilm",
                TenantId = "default",
                Endpoint = "http://ollama:11434",
                ApiFormat = "Ollama",
                ApiKey = null,
                Active = true,
                MaxConcurrentRequests = 2,
                MaxQueueDepth = 0,
                MaximumTimeoutMs = 60000,
                ContextSize = 0,
                SupportsToolCalling = false,
                ToolCallingApiFormat = null,
                SupportsParallelToolCalls = false,
                SupportsStreamingToolCalls = false,
                Labels = new List<string> { "production" },
                Tags = new Dictionary<string, string> { { "owner", "platform-team" } },
                HealthCheckEnabled = true,
                HealthCheckUrl = "http://ollama:11434/api/tags",
                HealthCheckMethod = "GET",
                HealthCheckIntervalMs = 30000,
                HealthCheckTimeoutMs = 10000,
                HealthCheckExpectedStatusCode = 200,
                HealthyThreshold = 2,
                UnhealthyThreshold = 2,
                HealthCheckUseAuth = false
            };
        }

        private static EnumerationResult<PartioEndpointConfig> ExampleEndpointPage()
        {
            return new EnumerationResult<PartioEndpointConfig>
            {
                Success = true,
                MaxResults = 1,
                TotalRecords = 1,
                RecordsRemaining = 0,
                ContinuationToken = null,
                EndOfResults = true,
                Objects = new List<PartioEndpointConfig> { ExampleEndpoint() },
                TotalMs = 0
            };
        }

        private static object ExampleEnumerateRequest()
        {
            return new
            {
                MaxResults = 100,
                ContinuationToken = (string)null,
                Order = "CreatedDescending",
                NameFilter = (string)null,
                LabelFilter = "production",
                TagKeyFilter = (string)null,
                TagValueFilter = (string)null,
                ActiveFilter = (bool?)true
            };
        }

        private static EndpointHealthStatus ExampleHealth()
        {
            return new EndpointHealthStatus
            {
                EndpointId = _EndpointId,
                EndpointName = "Ollama Embeddings",
                TenantId = "default",
                IsHealthy = true,
                FirstCheckUtc = ApiExamples.Created,
                LastCheckUtc = ApiExamples.Updated,
                LastHealthyUtc = ApiExamples.Updated,
                LastUnhealthyUtc = null,
                LastStateChangeUtc = ApiExamples.Created,
                TotalUptimeMs = 58500000,
                TotalDowntimeMs = 0,
                UptimePercentage = 100.0,
                ConsecutiveSuccesses = 1950,
                ConsecutiveFailures = 0,
                LastError = null,
                History = new List<HealthCheckRecord>
                {
                    new HealthCheckRecord { TimestampUtc = ApiExamples.Updated.AddSeconds(-30), Success = true },
                    new HealthCheckRecord { TimestampUtc = ApiExamples.Updated, Success = true }
                }
            };
        }

        private static object ExampleLoadRequest()
        {
            return new
            {
                Strategy = "Auto",
                TimeoutMs = 60000,
                KeepAlive = "30m",
                SampleInput = "Partio model load probe",
                MaxTokens = 1,
                RecordRequestHistory = true,
                RequireNativeLoad = false
            };
        }

        private static object ExampleLoadResponse()
        {
            return new
            {
                Success = true,
                StatusCode = 200,
                Outcome = "Loaded",
                EndpointType = "Embedding",
                EndpointId = _EndpointId,
                TenantId = "default",
                ApiFormat = "Ollama",
                Model = "all-minilm",
                Strategy = "NativeProviderLoad",
                Message = "Ollama accepted the preload request.",
                ResponseTimeMs = 482.5,
                StartedUtc = ApiExamples.Updated,
                CompletedUtc = ApiExamples.Updated.AddMilliseconds(482.5),
                RequestHistoryId = "req_01JH3ZB4E6F8G0H2J4K6M8N0P2",
                EmbeddingCalls = new List<EmbeddingCallDetail>(),
                CompletionCalls = (List<CompletionCallDetail>)null
            };
        }

        #endregion

        #region Public-Members

        /// <summary>PUT /v1.0/endpoints/embedding.</summary>
        public static OpenApiRouteMetadata Create => ApiDoc.Create("Create embedding endpoint", _Tag)
            .Describe(_Admin + "Creates an embedding endpoint in Partio. The caller's JSON is merged onto an empty object (TenantId defaults to \"default\"; Labels and Tags are normalized) and forwarded to Partio PUT /v1.0/endpoints/embedding. "
                + "MaxConcurrentRequests (default 2), MaxQueueDepth (default 0), and MaximumTimeoutMs (default 60000) control Partio's per-endpoint concurrency: with MaxQueueDepth 0 requests over the concurrency limit are rejected with 429, while a positive value queues them until a slot frees or the request times out with 504. "
                + "On success the endpoint is registered with AssistantHub's health-check service when HealthCheckEnabled and Active are true. "
                + _Proxied)
            .Body("Embedding endpoint definition. Model, Endpoint, and ApiFormat are required by Partio.", ExampleEndpointRequest())
            .Returns(201, "Endpoint created (Partio's response, relayed unchanged).", ExampleEndpoint())
            .Errors(400, 401, 403, 500);

        /// <summary>POST /v1.0/endpoints/embedding/enumerate.</summary>
        public static OpenApiRouteMetadata Enumerate => ApiDoc.Create("List embedding endpoints", _Tag)
            .Describe(_Admin + "Forwards the request body unchanged to Partio POST /v1.0/endpoints/embedding/enumerate and converts Partio's { Data, TotalCount, HasMore } envelope into the standard AssistantHub enumeration result. "
                + "On a non-2xx Partio response, Partio's status code and body are relayed unchanged.")
            .Body("Partio enumeration request. Optional; an empty body lists endpoints with Partio's defaults. Order is CreatedAscending, CreatedDescending, NameAscending, or NameDescending.", ExampleEnumerateRequest(), false)
            .Returns(200, "Page of embedding endpoints.", ExampleEndpointPage())
            .Errors(400, 401, 403, 500);

        /// <summary>GET /v1.0/endpoints/embedding/health.</summary>
        public static OpenApiRouteMetadata ListHealth => ApiDoc.Create("List embedding endpoint health", _Tag)
            .Describe(_Admin + "Returns health status from AssistantHub's local health-check service (Partio is not called) for every monitored endpoint. Only active endpoints with HealthCheckEnabled are monitored. "
                + "The list is not filtered by endpoint type, so monitored completion endpoints are included as well. Returns an empty array when nothing is monitored.")
            .Returns(200, "Health status of all monitored endpoints.", new List<EndpointHealthStatus> { ExampleHealth() })
            .Errors(401, 403, 500);

        /// <summary>GET /v1.0/endpoints/embedding/{endpointId}/health.</summary>
        public static OpenApiRouteMetadata ReadHealth => ApiDoc.Create("Get embedding endpoint health", _Tag)
            .Describe(_Admin + "Returns health status for one endpoint from AssistantHub's local health-check service (Partio is not called), including uptime percentage and the last 24 hours of check history. "
                + "Returns 404 when the endpoint is not being monitored (it does not exist, is inactive, or has HealthCheckEnabled false).")
            .Returns(200, "Endpoint health status.", ExampleHealth())
            .Errors(401, 403, 404, 500);

        /// <summary>POST /v1.0/endpoints/embedding/{endpointId}/test.</summary>
        public static OpenApiRouteMetadata Test => ApiDoc.Create("Test embedding endpoint", _Tag)
            .Describe(_Admin + "Runs a smoke test by sending the input as a one-element batch to Partio POST /v1.0/embed with this endpoint, then maps the first vector into a single-embedding response. "
                + "The endpointId in the path overrides any EndpointId in the body; property names are case-sensitive. Upstream failures are reported inside the response body (Success false with the upstream StatusCode and Error) while the HTTP status stays 200. "
                + "RequestHistoryId and EmbeddingCalls are not populated by this route.")
            .Body("Test input. Optional; an empty body embeds an empty string.", new EndpointExplorerEmbeddingRequest
            {
                EndpointId = _EndpointId,
                Input = "AssistantHub embedding smoke test input",
                L2Normalization = false
            }, false)
            .Returns(200, "Test result. Check Success and StatusCode for the upstream outcome.", new EndpointExplorerEmbeddingResponse
            {
                Success = true,
                StatusCode = 200,
                Error = null,
                EndpointId = _EndpointId,
                Model = "all-minilm",
                Input = "AssistantHub embedding smoke test input",
                Embedding = new List<float> { 0.0123f, -0.0456f, 0.0789f, 0.0211f },
                Dimensions = 384,
                ResponseTimeMs = 243,
                RequestHistoryId = null,
                EmbeddingCalls = new List<EmbeddingCallDetail>()
            })
            .Errors(401, 403, 500);

        /// <summary>POST /v1.0/endpoints/embedding/{endpointId}/load.</summary>
        public static OpenApiRouteMetadata Load => ApiDoc.Create("Load embedding endpoint model", _Tag)
            .Describe(_Admin + "Loads or warms the endpoint's model through Partio POST /v1.0/endpoints/embedding/{endpointId}/load. The body is forwarded unchanged (an empty body is sent as {}). "
                + "Partio's status code and model-load result are relayed unchanged; failures use the same response shape with Success false. "
                + "The X-Partio-Endpoint-Id, X-Model, and X-Partio-Model response headers are copied from Partio when present.")
            .Body("Model-load options. Optional; every field has a Partio default.", ExampleLoadRequest(), false)
            .Returns(200, "Model loaded or warmed.", ExampleLoadResponse())
            .Errors(400, 401, 403, 404, 409, 500)
            .Error(429, "Endpoint concurrency limit reached (relayed from Partio).", new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "Endpoint concurrency limit reached."))
            .Error(502, "Partio or upstream provider failure (relayed from Partio).", new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Upstream provider failure."))
            .Error(504, "Upstream provider timeout (relayed from Partio).", new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Upstream provider timeout."));

        /// <summary>GET /v1.0/endpoints/embedding/{endpointId}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get embedding endpoint", _Tag)
            .Describe(_Admin + "Returns one embedding endpoint from Partio GET /v1.0/endpoints/embedding/{endpointId}. " + _Proxied)
            .Returns(200, "Embedding endpoint (Partio's response, relayed unchanged).", ExampleEndpoint())
            .Errors(401, 403, 404, 500);

        /// <summary>PUT /v1.0/endpoints/embedding/{endpointId}.</summary>
        public static OpenApiRouteMetadata Update => ApiDoc.Create("Update embedding endpoint", _Tag)
            .Describe(_Admin + "Partially updates an embedding endpoint. AssistantHub reads the current endpoint from Partio and overlays only the fields supplied by the caller, preserving fields it does not manage (health-check configuration, context size, other services' labels and tags). "
                + "Labels and Tags are unioned with the existing values (caller wins on tag conflicts), and a blank ApiKey keeps the stored key. "
                + "The merged object is sent to Partio PUT /v1.0/endpoints/embedding/{endpointId}; on success the health-check service is updated. " + _Proxied)
            .Body("Fields to change; omitted fields keep their current values.", ExampleEndpointRequest())
            .Returns(200, "Updated embedding endpoint (Partio's response, relayed unchanged).", ExampleEndpoint())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/endpoints/embedding/{endpointId}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete embedding endpoint", _Tag)
            .Describe(_Admin + "Deletes an embedding endpoint in Partio and stops its health checks. Partio's status code is relayed; a 204 has no body, any other status relays Partio's JSON body.")
            .ReturnsNoContent(204, "Endpoint deleted.")
            .Errors(401, 403, 404, 500);

        /// <summary>HEAD /v1.0/endpoints/embedding/{endpointId}.</summary>
        public static OpenApiRouteMetadata Exists => ApiDoc.Create("Check embedding endpoint existence", _Tag)
            .Describe(_Admin + "Relays Partio's HEAD status: 200 when the endpoint exists, 404 otherwise. No response body.")
            .ReturnsNoContent(200, "Endpoint exists.")
            .ReturnsNoContent(404, "Endpoint not found.")
            .ReturnsNoContent(403, "Authorization failed.")
            .ReturnsNoContent(500, "Internal error.");

        #endregion
    }
}
