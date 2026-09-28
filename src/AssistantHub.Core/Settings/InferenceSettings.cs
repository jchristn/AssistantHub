namespace AssistantHub.Core.Settings
{
    using System;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Inference service settings.
    /// </summary>
    public class InferenceSettings
    {
        #region Public-Members

        /// <summary>
        /// Inference provider type.
        /// </summary>
        public InferenceProviderEnum Provider { get; set; } = InferenceProviderEnum.Ollama;

        /// <summary>
        /// Endpoint URL for the inference provider.
        /// </summary>
        public string Endpoint
        {
            get => _Endpoint;
            set { if (!String.IsNullOrEmpty(value)) _Endpoint = value; }
        }

        /// <summary>
        /// API key for the inference provider.
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Default model to use for inference.
        /// </summary>
        public string DefaultModel
        {
            get => _DefaultModel;
            set { if (!String.IsNullOrEmpty(value)) _DefaultModel = value; }
        }

        /// <summary>
        /// Browser URL for the inference provider dashboard.
        /// </summary>
        public string DashboardUrl
        {
            get => _DashboardUrl;
            set { if (value != null) _DashboardUrl = value; }
        }

        /// <summary>
        /// Timeout, in milliseconds, for an answer-model request (1,000 to 3,600,000). Default 300,000.
        /// </summary>
        public int RequestTimeoutMs
        {
            get => _RequestTimeoutMs;
            set => _RequestTimeoutMs = Math.Clamp(value, 1000, 3600000);
        }

        /// <summary>
        /// Timeout, in milliseconds, for a utility-model step (retrieval gate, query or conversation rewrite, LLM
        /// rerank), from 1,000 to 600,000. A timed-out step falls back to its default
        /// behavior. Default 30,000.
        /// </summary>
        public int UtilityTimeoutMs
        {
            get => _UtilityTimeoutMs;
            set => _UtilityTimeoutMs = Math.Clamp(value, 1000, 600000);
        }

        /// <summary>
        /// Number of times the answer model is retried after a transient failure (HTTP 408, 429, 502, 503 or 504)
        /// before any output was sent, from 0 to 5. Default 2.
        /// </summary>
        public int MaxRetries
        {
            get => _MaxRetries;
            set => _MaxRetries = Math.Clamp(value, 0, 5);
        }

        /// <summary>
        /// Base delay, in milliseconds, before retrying the answer model (0 to 30,000). It doubles per attempt, with
        /// jitter. Default 500.
        /// </summary>
        public int RetryDelayMs
        {
            get => _RetryDelayMs;
            set => _RetryDelayMs = Math.Clamp(value, 0, 30000);
        }

        /// <summary>
        /// Consecutive failures of a utility step on one endpoint that open its circuit breaker (1 to 100). While
        /// open, the step is skipped and falls back to its default behavior. Default 3.
        /// </summary>
        public int CircuitBreakerFailures
        {
            get => _CircuitBreakerFailures;
            set => _CircuitBreakerFailures = Math.Clamp(value, 1, 100);
        }

        /// <summary>
        /// How long, in milliseconds, an open circuit breaker skips its step before trying again (1,000 to
        /// 3,600,000). Default 30,000.
        /// </summary>
        public int CircuitBreakerOpenMs
        {
            get => _CircuitBreakerOpenMs;
            set => _CircuitBreakerOpenMs = Math.Clamp(value, 1000, 3600000);
        }

        #endregion

        #region Private-Members

        private int _RequestTimeoutMs = 300000;
        private int _UtilityTimeoutMs = 30000;
        private int _MaxRetries = 2;
        private int _RetryDelayMs = 500;
        private int _CircuitBreakerFailures = 3;
        private int _CircuitBreakerOpenMs = 30000;
        private string _Endpoint = "http://localhost:11434";
        private string _DefaultModel = "gemma3:4b";
        private string _DashboardUrl = "";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public InferenceSettings()
        {
        }

        #endregion
    }
}
