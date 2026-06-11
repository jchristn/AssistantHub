namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Normalized token counters.
    /// </summary>
    public class AssistantTokenUsageTelemetry
    {
        /// <summary>
        /// Input token count, when reported or estimated.
        /// </summary>
        [JsonPropertyName("Input")]
        public int? Input { get; set; }

        /// <summary>
        /// Output token count, when reported or estimated.
        /// </summary>
        [JsonPropertyName("Output")]
        public int? Output { get; set; }

        /// <summary>
        /// Total token count, when reported or estimated.
        /// </summary>
        [JsonPropertyName("Total")]
        public int? Total { get; set; }

        /// <summary>
        /// Reasoning token count, when reported by the provider.
        /// </summary>
        [JsonPropertyName("Reasoning")]
        public int? Reasoning { get; set; }

        /// <summary>
        /// Tool-definition token count, when reported by the provider.
        /// </summary>
        [JsonPropertyName("ToolDefinitions")]
        public int? ToolDefinitions { get; set; }

        /// <summary>
        /// Provider prompt-evaluation token count, when reported.
        /// </summary>
        [JsonPropertyName("PromptEvalCount")]
        public int? PromptEvalCount { get; set; }

        /// <summary>
        /// Provider generation token count, when reported.
        /// </summary>
        [JsonPropertyName("EvalCount")]
        public int? EvalCount { get; set; }
    }
}
