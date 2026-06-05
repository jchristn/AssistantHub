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
    internal class MockAssistantMethods : IAssistantMethods
    {
        public ConcurrentDictionary<string, Assistant> Store { get; } = new();

        public Task<Assistant> CreateAsync(Assistant assistant, CancellationToken token = default)
        {
            assistant.CreatedUtc = DateTime.UtcNow;
            assistant.LastUpdateUtc = DateTime.UtcNow;
            Store[assistant.Id] = assistant;
            return Task.FromResult(assistant);
        }

        public Task<Assistant> ReadAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.TryGetValue(id, out Assistant? a) ? a : null);

        public Task<Assistant> UpdateAsync(Assistant assistant, CancellationToken token = default)
        {
            assistant.LastUpdateUtc = DateTime.UtcNow;
            Store[assistant.Id] = assistant;
            return Task.FromResult(assistant);
        }

        public Task DeleteAsync(string id, CancellationToken token = default)
        {
            Store.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.ContainsKey(id));

        public Task<EnumerationResult<Assistant>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
            => Task.FromResult(MockDatabaseDriver.Paginate(Store.Values.Where(a => a.TenantId == tenantId).ToList(), query));

        public Task<long> GetCountAsync(CancellationToken token = default)
            => Task.FromResult((long)Store.Count);
    }
}
