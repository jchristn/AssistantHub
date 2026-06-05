namespace AssistantHub.Core.Models
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
        public int? Input { get; set; } = null;

        /// <summary>
        /// Output token count, when reported or estimated.
        /// </summary>
        public int? Output { get; set; } = null;

        /// <summary>
        /// Total token count, when reported or estimated.
        /// </summary>
        public int? Total { get; set; } = null;

        /// <summary>
        /// Provider prompt-evaluation token count, when reported.
        /// </summary>
        public int? PromptEvalCount { get; set; } = null;

        /// <summary>
        /// Provider generation token count, when reported.
        /// </summary>
        public int? EvalCount { get; set; } = null;
    }
}
