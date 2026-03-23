namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;
    using AssistantHub.Sdk.Enums;

    /// <summary>
    /// API error response.
    /// </summary>
    public class ApiErrorResponse
    {
        /// <summary>
        /// Error type.
        /// </summary>
        [JsonPropertyName("Error")]
        public ApiErrorEnum Error { get; set; }

        /// <summary>
        /// Error message.
        /// </summary>
        [JsonPropertyName("Message")]
        public string Message { get; set; }

        /// <summary>
        /// HTTP status code.
        /// </summary>
        [JsonPropertyName("StatusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// Additional context.
        /// </summary>
        [JsonPropertyName("Context")]
        public string Context { get; set; }

        /// <summary>
        /// Error description.
        /// </summary>
        [JsonPropertyName("Description")]
        public string Description { get; set; }
    }
}
