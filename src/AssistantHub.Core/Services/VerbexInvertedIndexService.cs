namespace AssistantHub.Core.Services
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Verbex implementation of the inverted index service.
    /// </summary>
    public class VerbexInvertedIndexService : IInvertedIndexService
    {
        #region Private-Members

        private static readonly HttpClient _SharedHttpClient = new HttpClient();
        private readonly HttpClient _HttpClient;
        private readonly VerbexSettings _Settings;
        private readonly LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Verbex settings.</param>
        /// <param name="logging">Logging module.</param>
        public VerbexInvertedIndexService(VerbexSettings settings, LoggingModule logging)
            : this(settings, logging, _SharedHttpClient)
        {
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Verbex settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="httpClient">HTTP client.</param>
        public VerbexInvertedIndexService(VerbexSettings settings, LoggingModule logging, HttpClient httpClient)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePathAndQuery, string body = null)
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

            _Logging.Debug("[VerbexInvertedIndexService] " + method.Method + " " + path);
            return await _HttpClient.SendAsync(request).ConfigureAwait(false);
        }

        #endregion
    }
}
