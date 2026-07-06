namespace AssistantHub.Core.Services
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Executes eval questions through the product chat pipeline without coupling Core to the server implementation.
    /// </summary>
    public interface IEvalChatExecutor
    {
        /// <summary>
        /// Execute a single eval question through the assistant chat rail.
        /// </summary>
        /// <param name="request">Eval chat execution request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Eval chat execution result.</returns>
        Task<EvalChatExecutionResult> ExecuteAsync(EvalChatExecutionRequest request, CancellationToken token = default);
    }

    /// <summary>
    /// Request for executing a single eval fact through the assistant chat rail.
    /// </summary>
    public class EvalChatExecutionRequest
    {
        /// <summary>
        /// Assistant identifier.
        /// </summary>
        public string AssistantId { get; set; } = null;

        /// <summary>
        /// Messages to send through the assistant chat rail.
        /// </summary>
        public List<ChatCompletionMessage> Messages { get; set; } = new List<ChatCompletionMessage>();

        /// <summary>
        /// Origin label to persist on chat history.
        /// </summary>
        public string Origin { get; set; } = "eval";
    }

    /// <summary>
    /// Result from executing a single eval fact through the assistant chat rail.
    /// </summary>
    public class EvalChatExecutionResult
    {
        /// <summary>
        /// Whether execution succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Error message when execution fails.
        /// </summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>
        /// Final assistant response text.
        /// </summary>
        public string ResponseText { get; set; } = null;

        /// <summary>
        /// Persisted chat history identifier.
        /// </summary>
        public string ChatHistoryId { get; set; } = null;

        /// <summary>
        /// Trace identifier used for this eval turn.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// Retrieval metadata captured from the chat response.
        /// </summary>
        public ChatCompletionRetrieval Retrieval { get; set; } = null;

        /// <summary>
        /// Citation metadata captured from the chat response.
        /// </summary>
        public ChatCompletionCitations Citations { get; set; } = null;

        /// <summary>
        /// Safe tool-call trace metadata captured from the chat response.
        /// </summary>
        public List<ChatCompletionToolTrace> ToolCalls { get; set; } = null;
    }
}
