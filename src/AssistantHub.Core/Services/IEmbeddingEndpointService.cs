namespace AssistantHub.Core.Services
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Embedding endpoint management service abstraction.
    /// </summary>
    public interface IEmbeddingEndpointService
    {
        /// <summary>
        /// Send a request to the backing embedding endpoint management service.
        /// </summary>
        Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePathAndQuery, string body = null, CancellationToken token = default);
    }
}
