namespace AssistantHub.Server.OpenApi
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for completion endpoint routes (proxied to Partio).
    /// </summary>
    public static class CompletionEndpointApiDocs
    {
        #region Private-Members

        private const string _Tag = "Completion Endpoints";

        private const string _EndpointId = "cep_01JH3ZA1B3C5D7E9F1G3H5J7K9";

        private const string _Admin = "Global administrators only; other callers receive 403. ";

        private const string _Proxied = "The request is forwarded to Partio and Partio's HTTP status code is relayed; non-2xx bodies are relayed unchanged, so Partio validation (400) and not-found (404) errors pass through. ";

        private const string _ToolFields = "SupportsToolCalling, ToolCallingApiFormat (OllamaChat or OpenAIChatCompletions), SupportsParallelToolCalls, and SupportsStreamingToolCalls are stored in Partio as the reserved label 'assistanthub:tool-calling' and reserved tags 'AssistantHub.SupportsToolCalling', 'AssistantHub.ToolCallingApiFormat', 'AssistantHub.SupportsParallelToolCalls', and 'AssistantHub.SupportsStreamingToolCalls'; other labels and tags are preserved. ";

        private static PartioEndpointRequest ExampleEndpointRequest()
        {
            return new PartioEndpointRequest
            {
                Name = "Ollama Gemma",
                Model = "gemma3:4b",
                Endpoint = "http://ollama:11434",
                ApiFormat = "Ollama",
                ApiKey = null,
                Active = true,
                MaxConcurrentRequests = 2,
                MaxQueueDepth = 4,
                MaximumTimeoutMs = 120000,
                ContextSize = 8192,
                SupportsToolCalling = true,
                ToolCallingApiFormat = "OllamaChat",
                SupportsParallelToolCalls = false,
                SupportsStreamingToolCalls = false,
                EnableRequestHistory = true,
                Labels = new List<string> { "production" },
                Tags = new Dictionary<string, string> { { "owner", "assistant-team" } },
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
                Name = "Ollama Gemma",
                Model = "gemma3:4b",
                TenantId = "default",
                Endpoint = "http://ollama:11434",
                ApiFormat = "Ollama",
                ApiKey = null,
                Active = true,
                MaxConcurrentRequests = 2,
                MaxQueueDepth = 4,
                MaximumTimeoutMs = 120000,
                ContextSize = 8192,
                SupportsToolCalling = true,
                ToolCallingApiFormat = "OllamaChat",
                SupportsParallelToolCalls = false,
                SupportsStreamingToolCalls = false,
                Labels = new List<string> { "production", "assistanthub:tool-calling" },
                Tags = new Dictionary<string, string>
                {
                    { "owner", "assistant-team" },
                    { "AssistantHub.SupportsToolCalling", "true" },
                    { "AssistantHub.ToolCallingApiFormat", "OllamaChat" },
                    { "AssistantHub.SupportsParallelToolCalls", "false" },
                    { "AssistantHub.SupportsStreamingToolCalls", "false" }
                },
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
                EndpointName = "Ollama Gemma",
                TenantId = "default",
                IsHealthy = true,
                FirstCheckUtc = ApiExamples.Created,
                LastCheckUtc = ApiExamples.Updated,
                LastHealthyUtc = ApiExamples.Updated,
                LastUnhealthyUtc = ApiExamples.Created.AddMinutes(5),
                LastStateChangeUtc = ApiExamples.Created.AddMinutes(6),
                TotalUptimeMs = 58140000,
                TotalDowntimeMs = 60000,
                UptimePercentage = 99.9,
                ConsecutiveSuccesses = 1938,
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
                EndpointType = "Completion",
                EndpointId = _EndpointId,
                TenantId = "default",
                ApiFormat = "Ollama",
                Model = "gemma3:4b",
                Strategy = "NativeProviderLoad",
                Message = "Ollama accepted the preload request.",
                ResponseTimeMs = 482.5,
                StartedUtc = ApiExamples.Updated,
                CompletedUtc = ApiExamples.Updated.AddMilliseconds(482.5),
                RequestHistoryId = "req_01JH3ZB4E6F8G0H2J4K6M8N0P2",
                EmbeddingCalls = (List<EmbeddingCallDetail>)null,
                CompletionCalls = new List<CompletionCallDetail>()
            };
        }

        private static EndpointExplorerCompletionResponse ExampleTestResponse()
        {
            return new EndpointExplorerCompletionResponse
            {
                Success = true,
                StatusCode = 200,
                Error = null,
                EndpointId = _EndpointId,
                Model = "gemma3:4b",
                Prompt = "Respond with a one-sentence smoke test confirmation.",
                SystemPrompt = "You are a concise and accurate assistant.",
                Output = "AssistantHub can successfully reach this inference endpoint.",
                ResponseTimeMs = 418,
                RequestHistoryId = "req_01JH3ZB5F7G9H1J3K5M7N9P1Q3",
                CompletionCalls = new List<CompletionCallDetail>
                {
                    new CompletionCallDetail
                    {
                        Url = "http://ollama:11434/api/generate",
                        Method = "POST",
                        StatusCode = 200,
                        ResponseTimeMs = 394,
                        Success = true,
                        Error = null,
                        TimestampUtc = ApiExamples.Updated
                    }
                }
            };
        }

        #endregion

        #region Public-Members

        /// <summary>PUT /v1.0/endpoints/completion.</summary>
        public static OpenApiRouteMetadata Create => ApiDoc.Create("Create completion endpoint", _Tag)
            .Describe(_Admin + "Creates a completion (inference) endpoint in Partio. The caller's JSON is merged onto an empty object (TenantId defaults to \"default\") and forwarded to Partio PUT /v1.0/endpoints/completion. "
                + _ToolFields
                + "MaxConcurrentRequests (default 2), MaxQueueDepth (default 0), and MaximumTimeoutMs (default 60000) control Partio's per-endpoint concurrency: with MaxQueueDepth 0 requests over the concurrency limit are rejected with 429, while a positive value queues them until a slot frees or the request times out with 504. "
                + "On success the response is re-serialized with the tool-calling fields read back from the tags, and the endpoint is registered with AssistantHub's health-check service when HealthCheckEnabled and Active are true. "
                + _Proxied)
            .Body("Completion endpoint definition. Model, Endpoint, and ApiFormat are required by Partio.", ExampleEndpointRequest())
            .Returns(201, "Endpoint created.", ExampleEndpoint())
            .Errors(400, 401, 403, 500);

        /// <summary>POST /v1.0/endpoints/completion/enumerate.</summary>
        public static OpenApiRouteMetadata Enumerate => ApiDoc.Create("List completion endpoints", _Tag)
            .Describe(_Admin + "Forwards the request body unchanged to Partio POST /v1.0/endpoints/completion/enumerate and converts Partio's { Data, TotalCount, HasMore } envelope into the standard AssistantHub enumeration result, with tool-calling fields read back from each endpoint's tags. "
                + "On a non-2xx Partio response, Partio's status code and body are relayed unchanged.")
            .Body("Partio enumeration request. Optional; an empty body lists endpoints with Partio's defaults. Order is CreatedAscending, CreatedDescending, NameAscending, or NameDescending.", ExampleEnumerateRequest(), false)
            .Returns(200, "Page of completion endpoints.", ExampleEndpointPage())
            .Errors(400, 401, 403, 500);

        /// <summary>GET /v1.0/endpoints/completion/health.</summary>
        public static OpenApiRouteMetadata ListHealth => ApiDoc.Create("List completion endpoint health", _Tag)
            .Describe(_Admin + "Returns health status from AssistantHub's local health-check service (Partio is not called) for every monitored endpoint. Only active endpoints with HealthCheckEnabled are monitored. "
                + "The list is not filtered by endpoint type, so monitored embedding endpoints are included as well. Returns an empty array when nothing is monitored.")
            .Returns(200, "Health status of all monitored endpoints.", new List<EndpointHealthStatus> { ExampleHealth() })
            .Errors(401, 403, 500);

        /// <summary>GET /v1.0/endpoints/completion/{endpointId}/health.</summary>
        public static OpenApiRouteMetadata ReadHealth => ApiDoc.Create("Get completion endpoint health", _Tag)
            .Describe(_Admin + "Returns health status for one endpoint from AssistantHub's local health-check service (Partio is not called), including uptime percentage and the last 24 hours of check history. "
                + "Returns 404 when the endpoint is not being monitored (it does not exist, is inactive, or has HealthCheckEnabled false).")
            .Returns(200, "Endpoint health status.", ExampleHealth())
            .Errors(401, 403, 404, 500);

        /// <summary>POST /v1.0/endpoints/completion/{endpointId}/test.</summary>
        public static OpenApiRouteMetadata Test => ApiDoc.Create("Test completion endpoint", _Tag)
            .Describe(_Admin + "Runs a smoke test by sending the prompt to Partio POST /v1.0/completion with this endpoint. The endpointId in the path overrides any EndpointId in the body; property names are case-sensitive. "
                + "Partio's status code and response body are relayed unchanged; on failure the same shape is returned with Success false and Error set. Partio requires a non-empty Prompt.")
            .Body("Test prompt. MaxTokens defaults to 512 and TimeoutMs to 60000.", new EndpointExplorerCompletionRequest
            {
                EndpointId = _EndpointId,
                Prompt = "Respond with a one-sentence smoke test confirmation.",
                SystemPrompt = "You are a concise and accurate assistant.",
                MaxTokens = 512,
                TimeoutMs = 60000
            }, false)
            .Returns(200, "Completion test result.", ExampleTestResponse())
            .Errors(400, 401, 403, 404, 500)
            .Error(429, "Endpoint concurrency limit reached (relayed from Partio).", new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "Endpoint concurrency limit reached."))
            .Error(502, "Upstream provider failure (relayed from Partio).", new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Upstream provider failure."))
            .Error(504, "Upstream provider timeout (relayed from Partio).", new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Upstream provider timeout."));

        /// <summary>POST /v1.0/endpoints/completion/{endpointId}/load.</summary>
        public static OpenApiRouteMetadata Load => ApiDoc.Create("Load completion endpoint model", _Tag)
            .Describe(_Admin + "Loads or warms the endpoint's model through Partio POST /v1.0/endpoints/completion/{endpointId}/load. The body is forwarded unchanged (an empty body is sent as {}). "
                + "Partio's status code and model-load result are relayed unchanged; failures use the same response shape with Success false. "
                + "The X-Partio-Endpoint-Id, X-Model, and X-Partio-Model response headers are copied from Partio when present.")
            .Body("Model-load options. Optional; every field has a Partio default. Strategy is Auto, NativeProviderLoad, or WarmRequest.", ExampleLoadRequest(), false)
            .Returns(200, "Model loaded or warmed.", ExampleLoadResponse())
            .Errors(400, 401, 403, 404, 409, 500)
            .Error(429, "Endpoint concurrency limit reached (relayed from Partio).", new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "Endpoint concurrency limit reached."))
            .Error(502, "Partio or upstream provider failure (relayed from Partio).", new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Upstream provider failure."))
            .Error(504, "Upstream provider timeout (relayed from Partio).", new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Upstream provider timeout."));

        /// <summary>GET /v1.0/endpoints/completion/{endpointId}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get completion endpoint", _Tag)
            .Describe(_Admin + "Returns one completion endpoint from Partio GET /v1.0/endpoints/completion/{endpointId}, re-serialized with the tool-calling fields read back from its tags. " + _Proxied)
            .Returns(200, "Completion endpoint.", ExampleEndpoint())
            .Errors(401, 403, 404, 500);

        /// <summary>PUT /v1.0/endpoints/completion/{endpointId}.</summary>
        public static OpenApiRouteMetadata Update => ApiDoc.Create("Update completion endpoint", _Tag)
            .Describe(_Admin + "Partially updates a completion endpoint. AssistantHub reads the current endpoint from Partio and overlays only the fields supplied by the caller, preserving fields it does not manage. "
                + "Labels and Tags are unioned with the existing values (caller wins on tag conflicts), and a blank ApiKey keeps the stored key. Tool-calling fields are rewritten into the reserved label and tags only when the caller supplies at least one of them. "
                + "The merged object is sent to Partio PUT /v1.0/endpoints/completion/{endpointId}; on success the response is re-serialized with tool-calling fields and the health-check service is updated. " + _Proxied)
            .Body("Fields to change; omitted fields keep their current values.", ExampleEndpointRequest())
            .Returns(200, "Updated completion endpoint.", ExampleEndpoint())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/endpoints/completion/{endpointId}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete completion endpoint", _Tag)
            .Describe(_Admin + "Deletes a completion endpoint in Partio and stops its health checks. Partio's status code is relayed; a 204 has no body, any other status relays Partio's JSON body.")
            .ReturnsNoContent(204, "Endpoint deleted.")
            .Errors(401, 403, 404, 500);

        /// <summary>HEAD /v1.0/endpoints/completion/{endpointId}.</summary>
        public static OpenApiRouteMetadata Exists => ApiDoc.Create("Check completion endpoint existence", _Tag)
            .Describe(_Admin + "Relays Partio's HEAD status: 200 when the endpoint exists, 404 otherwise. No response body.")
            .ReturnsNoContent(200, "Endpoint exists.")
            .ReturnsNoContent(404, "Endpoint not found.")
            .ReturnsNoContent(403, "Authorization failed.")
            .ReturnsNoContent(500, "Internal error.");

        #endregion
    }
}
