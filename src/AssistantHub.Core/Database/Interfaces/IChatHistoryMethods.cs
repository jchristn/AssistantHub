namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Chat history database methods interface.
    /// </summary>
    public interface IChatHistoryMethods
    {
        /// <summary>
        /// Create a chat history record.
        /// </summary>
        /// <param name="history">Chat history record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created chat history record.</returns>
        Task<ChatHistory> CreateAsync(ChatHistory history, CancellationToken token = default);

        /// <summary>
        /// Read a chat history record by identifier.
        /// </summary>
        /// <param name="id">Chat history identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat history record.</returns>
        Task<ChatHistory> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Delete a chat history record.
        /// </summary>
        /// <param name="id">Chat history identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate chat history records.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing chat history records.</returns>
        Task<EnumerationResult<ChatHistory>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Delete all chat history records belonging to an assistant.
        /// </summary>
        /// <param name="assistantId">Assistant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default);

        /// <summary>
        /// Delete chat history records older than the specified retention period.
        /// </summary>
        /// <param name="retentionDays">Number of days to retain.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default);
    }
}
