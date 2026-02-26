namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Crawl plan database methods interface.
    /// </summary>
    public interface ICrawlPlanMethods
    {
        /// <summary>
        /// Create a crawl plan record.
        /// </summary>
        /// <param name="plan">Crawl plan record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created crawl plan record.</returns>
        Task<CrawlPlan> CreateAsync(CrawlPlan plan, CancellationToken token = default);

        /// <summary>
        /// Read a crawl plan record by identifier.
        /// </summary>
        /// <param name="id">Crawl plan identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Crawl plan record.</returns>
        Task<CrawlPlan> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Update a crawl plan record.
        /// </summary>
        /// <param name="plan">Crawl plan record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated crawl plan record.</returns>
        Task<CrawlPlan> UpdateAsync(CrawlPlan plan, CancellationToken token = default);

        /// <summary>
        /// Update the state of a crawl plan.
        /// </summary>
        /// <param name="id">Crawl plan identifier.</param>
        /// <param name="state">New state.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateStateAsync(string id, CrawlPlanStateEnum state, CancellationToken token = default);

        /// <summary>
        /// Delete a crawl plan record.
        /// </summary>
        /// <param name="id">Crawl plan identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Check if a crawl plan record exists.
        /// </summary>
        /// <param name="id">Crawl plan identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the record exists.</returns>
        Task<bool> ExistsAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate crawl plan records scoped to a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing crawl plan records.</returns>
        Task<EnumerationResult<CrawlPlan>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default);
    }
}
