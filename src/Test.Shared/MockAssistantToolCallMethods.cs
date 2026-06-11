namespace Test.Shared
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;

    internal class MockAssistantToolCallMethods : IAssistantToolCallMethods
    {
        public ConcurrentDictionary<string, AssistantToolCallRecord> Store { get; } = new();

        public Task<AssistantToolCallRecord> CreateAsync(AssistantToolCallRecord record, CancellationToken token = default)
        {
            if (record.CreatedUtc == default) record.CreatedUtc = DateTime.UtcNow;
            if (record.LastUpdateUtc == default) record.LastUpdateUtc = record.CreatedUtc;
            Store[record.Id] = record;
            return Task.FromResult(record);
        }

        public async Task CreateManyAsync(IEnumerable<AssistantToolCallRecord> records, CancellationToken token = default)
        {
            if (records == null) return;
            foreach (AssistantToolCallRecord record in records)
                await CreateAsync(record, token).ConfigureAwait(false);
        }

        public Task<AssistantToolCallRecord> ReadAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.TryGetValue(id, out AssistantToolCallRecord record) ? record : null);

        public Task<EnumerationResult<AssistantToolCallRecord>> EnumerateAsync(string tenantId, EnumerationQuery query, string assistantId = null, CancellationToken token = default)
        {
            query ??= new EnumerationQuery();
            IEnumerable<AssistantToolCallRecord> records = Store.Values.Where(record => record.TenantId == tenantId);
            string effectiveAssistantId = !String.IsNullOrWhiteSpace(assistantId) ? assistantId : query.AssistantIdFilter;
            if (!String.IsNullOrWhiteSpace(effectiveAssistantId))
                records = records.Where(record => record.AssistantId == effectiveAssistantId);
            if (!String.IsNullOrWhiteSpace(query.ThreadIdFilter))
                records = records.Where(record => record.ThreadId == query.ThreadIdFilter);
            if (!String.IsNullOrWhiteSpace(query.RequestHistoryIdFilter))
                records = records.Where(record => record.RequestHistoryId == query.RequestHistoryIdFilter);
            if (!String.IsNullOrWhiteSpace(query.ChatHistoryIdFilter))
                records = records.Where(record => record.ChatHistoryId == query.ChatHistoryIdFilter);
            if (!String.IsNullOrWhiteSpace(query.TraceIdFilter))
                records = records.Where(record => record.TraceId == query.TraceIdFilter);
            if (!String.IsNullOrWhiteSpace(query.ToolNameFilter))
                records = records.Where(record => record.ToolName == query.ToolNameFilter);
            if (query.SuccessFilter.HasValue)
                records = records.Where(record => record.Success == query.SuccessFilter.Value);
            if (query.DeniedFilter.HasValue)
                records = records.Where(record => record.Denied == query.DeniedFilter.Value);
            if (query.StartUtc.HasValue)
                records = records.Where(record => record.CreatedUtc >= query.StartUtc.Value);
            if (query.EndUtc.HasValue)
                records = records.Where(record => record.CreatedUtc <= query.EndUtc.Value);

            records = query.Ordering == EnumerationOrderEnum.CreatedAscending
                ? records.OrderBy(record => record.CreatedUtc)
                : records.OrderByDescending(record => record.CreatedUtc);

            return Task.FromResult(MockDatabaseDriver.Paginate(records.ToList(), query));
        }

        public Task<List<AssistantToolCallRecord>> ListByChatHistoryIdAsync(string chatHistoryId, CancellationToken token = default)
            => Task.FromResult(Store.Values
                .Where(record => record.ChatHistoryId == chatHistoryId)
                .OrderBy(record => record.SequenceNumber)
                .ThenBy(record => record.CreatedUtc)
                .ToList());

        public Task AttachChatHistoryIdByTraceIdAsync(string traceId, string chatHistoryId, CancellationToken token = default)
        {
            foreach (AssistantToolCallRecord record in Store.Values.Where(record => record.TraceId == traceId))
            {
                record.ChatHistoryId = chatHistoryId;
                record.LastUpdateUtc = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(string id, CancellationToken token = default)
            => Task.FromResult(Store.TryRemove(id, out _));

        public Task<int> DeleteByFilterAsync(string tenantId, EnumerationQuery query, string assistantId = null, CancellationToken token = default)
        {
            query ??= new EnumerationQuery();
            string effectiveAssistantId = !String.IsNullOrWhiteSpace(assistantId) ? assistantId : query.AssistantIdFilter;
            List<string> ids = Store.Values
                .Where(record => record.TenantId == tenantId)
                .Where(record => String.IsNullOrWhiteSpace(effectiveAssistantId) || record.AssistantId == effectiveAssistantId)
                .Where(record => String.IsNullOrWhiteSpace(query.ThreadIdFilter) || record.ThreadId == query.ThreadIdFilter)
                .Where(record => String.IsNullOrWhiteSpace(query.RequestHistoryIdFilter) || record.RequestHistoryId == query.RequestHistoryIdFilter)
                .Where(record => String.IsNullOrWhiteSpace(query.ChatHistoryIdFilter) || record.ChatHistoryId == query.ChatHistoryIdFilter)
                .Where(record => String.IsNullOrWhiteSpace(query.TraceIdFilter) || record.TraceId == query.TraceIdFilter)
                .Where(record => String.IsNullOrWhiteSpace(query.ToolNameFilter) || record.ToolName == query.ToolNameFilter)
                .Where(record => !query.SuccessFilter.HasValue || record.Success == query.SuccessFilter.Value)
                .Where(record => !query.DeniedFilter.HasValue || record.Denied == query.DeniedFilter.Value)
                .Where(record => !query.StartUtc.HasValue || record.CreatedUtc >= query.StartUtc.Value)
                .Where(record => !query.EndUtc.HasValue || record.CreatedUtc <= query.EndUtc.Value)
                .Select(record => record.Id)
                .ToList();

            int deleted = 0;
            foreach (string id in ids)
            {
                if (Store.TryRemove(id, out _)) deleted++;
            }

            return Task.FromResult(deleted);
        }

        public Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            List<string> ids = Store.Values
                .Where(record => record.CreatedUtc < cutoff)
                .Select(record => record.Id)
                .ToList();

            foreach (string id in ids)
                Store.TryRemove(id, out _);

            return Task.CompletedTask;
        }
    }
}
