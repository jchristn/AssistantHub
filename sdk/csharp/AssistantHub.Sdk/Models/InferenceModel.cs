namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Model available on an inference provider.
    /// </summary>
    public class InferenceModel
    {
        /// <summary>
        /// Model name.
        /// </summary>
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        /// <summary>
        /// Model size in bytes.
        /// </summary>
        [JsonPropertyName("SizeBytes")]
        public long SizeBytes { get; set; }

        /// <summary>
        /// Last modified timestamp.
        /// </summary>
        [JsonPropertyName("ModifiedUtc")]
        public DateTime? ModifiedUtc { get; set; }

        /// <summary>
        /// Model owner.
        /// </summary>
        [JsonPropertyName("OwnedBy")]
        public string OwnedBy { get; set; }

        /// <summary>
        /// Whether model pulling is supported.
        /// </summary>
        [JsonPropertyName("PullSupported")]
        public bool PullSupported { get; set; }
    }
}
