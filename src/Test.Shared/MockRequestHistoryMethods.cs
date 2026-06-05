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
    internal class MockRequestHistoryMethods : IRequestHistoryMethods
    {
        public ConcurrentDictionary<string, RequestHistoryEntry> Store { get; } = new();

        public Task<RequestHistoryEntry> CreateAsync(RequestHistoryEntry entry, CancellationToken token = default)
        {
            entry.CreatedUtc = DateTime.UtcNow;
            entry.LastUpdateUtc = DateTime.UtcNow;
            Store[entry.Id] = entry;
            return Task.FromResult(entry);
        }

        public Task<RequestHistoryEntry> ReadAsync(string id, bool includeDetails = true, CancellationToken token = default)
        {
            if (!Store.TryGetValue(id, out RequestHistoryEntry entry))
                return Task.FromResult<RequestHistoryEntry>(null);

            if (includeDetails) return Task.FromResult(entry);

            RequestHistoryEntry copy = new RequestHistoryEntry
            {
                Id = entry.Id,
                TraceId = entry.TraceId,
                ChatHistoryId = entry.ChatHistoryId,
                TenantId = entry.TenantId,
                UserId = entry.UserId,
                CredentialId = entry.CredentialId,
                AssistantId = entry.AssistantId,
                ThreadId = entry.ThreadId,
                PrincipalName = entry.PrincipalName,
                RequestType = entry.RequestType,
                SourceType = entry.SourceType,
                HttpMethod = entry.HttpMethod,
                RouteTemplate = entry.RouteTemplate,
                RequestPath = entry.RequestPath,
                RequestUrl = entry.RequestUrl,
                SourceIp = entry.SourceIp,
                StatusCode = entry.StatusCode,
                Success = entry.Success,
                DurationMs = entry.DurationMs,
                RequestContentType = entry.RequestContentType,
                ResponseContentType = entry.ResponseContentType,
                RequestSizeBytes = entry.RequestSizeBytes,
                ResponseSizeBytes = entry.ResponseSizeBytes,
                RequestBodyTruncated = entry.RequestBodyTruncated,
                ResponseBodyTruncated = entry.ResponseBodyTruncated,
                RequestBodyIsBinary = entry.RequestBodyIsBinary,
                ResponseBodyIsBinary = entry.ResponseBodyIsBinary,
                CreatedUtc = entry.CreatedUtc,
                LastUpdateUtc = entry.LastUpdateUtc
            };

            return Task.FromResult(copy);
        }

        public Task<EnumerationResult<RequestHistoryEntry>> EnumerateAsync(RequestHistorySearchFilter filter, CancellationToken token = default)
        {
            filter ??= new RequestHistorySearchFilter();
            List<RequestHistoryEntry> items = ApplyFilter(filter).OrderByDescending(entry => entry.CreatedUtc).ToList();
            return Task.FromResult(MockDatabaseDriver.Paginate(items, new EnumerationQuery
            {
                MaxResults = filter.MaxResults,
                ContinuationToken = filter.ContinuationToken,
                Ordering = filter.Ordering
            }));
        }

        public Task<RequestHistorySummaryResult> SummarizeAsync(RequestHistorySearchFilter filter, CancellationToken token = default)
        {
            List<RequestHistoryEntry> items = ApplyFilter(filter ?? new RequestHistorySearchFilter()).ToList();
            RequestHistorySummaryResult summary = new RequestHistorySummaryResult
            {
                TotalCount = items.Count,
                TotalSuccess = items.Count(entry => entry.Success),
                TotalFailure = items.Count(entry => !entry.Success),
                AverageDurationMs = items.Count > 0 ? items.Average(entry => entry.DurationMs) : 0
            };

            return Task.FromResult(summary);
        }

        public Task<bool> DeleteAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.TryRemove(id, out _));

        public Task<int> DeleteByFilterAsync(RequestHistorySearchFilter filter, CancellationToken token = default)
        {
            List<RequestHistoryEntry> matches = ApplyFilter(filter ?? new RequestHistorySearchFilter()).ToList();
            int deleted = 0;
            foreach (RequestHistoryEntry entry in matches)
            {
                if (Store.TryRemove(entry.Id, out _)) deleted++;
            }

            return Task.FromResult(deleted);
        }

        public Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            foreach (RequestHistoryEntry entry in Store.Values.Where(entry => entry.CreatedUtc < cutoff).ToList())
                Store.TryRemove(entry.Id, out _);
            return Task.CompletedTask;
        }

        private IEnumerable<RequestHistoryEntry> ApplyFilter(RequestHistorySearchFilter filter)
        {
            IEnumerable<RequestHistoryEntry> query = Store.Values;
            if (!String.IsNullOrEmpty(filter.TenantId)) query = query.Where(entry => entry.TenantId == filter.TenantId);
            if (!String.IsNullOrEmpty(filter.AssistantId)) query = query.Where(entry => entry.AssistantId == filter.AssistantId);
            if (!String.IsNullOrEmpty(filter.ThreadId)) query = query.Where(entry => entry.ThreadId == filter.ThreadId);
            if (!String.IsNullOrEmpty(filter.RequestType)) query = query.Where(entry => entry.RequestType == filter.RequestType);
            if (!String.IsNullOrEmpty(filter.HttpMethod)) query = query.Where(entry => entry.HttpMethod == filter.HttpMethod);
            if (filter.StatusCode.HasValue) query = query.Where(entry => entry.StatusCode == filter.StatusCode.Value);
            if (filter.Success.HasValue) query = query.Where(entry => entry.Success == filter.Success.Value);
            return query;
        }
    }
}
