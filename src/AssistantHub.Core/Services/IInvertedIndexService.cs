namespace AssistantHub.Core.Services
{
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Inverted index service abstraction.
    /// </summary>
    public interface IInvertedIndexService
    {
        /// <summary>
        /// Send a request to the backing inverted index service.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="relativePathAndQuery">Relative service path and query string.</param>
        /// <param name="body">Optional JSON request body.</param>
        /// <returns>HTTP response from the backing service.</returns>
        Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePathAndQuery, string body = null);
    }
}
