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
    internal class MockCrawlOperationMethods : ICrawlOperationMethods
    {
        public ConcurrentDictionary<string, CrawlOperation> Store { get; } = new();

        public Task<CrawlOperation> CreateAsync(CrawlOperation operation, CancellationToken token = default)
        {
            Store[operation.Id] = operation;
            return Task.FromResult(operation);
        }

        public Task<CrawlOperation> ReadAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.TryGetValue(id, out CrawlOperation? o) ? o : null);

        public Task<CrawlOperation> UpdateAsync(CrawlOperation operation, CancellationToken token = default)
        {
            Store[operation.Id] = operation;
            return Task.FromResult(operation);
        }

        public Task DeleteAsync(string id, CancellationToken token = default)
        {
            Store.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.ContainsKey(id));

        public Task<EnumerationResult<CrawlOperation>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
            => Task.FromResult(MockDatabaseDriver.Paginate(Store.Values.Where(o => o.TenantId == tenantId).ToList(), query));

        public Task<EnumerationResult<CrawlOperation>> EnumerateByCrawlPlanAsync(string crawlPlanId, EnumerationQuery query, CancellationToken token = default)
            => Task.FromResult(MockDatabaseDriver.Paginate(Store.Values.Where(o => o.CrawlPlanId == crawlPlanId).ToList(), query));

        public Task DeleteByCrawlPlanAsync(string crawlPlanId, CancellationToken token = default)
        {
            foreach (CrawlOperation? o in Store.Values.Where(o => o.CrawlPlanId == crawlPlanId).ToList())
                Store.TryRemove(o.Id, out _);
            return Task.CompletedTask;
        }

        public Task DeleteExpiredAsync(string crawlPlanId, int retentionDays, CancellationToken token = default)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            foreach (CrawlOperation? o in Store.Values.Where(o => o.CrawlPlanId == crawlPlanId && o.CreatedUtc < cutoff).ToList())
                Store.TryRemove(o.Id, out _);
            return Task.CompletedTask;
        }
    }
}
