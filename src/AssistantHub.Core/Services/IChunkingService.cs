namespace AssistantHub.Core.Services
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Chunking service abstraction.
    /// </summary>
    public interface IChunkingService
    {
        /// <summary>
        /// Send a request to the backing chunking service.
        /// </summary>
        Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePathAndQuery, string body = null, CancellationToken token = default);
    }
}
