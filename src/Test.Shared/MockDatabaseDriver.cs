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

        // --- User ---

        // --- Credential ---

        // --- Assistant ---

        // --- AssistantSettings ---

        // --- AssistantDocument ---

        // --- AssistantFeedback ---

        // --- IngestionRule ---

        // --- ChatHistory ---

        // --- ChatHistoryPerformanceEvent ---

        // --- RequestHistory ---

        // --- CrawlPlan ---

        // --- CrawlOperation ---

        // --- Pagination helper ---
        internal static EnumerationResult<T> Paginate<T>(List<T> items, EnumerationQuery query)
        {
            int skip = 0;
            if (!string.IsNullOrEmpty(query.ContinuationToken) && int.TryParse(query.ContinuationToken, out int ct))
                skip = ct;

            int total = items.Count;
            List<T> page = items.Skip(skip).Take(query.MaxResults).ToList();
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
