namespace AssistantHub.Core.Settings
{
    using System;

    /// <summary>
    /// A cross-encoder rerank service that assistants can use with RerankerType "CrossEncoder". Rerankers are
    /// configured by the server operator, so API keys stay in server settings and are never returned by the API.
    /// </summary>
    public class RerankerSettings
    {
        #region Public-Members

        /// <summary>
        /// Identifier that assistants reference in RerankEndpointId.
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Display name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Request format: "Tei" (HuggingFace text-embeddings-inference, POST {Endpoint}/rerank) or "Cohere"
        /// (Cohere-compatible, POST {Endpoint}/v1/rerank with a model and top_n).
        /// </summary>
        public string Format
        {
            get => _Format;
            set => _Format = String.Equals(value, "Cohere", StringComparison.OrdinalIgnoreCase) ? "Cohere" : "Tei";
        }

        /// <summary>
        /// Base URL of the rerank service, for example http://reranker:80.
        /// </summary>
        public string Endpoint { get; set; } = null;

        /// <summary>
        /// Model name sent with Cohere-format requests.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Optional bearer token.
        /// </summary>
        public string ApiKey { get; set; } = null;

        /// <summary>
        /// Timeout for one rerank request, in milliseconds (1,000 to 120,000). Default 10,000.
        /// </summary>
        public int TimeoutMs
        {
            get => _TimeoutMs;
            set => _TimeoutMs = Math.Clamp(value, 1000, 120000);
        }

        /// <summary>
        /// Characters of each passage sent to the reranker (200 to 20,000). Default 2,000.
        /// </summary>
        public int MaxPassageCharacters
        {
            get => _MaxPassageCharacters;
            set => _MaxPassageCharacters = Math.Clamp(value, 200, 20000);
        }

        #endregion

        #region Private-Members

        private string _Format = "Tei";
        private int _TimeoutMs = 10000;
        private int _MaxPassageCharacters = 2000;

        #endregion
    }
}
