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
        /// <returns>InferenceResult.</returns>
        public static InferenceResult FromSuccess(string content)
        {
            return new InferenceResult
            {
                Success = true,
                Content = content
            };
        }

        /// <summary>
        /// Create a failed result.
        /// </summary>
        /// <param name="errorMessage">Error message.</param>
        /// <returns>InferenceResult.</returns>
        public static InferenceResult FromError(string errorMessage)
        {
            return new InferenceResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        #endregion
    }
}
