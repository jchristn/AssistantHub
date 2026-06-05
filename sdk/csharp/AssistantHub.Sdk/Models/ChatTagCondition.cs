namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A single tag condition for metadata filtering.
    /// </summary>
    public class ChatTagCondition
    {
        /// <summary>
        /// Tag key.
        /// </summary>
        [JsonPropertyName("key")]
        public string Key { get; set; }

        /// <summary>
        /// Condition operator (e.g. "Equals").
        /// </summary>
        [JsonPropertyName("condition")]
        public string Condition { get; set; }

        /// <summary>
        /// Tag value.
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
