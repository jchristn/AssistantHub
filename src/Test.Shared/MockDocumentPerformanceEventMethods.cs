namespace Test.Shared
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Models;

    internal class MockDocumentPerformanceEventMethods : IDocumentPerformanceEventMethods
    {
        public ConcurrentDictionary<string, DocumentPerformanceEvent> Store { get; } = new();

        public Task<DocumentPerformanceEvent> CreateAsync(DocumentPerformanceEvent evt, CancellationToken token = default)
        {
            evt.CreatedUtc = DateTime.UtcNow;
            Store[evt.Id] = evt;
            return Task.FromResult(evt);
        }

        public async Task CreateManyAsync(IEnumerable<DocumentPerformanceEvent> events, CancellationToken token = default)
        {
            if (events == null) return;
            foreach (DocumentPerformanceEvent evt in events)
                await CreateAsync(evt, token).ConfigureAwait(false);
        }

        public Task<List<DocumentPerformanceEvent>> ListByDocumentIdAsync(string documentId, CancellationToken token = default)
            => Task.FromResult(Store.Values.Where(evt => evt.DocumentId == documentId).OrderBy(evt => evt.SequenceNumber).ToList());

        public Task DeleteByDocumentIdAsync(string documentId, CancellationToken token = default)
        {
            foreach (DocumentPerformanceEvent evt in Store.Values.Where(evt => evt.DocumentId == documentId).ToList())
                Store.TryRemove(evt.Id, out _);
            return Task.CompletedTask;
        }

        public Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            foreach (DocumentPerformanceEvent evt in Store.Values.Where(evt => evt.CreatedUtc < cutoff).ToList())
                Store.TryRemove(evt.Id, out _);
            return Task.CompletedTask;
        }
    }
}
