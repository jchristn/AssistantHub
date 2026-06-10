namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Image metadata returned by Tavily search.
    /// </summary>
    public class TavilySearchImage
    {
        /// <summary>
        /// Image URL.
        /// </summary>
        public string Url { get; set; } = null;

        /// <summary>
        /// Optional image description.
        /// </summary>
        public string Description { get; set; } = null;
    }
}
