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
    internal class MockAssistantSettingsMethods : IAssistantSettingsMethods
    {
        public ConcurrentDictionary<string, AssistantSettings> Store { get; } = new();

        public Task<AssistantSettings> CreateAsync(AssistantSettings settings, CancellationToken token = default)
        {
            settings.CreatedUtc = DateTime.UtcNow;
            settings.LastUpdateUtc = DateTime.UtcNow;
            Store[settings.Id] = settings;
            return Task.FromResult(settings);
        }

        public Task<AssistantSettings> ReadAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.TryGetValue(id, out AssistantSettings? s) ? s : null);

        public Task<AssistantSettings> ReadByAssistantIdAsync(string assistantId, CancellationToken token = default)
            => Task.FromResult(Store.Values.FirstOrDefault(s => s.AssistantId == assistantId));

        public Task<AssistantSettings> UpdateAsync(AssistantSettings settings, CancellationToken token = default)
        {
            settings.LastUpdateUtc = DateTime.UtcNow;
            Store[settings.Id] = settings;
            return Task.FromResult(settings);
        }

        public Task DeleteAsync(string id, CancellationToken token = default)
        {
            Store.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default)
        {
            foreach (AssistantSettings? s in Store.Values.Where(s => s.AssistantId == assistantId).ToList())
                Store.TryRemove(s.Id, out _);
            return Task.CompletedTask;
        }
    }
}
