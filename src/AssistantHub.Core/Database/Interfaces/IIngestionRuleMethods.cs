namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Ingestion rule database methods interface.
    /// </summary>
    public interface IIngestionRuleMethods
    {
        /// <summary>
        /// Create an ingestion rule record.
        /// </summary>
        /// <param name="rule">Ingestion rule record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created ingestion rule record.</returns>
        Task<IngestionRule> CreateAsync(IngestionRule rule, CancellationToken token = default);

        /// <summary>
        /// Read an ingestion rule record by identifier.
        /// </summary>
        /// <param name="id">Ingestion rule identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Ingestion rule record.</returns>
        Task<IngestionRule> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Read an ingestion rule record by tenant and name.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="name">Ingestion rule name.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Ingestion rule record.</returns>
        Task<IngestionRule> ReadByNameAsync(string tenantId, string name, CancellationToken token = default);

        /// <summary>
        /// Update an ingestion rule record.
        /// </summary>
        /// <param name="rule">Ingestion rule record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated ingestion rule record.</returns>
        Task<IngestionRule> UpdateAsync(IngestionRule rule, CancellationToken token = default);

        /// <summary>
        /// Delete an ingestion rule record.
        /// </summary>
        /// <param name="id">Ingestion rule identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Check if an ingestion rule record exists.
        /// </summary>
        /// <param name="id">Ingestion rule identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the record exists.</returns>
        Task<bool> ExistsAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate ingestion rule records scoped to a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing ingestion rule records.</returns>
        Task<EnumerationResult<IngestionRule>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default);
    }
}
