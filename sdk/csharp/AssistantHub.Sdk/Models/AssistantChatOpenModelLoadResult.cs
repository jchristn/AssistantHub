namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Result of loading one configured assistant endpoint model during chat open.
    /// </summary>
    public class AssistantChatOpenModelLoadResult
    {
        /// <summary>
        /// Endpoint type, either Completion or Embedding.
        /// </summary>
        [JsonPropertyName("EndpointType")]
        public string EndpointType { get; set; }

        /// <summary>
        /// Whether the model load request succeeded.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// HTTP status code returned by the upstream model-load request.
        /// </summary>
        [JsonPropertyName("StatusCode")]
        public int StatusCode { get; set; }
    }
}
