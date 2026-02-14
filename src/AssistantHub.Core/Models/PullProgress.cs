namespace AssistantHub.Core.Models
{
    using System;

    /// <summary>
    /// Represents the progress of a model pull operation.
    /// </summary>
    public class PullProgress
    {
        /// <summary>
        /// Name of the model being pulled.
        /// </summary>
        public string ModelName { get; set; } = null;

        /// <summary>
        /// Current status message from the pull operation.
        /// </summary>
        public string Status { get; set; } = null;

        /// <summary>
        /// Digest of the layer currently being downloaded.
        /// </summary>
        public string Digest { get; set; } = null;

        /// <summary>
        /// Total bytes to download for the current layer.
        /// </summary>
        public long TotalBytes { get; set; } = 0;

        /// <summary>
        /// Bytes completed for the current layer.
        /// </summary>
        public long CompletedBytes { get; set; } = 0;

        /// <summary>
        /// Percentage complete (0-100).
        /// </summary>
        public int PercentComplete { get; set; } = 0;

        /// <summary>
        /// Whether the pull operation has completed.
        /// </summary>
        public bool IsComplete { get; set; } = false;

        /// <summary>
        /// Whether the pull operation encountered an error.
        /// </summary>
        public bool HasError { get; set; } = false;

        /// <summary>
        /// Error message if the pull operation failed.
        /// </summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>
        /// When the pull operation started (UTC).
        /// </summary>
        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    }
}
