namespace AssistantHub.Core.Helpers
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Helper methods for inference provider resolution and provider-specific defaults.
    /// </summary>
    public static class InferenceProviderHelper
    {
        /// <summary>
        /// Describe a secret for logs without revealing it: "(not set)" or "(set, N characters)".
        /// </summary>
        /// <param name="secret">API key or token.</param>
        /// <returns>A description safe to log.</returns>
        public static string DescribeSecret(string secret)
        {
            return String.IsNullOrEmpty(secret) ? "(not set)" : "(set, " + secret.Length + " characters)";
        }

        /// <summary>
        /// Resolve an inference provider from a Partio API format string.
        /// </summary>
        /// <param name="apiFormat">API format string.</param>
        /// <param name="fallback">Fallback provider if format is missing or unknown.</param>
        /// <returns>Inference provider.</returns>
        public static InferenceProviderEnum FromApiFormat(string apiFormat, InferenceProviderEnum fallback = InferenceProviderEnum.Ollama)
        {
            if (String.IsNullOrEmpty(apiFormat)) return fallback;

            if (apiFormat.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
                return InferenceProviderEnum.OpenAI;

            if (apiFormat.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
                return InferenceProviderEnum.Gemini;

            if (apiFormat.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
                return InferenceProviderEnum.Ollama;

            if (apiFormat.Equals("OllamaCloud", StringComparison.OrdinalIgnoreCase))
                return InferenceProviderEnum.OllamaCloud;
            if (apiFormat.Equals("xAI", StringComparison.OrdinalIgnoreCase) || apiFormat.Equals("Grok", StringComparison.OrdinalIgnoreCase))
                return InferenceProviderEnum.xAI;

            if (Enum.TryParse(apiFormat, true, out InferenceProviderEnum parsed)) return parsed;
            return fallback;
        }

        /// <summary>
        /// Get the default endpoint for a provider.
        /// </summary>
        /// <param name="provider">Inference provider.</param>
        /// <returns>Default endpoint URL.</returns>
        public static string GetDefaultEndpoint(InferenceProviderEnum provider)
        {
            switch (provider)
            {
                case InferenceProviderEnum.Anthropic: return "https://api.anthropic.com";
                case InferenceProviderEnum.Cohere: return "https://api.cohere.com";
                case InferenceProviderEnum.AzureOpenAI:
                case InferenceProviderEnum.Bedrock:
                case InferenceProviderEnum.VertexAI: return "";
                case InferenceProviderEnum.vLLM: return "http://localhost:8000";
                case InferenceProviderEnum.OllamaCloud:
                    return "https://ollama.com";
                case InferenceProviderEnum.xAI:
                    return "https://api.x.ai";
                case InferenceProviderEnum.OpenAI:
                    return "https://api.openai.com/v1";
                case InferenceProviderEnum.Gemini:
                    return "https://generativelanguage.googleapis.com";
                case InferenceProviderEnum.Ollama:
                default:
                    return "http://localhost:11434";
            }
        }

        /// <summary>
        /// Get a provider-specific model listing or health check URL.
        /// </summary>
        /// <param name="endpoint">Configured provider endpoint.</param>
        /// <param name="provider">Inference provider.</param>
        /// <returns>Provider-specific URL.</returns>
        public static string GetModelsUrl(string endpoint, InferenceProviderEnum provider)
        {
            switch (provider)
            {
                case InferenceProviderEnum.vLLM:
                case InferenceProviderEnum.xAI:
                case InferenceProviderEnum.OpenAI:
                    return AppendVersionedPath(endpoint, "/v1", "/models");
                case InferenceProviderEnum.Gemini:
                    return AppendVersionedPath(endpoint, "/v1beta", "/models");
                case InferenceProviderEnum.Ollama:
                default:
                    return endpoint.TrimEnd('/') + "/api/tags";
            }
        }

        /// <summary>
        /// Apply authentication headers for the given provider.
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <param name="provider">Inference provider.</param>
        /// <param name="apiKey">API key.</param>
        public static void ApplyAuthentication(HttpRequestMessage request, InferenceProviderEnum provider, string apiKey, ProviderSettings settings = null)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            EndpointAuthSettings auth = provider is InferenceProviderEnum.Ollama or InferenceProviderEnum.OpenAI or InferenceProviderEnum.vLLM ? settings?.Auth : null;
            if (auth != null)
            {
                request.Headers.Authorization = null;
                switch (auth.Mode)
                {
                    case EndpointAuthModes.None: return;
                    case EndpointAuthModes.Header: request.Headers.Add(auth.HeaderName, apiKey); return;
                    case EndpointAuthModes.Basic:
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(auth.Username + ":" + apiKey))); return;
                    case EndpointAuthModes.QueryParameter:
                        UriBuilder uri = new(request.RequestUri);
                        uri.Query = uri.Query.TrimStart('?') + (String.IsNullOrEmpty(uri.Query) ? "" : "&") + Uri.EscapeDataString(auth.QueryParameterName) + "=" + Uri.EscapeDataString(apiKey);
                        request.RequestUri = uri.Uri; return;
                }
            }
            if (String.IsNullOrEmpty(apiKey)) return;

            if (provider == InferenceProviderEnum.Gemini)
            {
                request.Headers.Add("x-goog-api-key", apiKey);
            }
            else
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
        }

        /// <summary>
        /// Build the provider-specific chat/completion URL.
        /// </summary>
        /// <param name="endpoint">Configured provider endpoint.</param>
        /// <param name="provider">Inference provider.</param>
        /// <param name="model">Model name when required by the provider.</param>
        /// <param name="streaming">Whether to target the streaming endpoint.</param>
        /// <returns>URL.</returns>
        public static string GetCompletionUrl(string endpoint, InferenceProviderEnum provider, string model, bool streaming)
        {
            switch (provider)
            {
                case InferenceProviderEnum.vLLM:
                case InferenceProviderEnum.xAI:
                case InferenceProviderEnum.OpenAI:
                    return AppendVersionedPath(endpoint, "/v1", "/chat/completions");
                case InferenceProviderEnum.Gemini:
                    if (String.IsNullOrEmpty(model)) throw new ArgumentNullException(nameof(model));
                    string suffix = streaming
                        ? "/models/" + model + ":streamGenerateContent?alt=sse"
                        : "/models/" + model + ":generateContent";
                    return AppendVersionedPath(endpoint, "/v1beta", suffix);
                case InferenceProviderEnum.Ollama:
                default:
                    return endpoint.TrimEnd('/') + "/api/chat";
            }
        }

        private static string AppendVersionedPath(string endpoint, string versionSegment, string path)
        {
            string trimmedEndpoint = (endpoint ?? String.Empty).TrimEnd('/');
            if (String.IsNullOrEmpty(trimmedEndpoint))
                trimmedEndpoint = versionSegment.StartsWith("/")
                    ? versionSegment
                    : "/" + versionSegment;

            if (trimmedEndpoint.EndsWith(versionSegment, StringComparison.OrdinalIgnoreCase))
                return trimmedEndpoint + path;

            return trimmedEndpoint + versionSegment + path;
        }
    }
}
