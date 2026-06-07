namespace AssistantHub.Sdk.Models
{
    /// <summary>
    /// Crawl repository connectivity test result.
    /// </summary>
    public class CrawlConnectivityResult
    {
        /// <summary>
        /// True when the repository was reachable.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Human-readable connectivity status.
        /// </summary>
        public string Message { get; set; } = null;
    }
}
