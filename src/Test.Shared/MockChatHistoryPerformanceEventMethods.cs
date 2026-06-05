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
    internal class MockChatHistoryPerformanceEventMethods : IChatHistoryPerformanceEventMethods
    {
        public ConcurrentDictionary<string, ChatHistoryPerformanceEvent> Store { get; } = new();

        public Task<ChatHistoryPerformanceEvent> CreateAsync(ChatHistoryPerformanceEvent evt, CancellationToken token = default)
        {
            evt.CreatedUtc = DateTime.UtcNow;
            Store[evt.Id] = evt;
            return Task.FromResult(evt);
        }

        public async Task CreateManyAsync(IEnumerable<ChatHistoryPerformanceEvent> events, CancellationToken token = default)
        {
            if (events == null) return;
            foreach (ChatHistoryPerformanceEvent evt in events)
                await CreateAsync(evt, token).ConfigureAwait(false);
        }

        public Task<List<ChatHistoryPerformanceEvent>> ListByChatHistoryIdAsync(string chatHistoryId, CancellationToken token = default)
            => Task.FromResult(Store.Values.Where(evt => evt.ChatHistoryId == chatHistoryId).OrderBy(evt => evt.SequenceNumber).ToList());

        public Task DeleteByChatHistoryIdAsync(string chatHistoryId, CancellationToken token = default)
        {
            foreach (ChatHistoryPerformanceEvent evt in Store.Values.Where(evt => evt.ChatHistoryId == chatHistoryId).ToList())
                Store.TryRemove(evt.Id, out _);
            return Task.CompletedTask;
        }

        public Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            foreach (ChatHistoryPerformanceEvent evt in Store.Values.Where(evt => evt.CreatedUtc < cutoff).ToList())
                Store.TryRemove(evt.Id, out _);
            return Task.CompletedTask;
        }
    }
}
