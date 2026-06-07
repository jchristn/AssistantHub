namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Crawl repository connectivity test result.
    /// </summary>
    public class CrawlConnectivityResult
    {
        #region Public-Members

        /// <summary>
        /// True when the repository was reachable.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Human-readable connectivity status.
        /// </summary>
        public string Message { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CrawlConnectivityResult()
        {
        }

        #endregion
    }
}
