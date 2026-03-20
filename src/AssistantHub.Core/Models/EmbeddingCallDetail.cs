namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Captures details of a single HTTP call made to an upstream embedding endpoint.
    /// </summary>
    public class EmbeddingCallDetail
    {
        /// <summary>
        /// Full URL called.
        /// </summary>
        public string Url { get; set; } = null;

        /// <summary>
        /// HTTP method.
        /// </summary>
        public string Method { get; set; } = null;

        /// <summary>
        /// Headers sent upstream.
        /// </summary>
        public Dictionary<string, string> RequestHeaders { get; set; } = null;

        /// <summary>
        /// Body sent upstream.
        /// </summary>
        public string RequestBody { get; set; } = null;

        /// <summary>
        /// HTTP status code returned.
        /// </summary>
        public int? StatusCode { get; set; } = null;

        /// <summary>
        /// Response headers.
        /// </summary>
        public Dictionary<string, string> ResponseHeaders { get; set; } = null;

        /// <summary>
        /// Response body.
        /// </summary>
        public string ResponseBody { get; set; } = null;

        /// <summary>
        /// Timing for this call in milliseconds.
        /// </summary>
        public long? ResponseTimeMs { get; set; } = null;

        /// <summary>
        /// Whether the call succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Error message if the call failed.
        /// </summary>
        public string Error { get; set; } = null;

        /// <summary>
        /// UTC timestamp when the call was initiated.
        /// </summary>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}
