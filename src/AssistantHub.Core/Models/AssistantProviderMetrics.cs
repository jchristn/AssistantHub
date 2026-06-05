namespace AssistantHub.Core.Models
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
        public double? QueueMs { get; set; } = null;

        /// <summary>
        /// Provider model-load duration in milliseconds, when reported.
        /// </summary>
        public double? LoadMs { get; set; } = null;

        /// <summary>
        /// Provider prompt-evaluation duration in milliseconds, when reported.
        /// </summary>
        public double? PromptEvalMs { get; set; } = null;

        /// <summary>
        /// Provider generation duration in milliseconds, when reported.
        /// </summary>
        public double? GenerationMs { get; set; } = null;

        /// <summary>
        /// Provider-reported total duration in milliseconds, when reported.
        /// </summary>
        public double? TotalMs { get; set; } = null;

        /// <summary>
        /// Provider generation throughput in tokens per second, when derivable.
        /// </summary>
        public double? TokensPerSecond { get; set; } = null;

        /// <summary>
        /// Provider request identifier, when reported.
        /// </summary>
        public string RequestId { get; set; } = null;
    }
}
