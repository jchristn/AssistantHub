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

    /// <summary>
    /// In-memory mock database driver for unit testing.
    /// </summary>
    public class MockDatabaseDriver : DatabaseDriverBase
    {
        public MockDatabaseDriver()
        {
            Tenant = new MockTenantMethods();
            User = new MockUserMethods();
            Credential = new MockCredentialMethods();
            Assistant = new MockAssistantMethods();
            AssistantSettings = new MockAssistantSettingsMethods();
            AssistantDocument = new MockAssistantDocumentMethods();
            AssistantFeedback = new MockAssistantFeedbackMethods();
            IngestionRule = new MockIngestionRuleMethods();
            ChatHistory = new MockChatHistoryMethods();
            ChatHistoryPerformanceEvent = new MockChatHistoryPerformanceEventMethods();
            RequestHistory = new MockRequestHistoryMethods();
            CrawlPlan = new MockCrawlPlanMethods();
            CrawlOperation = new MockCrawlOperationMethods();
        }

        public override Task InitializeAsync(CancellationToken token = default) => Task.CompletedTask;

        public override Task<DataTable> ExecuteQueryAsync(string query, bool isTransaction = false, CancellationToken token = default)
            => Task.FromResult(new DataTable());

        public override Task<DataTable> ExecuteQueriesAsync(IEnumerable<string> queries, bool isTransaction = false, CancellationToken token = default)
            => Task.FromResult(new DataTable());

        // --- Tenant ---
        public class MockTenantMethods : ITenantMethods
        {
            public ConcurrentDictionary<string, TenantMetadata> Store { get; } = new();

            public Task<TenantMetadata> CreateAsync(TenantMetadata tenant, CancellationToken token = default)
            {
                tenant.CreatedUtc = DateTime.UtcNow;
                tenant.LastUpdateUtc = DateTime.UtcNow;
                Store[tenant.Id] = tenant;
                return Task.FromResult(tenant);
            }

            public Task<TenantMetadata> ReadByIdAsync(string id, CancellationToken token = default)
                => Task.FromResult(Store.TryGetValue(id, out var t) ? t : null);

            public Task<TenantMetadata> ReadByNameAsync(string name, CancellationToken token = default)
                => Task.FromResult(Store.Values.FirstOrDefault(t => t.Name == name));

            public Task<TenantMetadata> UpdateAsync(TenantMetadata tenant, CancellationToken token = default)
            {
                tenant.LastUpdateUtc = DateTime.UtcNow;
                Store[tenant.Id] = tenant;
                return Task.FromResult(tenant);
            }

            public Task DeleteByIdAsync(string id, CancellationToken token = default)
            {
                Store.TryRemove(id, out _);
                return Task.CompletedTask;
            }

            public Task<bool> ExistsByIdAsync(string id, CancellationToken token = default)
                => Task.FromResult(Store.ContainsKey(id));

            public Task<EnumerationResult<TenantMetadata>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
                => Task.FromResult(Paginate(Store.Values.ToList(), query));

            public Task<long> GetCountAsync(CancellationToken token = default)
                => Task.FromResult((long)Store.Count);
        }

        // --- User ---
        public class MockUserMethods : IUserMethods
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
                => Task.FromResult(Store.TryGetValue(id, out var u) ? u : null);

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
                => Task.FromResult(Paginate(Store.Values.Where(u => u.TenantId == tenantId).ToList(), query));

            public Task<long> GetCountAsync(CancellationToken token = default)
                => Task.FromResult((long)Store.Count);
        }

        // --- Credential ---
        public class MockCredentialMethods : ICredentialMethods
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
                => Task.FromResult(Store.TryGetValue(id, out var c) ? c : null);

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
                => Task.FromResult(Paginate(Store.Values.Where(c => c.TenantId == tenantId).ToList(), query));

            public Task DeleteByUserIdAsync(string userId, CancellationToken token = default)
            {
                foreach (var c in Store.Values.Where(c => c.UserId == userId).ToList())
                    Store.TryRemove(c.Id, out _);
                return Task.CompletedTask;
            }
        }

        // --- Assistant ---
        public class MockAssistantMethods : IAssistantMethods
        {
            public ConcurrentDictionary<string, Assistant> Store { get; } = new();

            public Task<Assistant> CreateAsync(Assistant assistant, CancellationToken token = default)
            {
                assistant.CreatedUtc = DateTime.UtcNow;
                assistant.LastUpdateUtc = DateTime.UtcNow;
                Store[assistant.Id] = assistant;
                return Task.FromResult(assistant);
            }

            public Task<Assistant> ReadAsync(string id, CancellationToken token = default)
                => Task.FromResult(Store.TryGetValue(id, out var a) ? a : null);

            public Task<Assistant> UpdateAsync(Assistant assistant, CancellationToken token = default)
            {
                assistant.LastUpdateUtc = DateTime.UtcNow;
                Store[assistant.Id] = assistant;
                return Task.FromResult(assistant);
            }

            public Task DeleteAsync(string id, CancellationToken token = default)
            {
                Store.TryRemove(id, out _);
                return Task.CompletedTask;
            }

            public Task<bool> ExistsAsync(string id, CancellationToken token = default)
                => Task.FromResult(Store.ContainsKey(id));

            public Task<EnumerationResult<Assistant>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
                => Task.FromResult(Paginate(Store.Values.Where(a => a.TenantId == tenantId).ToList(), query));

            public Task<long> GetCountAsync(CancellationToken token = default)
                => Task.FromResult((long)Store.Count);
        }

        // --- AssistantSettings ---
        public class MockAssistantSettingsMethods : IAssistantSettingsMethods
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
                => Task.FromResult(Store.TryGetValue(id, out var s) ? s : null);

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
                foreach (var s in Store.Values.Where(s => s.AssistantId == assistantId).ToList())
                    Store.TryRemove(s.Id, out _);
                return Task.CompletedTask;
            }
        }

        // --- AssistantDocument ---
        public class MockAssistantDocumentMethods : IAssistantDocumentMethods
        {
            public ConcurrentDictionary<string, AssistantDocument> Store { get; } = new();

            public Task<AssistantDocument> CreateAsync(AssistantDocument document, CancellationToken token = default)
            {
                Store[document.Id] = document;
                return Task.FromResult(document);
            }

            public Task<AssistantDocument> ReadAsync(string id, CancellationToken token = default)
                => Task.FromResult(Store.TryGetValue(id, out var d) ? d : null);

            public Task<AssistantDocument> UpdateAsync(AssistantDocument document, CancellationToken token = default)
            {
                Store[document.Id] = document;
                return Task.FromResult(document);
            }

            public Task UpdateStatusAsync(string id, DocumentStatusEnum status, string statusMessage, CancellationToken token = default)
            {
                if (Store.TryGetValue(id, out var d))
                {
                    d.Status = status;
                    d.StatusMessage = statusMessage;
                }
                return Task.CompletedTask;
            }

            public Task DeleteAsync(string id, CancellationToken token = default)
            {
                Store.TryRemove(id, out _);
                return Task.CompletedTask;
            }

            public Task<bool> ExistsAsync(string id, CancellationToken token = default)
                => Task.FromResult(Store.ContainsKey(id));

            public Task<EnumerationResult<AssistantDocument>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
                => Task.FromResult(Paginate(Store.Values.Where(d => d.TenantId == tenantId).ToList(), query));

            public Task UpdateChunkRecordIdsAsync(string id, string chunkRecordIdsJson, CancellationToken token = default)
            {
                if (Store.TryGetValue(id, out var d))
                    d.ChunkRecordIds = chunkRecordIdsJson;
                return Task.CompletedTask;
            }
        }

        // --- AssistantFeedback ---
        public class MockAssistantFeedbackMethods : IAssistantFeedbackMethods
        {
            public ConcurrentDictionary<string, AssistantFeedback> Store { get; } = new();

            public Task<AssistantFeedback> CreateAsync(AssistantFeedback feedback, CancellationToken token = default)
            {
                Store[feedback.Id] = feedback;
                return Task.FromResult(feedback);
            }

            public Task<AssistantFeedback> ReadAsync(string id, CancellationToken token = default)
                => Task.FromResult(Store.TryGetValue(id, out var f) ? f : null);

            public Task DeleteAsync(string id, CancellationToken token = default)
            {
                Store.TryRemove(id, out _);
                return Task.CompletedTask;
            }

            public Task<EnumerationResult<AssistantFeedback>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
                => Task.FromResult(Paginate(Store.Values.Where(f => f.TenantId == tenantId).ToList(), query));

            public Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default)
            {
                foreach (var f in Store.Values.Where(f => f.AssistantId == assistantId).ToList())
                    Store.TryRemove(f.Id, out _);
                return Task.CompletedTask;
            }
        }

        // --- IngestionRule ---
        public class MockIngestionRuleMethods : IIngestionRuleMethods
        {
            public ConcurrentDictionary<string, IngestionRule> Store { get; } = new();

            public Task<IngestionRule> CreateAsync(IngestionRule rule, CancellationToken token = default)
            {
                Store[rule.Id] = rule;
                return Task.FromResult(rule);
            }

            public Task<IngestionRule> ReadAsync(string id, CancellationToken token = default)
                => Task.FromResult(Store.TryGetValue(id, out var r) ? r : null);

            public Task<IngestionRule> ReadByNameAsync(string tenantId, string name, CancellationToken token = default)
                => Task.FromResult(Store.Values.FirstOrDefault(r => r.TenantId == tenantId && r.Name == name));

            public Task<IngestionRule> UpdateAsync(IngestionRule rule, CancellationToken token = default)
            {
                Store[rule.Id] = rule;
                return Task.FromResult(rule);
            }

            public Task DeleteAsync(string id, CancellationToken token = default)
            {
                Store.TryRemove(id, out _);
                return Task.CompletedTask;
            }

            public Task<bool> ExistsAsync(string id, CancellationToken token = default)
                => Task.FromResult(Store.ContainsKey(id));

            public Task<EnumerationResult<IngestionRule>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
                => Task.FromResult(Paginate(Store.Values.Where(r => r.TenantId == tenantId).ToList(), query));
        }

        // --- ChatHistory ---
        public class MockChatHistoryMethods : IChatHistoryMethods
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
                => Task.FromResult(Store.TryGetValue(id, out var h) ? h : null);

            public Task DeleteAsync(string id, CancellationToken token = default)
            {
                Store.TryRemove(id, out _);
                return Task.CompletedTask;
            }

            public Task<EnumerationResult<ChatHistory>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
                => Task.FromResult(Paginate(Store.Values.Where(h => h.TenantId == tenantId).ToList(), query));

            public Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default)
            {
                foreach (var h in Store.Values.Where(h => h.AssistantId == assistantId).ToList())
                    Store.TryRemove(h.Id, out _);
                return Task.CompletedTask;
            }

            public Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default)
            {
                DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
                foreach (var h in Store.Values.Where(h => h.CreatedUtc < cutoff).ToList())
                    Store.TryRemove(h.Id, out _);
                return Task.CompletedTask;
            }
        }

        // --- ChatHistoryPerformanceEvent ---
        public class MockChatHistoryPerformanceEventMethods : IChatHistoryPerformanceEventMethods
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

        // --- RequestHistory ---
        public class MockRequestHistoryMethods : IRequestHistoryMethods
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
                return Task.FromResult(Paginate(items, new EnumerationQuery
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

        // --- CrawlPlan ---
        public class MockCrawlPlanMethods : ICrawlPlanMethods
        {
            public ConcurrentDictionary<string, CrawlPlan> Store { get; } = new();

            public Task<CrawlPlan> CreateAsync(CrawlPlan plan, CancellationToken token = default)
            {
                Store[plan.Id] = plan;
                return Task.FromResult(plan);
            }

            public Task<CrawlPlan> ReadAsync(string id, CancellationToken token = default)
                => Task.FromResult(Store.TryGetValue(id, out var p) ? p : null);

            public Task<CrawlPlan> UpdateAsync(CrawlPlan plan, CancellationToken token = default)
            {
                Store[plan.Id] = plan;
                return Task.FromResult(plan);
            }

            public Task UpdateStateAsync(string id, CrawlPlanStateEnum state, CancellationToken token = default)
            {
                if (Store.TryGetValue(id, out var p))
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
                => Task.FromResult(Paginate(Store.Values.Where(p => p.TenantId == tenantId).ToList(), query));
        }

        // --- CrawlOperation ---
        public class MockCrawlOperationMethods : ICrawlOperationMethods
        {
            public ConcurrentDictionary<string, CrawlOperation> Store { get; } = new();

            public Task<CrawlOperation> CreateAsync(CrawlOperation operation, CancellationToken token = default)
            {
                Store[operation.Id] = operation;
                return Task.FromResult(operation);
            }

            public Task<CrawlOperation> ReadAsync(string id, CancellationToken token = default)
                => Task.FromResult(Store.TryGetValue(id, out var o) ? o : null);

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
                => Task.FromResult(Paginate(Store.Values.Where(o => o.TenantId == tenantId).ToList(), query));

            public Task<EnumerationResult<CrawlOperation>> EnumerateByCrawlPlanAsync(string crawlPlanId, EnumerationQuery query, CancellationToken token = default)
                => Task.FromResult(Paginate(Store.Values.Where(o => o.CrawlPlanId == crawlPlanId).ToList(), query));

            public Task DeleteByCrawlPlanAsync(string crawlPlanId, CancellationToken token = default)
            {
                foreach (var o in Store.Values.Where(o => o.CrawlPlanId == crawlPlanId).ToList())
                    Store.TryRemove(o.Id, out _);
                return Task.CompletedTask;
            }

            public Task DeleteExpiredAsync(string crawlPlanId, int retentionDays, CancellationToken token = default)
            {
                DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
                foreach (var o in Store.Values.Where(o => o.CrawlPlanId == crawlPlanId && o.CreatedUtc < cutoff).ToList())
                    Store.TryRemove(o.Id, out _);
                return Task.CompletedTask;
            }
        }

        // --- Pagination helper ---
        private static EnumerationResult<T> Paginate<T>(List<T> items, EnumerationQuery query)
        {
            int skip = 0;
            if (!string.IsNullOrEmpty(query.ContinuationToken) && int.TryParse(query.ContinuationToken, out int ct))
                skip = ct;

            int total = items.Count;
            var page = items.Skip(skip).Take(query.MaxResults).ToList();
            int nextSkip = skip + page.Count;
            bool endOfResults = nextSkip >= total;

            return new EnumerationResult<T>
            {
                Success = true,
                MaxResults = query.MaxResults,
                TotalRecords = total,
                RecordsRemaining = Math.Max(0, total - nextSkip),
                ContinuationToken = endOfResults ? null : nextSkip.ToString(),
                EndOfResults = endOfResults,
                Objects = page
            };
        }
    }
}
