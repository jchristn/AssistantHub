namespace AssistantHub.Sdk
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk.Exceptions;
    using AssistantHub.Sdk.Models;

    /// <summary>
    /// Base client for the AssistantHub API providing HTTP communication, serialization, and error handling.
    /// </summary>
    public class AssistantHubClientBase : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Base URL of the AssistantHub server.
        /// </summary>
        public string BaseUrl
        {
            get => _BaseUrl;
        }

        #endregion

        #region Private-Members

        private readonly string _BaseUrl;
        private readonly string _ApiKey;
        private readonly HttpClient _HttpClient;
        private readonly bool _OwnsHttpClient;
        private bool _Disposed;

        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the client base.
        /// </summary>
        /// <param name="baseUrl">Base URL of the AssistantHub server.</param>
        /// <param name="apiKey">Optional API key for authentication.</param>
        public AssistantHubClientBase(string baseUrl, string apiKey = null)
        {
            if (String.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));

            _BaseUrl = baseUrl.TrimEnd('/');
            _ApiKey = apiKey;
            _HttpClient = new HttpClient();
            _OwnsHttpClient = true;

            ConfigureHttpClient();
        }

        /// <summary>
        /// Instantiate the client base with a provided HttpClient.
        /// </summary>
        /// <param name="baseUrl">Base URL of the AssistantHub server.</param>
        /// <param name="httpClient">HttpClient instance to use.</param>
        /// <param name="apiKey">Optional API key for authentication.</param>
        public AssistantHubClientBase(string baseUrl, HttpClient httpClient, string apiKey = null)
        {
            if (String.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));
            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));

            _BaseUrl = baseUrl.TrimEnd('/');
            _ApiKey = apiKey;
            _HttpClient = httpClient;
            _OwnsHttpClient = false;

            ConfigureHttpClient();
        }

        #endregion

        #region Protected-Methods

        /// <summary>
        /// Send an HTTP request and deserialize the response.
        /// </summary>
        /// <typeparam name="T">Response type.</typeparam>
        /// <param name="method">HTTP method.</param>
        /// <param name="path">Request path (relative to base URL).</param>
        /// <param name="body">Optional request body to serialize as JSON.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Deserialized response.</returns>
        protected async Task<T> SendAsync<T>(HttpMethod method, string path, object body = null, CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage request = BuildRequest(method, path, body))
            {
                using (HttpResponseMessage response = await _HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        HandleErrorResponse((int)response.StatusCode, responseBody);
                    }

                    return DeserializeJson<T>(responseBody);
                }
            }
        }

        /// <summary>
        /// Send an HTTP request without a response body.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="path">Request path (relative to base URL).</param>
        /// <param name="body">Optional request body to serialize as JSON.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected async Task SendAsync(HttpMethod method, string path, object body = null, CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage request = BuildRequest(method, path, body))
            {
                using (HttpResponseMessage response = await _HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        HandleErrorResponse((int)response.StatusCode, responseBody);
                    }
                }
            }
        }

        /// <summary>
        /// Serialize an object to JSON.
        /// </summary>
        /// <param name="obj">Object to serialize.</param>
        /// <returns>JSON string.</returns>
        protected string SerializeJson(object obj)
        {
            return JsonSerializer.Serialize(obj, _JsonOptions);
        }

        /// <summary>
        /// Deserialize a JSON string to an object.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <param name="json">JSON string.</param>
        /// <returns>Deserialized object.</returns>
        protected T DeserializeJson<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _JsonOptions);
        }

        #endregion

        #region Private-Methods

        private void ConfigureHttpClient()
        {
            _HttpClient.DefaultRequestHeaders.Accept.Clear();
            _HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!String.IsNullOrEmpty(_ApiKey))
            {
                _HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _ApiKey);
            }
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string path, object body)
        {
            string url = _BaseUrl + path;
            HttpRequestMessage request = new HttpRequestMessage(method, url);

            if (body != null)
            {
                string json = SerializeJson(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return request;
        }

        private void HandleErrorResponse(int statusCode, string responseBody)
        {
            string message = responseBody;

            try
            {
                ApiErrorResponse errorResponse = DeserializeJson<ApiErrorResponse>(responseBody);
                if (errorResponse != null && !String.IsNullOrEmpty(errorResponse.Message))
                {
                    message = errorResponse.Message;
                    if (!String.IsNullOrEmpty(errorResponse.Description))
                    {
                        message = message + ": " + errorResponse.Description;
                    }
                }
            }
            catch
            {
                // Use raw response body as message
            }

            switch (statusCode)
            {
                case 401:
                    throw new AuthenticationException(message);
                case 404:
                    throw new NotFoundException(message);
                case 400:
                    throw new ValidationException(message);
                default:
                    throw new AssistantHubException(message, statusCode);
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose the client and release resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the client and release resources.
        /// </summary>
        /// <param name="disposing">Whether managed resources should be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing && _OwnsHttpClient)
                {
                    _HttpClient.Dispose();
                }

                _Disposed = true;
            }
        }

        #endregion
    }
}
