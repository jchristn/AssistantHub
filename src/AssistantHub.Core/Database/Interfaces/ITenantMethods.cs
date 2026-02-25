namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Tenant database methods interface.
    /// </summary>
    public interface ITenantMethods
    {
        /// <summary>
        /// Create a tenant record.
        /// </summary>
        /// <param name="tenant">Tenant metadata record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created tenant metadata record.</returns>
        Task<TenantMetadata> CreateAsync(TenantMetadata tenant, CancellationToken token = default);

        /// <summary>
        /// Read a tenant record by identifier.
        /// </summary>
        /// <param name="id">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tenant metadata record.</returns>
        Task<TenantMetadata> ReadByIdAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Read a tenant record by name.
        /// </summary>
        /// <param name="name">Tenant name.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tenant metadata record.</returns>
        Task<TenantMetadata> ReadByNameAsync(string name, CancellationToken token = default);

        /// <summary>
        /// Update a tenant record.
        /// </summary>
        /// <param name="tenant">Tenant metadata record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated tenant metadata record.</returns>
        Task<TenantMetadata> UpdateAsync(TenantMetadata tenant, CancellationToken token = default);

        /// <summary>
        /// Delete a tenant record.
        /// </summary>
        /// <param name="id">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteByIdAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Check if a tenant record exists.
        /// </summary>
        /// <param name="id">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the record exists.</returns>
        Task<bool> ExistsByIdAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate tenant records.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing tenant metadata records.</returns>
        Task<EnumerationResult<TenantMetadata>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Get the count of tenant records.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Count of tenant records.</returns>
        Task<long> GetCountAsync(CancellationToken token = default);
    }
}
