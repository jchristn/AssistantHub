namespace AssistantHub.Server.Services
{
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Executes policy-approved server-side assistant tools.
    /// </summary>
    public interface IAssistantToolExecutor
    {
        /// <summary>
        /// Execute a server-side assistant tool.
        /// </summary>
        /// <param name="context">Tool execution context.</param>
        /// <param name="request">Tool execution request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tool execution result.</returns>
        Task<AssistantToolExecutionResult> ExecuteAsync(
            AssistantToolExecutionContext context,
            AssistantToolExecutionRequest request,
            CancellationToken token = default);
    }
}
