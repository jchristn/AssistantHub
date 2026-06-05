namespace AssistantHub.Sdk.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Response from Slack connectivity verification.
    /// </summary>
    public class SlackVerificationResponse
    {
        /// <summary>
        /// True if all requested checks succeeded.
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// Bot token validation result.
        /// </summary>
        [JsonPropertyName("BotToken")]
        public SlackVerificationCheck BotToken { get; set; }

        /// <summary>
        /// Channel validation result.
        /// </summary>
        [JsonPropertyName("Channel")]
        public SlackVerificationCheck Channel { get; set; }

        /// <summary>
        /// Socket mode connectivity result.
        /// </summary>
        [JsonPropertyName("SocketMode")]
        public SlackVerificationCheck SocketMode { get; set; }
    }
}
