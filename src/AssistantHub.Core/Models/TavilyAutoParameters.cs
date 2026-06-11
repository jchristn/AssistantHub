namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Provider-selected Tavily search parameters.
    /// </summary>
    public class TavilyAutoParameters
    {
        /// <summary>
        /// Provider-selected topic.
        /// </summary>
        public string Topic { get; set; } = null;

        /// <summary>
        /// Provider-selected search depth.
        /// </summary>
        public string SearchDepth { get; set; } = null;
    }
}
