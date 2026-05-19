namespace AssistantHub.Sdk.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Draft Slack settings used for connectivity verification.
    /// </summary>
    public class SlackVerificationRequest
    {
        /// <summary>
        /// Whether Slack should be enabled.
        /// </summary>
        [JsonPropertyName("EnableSlack")]
        public bool EnableSlack { get; set; } = false;

        /// <summary>
        /// Slack app token.
        /// </summary>
        [JsonPropertyName("SlackAppToken")]
        public string SlackAppToken { get; set; }

        /// <summary>
        /// Slack bot token.
        /// </summary>
        [JsonPropertyName("SlackBotToken")]
        public string SlackBotToken { get; set; }

        /// <summary>
        /// Slack channel identifier.
        /// </summary>
        [JsonPropertyName("SlackChannelId")]
        public string SlackChannelId { get; set; }

        /// <summary>
        /// Slack message prefix.
        /// </summary>
        [JsonPropertyName("SlackMessagePrefix")]
        public string SlackMessagePrefix { get; set; }
    }

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
