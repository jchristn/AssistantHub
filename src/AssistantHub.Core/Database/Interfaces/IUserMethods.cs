namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// User database methods interface.
    /// </summary>
    public interface IUserMethods
    {
        /// <summary>
        /// Create a user record.
        /// </summary>
        /// <param name="user">User master record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created user master record.</returns>
        Task<UserMaster> CreateAsync(UserMaster user, CancellationToken token = default);

        /// <summary>
        /// Read a user record by identifier.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>User master record.</returns>
        Task<UserMaster> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Read a user record by tenant and email address.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="email">Email address.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>User master record.</returns>
        Task<UserMaster> ReadByEmailAsync(string tenantId, string email, CancellationToken token = default);

        /// <summary>
        /// Update a user record.
        /// </summary>
        /// <param name="user">User master record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated user master record.</returns>
        Task<UserMaster> UpdateAsync(UserMaster user, CancellationToken token = default);

        /// <summary>
        /// Delete a user record.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Check if a user record exists.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the record exists.</returns>
        Task<bool> ExistsAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate user records scoped to a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing user master records.</returns>
        Task<EnumerationResult<UserMaster>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Get the count of user records.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Count of user records.</returns>
        Task<long> GetCountAsync(CancellationToken token = default);
    }
}
