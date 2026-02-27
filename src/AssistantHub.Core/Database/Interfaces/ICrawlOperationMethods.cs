namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Crawl operation database methods interface.
    /// </summary>
    public interface ICrawlOperationMethods
    {
        /// <summary>
        /// Create a crawl operation record.
        /// </summary>
        /// <param name="operation">Crawl operation record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created crawl operation record.</returns>
        Task<CrawlOperation> CreateAsync(CrawlOperation operation, CancellationToken token = default);

        /// <summary>
        /// Read a crawl operation record by identifier.
        /// </summary>
        /// <param name="id">Crawl operation identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Crawl operation record.</returns>
        Task<CrawlOperation> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Update a crawl operation record.
        /// </summary>
        /// <param name="operation">Crawl operation record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated crawl operation record.</returns>
        Task<CrawlOperation> UpdateAsync(CrawlOperation operation, CancellationToken token = default);

        /// <summary>
        /// Delete a crawl operation record.
        /// </summary>
        /// <param name="id">Crawl operation identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Check if a crawl operation record exists.
        /// </summary>
        /// <param name="id">Crawl operation identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the record exists.</returns>
        Task<bool> ExistsAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate crawl operation records scoped to a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing crawl operation records.</returns>
        Task<EnumerationResult<CrawlOperation>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Enumerate crawl operation records by crawl plan identifier.
        /// </summary>
        /// <param name="crawlPlanId">Crawl plan identifier.</param>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing crawl operation records.</returns>
        Task<EnumerationResult<CrawlOperation>> EnumerateByCrawlPlanAsync(string crawlPlanId, EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Delete all crawl operation records for a crawl plan.
        /// </summary>
        /// <param name="crawlPlanId">Crawl plan identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteByCrawlPlanAsync(string crawlPlanId, CancellationToken token = default);

        /// <summary>
        /// Delete expired crawl operation records.
        /// </summary>
        /// <param name="crawlPlanId">Crawl plan identifier.</param>
        /// <param name="retentionDays">Retention period in days.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteExpiredAsync(string crawlPlanId, int retentionDays, CancellationToken token = default);
    }
}
