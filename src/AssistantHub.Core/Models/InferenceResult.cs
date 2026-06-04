namespace AssistantHub.Core.Models
{
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
        /// <returns>InferenceResult.</returns>
        public static InferenceResult FromSuccess(string content, AssistantPerformanceStage telemetry = null)
        {
            return new InferenceResult
            {
                Success = true,
                Content = content,
                Telemetry = telemetry
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
                Telemetry = telemetry
            };
        }

        #endregion
    }
}
