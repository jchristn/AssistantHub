namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Draft Slack settings used for connectivity verification.
    /// </summary>
    public class SlackVerificationRequest
    {
        /// <summary>
        /// Enable Slack.
        /// </summary>
        public bool EnableSlack { get; set; } = false;

        /// <summary>
        /// Slack app token.
        /// </summary>
        public string SlackAppToken { get; set; } = null;

        /// <summary>
        /// Slack bot token.
        /// </summary>
        public string SlackBotToken { get; set; } = null;

        /// <summary>
        /// Slack channel identifier.
        /// </summary>
        public string SlackChannelId { get; set; } = null;

        /// <summary>
        /// Slack message prefix.
        /// </summary>
        public string SlackMessagePrefix { get; set; } = null;
    }
}
