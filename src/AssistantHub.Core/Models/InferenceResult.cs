namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Result from an inference request, carrying either a response or error details.
    /// </summary>
    public class InferenceResult
    {
        #region Public-Members

        /// <summary>
        /// Indicates whether inference succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// The generated response content, or null on failure.
        /// </summary>
        public string Content { get; set; } = null;

        /// <summary>
        /// Model-requested tool calls, if the provider finished with tool calls.
        /// </summary>
        public List<AssistantModelToolCall> ToolCalls { get; set; } = new List<AssistantModelToolCall>();

        /// <summary>
        /// Provider finish reason, such as stop, length, tool_calls, or error.
        /// </summary>
        public string FinishReason { get; set; } = null;

        /// <summary>
        /// Error message describing what went wrong, or null on success.
        /// </summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>
        /// Provider-agnostic telemetry captured while servicing the inference call.
        /// </summary>
        public AssistantPerformanceStage Telemetry { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public InferenceResult()
        {
        }

        /// <summary>
        /// Create a successful result.
        /// </summary>
        /// <param name="content">Generated response content.</param>
        /// <param name="telemetry">Provider-agnostic telemetry captured for the inference call.</param>
        /// <param name="finishReason">Provider finish reason.</param>
        /// <param name="toolCalls">Model-requested tool calls.</param>
        /// <returns>InferenceResult.</returns>
        public static InferenceResult FromSuccess(
            string content,
            AssistantPerformanceStage telemetry = null,
            string finishReason = "stop",
            List<AssistantModelToolCall> toolCalls = null)
        {
            return new InferenceResult
            {
                Success = true,
                Content = content,
                Telemetry = telemetry,
                FinishReason = finishReason,
                ToolCalls = toolCalls ?? new List<AssistantModelToolCall>()
            };
        }

        /// <summary>
        /// Create a failed result.
        /// </summary>
        /// <param name="errorMessage">Error message.</param>
        /// <param name="telemetry">Provider-agnostic telemetry captured for the inference call.</param>
        /// <returns>InferenceResult.</returns>
        public static InferenceResult FromError(string errorMessage, AssistantPerformanceStage telemetry = null)
        {
            return new InferenceResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Telemetry = telemetry,
                FinishReason = "error"
            };
        }

        #endregion
    }
}
