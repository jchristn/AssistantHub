namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Credential database methods interface.
    /// </summary>
    public interface ICredentialMethods
    {
        /// <summary>
        /// Create a credential record.
        /// </summary>
        /// <param name="credential">Credential record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created credential record.</returns>
        Task<Credential> CreateAsync(Credential credential, CancellationToken token = default);

        /// <summary>
        /// Read a credential record by identifier.
        /// </summary>
        /// <param name="id">Credential identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Credential record.</returns>
        Task<Credential> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Read a credential record by bearer token.
        /// </summary>
        /// <param name="bearerToken">Bearer token value.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Credential record.</returns>
        Task<Credential> ReadByBearerTokenAsync(string bearerToken, CancellationToken token = default);

        /// <summary>
        /// Update a credential record.
        /// </summary>
        /// <param name="credential">Credential record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated credential record.</returns>
        Task<Credential> UpdateAsync(Credential credential, CancellationToken token = default);

        /// <summary>
        /// Delete a credential record.
        /// </summary>
        /// <param name="id">Credential identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Check if a credential record exists.
        /// </summary>
        /// <param name="id">Credential identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the record exists.</returns>
        Task<bool> ExistsAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate credential records.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing credential records.</returns>
        Task<EnumerationResult<Credential>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Delete all credential records belonging to a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteByUserIdAsync(string userId, CancellationToken token = default);
    }
}
