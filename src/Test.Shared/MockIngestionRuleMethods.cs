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
    internal class MockIngestionRuleMethods : IIngestionRuleMethods
    {
        public ConcurrentDictionary<string, IngestionRule> Store { get; } = new();

        public Task<IngestionRule> CreateAsync(IngestionRule rule, CancellationToken token = default)
        {
            Store[rule.Id] = rule;
            return Task.FromResult(rule);
        }

        public Task<IngestionRule> ReadAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.TryGetValue(id, out IngestionRule? r) ? r : null);

        public Task<IngestionRule> ReadByNameAsync(string tenantId, string name, CancellationToken token = default)
            => Task.FromResult(Store.Values.FirstOrDefault(r => r.TenantId == tenantId && r.Name == name));

        public Task<IngestionRule> UpdateAsync(IngestionRule rule, CancellationToken token = default)
        {
            Store[rule.Id] = rule;
            return Task.FromResult(rule);
        }

        public Task DeleteAsync(string id, CancellationToken token = default)
        {
            Store.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.ContainsKey(id));

        public Task<EnumerationResult<IngestionRule>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
            => Task.FromResult(MockDatabaseDriver.Paginate(Store.Values.Where(r => r.TenantId == tenantId).ToList(), query));
    }
}
