namespace Test.Shared
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    internal class MockAssistantDocumentMethods : IAssistantDocumentMethods
    {
        public ConcurrentDictionary<string, AssistantDocument> Store { get; } = new();

        public Task<AssistantDocument> CreateAsync(AssistantDocument document, CancellationToken token = default)
        {
            Store[document.Id] = document;
            return Task.FromResult(document);
        }

        public Task<AssistantDocument> ReadAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.TryGetValue(id, out AssistantDocument? d) ? d : null);

        public Task<AssistantDocument> UpdateAsync(AssistantDocument document, CancellationToken token = default)
        {
            Store[document.Id] = document;
            return Task.FromResult(document);
        }

        public Task UpdateStatusAsync(string id, DocumentStatusEnum status, string statusMessage, CancellationToken token = default)
        {
            if (Store.TryGetValue(id, out AssistantDocument? d))
            {
                d.Status = status;
                d.StatusMessage = statusMessage;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken token = default)
        {
            Store.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.ContainsKey(id));

        public Task<EnumerationResult<AssistantDocument>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
            => Task.FromResult(MockDatabaseDriver.Paginate(Store.Values.Where(d => d.TenantId == tenantId).ToList(), query));

        public Task UpdateChunkRecordIdsAsync(string id, string chunkRecordIdsJson, CancellationToken token = default)
        {
            if (Store.TryGetValue(id, out AssistantDocument? d))
                d.ChunkRecordIds = chunkRecordIdsJson;
            return Task.CompletedTask;
        }

        public Task UpdateVerbexIndexMetadataAsync(string id, string verbexTenantId, string verbexIndexId, string verbexRecordId, CancellationToken token = default)
        {
            if (Store.TryGetValue(id, out AssistantDocument? d))
            {
                d.VerbexTenantId = verbexTenantId;
                d.VerbexIndexId = verbexIndexId;
                d.VerbexRecordId = verbexRecordId;
            }
            return Task.CompletedTask;
        }
    }
}
