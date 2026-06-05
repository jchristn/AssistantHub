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
    internal class MockUserMethods : IUserMethods
    {
        public ConcurrentDictionary<string, UserMaster> Store { get; } = new();

        public Task<UserMaster> CreateAsync(UserMaster user, CancellationToken token = default)
        {
            user.CreatedUtc = DateTime.UtcNow;
            user.LastUpdateUtc = DateTime.UtcNow;
            Store[user.Id] = user;
            return Task.FromResult(user);
        }

        public Task<UserMaster> ReadAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.TryGetValue(id, out UserMaster? u) ? u : null);

        public Task<UserMaster> ReadByEmailAsync(string tenantId, string email, CancellationToken token = default)
            => Task.FromResult(Store.Values.FirstOrDefault(u => u.TenantId == tenantId && u.Email == email));

        public Task<UserMaster> UpdateAsync(UserMaster user, CancellationToken token = default)
        {
            user.LastUpdateUtc = DateTime.UtcNow;
            Store[user.Id] = user;
            return Task.FromResult(user);
        }

        public Task DeleteAsync(string id, CancellationToken token = default)
        {
            Store.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.ContainsKey(id));

        public Task<EnumerationResult<UserMaster>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
            => Task.FromResult(MockDatabaseDriver.Paginate(Store.Values.Where(u => u.TenantId == tenantId).ToList(), query));

        public Task<long> GetCountAsync(CancellationToken token = default)
            => Task.FromResult((long)Store.Count);
    }
}
