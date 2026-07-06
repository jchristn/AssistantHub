namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

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

        /// <summary>
        /// Optional execution mode. ChatRail uses the normal assistant chat/RAG path; InferenceOnly uses the legacy model-only path.
        /// </summary>
        public string ExecutionMode { get; set; } = "ChatRail";

        /// <summary>
        /// Optional eval fact categories to include in the run.
        /// Empty or null means all categories.
        /// </summary>
        public List<string> Categories { get; set; } = null;
    }
}
