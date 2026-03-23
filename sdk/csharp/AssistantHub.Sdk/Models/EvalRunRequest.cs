namespace AssistantHub.Sdk.Models
{
    using System.Text.Json.Serialization;

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
    }
}
