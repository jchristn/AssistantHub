namespace AssistantHub.Core.Database.Interfaces
{
    using System;
    using System.Collections.Generic;
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

        /// <summary>
        /// Update Verbex indexing metadata for a document after full-text indexing.
        /// </summary>
        /// <param name="id">Assistant document identifier.</param>
        /// <param name="verbexTenantId">Verbex tenant identifier.</param>
        /// <param name="verbexIndexId">Verbex index identifier.</param>
        /// <param name="verbexRecordId">Verbex record identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateVerbexIndexMetadataAsync(string id, string verbexTenantId, string verbexIndexId, string verbexRecordId, CancellationToken token = default);

        /// <summary>
        /// Update a document's supersession links.
        /// </summary>
        /// <param name="id">Document identifier.</param>
        /// <param name="supersedesJson">JSON array of document identifiers this document replaces, or null.</param>
        /// <param name="supersededBy">Identifier of the document that replaces this one, or null.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateSupersessionAsync(string id, string supersedesJson, string supersededBy, CancellationToken token = default);

        /// <summary>
        /// Update a document's content hash and near-duplicate matches.
        /// </summary>
        /// <param name="id">Document identifier.</param>
        /// <param name="contentSha256">SHA-256 of the uploaded bytes (hex), or null.</param>
        /// <param name="nearDuplicatesJson">JSON array of near-duplicate matches, or null.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateContentHashAsync(string id, string contentSha256, string nearDuplicatesJson, CancellationToken token = default);

        /// <summary>
        /// Read the documents in a tenant (optionally one collection) with a given content hash.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="collectionId">Optional collection identifier.</param>
        /// <param name="contentSha256">SHA-256 of the content (hex).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Matching documents.</returns>
        Task<List<AssistantDocument>> ReadByContentHashAsync(string tenantId, string collectionId, string contentSha256, CancellationToken token = default);

        /// <summary>
        /// Read the documents that a given document supersedes (their SupersededBy is that document).
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="replacementId">Identifier of the replacing document.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Superseded documents.</returns>
        Task<List<AssistantDocument>> ReadSupersededByAsync(string tenantId, string replacementId, CancellationToken token = default);
    }
}
