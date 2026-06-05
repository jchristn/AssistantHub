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
}
