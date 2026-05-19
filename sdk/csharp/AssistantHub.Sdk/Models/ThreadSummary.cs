namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Summary information for a conversation thread.
    /// </summary>
    public class ThreadSummary
    {
        /// <summary>
        /// Thread identifier.
        /// </summary>
        [JsonPropertyName("ThreadId")]
        public string ThreadId { get; set; }

        /// <summary>
        /// Assistant identifier associated with the thread.
        /// </summary>
        [JsonPropertyName("AssistantId")]
        public string AssistantId { get; set; }

        /// <summary>
        /// Timestamp of the first message in the thread.
        /// </summary>
        [JsonPropertyName("FirstMessageUtc")]
        public DateTime FirstMessageUtc { get; set; }

        /// <summary>
        /// Timestamp of the most recent message in the thread.
        /// </summary>
        [JsonPropertyName("LastMessageUtc")]
        public DateTime LastMessageUtc { get; set; }

        /// <summary>
        /// Number of turns recorded in the thread.
        /// </summary>
        [JsonPropertyName("TurnCount")]
        public int TurnCount { get; set; }
    }
}
