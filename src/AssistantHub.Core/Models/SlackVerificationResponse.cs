namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Response from Slack connectivity verification.
    /// </summary>
    public class SlackVerificationResponse
    {
        /// <summary>
        /// True if all requested verification checks succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Bot token validation result.
        /// </summary>
        public SlackVerificationCheck BotToken { get; set; } = new SlackVerificationCheck();

        /// <summary>
        /// Channel validation result.
        /// </summary>
        public SlackVerificationCheck Channel { get; set; } = new SlackVerificationCheck();

        /// <summary>
        /// Socket Mode connectivity result.
        /// </summary>
        public SlackVerificationCheck SocketMode { get; set; } = new SlackVerificationCheck();
    }
}
