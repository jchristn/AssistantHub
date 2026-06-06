namespace AssistantHub.Core.Services
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Partio implementation of inference endpoint management.
    /// </summary>
    public class PartioInferenceEndpointService : IInferenceEndpointService
    {
        private readonly IChunkingService _Partio;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public PartioInferenceEndpointService(ChunkingSettings settings, LoggingModule logging)
        {
            _Partio = new PartioChunkingService(settings ?? throw new ArgumentNullException(nameof(settings)), logging ?? throw new ArgumentNullException(nameof(logging)));
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePathAndQuery, string body = null, CancellationToken token = default)
        {
            return _Partio.SendAsync(method, relativePathAndQuery, body, token);
        }
    }
}
