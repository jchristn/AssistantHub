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
    internal class MockTenantMethods : ITenantMethods
    {
        public ConcurrentDictionary<string, TenantMetadata> Store { get; } = new();

        public Task<TenantMetadata> CreateAsync(TenantMetadata tenant, CancellationToken token = default)
        {
            tenant.CreatedUtc = DateTime.UtcNow;
            tenant.LastUpdateUtc = DateTime.UtcNow;
            Store[tenant.Id] = tenant;
            return Task.FromResult(tenant);
        }

        public Task<TenantMetadata> ReadByIdAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.TryGetValue(id, out TenantMetadata? t) ? t : null);

        public Task<TenantMetadata> ReadByNameAsync(string name, CancellationToken token = default)
            => Task.FromResult(Store.Values.FirstOrDefault(t => t.Name == name));

        public Task<TenantMetadata> UpdateAsync(TenantMetadata tenant, CancellationToken token = default)
        {
            tenant.LastUpdateUtc = DateTime.UtcNow;
            Store[tenant.Id] = tenant;
            return Task.FromResult(tenant);
        }

        public Task DeleteByIdAsync(string id, CancellationToken token = default)
        {
            Store.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByIdAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.ContainsKey(id));

        public Task<EnumerationResult<TenantMetadata>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
            => Task.FromResult(MockDatabaseDriver.Paginate(Store.Values.ToList(), query));

        public Task<long> GetCountAsync(CancellationToken token = default)
            => Task.FromResult((long)Store.Count);
    }
}
