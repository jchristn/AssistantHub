namespace AssistantHub.Core.Models
{
    /// <summary>
    /// One provider attempt in a web search request.
    /// </summary>
    public class WebSearchProviderAttempt
    {
        /// <summary>
        /// Provider name.
        /// </summary>
        public string ProviderName { get; set; } = null;

        /// <summary>
        /// Provider type.
        /// </summary>
        public string ProviderType { get; set; } = null;

        /// <summary>
        /// Whether the attempt succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Duration in milliseconds.
        /// </summary>
        public double DurationMs { get; set; } = 0;

        /// <summary>
        /// Provider error message when failed.
        /// </summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>
        /// Provider usage credits when available.
        /// </summary>
        public int? CreditsUsed { get; set; } = null;
    }
}
