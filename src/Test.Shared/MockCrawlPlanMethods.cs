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
    internal class MockCrawlPlanMethods : ICrawlPlanMethods
    {
        public ConcurrentDictionary<string, CrawlPlan> Store { get; } = new();

        public Task<CrawlPlan> CreateAsync(CrawlPlan plan, CancellationToken token = default)
        {
            Store[plan.Id] = plan;
            return Task.FromResult(plan);
        }

        public Task<CrawlPlan> ReadAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.TryGetValue(id, out CrawlPlan? p) ? p : null);

        public Task<CrawlPlan> UpdateAsync(CrawlPlan plan, CancellationToken token = default)
        {
            Store[plan.Id] = plan;
            return Task.FromResult(plan);
        }

        public Task UpdateStateAsync(string id, CrawlPlanStateEnum state, CancellationToken token = default)
        {
            if (Store.TryGetValue(id, out CrawlPlan? p))
                p.State = state;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken token = default)
        {
            Store.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.ContainsKey(id));

        public Task<EnumerationResult<CrawlPlan>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
            => Task.FromResult(MockDatabaseDriver.Paginate(Store.Values.Where(p => p.TenantId == tenantId).ToList(), query));
    }
}
