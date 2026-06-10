namespace AssistantHub.Core.Database.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Assistant tool-call trace database methods.
    /// </summary>
    public interface IAssistantToolCallMethods
    {
        /// <summary>
        /// Create a tool-call record.
        /// </summary>
        Task<AssistantToolCallRecord> CreateAsync(AssistantToolCallRecord record, CancellationToken token = default);

        /// <summary>
        /// Create multiple tool-call records.
        /// </summary>
        Task CreateManyAsync(IEnumerable<AssistantToolCallRecord> records, CancellationToken token = default);

        /// <summary>
        /// Read a tool-call record by ID.
        /// </summary>
        Task<AssistantToolCallRecord> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate tool-call records.
        /// </summary>
        Task<EnumerationResult<AssistantToolCallRecord>> EnumerateAsync(string tenantId, EnumerationQuery query, string assistantId = null, CancellationToken token = default);

        /// <summary>
        /// List tool-call records for a chat history row.
        /// </summary>
        Task<List<AssistantToolCallRecord>> ListByChatHistoryIdAsync(string chatHistoryId, CancellationToken token = default);

        /// <summary>
        /// Attach a persisted chat history ID to records with the matching trace ID.
        /// </summary>
        Task AttachChatHistoryIdByTraceIdAsync(string traceId, string chatHistoryId, CancellationToken token = default);

        /// <summary>
        /// Delete a tool-call record by ID.
        /// </summary>
        Task<bool> DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Delete tool-call records matching the supplied scoped filter.
        /// </summary>
        Task<int> DeleteByFilterAsync(string tenantId, EnumerationQuery query, string assistantId = null, CancellationToken token = default);

        /// <summary>
        /// Delete expired tool-call records.
        /// </summary>
        Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default);
    }
}
