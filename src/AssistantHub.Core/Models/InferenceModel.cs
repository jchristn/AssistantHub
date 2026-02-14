namespace AssistantHub.Core.Models
{
    using System;

    /// <summary>
    /// Represents a model available on the configured inference provider.
    /// </summary>
    public class InferenceModel
    {
        /// <summary>
        /// Model name (e.g. "gemma3:4b").
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Model size on disk in bytes.
        /// </summary>
        public long SizeBytes { get; set; } = 0;

        /// <summary>
        /// Last modified timestamp (UTC).
        /// </summary>
        public DateTime? ModifiedUtc { get; set; } = null;

        /// <summary>
        /// Owner of the model (OpenAI only, null for Ollama).
        /// </summary>
        public string OwnedBy { get; set; } = null;

        /// <summary>
        /// Whether the provider supports pulling new models.
        /// </summary>
        public bool PullSupported { get; set; } = false;
    }
}
