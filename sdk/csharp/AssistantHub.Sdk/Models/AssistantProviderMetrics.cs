namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Provider-native metrics normalized into common fields.
    /// </summary>
    public class AssistantProviderMetrics
    {
        /// <summary>
        /// Provider queue duration in milliseconds, when reported.
        /// </summary>
        [JsonPropertyName("QueueMs")]
        public double? QueueMs { get; set; }

        /// <summary>
        /// Provider model-load duration in milliseconds, when reported.
        /// </summary>
        [JsonPropertyName("LoadMs")]
        public double? LoadMs { get; set; }

        /// <summary>
        /// Provider prompt-evaluation duration in milliseconds, when reported.
        /// </summary>
        [JsonPropertyName("PromptEvalMs")]
        public double? PromptEvalMs { get; set; }

        /// <summary>
        /// Provider generation duration in milliseconds, when reported.
        /// </summary>
        [JsonPropertyName("GenerationMs")]
        public double? GenerationMs { get; set; }

        /// <summary>
        /// Provider-reported total duration in milliseconds, when reported.
        /// </summary>
        [JsonPropertyName("TotalMs")]
        public double? TotalMs { get; set; }

        /// <summary>
        /// Provider generation throughput in tokens per second, when derivable.
        /// </summary>
        [JsonPropertyName("TokensPerSecond")]
        public double? TokensPerSecond { get; set; }

        /// <summary>
        /// Provider request identifier, when reported.
        /// </summary>
        [JsonPropertyName("RequestId")]
        public string RequestId { get; set; }
    }
}
