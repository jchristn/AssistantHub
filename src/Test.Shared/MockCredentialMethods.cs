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
    internal class MockCredentialMethods : ICredentialMethods
    {
        public ConcurrentDictionary<string, Credential> Store { get; } = new();

        public Task<Credential> CreateAsync(Credential credential, CancellationToken token = default)
        {
            credential.CreatedUtc = DateTime.UtcNow;
            credential.LastUpdateUtc = DateTime.UtcNow;
            Store[credential.Id] = credential;
            return Task.FromResult(credential);
        }

        public Task<Credential> ReadAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.TryGetValue(id, out Credential? c) ? c : null);

        public Task<Credential> ReadByBearerTokenAsync(string bearerToken, CancellationToken token = default)
            => Task.FromResult(Store.Values.FirstOrDefault(c => c.BearerToken == bearerToken));

        public Task<Credential> UpdateAsync(Credential credential, CancellationToken token = default)
        {
            credential.LastUpdateUtc = DateTime.UtcNow;
            Store[credential.Id] = credential;
            return Task.FromResult(credential);
        }

        public Task DeleteAsync(string id, CancellationToken token = default)
        {
            Store.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.ContainsKey(id));

        public Task<EnumerationResult<Credential>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
            => Task.FromResult(MockDatabaseDriver.Paginate(Store.Values.Where(c => c.TenantId == tenantId).ToList(), query));

        public Task DeleteByUserIdAsync(string userId, CancellationToken token = default)
        {
            foreach (Credential? c in Store.Values.Where(c => c.UserId == userId).ToList())
                Store.TryRemove(c.Id, out _);
            return Task.CompletedTask;
        }
    }
}
