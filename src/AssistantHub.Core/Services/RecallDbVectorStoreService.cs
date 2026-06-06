namespace AssistantHub.Core.Services
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// RecallDB implementation of the vector-store service.
    /// </summary>
    public class RecallDbVectorStoreService : IVectorStoreService
    {
        #region Private-Members

        private static readonly HttpClient _HttpClient = new HttpClient();
        private readonly RecallDbSettings _Settings;
        private readonly LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">RecallDB settings.</param>
        /// <param name="logging">Logging module.</param>
        public RecallDbVectorStoreService(RecallDbSettings settings, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePathAndQuery, string body = null, CancellationToken token = default)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (String.IsNullOrEmpty(relativePathAndQuery)) throw new ArgumentNullException(nameof(relativePathAndQuery));

            string endpoint = _Settings.Endpoint.TrimEnd('/');
            string path = relativePathAndQuery.StartsWith("/") ? relativePathAndQuery : "/" + relativePathAndQuery;
            string url = endpoint + path;

            HttpRequestMessage request = new HttpRequestMessage(method, url);
            if (!String.IsNullOrEmpty(_Settings.AccessKey))
                request.Headers.Add("Authorization", "Bearer " + _Settings.AccessKey);

            if (!String.IsNullOrEmpty(body) && method != HttpMethod.Get && method != HttpMethod.Head)
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            _Logging.Debug("[RecallDbVectorStoreService] " + method.Method + " " + path);
            return await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
        }

        #endregion
    }
}
