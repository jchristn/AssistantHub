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
    internal class MockAssistantFeedbackMethods : IAssistantFeedbackMethods
    {
        public ConcurrentDictionary<string, AssistantFeedback> Store { get; } = new();

        public Task<AssistantFeedback> CreateAsync(AssistantFeedback feedback, CancellationToken token = default)
        {
            Store[feedback.Id] = feedback;
            return Task.FromResult(feedback);
        }

        public Task<AssistantFeedback> ReadAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.TryGetValue(id, out AssistantFeedback? f) ? f : null);

        public Task DeleteAsync(string id, CancellationToken token = default)
        {
            Store.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<EnumerationResult<AssistantFeedback>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
            => Task.FromResult(MockDatabaseDriver.Paginate(Store.Values.Where(f => f.TenantId == tenantId).ToList(), query));

        public Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default)
        {
            foreach (AssistantFeedback? f in Store.Values.Where(f => f.AssistantId == assistantId).ToList())
                Store.TryRemove(f.Id, out _);
            return Task.CompletedTask;
        }
    }
}
