namespace AssistantHub.Core.Database.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Chat history performance event database methods.
    /// </summary>
    public interface IChatHistoryPerformanceEventMethods
    {
        /// <summary>
        /// Create a performance event.
        /// </summary>
        Task<ChatHistoryPerformanceEvent> CreateAsync(ChatHistoryPerformanceEvent evt, CancellationToken token = default);

        /// <summary>
        /// Create multiple performance events.
        /// </summary>
        Task CreateManyAsync(IEnumerable<ChatHistoryPerformanceEvent> events, CancellationToken token = default);

        /// <summary>
        /// List performance events for a chat history record.
        /// </summary>
        Task<List<ChatHistoryPerformanceEvent>> ListByChatHistoryIdAsync(string chatHistoryId, CancellationToken token = default);

        /// <summary>
        /// Delete performance events for a chat history record.
        /// </summary>
        Task DeleteByChatHistoryIdAsync(string chatHistoryId, CancellationToken token = default);

        /// <summary>
        /// Delete performance events older than a retention period.
        /// </summary>
        Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default);
    }
}
