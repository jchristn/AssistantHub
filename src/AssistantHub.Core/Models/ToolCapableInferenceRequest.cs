namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Provider-neutral non-streaming inference request that may expose tools to the model.
    /// </summary>
    public class ToolCapableInferenceRequest
    {
        /// <summary>
        /// Chat messages.
        /// </summary>
        public List<ChatCompletionMessage> Messages { get; set; } = new List<ChatCompletionMessage>();

        /// <summary>
        /// Model identifier.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Maximum completion tokens.
        /// </summary>
        public int MaxTokens { get; set; } = 0;

        /// <summary>
        /// Sampling temperature.
        /// </summary>
        public double Temperature { get; set; } = 0.0;

        /// <summary>
        /// Top-p sampling value.
        /// </summary>
        public double TopP { get; set; } = 1.0;

        /// <summary>
        /// Inference provider.
        /// </summary>
        public InferenceProviderEnum Provider { get; set; } = InferenceProviderEnum.Ollama;

        /// <summary>
        /// Provider endpoint.
        /// </summary>
        public string Endpoint { get; set; } = null;

        /// <summary>
        /// Provider API key.
        /// </summary>
        public string ApiKey { get; set; } = null;

        /// <summary>
        /// Tool definitions to expose to the model.
        /// </summary>
        public List<AssistantModelToolDefinition> Tools { get; set; } = new List<AssistantModelToolDefinition>();

        /// <summary>
        /// Tool choice mode. OpenAI-compatible providers commonly support "auto", "none", or "required".
        /// </summary>
        public string ToolChoice { get; set; } = "auto";
    }
}
