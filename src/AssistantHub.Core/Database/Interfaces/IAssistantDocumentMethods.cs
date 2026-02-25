namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Assistant document database methods interface.
    /// </summary>
    public interface IAssistantDocumentMethods
    {
        /// <summary>
        /// Create an assistant document record.
        /// </summary>
        /// <param name="document">Assistant document record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created assistant document record.</returns>
        Task<AssistantDocument> CreateAsync(AssistantDocument document, CancellationToken token = default);

        /// <summary>
        /// Read an assistant document record by identifier.
        /// </summary>
        /// <param name="id">Assistant document identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Assistant document record.</returns>
        Task<AssistantDocument> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Update an assistant document record.
        /// </summary>
        /// <param name="document">Assistant document record.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated assistant document record.</returns>
        Task<AssistantDocument> UpdateAsync(AssistantDocument document, CancellationToken token = default);

        /// <summary>
        /// Update the status of an assistant document record.
        /// </summary>
        /// <param name="id">Assistant document identifier.</param>
        /// <param name="status">Document status.</param>
        /// <param name="statusMessage">Status message.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateStatusAsync(string id, DocumentStatusEnum status, string statusMessage, CancellationToken token = default);

        /// <summary>
        /// Delete an assistant document record.
        /// </summary>
        /// <param name="id">Assistant document identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Check if an assistant document record exists.
        /// </summary>
        /// <param name="id">Assistant document identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the record exists.</returns>
        Task<bool> ExistsAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate assistant document records scoped to a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing assistant document records.</returns>
        Task<EnumerationResult<AssistantDocument>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Update the chunk record IDs for a document after ingestion.
        /// </summary>
        /// <param name="id">Assistant document identifier.</param>
        /// <param name="chunkRecordIdsJson">JSON array of record IDs.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateChunkRecordIdsAsync(string id, string chunkRecordIdsJson, CancellationToken token = default);
    }
}
