namespace AssistantHub.Server.Services
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Small JSON Schema subset used for model-facing assistant tool parameters.
    /// </summary>
    public class AssistantToolJsonSchema
    {
        /// <summary>
        /// JSON Schema type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = null;

        /// <summary>
        /// Human-readable field description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = null;

        /// <summary>
        /// Object properties.
        /// </summary>
        [JsonPropertyName("properties")]
        public Dictionary<string, AssistantToolJsonSchema> Properties { get; set; } = null;

        /// <summary>
        /// Required object properties.
        /// </summary>
        [JsonPropertyName("required")]
        public List<string> Required { get; set; } = null;

        /// <summary>
        /// Whether unspecified object properties are allowed.
        /// </summary>
        [JsonPropertyName("additionalProperties")]
        public bool? AdditionalProperties { get; set; } = null;

        /// <summary>
        /// Array item schema.
        /// </summary>
        [JsonPropertyName("items")]
        public AssistantToolJsonSchema Items { get; set; } = null;

        /// <summary>
        /// Allowed string values.
        /// </summary>
        [JsonPropertyName("enum")]
        public List<string> Enum { get; set; } = null;

        /// <summary>
        /// Numeric minimum.
        /// </summary>
        [JsonPropertyName("minimum")]
        public double? Minimum { get; set; } = null;

        /// <summary>
        /// Numeric maximum.
        /// </summary>
        [JsonPropertyName("maximum")]
        public double? Maximum { get; set; } = null;
    }
}
