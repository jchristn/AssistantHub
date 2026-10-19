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
        Ollama,

        /// <summary>
        /// Gemini.
        /// </summary>
        [EnumMember(Value = "Gemini")]
        Gemini,

        /// <summary>Ollama Cloud using the native Ollama API.</summary>
        [EnumMember(Value = "OllamaCloud")]
        OllamaCloud,

        /// <summary>xAI Grok using OpenAI-compatible chat completions.</summary>
        [EnumMember(Value = "xAI")]
        xAI
    }
}
