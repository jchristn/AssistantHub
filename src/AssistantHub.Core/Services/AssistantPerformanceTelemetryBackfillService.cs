namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using SyslogLogging;

    /// <summary>
    /// Backfills normalized assistant performance event rows from chat history telemetry JSON.
    /// </summary>
    public class AssistantPerformanceTelemetryBackfillService
    {
        private readonly string _Header = "[AssistantPerformanceTelemetryBackfillService] ";
        private readonly DatabaseDriverBase _Database;
        private readonly LoggingModule _Logging;

        /// <summary>
        /// Instantiate the assistant performance telemetry backfill service.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        public AssistantPerformanceTelemetryBackfillService(DatabaseDriverBase database, LoggingModule logging)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Logging = logging;
        }

        /// <summary>
        /// Backfill missing normalized performance event rows for all chat history records.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of inserted event rows.</returns>
        public async Task<int> BackfillMissingEventsAsync(CancellationToken token = default)
        {
            if (_Database.Tenant == null || _Database.ChatHistory == null || _Database.ChatHistoryPerformanceEvent == null)
                return 0;

            int inserted = 0;
            string continuationToken = null;

            do
            {
                EnumerationResult<TenantMetadata> tenants = await _Database.Tenant.EnumerateAsync(new EnumerationQuery
                {
                    MaxResults = 1000,
                    ContinuationToken = continuationToken
                }, token).ConfigureAwait(false);

                foreach (TenantMetadata tenant in tenants.Objects)
                {
                    inserted += await BackfillTenantAsync(tenant.Id, token).ConfigureAwait(false);
                }

                continuationToken = tenants.ContinuationToken;
                if (tenants.EndOfResults) break;
            }
            while (!String.IsNullOrEmpty(continuationToken));

            if (inserted > 0)
                _Logging?.Info(_Header + "backfilled " + inserted + " assistant performance event row(s)");

            return inserted;
        }

        private async Task<int> BackfillTenantAsync(string tenantId, CancellationToken token)
        {
            int inserted = 0;
            string continuationToken = null;

            do
            {
                EnumerationResult<ChatHistory> histories = await _Database.ChatHistory.EnumerateAsync(tenantId, new EnumerationQuery
                {
                    MaxResults = 1000,
                    ContinuationToken = continuationToken
                }, token).ConfigureAwait(false);

                foreach (ChatHistory history in histories.Objects)
                {
                    inserted += await BackfillChatHistoryAsync(history, token).ConfigureAwait(false);
                }

                continuationToken = histories.ContinuationToken;
                if (histories.EndOfResults) break;
            }
            while (!String.IsNullOrEmpty(continuationToken));

            return inserted;
        }

        private async Task<int> BackfillChatHistoryAsync(ChatHistory history, CancellationToken token)
        {
            if (history == null || String.IsNullOrWhiteSpace(history.PerformanceJson)) return 0;

            List<ChatHistoryPerformanceEvent> existing = await _Database.ChatHistoryPerformanceEvent.ListByChatHistoryIdAsync(history.Id, token).ConfigureAwait(false);
            if (existing.Count > 0) return 0;

            AssistantPerformanceTelemetry telemetry;
            try
            {
                telemetry = AssistantPerformanceTelemetryBuilder.Deserialize(history.PerformanceJson);
            }
            catch (Exception e)
            {
                _Logging?.Warn(_Header + "failed to deserialize performance telemetry for chat history " + history.Id + ": " + e.Message);
                return 0;
            }

            if (telemetry == null || telemetry.Stages == null || telemetry.Stages.Count < 1) return 0;

            HydrateTelemetryIdentifiers(telemetry, history);

            List<ChatHistoryPerformanceEvent> events = AssistantPerformanceTelemetryBuilder.ToEvents(telemetry, history.TenantId);
            if (events.Count < 1) return 0;

            try
            {
                await _Database.ChatHistoryPerformanceEvent.CreateManyAsync(events, token).ConfigureAwait(false);
                return events.Count;
            }
            catch (Exception e)
            {
                _Logging?.Warn(_Header + "failed to backfill " + events.Count + " performance event row(s) for chat history " + history.Id + ": " + e.Message);
                return 0;
            }
        }

        private static void HydrateTelemetryIdentifiers(AssistantPerformanceTelemetry telemetry, ChatHistory history)
        {
            if (String.IsNullOrEmpty(telemetry.ChatHistoryId)) telemetry.ChatHistoryId = history.Id;
            if (String.IsNullOrEmpty(telemetry.RequestHistoryId)) telemetry.RequestHistoryId = history.RequestHistoryId;
            if (String.IsNullOrEmpty(telemetry.TraceId)) telemetry.TraceId = history.TraceId;
            if (String.IsNullOrEmpty(telemetry.AssistantId)) telemetry.AssistantId = history.AssistantId;
            if (telemetry.CreatedUtc == default) telemetry.CreatedUtc = history.CreatedUtc;
        }
    }
}
