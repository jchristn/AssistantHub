namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Captured HTTP request and response entry.
    /// </summary>
    public class RequestHistoryEntry
    {
        /// <summary>
        /// Entry identifier.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Trace identifier shared by request history and assistant history.
        /// </summary>
        [JsonPropertyName("TraceId")]
        public string TraceId { get; set; }

        /// <summary>
        /// Chat history identifier associated with this request.
        /// </summary>
        [JsonPropertyName("ChatHistoryId")]
        public string ChatHistoryId { get; set; }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        [JsonPropertyName("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// User identifier.
        /// </summary>
        [JsonPropertyName("UserId")]
        public string UserId { get; set; }

        /// <summary>
        /// Credential identifier.
        /// </summary>
        [JsonPropertyName("CredentialId")]
        public string CredentialId { get; set; }

        /// <summary>
        /// Assistant identifier.
        /// </summary>
        [JsonPropertyName("AssistantId")]
        public string AssistantId { get; set; }

        /// <summary>
        /// Thread identifier.
        /// </summary>
        [JsonPropertyName("ThreadId")]
        public string ThreadId { get; set; }

        /// <summary>
        /// Principal display name.
        /// </summary>
        [JsonPropertyName("PrincipalName")]
        public string PrincipalName { get; set; }

        /// <summary>
        /// Request type.
        /// </summary>
        [JsonPropertyName("RequestType")]
        public string RequestType { get; set; }

        /// <summary>
        /// Source classification.
        /// </summary>
        [JsonPropertyName("SourceType")]
        public string SourceType { get; set; }

        /// <summary>
        /// HTTP method.
        /// </summary>
        [JsonPropertyName("HttpMethod")]
        public string HttpMethod { get; set; }

        /// <summary>
        /// Matched route template when known.
        /// </summary>
        [JsonPropertyName("RouteTemplate")]
        public string RouteTemplate { get; set; }

        /// <summary>
        /// Request path.
        /// </summary>
        [JsonPropertyName("RequestPath")]
        public string RequestPath { get; set; }

        /// <summary>
        /// Full request URL.
        /// </summary>
        [JsonPropertyName("RequestUrl")]
        public string RequestUrl { get; set; }

        /// <summary>
        /// Source IP address.
        /// </summary>
        [JsonPropertyName("SourceIp")]
        public string SourceIp { get; set; }

        /// <summary>
        /// Response status code.
        /// </summary>
        [JsonPropertyName("StatusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// Whether the request succeeded.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// Request duration in milliseconds.
        /// </summary>
        [JsonPropertyName("DurationMs")]
        public double DurationMs { get; set; }

        /// <summary>
        /// Request content type.
        /// </summary>
        [JsonPropertyName("RequestContentType")]
        public string RequestContentType { get; set; }

        /// <summary>
        /// Response content type.
        /// </summary>
        [JsonPropertyName("ResponseContentType")]
        public string ResponseContentType { get; set; }

        /// <summary>
        /// Request size in bytes.
        /// </summary>
        [JsonPropertyName("RequestSizeBytes")]
        public long RequestSizeBytes { get; set; }

        /// <summary>
        /// Response size in bytes.
        /// </summary>
        [JsonPropertyName("ResponseSizeBytes")]
        public long ResponseSizeBytes { get; set; }

        /// <summary>
        /// Whether request body capture was truncated.
        /// </summary>
        [JsonPropertyName("RequestBodyTruncated")]
        public bool RequestBodyTruncated { get; set; }

        /// <summary>
        /// Whether response body capture was truncated.
        /// </summary>
        [JsonPropertyName("ResponseBodyTruncated")]
        public bool ResponseBodyTruncated { get; set; }

        /// <summary>
        /// Whether the request body is represented as a binary placeholder.
        /// </summary>
        [JsonPropertyName("RequestBodyIsBinary")]
        public bool RequestBodyIsBinary { get; set; }

        /// <summary>
        /// Whether the response body is represented as a binary placeholder.
        /// </summary>
        [JsonPropertyName("ResponseBodyIsBinary")]
        public bool ResponseBodyIsBinary { get; set; }

        /// <summary>
        /// Route parameters.
        /// </summary>
        [JsonPropertyName("RouteParameters")]
        public Dictionary<string, string> RouteParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Query parameters.
        /// </summary>
        [JsonPropertyName("QueryParameters")]
        public Dictionary<string, string> QueryParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Request headers.
        /// </summary>
        [JsonPropertyName("RequestHeaders")]
        public Dictionary<string, string> RequestHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Response headers.
        /// </summary>
        [JsonPropertyName("ResponseHeaders")]
        public Dictionary<string, string> ResponseHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Request body.
        /// </summary>
        [JsonPropertyName("RequestBody")]
        public string RequestBody { get; set; }

        /// <summary>
        /// Response body.
        /// </summary>
        [JsonPropertyName("ResponseBody")]
        public string ResponseBody { get; set; }

        /// <summary>
        /// Timestamp when the record was created.
        /// </summary>
        [JsonPropertyName("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated.
        /// </summary>
        [JsonPropertyName("LastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; }
    }
}
