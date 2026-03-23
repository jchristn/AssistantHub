namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Model pull progress.
    /// </summary>
    public class PullProgress
    {
        /// <summary>
        /// Model name being pulled.
        /// </summary>
        [JsonPropertyName("ModelName")]
        public string ModelName { get; set; }

        /// <summary>
        /// Current status.
        /// </summary>
        [JsonPropertyName("Status")]
        public string Status { get; set; }

        /// <summary>
        /// Layer digest.
        /// </summary>
        [JsonPropertyName("Digest")]
        public string Digest { get; set; }

        /// <summary>
        /// Total bytes to download.
        /// </summary>
        [JsonPropertyName("TotalBytes")]
        public long TotalBytes { get; set; }

        /// <summary>
        /// Bytes downloaded so far.
        /// </summary>
        [JsonPropertyName("CompletedBytes")]
        public long CompletedBytes { get; set; }

        /// <summary>
        /// Percent complete (0.0 to 100.0).
        /// </summary>
        [JsonPropertyName("PercentComplete")]
        public double PercentComplete { get; set; }

        /// <summary>
        /// Whether the pull is complete.
        /// </summary>
        [JsonPropertyName("IsComplete")]
        public bool IsComplete { get; set; }

        /// <summary>
        /// Whether an error occurred.
        /// </summary>
        [JsonPropertyName("HasError")]
        public bool HasError { get; set; }

        /// <summary>
        /// Error message if applicable.
        /// </summary>
        [JsonPropertyName("ErrorMessage")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Timestamp when the pull started.
        /// </summary>
        [JsonPropertyName("StartedUtc")]
        public DateTime StartedUtc { get; set; }
    }
}
