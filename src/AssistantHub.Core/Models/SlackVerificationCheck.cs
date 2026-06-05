namespace AssistantHub.Core.Models
{

    /// <summary>
    /// Individual verification result.
    /// </summary>
    public class SlackVerificationCheck
    {
        /// <summary>
        /// True if the check succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// User-facing status message.
        /// </summary>
        public string Message { get; set; } = null;

        /// <summary>
        /// Safe detail payload for UI display.
        /// </summary>
        public object Details { get; set; } = null;
    }
}
