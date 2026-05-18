namespace AssistantHub.Core.Settings
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Request history settings.
    /// </summary>
    public class RequestHistorySettings
    {
        #region Public-Members

        /// <summary>
        /// Enable request-history capture.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Number of days to retain request-history records.
        /// </summary>
        public int RetentionDays
        {
            get => _RetentionDays;
            set => _RetentionDays = Math.Clamp(value, 1, 3650);
        }

        /// <summary>
        /// Cleanup interval in minutes.
        /// </summary>
        public int PurgeIntervalMinutes
        {
            get => _PurgeIntervalMinutes;
            set => _PurgeIntervalMinutes = Math.Clamp(value, 1, 1440);
        }

        /// <summary>
        /// Maximum request body bytes to capture.
        /// </summary>
        public int MaxRequestBodyBytes
        {
            get => _MaxRequestBodyBytes;
            set => _MaxRequestBodyBytes = Math.Clamp(value, 0, 1024 * 1024);
        }

        /// <summary>
        /// Maximum response body bytes to capture.
        /// </summary>
        public int MaxResponseBodyBytes
        {
            get => _MaxResponseBodyBytes;
            set => _MaxResponseBodyBytes = Math.Clamp(value, 0, 1024 * 1024);
        }

        /// <summary>
        /// Whether request headers are captured.
        /// </summary>
        public bool CaptureHeaders { get; set; } = true;

        /// <summary>
        /// Whether request and response bodies are captured.
        /// </summary>
        public bool CaptureBodies { get; set; } = true;

        /// <summary>
        /// Whether unauthenticated assistant traffic should be captured.
        /// </summary>
        public bool IncludeUnauthenticatedAssistantTraffic { get; set; } = true;

        /// <summary>
        /// Response content types to skip body capture for.
        /// </summary>
        public List<string> ExcludedContentTypes { get; set; } = new List<string>
        {
            "application/octet-stream",
            "application/zip",
            "application/x-gzip",
            "audio/",
            "image/",
            "video/"
        };

        /// <summary>
        /// Header names to redact.
        /// </summary>
        public List<string> RedactedHeaders { get; set; } = new List<string>
        {
            "authorization",
            "proxy-authorization",
            "cookie",
            "set-cookie",
            "x-api-key",
            "api-key",
            "x-password",
            "x-token"
        };

        /// <summary>
        /// JSON field names to redact.
        /// </summary>
        public List<string> RedactedJsonFields { get; set; } = new List<string>
        {
            "password",
            "bearerToken",
            "apiKey",
            "authorization",
            "slackAppToken",
            "slackBotToken",
            "secret",
            "accessKey"
        };

        #endregion

        #region Private-Members

        private int _RetentionDays = 7;
        private int _PurgeIntervalMinutes = 60;
        private int _MaxRequestBodyBytes = 65536;
        private int _MaxResponseBodyBytes = 65536;

        #endregion
    }
}
