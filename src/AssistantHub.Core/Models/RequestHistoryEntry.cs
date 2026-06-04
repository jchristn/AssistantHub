namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.Json;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Captured HTTP request and response entry.
    /// </summary>
    public class RequestHistoryEntry
    {
        #region Public-Members

        /// <summary>
        /// Entry identifier.
        /// </summary>
        public string Id
        {
            get => _Id;
            set => _Id = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Id));
        }

        /// <summary>
        /// Trace identifier shared by chat history, request history, and logs.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// Chat history identifier associated with this request, when available.
        /// </summary>
        public string ChatHistoryId { get; set; } = null;

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// User identifier.
        /// </summary>
        public string UserId { get; set; } = null;

        /// <summary>
        /// Credential identifier.
        /// </summary>
        public string CredentialId { get; set; } = null;

        /// <summary>
        /// Assistant identifier.
        /// </summary>
        public string AssistantId { get; set; } = null;

        /// <summary>
        /// Thread identifier.
        /// </summary>
        public string ThreadId { get; set; } = null;

        /// <summary>
        /// Principal display name.
        /// </summary>
        public string PrincipalName { get; set; } = null;

        /// <summary>
        /// Request type.
        /// </summary>
        public string RequestType { get; set; } = "SystemApi";

        /// <summary>
        /// Source classification.
        /// </summary>
        public string SourceType { get; set; } = "api";

        /// <summary>
        /// HTTP method.
        /// </summary>
        public string HttpMethod { get; set; } = "GET";

        /// <summary>
        /// Matched route template when known.
        /// </summary>
        public string RouteTemplate { get; set; } = null;

        /// <summary>
        /// Request path.
        /// </summary>
        public string RequestPath { get; set; } = "/";

        /// <summary>
        /// Full request URL.
        /// </summary>
        public string RequestUrl { get; set; } = "/";

        /// <summary>
        /// Source IP address.
        /// </summary>
        public string SourceIp { get; set; } = null;

        /// <summary>
        /// Response status code.
        /// </summary>
        public int StatusCode { get; set; } = 0;

        /// <summary>
        /// Whether the request succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Request duration in milliseconds.
        /// </summary>
        public double DurationMs { get; set; } = 0;

        /// <summary>
        /// Request content type.
        /// </summary>
        public string RequestContentType { get; set; } = null;

        /// <summary>
        /// Response content type.
        /// </summary>
        public string ResponseContentType { get; set; } = null;

        /// <summary>
        /// Request size in bytes.
        /// </summary>
        public long RequestSizeBytes { get; set; } = 0;

        /// <summary>
        /// Response size in bytes.
        /// </summary>
        public long ResponseSizeBytes { get; set; } = 0;

        /// <summary>
        /// Whether request body capture was truncated.
        /// </summary>
        public bool RequestBodyTruncated { get; set; } = false;

        /// <summary>
        /// Whether response body capture was truncated.
        /// </summary>
        public bool ResponseBodyTruncated { get; set; } = false;

        /// <summary>
        /// Whether the request body is represented as a binary placeholder.
        /// </summary>
        public bool RequestBodyIsBinary { get; set; } = false;

        /// <summary>
        /// Whether the response body is represented as a binary placeholder.
        /// </summary>
        public bool ResponseBodyIsBinary { get; set; } = false;

        /// <summary>
        /// Route parameters.
        /// </summary>
        public Dictionary<string, string> RouteParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Query parameters.
        /// </summary>
        public Dictionary<string, string> QueryParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Request headers.
        /// </summary>
        public Dictionary<string, string> RequestHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Response headers.
        /// </summary>
        public Dictionary<string, string> ResponseHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Request body.
        /// </summary>
        public string RequestBody { get; set; } = null;

        /// <summary>
        /// Response body.
        /// </summary>
        public string ResponseBody { get; set; } = null;

        /// <summary>
        /// Timestamp when the record was created.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the record was last updated.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.NewRequestHistoryId();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistoryEntry()
        {
        }

        /// <summary>
        /// Construct from a data row.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>Request history entry.</returns>
        public static RequestHistoryEntry FromDataRow(DataRow row)
        {
            if (row == null) return null;

            RequestHistoryEntry obj = new RequestHistoryEntry
            {
                Id = DataTableHelper.GetStringValue(row, "id"),
                TraceId = DataTableHelper.GetStringValue(row, "trace_id"),
                ChatHistoryId = DataTableHelper.GetStringValue(row, "chat_history_id"),
                TenantId = DataTableHelper.GetStringValue(row, "tenant_id"),
                UserId = DataTableHelper.GetStringValue(row, "user_id"),
                CredentialId = DataTableHelper.GetStringValue(row, "credential_id"),
                AssistantId = DataTableHelper.GetStringValue(row, "assistant_id"),
                ThreadId = DataTableHelper.GetStringValue(row, "thread_id"),
                PrincipalName = DataTableHelper.GetStringValue(row, "principal_name"),
                RequestType = DataTableHelper.GetStringValue(row, "request_type") ?? "SystemApi",
                SourceType = DataTableHelper.GetStringValue(row, "source_type") ?? "api",
                HttpMethod = DataTableHelper.GetStringValue(row, "http_method") ?? "GET",
                RouteTemplate = DataTableHelper.GetStringValue(row, "route_template"),
                RequestPath = DataTableHelper.GetStringValue(row, "request_path") ?? "/",
                RequestUrl = DataTableHelper.GetStringValue(row, "request_url") ?? "/",
                SourceIp = DataTableHelper.GetStringValue(row, "source_ip"),
                StatusCode = DataTableHelper.GetIntValue(row, "status_code"),
                Success = DataTableHelper.GetBooleanValue(row, "success"),
                DurationMs = DataTableHelper.GetDoubleValue(row, "duration_ms"),
                RequestContentType = DataTableHelper.GetStringValue(row, "request_content_type"),
                ResponseContentType = DataTableHelper.GetStringValue(row, "response_content_type"),
                RequestSizeBytes = DataTableHelper.GetLongValue(row, "request_size_bytes"),
                ResponseSizeBytes = DataTableHelper.GetLongValue(row, "response_size_bytes"),
                RequestBodyTruncated = DataTableHelper.GetBooleanValue(row, "request_body_truncated"),
                ResponseBodyTruncated = DataTableHelper.GetBooleanValue(row, "response_body_truncated"),
                RequestBodyIsBinary = DataTableHelper.GetBooleanValue(row, "request_body_is_binary"),
                ResponseBodyIsBinary = DataTableHelper.GetBooleanValue(row, "response_body_is_binary"),
                RouteParameters = DeserializeDictionary(DataTableHelper.GetStringValue(row, "route_parameters_json")),
                QueryParameters = DeserializeDictionary(DataTableHelper.GetStringValue(row, "query_parameters_json")),
                RequestHeaders = DeserializeDictionary(DataTableHelper.GetStringValue(row, "request_headers_json")),
                ResponseHeaders = DeserializeDictionary(DataTableHelper.GetStringValue(row, "response_headers_json")),
                RequestBody = DataTableHelper.GetStringValue(row, "request_body"),
                ResponseBody = DataTableHelper.GetStringValue(row, "response_body"),
                CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc"),
                LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc")
            };

            return obj;
        }

        #endregion

        #region Private-Methods

        private static Dictionary<string, string> DeserializeDictionary(string json)
        {
            if (String.IsNullOrEmpty(json)) return new Dictionary<string, string>();
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        #endregion
    }
}
