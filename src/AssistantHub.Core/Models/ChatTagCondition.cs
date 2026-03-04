namespace AssistantHub.Core.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// A single tag condition used to filter retrieval results by key-value metadata.
    /// Maps to RecallDB's TagCondition structure.
    /// </summary>
    public class ChatTagCondition
    {
        /// <summary>
        /// Tag key to match against.
        /// </summary>
        [JsonPropertyName("key")]
        public string Key { get; set; } = null;

        /// <summary>
        /// Condition type: Equals, NotEquals, Contains, ContainsNot, StartsWith, EndsWith, IsNull, IsNotNull, GreaterThan, LessThan.
        /// </summary>
        [JsonPropertyName("condition")]
        public string Condition { get; set; } = "Equals";

        /// <summary>
        /// Value to compare against.
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = null;
    }
}
