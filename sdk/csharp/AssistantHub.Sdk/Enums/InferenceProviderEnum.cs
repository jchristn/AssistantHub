namespace AssistantHub.Sdk.Enums
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
        xAI,
        /// <summary>vLLM completion provider.</summary>
        [EnumMember(Value = "vLLM" )]
        vLLM,
        /// <summary>AzureOpenAI completion provider.</summary>
        [EnumMember(Value = "AzureOpenAI" )]
        AzureOpenAI,
        /// <summary>Anthropic completion provider.</summary>
        [EnumMember(Value = "Anthropic" )]
        Anthropic,
        /// <summary>Cohere completion provider.</summary>
        [EnumMember(Value = "Cohere" )]
        Cohere,
        /// <summary>Bedrock completion provider.</summary>
        [EnumMember(Value = "Bedrock" )]
        Bedrock,
        /// <summary>VertexAI completion provider.</summary>
        [EnumMember(Value = "VertexAI" )]
        VertexAI
    }
}
