namespace AssistantHub.Core.Helpers
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Helper methods for inference provider resolution and provider-specific defaults.
    /// </summary>
    public static class InferenceProviderHelper
    {
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
        public static void ApplyAuthentication(HttpRequestMessage request, InferenceProviderEnum provider, string apiKey)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
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
