namespace AssistantHub.Core.Models
{
    /// <summary>
    /// Request payload for starting an evaluation run.
    /// </summary>
    public class EvalRunRequest
    {
        /// <summary>
        /// Assistant identifier.
        /// </summary>
        public string AssistantId { get; set; } = null;

        /// <summary>
        /// Optional judge prompt override.
        /// </summary>
        public string JudgePrompt { get; set; } = null;
    }
}
