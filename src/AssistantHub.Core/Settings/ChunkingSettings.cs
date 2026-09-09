namespace AssistantHub.Core.Settings
{
    using System;

    /// <summary>
    /// Chunking service settings.
    /// </summary>
    public class ChunkingSettings
    {
        #region Public-Members

        /// <summary>
        /// Endpoint URL for the chunking service.
        /// </summary>
        public string Endpoint
        {
            get => _Endpoint;
            set { if (!String.IsNullOrEmpty(value)) _Endpoint = value; }
        }

        /// <summary>
        /// Access key for the chunking service.
        /// </summary>
        public string AccessKey { get; set; } = "";

        /// <summary>
        /// Endpoint identifier.
        /// </summary>
        public string EndpointId
        {
            get => _EndpointId;
            set { if (!String.IsNullOrEmpty(value)) _EndpointId = value; }
        }

        /// <summary>
        /// Browser URL for the chunking and embeddings service dashboard.
        /// </summary>
        public string DashboardUrl
        {
            get => _DashboardUrl;
            set { if (value != null) _DashboardUrl = value; }
        }

        /// <summary>
        /// Number of times to retry a Partio processing/embedding call after a transient failure
        /// (HTTP 408, 429, 502, 503, or 504) before giving up. Zero disables retries.
        /// Partio v0.5.0 enforces per-endpoint concurrency and queue limits and can return 429
        /// (queue full) or 504 (queued-wait/upstream timeout) under load, which are safe to retry.
        /// </summary>
        public int MaxRetries
        {
            get => _MaxRetries;
            set => _MaxRetries = value < 0 ? 0 : value;
        }

        /// <summary>
        /// Base delay, in milliseconds, between transient-failure retries of a Partio call.
        /// The effective delay grows exponentially per attempt and is capped at 60,000 ms.
        /// Zero disables the delay (retries fire immediately).
        /// </summary>
        public int RetryDelayMs
        {
            get => _RetryDelayMs;
            set => _RetryDelayMs = value < 0 ? 0 : value;
        }

        #endregion

        #region Private-Members

        private string _Endpoint = "http://localhost:8321";
        private string _EndpointId = "default";
        private string _DashboardUrl = "";
        private int _MaxRetries = 3;
        private int _RetryDelayMs = 1000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChunkingSettings()
        {
        }

        #endregion
    }
}
