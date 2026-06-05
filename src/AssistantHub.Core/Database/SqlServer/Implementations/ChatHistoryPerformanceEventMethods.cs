#pragma warning disable CS8625, CS8603

namespace AssistantHub.Core.Database.SqlServer.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// SQL Server chat history performance event methods.
    /// </summary>
    public class ChatHistoryPerformanceEventMethods : IChatHistoryPerformanceEventMethods
    {
        private readonly SqlServerDatabaseDriver _Driver;

        /// <summary>
        /// Instantiate the SQL Server performance-event data access layer.
        /// </summary>
        /// <param name="driver">SQL Server database driver.</param>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public ChatHistoryPerformanceEventMethods(SqlServerDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        /// <inheritdoc />
        public async Task<ChatHistoryPerformanceEvent> CreateAsync(ChatHistoryPerformanceEvent evt, CancellationToken token = default)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));
            if (String.IsNullOrEmpty(evt.Id)) evt.Id = IdGenerator.NewChatHistoryPerformanceEventId();
            evt.CreatedUtc = DateTime.UtcNow;
            await _Driver.ExecuteQueryAsync(BuildInsert(evt), true, token).ConfigureAwait(false);
            return evt;
        }

        /// <inheritdoc />
        public async Task CreateManyAsync(IEnumerable<ChatHistoryPerformanceEvent> events, CancellationToken token = default)
        {
            if (events == null) return;
            List<string> queries = events.Where(e => e != null).Select(BuildInsert).ToList();
            if (queries.Count < 1) return;
            await _Driver.ExecuteQueriesAsync(queries, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<ChatHistoryPerformanceEvent>> ListByChatHistoryIdAsync(string chatHistoryId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(chatHistoryId)) throw new ArgumentNullException(nameof(chatHistoryId));
            DataTable result = await _Driver.ExecuteQueryAsync("SELECT * FROM chat_history_performance_events WHERE chat_history_id = '" + _Driver.Sanitize(chatHistoryId) + "' ORDER BY sequence_number ASC, created_utc ASC;", false, token).ConfigureAwait(false);
            List<ChatHistoryPerformanceEvent> ret = new List<ChatHistoryPerformanceEvent>();
            if (result == null) return ret;
            foreach (DataRow row in result.Rows) ret.Add(ChatHistoryPerformanceEvent.FromDataRow(row));
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteByChatHistoryIdAsync(string chatHistoryId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(chatHistoryId)) throw new ArgumentNullException(nameof(chatHistoryId));
            await _Driver.ExecuteQueryAsync("DELETE FROM chat_history_performance_events WHERE chat_history_id = '" + _Driver.Sanitize(chatHistoryId) + "';", true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            await _Driver.ExecuteQueryAsync("DELETE FROM chat_history_performance_events WHERE created_utc < '" + _Driver.FormatDateTime(cutoff) + "';", true, token).ConfigureAwait(false);
        }

        private string BuildInsert(ChatHistoryPerformanceEvent evt)
        {
            if (String.IsNullOrEmpty(evt.Id)) evt.Id = IdGenerator.NewChatHistoryPerformanceEventId();
            if (evt.CreatedUtc == default) evt.CreatedUtc = DateTime.UtcNow;
            return "INSERT INTO chat_history_performance_events (id, tenant_id, assistant_id, chat_history_id, request_history_id, trace_id, sequence_number, stage, phase, kind, endpoint_id, endpoint_name, endpoint_type, provider, api_format, model, started_utc, finished_utc, duration_ms, success, http_status_code, error_type, error_message, input_tokens, output_tokens, total_tokens, chunks_input, chunks_output, retrieval_query_count, endpoint_limiter_wait_ms, request_to_headers_ms, headers_to_first_token_ms, first_token_to_last_token_ms, client_total_ms, provider_queue_ms, provider_load_ms, provider_prompt_eval_ms, provider_generation_ms, provider_total_ms, provider_tokens_per_second, provider_request_id, metadata_json, provider_metrics_json, provider_raw_json, created_utc) VALUES (" +
                _Driver.FormatNullableString(evt.Id) + ", " + _Driver.FormatNullableString(evt.TenantId) + ", " + _Driver.FormatNullableString(evt.AssistantId) + ", " + _Driver.FormatNullableString(evt.ChatHistoryId) + ", " + _Driver.FormatNullableString(evt.RequestHistoryId) + ", " + _Driver.FormatNullableString(evt.TraceId) + ", " + evt.SequenceNumber + ", " + _Driver.FormatNullableString(evt.Stage) + ", " + _Driver.FormatNullableString(evt.Phase) + ", " + _Driver.FormatNullableString(evt.Kind) + ", " + _Driver.FormatNullableString(evt.EndpointId) + ", " + _Driver.FormatNullableString(evt.EndpointName) + ", " + _Driver.FormatNullableString(evt.EndpointType) + ", " + _Driver.FormatNullableString(evt.Provider) + ", " + _Driver.FormatNullableString(evt.ApiFormat) + ", " + _Driver.FormatNullableString(evt.Model) + ", " + _Driver.FormatNullableDateTime(evt.StartedUtc) + ", " + _Driver.FormatNullableDateTime(evt.FinishedUtc) + ", " + _Driver.FormatDouble(evt.DurationMs) + ", " + _Driver.FormatBoolean(evt.Success) + ", " + FormatNullableInt(evt.HttpStatusCode) + ", " + _Driver.FormatNullableString(evt.ErrorType) + ", " + _Driver.FormatNullableString(evt.ErrorMessage) + ", " + FormatNullableInt(evt.InputTokens) + ", " + FormatNullableInt(evt.OutputTokens) + ", " + FormatNullableInt(evt.TotalTokens) + ", " + FormatNullableInt(evt.ChunksInput) + ", " + FormatNullableInt(evt.ChunksOutput) + ", " + FormatNullableInt(evt.RetrievalQueryCount) + ", " + FormatNullableDouble(evt.EndpointLimiterWaitMs) + ", " + FormatNullableDouble(evt.RequestToHeadersMs) + ", " + FormatNullableDouble(evt.HeadersToFirstTokenMs) + ", " + FormatNullableDouble(evt.FirstTokenToLastTokenMs) + ", " + FormatNullableDouble(evt.ClientTotalMs) + ", " + FormatNullableDouble(evt.ProviderQueueMs) + ", " + FormatNullableDouble(evt.ProviderLoadMs) + ", " + FormatNullableDouble(evt.ProviderPromptEvalMs) + ", " + FormatNullableDouble(evt.ProviderGenerationMs) + ", " + FormatNullableDouble(evt.ProviderTotalMs) + ", " + FormatNullableDouble(evt.ProviderTokensPerSecond) + ", " + _Driver.FormatNullableString(evt.ProviderRequestId) + ", " + _Driver.FormatNullableString(evt.MetadataJson) + ", " + _Driver.FormatNullableString(evt.ProviderMetricsJson) + ", " + _Driver.FormatNullableString(evt.ProviderRawJson) + ", " + _Driver.FormatNullableDateTime(evt.CreatedUtc) + ");";
        }

        private static string FormatNullableInt(int? value) => value.HasValue ? value.Value.ToString() : "NULL";
        private string FormatNullableDouble(double? value) => value.HasValue ? _Driver.FormatDouble(value.Value) : "NULL";
    }
}

#pragma warning restore CS8625, CS8603
