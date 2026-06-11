namespace AssistantHub.Core.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Function payload for a model-requested tool call.
    /// </summary>
    public class AssistantModelToolFunctionCall
    {
        /// <summary>
        /// Function/tool name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null;

        /// <summary>
        /// JSON-serialized function/tool arguments.
        /// </summary>
        [JsonPropertyName("arguments")]
        [JsonConverter(typeof(JsonStringOrRawJsonConverter))]
        public string Arguments { get; set; } = null;
    }
}
