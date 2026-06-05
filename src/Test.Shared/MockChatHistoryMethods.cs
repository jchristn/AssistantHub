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
    internal class MockChatHistoryMethods : IChatHistoryMethods
    {
        public ConcurrentDictionary<string, ChatHistory> Store { get; } = new();

        public Task<ChatHistory> CreateAsync(ChatHistory history, CancellationToken token = default)
        {
            history.CreatedUtc = DateTime.UtcNow;
            history.LastUpdateUtc = DateTime.UtcNow;
            Store[history.Id] = history;
            return Task.FromResult(history);
        }

        public Task<ChatHistory> ReadAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.TryGetValue(id, out ChatHistory? h) ? h : null);

        public Task DeleteAsync(string id, CancellationToken token = default)
        {
            Store.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<EnumerationResult<ChatHistory>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
            => Task.FromResult(MockDatabaseDriver.Paginate(Store.Values.Where(h => h.TenantId == tenantId).ToList(), query));

        public Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default)
        {
            foreach (ChatHistory? h in Store.Values.Where(h => h.AssistantId == assistantId).ToList())
                Store.TryRemove(h.Id, out _);
            return Task.CompletedTask;
        }

        public Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            foreach (ChatHistory? h in Store.Values.Where(h => h.CreatedUtc < cutoff).ToList())
                Store.TryRemove(h.Id, out _);
            return Task.CompletedTask;
        }
    }
}
