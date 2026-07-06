namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Request to start an evaluation run.
    /// </summary>
    public class EvalRunRequest
    {
        /// <summary>
        /// Assistant identifier.
        /// </summary>
        [JsonPropertyName("AssistantId")]
        public string AssistantId { get; set; }

        /// <summary>
        /// Optional judge prompt override.
        /// </summary>
        [JsonPropertyName("JudgePrompt")]
        public string JudgePrompt { get; set; }

        /// <summary>
        /// Execution mode: ChatRail or InferenceOnly.
        /// </summary>
        [JsonPropertyName("ExecutionMode")]
        public string ExecutionMode { get; set; }

        /// <summary>
        /// Optional eval fact categories to include.
        /// </summary>
        [JsonPropertyName("Categories")]
        public List<string> Categories { get; set; }
    }
}
