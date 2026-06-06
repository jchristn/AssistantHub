namespace AssistantHub.Core.Services
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Vector-store service abstraction.
    /// </summary>
    public interface IVectorStoreService
    {
        /// <summary>
        /// Send a request to the backing vector-store service.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="relativePathAndQuery">Relative service path and query string.</param>
        /// <param name="body">Optional JSON request body.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>HTTP response from the backing service.</returns>
        Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePathAndQuery, string body = null, CancellationToken token = default);
    }
}
