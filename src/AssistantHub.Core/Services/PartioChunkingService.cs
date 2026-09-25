namespace AssistantHub.Core.Services
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Partio implementation of the chunking service.
    /// </summary>
    public class PartioChunkingService : IChunkingService
    {
        // The per-request timeout comes from ChunkingSettings.RequestTimeoutMs; the client's own 100-second default would
        // cancel chunking and embedding of any large document.
        private static readonly HttpClient _HttpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        private readonly ChunkingSettings _Settings;
        private readonly LoggingModule _Logging;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public PartioChunkingService(ChunkingSettings settings, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePathAndQuery, string body = null, CancellationToken token = default)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (String.IsNullOrEmpty(relativePathAndQuery)) throw new ArgumentNullException(nameof(relativePathAndQuery));

            string path = relativePathAndQuery.StartsWith("/") ? relativePathAndQuery : "/" + relativePathAndQuery;
            string url = _Settings.Endpoint.TrimEnd('/') + path;

            HttpRequestMessage request = new HttpRequestMessage(method, url);
            if (!String.IsNullOrEmpty(_Settings.AccessKey))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _Settings.AccessKey);

            if (!String.IsNullOrEmpty(body) && method != HttpMethod.Get && method != HttpMethod.Head)
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            _Logging.Debug("[PartioChunkingService] " + method.Method + " " + path);
            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                timeout.CancelAfter(_Settings.RequestTimeoutMs);
                try
                {
                    return await _HttpClient.SendAsync(request, timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    throw new TimeoutException("Partio request " + method.Method + " " + path + " did not complete within " + _Settings.RequestTimeoutMs + " ms (Chunking.RequestTimeoutMs).");
                }
            }
        }
    }
}
