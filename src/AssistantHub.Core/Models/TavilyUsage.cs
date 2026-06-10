namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Tavily usage metadata.
    /// </summary>
    public class TavilyUsage
    {
        /// <summary>
        /// Credits used by the request.
        /// </summary>
        public int? CreditsUsed { get; set; } = null;
    }
}
