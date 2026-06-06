namespace AssistantHub.Core.Services
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Inference endpoint management service abstraction.
    /// </summary>
    public interface IInferenceEndpointService
    {
        /// <summary>
        /// Send a request to the backing inference endpoint management service.
        /// </summary>
        Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePathAndQuery, string body = null, CancellationToken token = default);
    }
}
