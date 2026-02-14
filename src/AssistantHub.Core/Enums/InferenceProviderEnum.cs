namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Inference provider enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum InferenceProviderEnum
    {
        /// <summary>
        /// OpenAI.
        /// </summary>
        [EnumMember(Value = "OpenAI")]
        OpenAI,

        /// <summary>
        /// Ollama.
        /// </summary>
        [EnumMember(Value = "Ollama")]
        Ollama
    }
}
