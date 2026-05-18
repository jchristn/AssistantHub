namespace AssistantHub.Core.Database.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Request-history database methods interface.
    /// </summary>
    public interface IRequestHistoryMethods
    {
        /// <summary>
        /// Create a request-history entry.
        /// </summary>
        /// <param name="entry">Request-history entry.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created entry.</returns>
        Task<RequestHistoryEntry> CreateAsync(RequestHistoryEntry entry, CancellationToken token = default);

        /// <summary>
        /// Read a request-history entry by identifier.
        /// </summary>
        /// <param name="id">Entry identifier.</param>
        /// <param name="includeDetails">Whether request and response payload fields should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Request-history entry.</returns>
        Task<RequestHistoryEntry> ReadAsync(string id, bool includeDetails = true, CancellationToken token = default);

        /// <summary>
        /// Enumerate request-history entries.
        /// </summary>
        /// <param name="filter">Search filter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        Task<EnumerationResult<RequestHistoryEntry>> EnumerateAsync(RequestHistorySearchFilter filter, CancellationToken token = default);

        /// <summary>
        /// Summarize request-history entries.
        /// </summary>
        /// <param name="filter">Search filter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Summary result.</returns>
        Task<RequestHistorySummaryResult> SummarizeAsync(RequestHistorySearchFilter filter, CancellationToken token = default);

        /// <summary>
        /// Delete a request-history entry by identifier.
        /// </summary>
        /// <param name="id">Entry identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if deleted.</returns>
        Task<bool> DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Delete request-history entries matching a filter.
        /// </summary>
        /// <param name="filter">Search filter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Deleted row count.</returns>
        Task<int> DeleteByFilterAsync(RequestHistorySearchFilter filter, CancellationToken token = default);

        /// <summary>
        /// Delete expired request-history entries.
        /// </summary>
        /// <param name="retentionDays">Retention days.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default);
    }
}
