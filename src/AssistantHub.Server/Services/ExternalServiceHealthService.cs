namespace AssistantHub.Server.Services
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Validates connectivity to subordinate services used by AssistantHub.
    /// </summary>
    public class ExternalServiceHealthService : IExternalServiceHealthService
    {
        private const string _Header = "[ExternalServiceHealthService] ";
        private readonly AssistantHubSettings _Settings;
        private readonly LoggingModule _Logging;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ExternalServiceHealthService(AssistantHubSettings settings, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        /// <inheritdoc />
        public async Task<bool> ValidateConnectivityAsync()
        {
            _Logging.Info(_Header + "validating connectivity to subordinate services");

            bool allSucceeded = true;

            using (HttpClient http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(10);

                allSucceeded &= await CheckServiceAsync(http, "S3 (Less3)",
                    HttpMethod.Head,
                    _Settings.S3.EndpointUrl.TrimEnd('/') + "/").ConfigureAwait(false);

                allSucceeded &= await CheckServiceAsync(http, "DocumentAtom",
                    HttpMethod.Head,
                    _Settings.DocumentAtom.Endpoint.TrimEnd('/') + "/").ConfigureAwait(false);

                allSucceeded &= await CheckServiceAsync(http, "Chunking (Partio)",
                    HttpMethod.Head,
                    _Settings.Chunking.Endpoint.TrimEnd('/') + "/",
                    bearerToken: _Settings.Chunking.AccessKey).ConfigureAwait(false);

                allSucceeded &= await CheckServiceAsync(http, "Embeddings (Partio)",
                    HttpMethod.Head,
                    _Settings.Embeddings.Endpoint.TrimEnd('/') + "/",
                    bearerToken: _Settings.Embeddings.AccessKey).ConfigureAwait(false);

                allSucceeded &= await CheckServiceAsync(http, "Inference (" + _Settings.Inference.Provider.ToString() + ")",
                    HttpMethod.Get,
                    InferenceProviderHelper.GetModelsUrl(_Settings.Inference.Endpoint, _Settings.Inference.Provider),
                    _Settings.Inference.Provider,
                    _Settings.Inference.ApiKey).ConfigureAwait(false);

                allSucceeded &= await CheckServiceAsync(http, "RecallDb",
                    HttpMethod.Head,
                    _Settings.RecallDb.Endpoint.TrimEnd('/') + "/",
                    bearerToken: _Settings.RecallDb.AccessKey).ConfigureAwait(false);

                allSucceeded &= await CheckServiceAsync(http, "Verbex",
                    HttpMethod.Head,
                    _Settings.Verbex.Endpoint.TrimEnd('/') + "/",
                    bearerToken: _Settings.Verbex.AccessKey).ConfigureAwait(false);
            }

            return allSucceeded;
        }

        private async Task<bool> CheckServiceAsync(
            HttpClient http,
            string serviceName,
            HttpMethod method,
            string url,
            string bearerToken = null)
        {
            return await CheckServiceAsync(http, serviceName, method, url, null, bearerToken).ConfigureAwait(false);
        }

        private async Task<bool> CheckServiceAsync(
            HttpClient http,
            string serviceName,
            HttpMethod method,
            string url,
            InferenceProviderEnum? inferenceProvider,
            string apiKey = null)
        {
            try
            {
                using (HttpRequestMessage req = new HttpRequestMessage(method, url))
                {
                    if (inferenceProvider.HasValue)
                    {
                        InferenceProviderHelper.ApplyAuthentication(req, inferenceProvider.Value, apiKey);
                    }
                    else if (!String.IsNullOrEmpty(apiKey))
                    {
                        req.Headers.Add("Authorization", "Bearer " + apiKey);
                    }

                    HttpResponseMessage resp = await http.SendAsync(req).ConfigureAwait(false);
                    if (resp.IsSuccessStatusCode)
                    {
                        _Logging.Info(_Header + serviceName + " reachable at " + url + " (HTTP " + (int)resp.StatusCode + ")");
                        return true;
                    }

                    _Logging.Warn(_Header + serviceName + " returned HTTP " + (int)resp.StatusCode + " at " + url);
                    return false;
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + serviceName + " unreachable at " + url + ": " + e.Message);
                return false;
            }
        }
    }
}
