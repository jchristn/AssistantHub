namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Assistant feedback database methods interface.
    /// </summary>
    public interface IAssistantFeedbackMethods
    {
        /// <summary>
        /// Create an assistant feedback record.
        /// </summary>
        /// <param name="feedback">Assistant feedback record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created assistant feedback record.</returns>
        Task<AssistantFeedback> CreateAsync(AssistantFeedback feedback, CancellationToken token = default);

        /// <summary>
        /// Read an assistant feedback record by identifier.
        /// </summary>
        /// <param name="id">Assistant feedback identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Assistant feedback record.</returns>
        Task<AssistantFeedback> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Delete an assistant feedback record.
        /// </summary>
        /// <param name="id">Assistant feedback identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate assistant feedback records scoped to a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing assistant feedback records.</returns>
        Task<EnumerationResult<AssistantFeedback>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Delete all assistant feedback records belonging to an assistant.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default);
    }
}
