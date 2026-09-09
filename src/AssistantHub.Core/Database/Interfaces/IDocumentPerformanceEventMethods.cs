namespace AssistantHub.Core.Database.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Document performance event database methods.
    /// </summary>
    public interface IDocumentPerformanceEventMethods
    {
        /// <summary>
        /// Create a performance event.
        /// </summary>
        Task<DocumentPerformanceEvent> CreateAsync(DocumentPerformanceEvent evt, CancellationToken token = default);

        /// <summary>
        /// Create multiple performance events.
        /// </summary>
        Task CreateManyAsync(IEnumerable<DocumentPerformanceEvent> events, CancellationToken token = default);

        /// <summary>
        /// List performance events for a document, ordered by pipeline sequence.
        /// </summary>
        Task<List<DocumentPerformanceEvent>> ListByDocumentIdAsync(string documentId, CancellationToken token = default);

        /// <summary>
        /// List performance events for a tenant created on or after a timestamp, most recent first, bounded by maxResults.
        /// </summary>
        Task<List<DocumentPerformanceEvent>> ListByTenantAsync(string tenantId, DateTime sinceUtc, int maxResults, CancellationToken token = default);

        /// <summary>
        /// Delete performance events for a document.
        /// </summary>
        Task DeleteByDocumentIdAsync(string documentId, CancellationToken token = default);

        /// <summary>
        /// Delete performance events older than a retention period.
        /// </summary>
        Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default);
    }
}
