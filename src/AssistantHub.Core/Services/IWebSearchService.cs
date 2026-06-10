namespace AssistantHub.Core.Services
{
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Provider-agnostic web search service.
    /// </summary>
    public interface IWebSearchService
    {
        /// <summary>
        /// Execute a web search through the configured provider.
        /// </summary>
        /// <param name="request">Search request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search response.</returns>
        Task<WebSearchResponse> SearchAsync(WebSearchRequest request, CancellationToken token = default);
    }
}
