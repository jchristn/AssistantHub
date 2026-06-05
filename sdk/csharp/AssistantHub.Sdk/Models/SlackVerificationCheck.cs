namespace AssistantHub.Sdk.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Individual Slack verification check result.
    /// </summary>
    public class SlackVerificationCheck
    {
        /// <summary>
        /// Whether the check succeeded.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// Human-readable message.
        /// </summary>
        [JsonPropertyName("Message")]
        public string Message { get; set; }

        /// <summary>
        /// Safe diagnostic details.
        /// </summary>
        [JsonPropertyName("Details")]
        public JsonElement? Details { get; set; }
    }
}
