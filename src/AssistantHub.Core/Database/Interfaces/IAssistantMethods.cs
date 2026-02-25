namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Assistant database methods interface.
    /// </summary>
    public interface IAssistantMethods
    {
        /// <summary>
        /// Create an assistant record.
        /// </summary>
        /// <param name="assistant">Assistant record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created assistant record.</returns>
        Task<Assistant> CreateAsync(Assistant assistant, CancellationToken token = default);

        /// <summary>
        /// Read an assistant record by identifier.
        /// </summary>
        /// <param name="id">Assistant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Assistant record.</returns>
        Task<Assistant> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Update an assistant record.
        /// </summary>
        /// <param name="assistant">Assistant record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated assistant record.</returns>
        Task<Assistant> UpdateAsync(Assistant assistant, CancellationToken token = default);

        /// <summary>
        /// Delete an assistant record.
        /// </summary>
        /// <param name="id">Assistant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Check if an assistant record exists.
        /// </summary>
        /// <param name="id">Assistant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the record exists.</returns>
        Task<bool> ExistsAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate assistant records scoped to a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing assistant records.</returns>
        Task<EnumerationResult<Assistant>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Get the count of assistant records.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Count of assistant records.</returns>
        Task<long> GetCountAsync(CancellationToken token = default);
    }
}
