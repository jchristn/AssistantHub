#pragma warning disable CS8625, CS8603

namespace AssistantHub.Core.Database.Mysql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// MySQL assistant tool-call trace methods.
    /// </summary>
    public class AssistantToolCallMethods : IAssistantToolCallMethods
    {
        private readonly MysqlDatabaseDriver _Driver;

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="driver">MySQL database driver.</param>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public AssistantToolCallMethods(MysqlDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        /// <inheritdoc />
        public async Task<AssistantToolCallRecord> CreateAsync(AssistantToolCallRecord record, CancellationToken token = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (String.IsNullOrEmpty(record.Id)) record.Id = IdGenerator.NewAssistantToolCallRecordId();
            if (record.CreatedUtc == default) record.CreatedUtc = DateTime.UtcNow;

            await _Driver.ExecuteQueryAsync(BuildInsert(record), true, token).ConfigureAwait(false);
            return record;
        }

        /// <inheritdoc />
        public async Task CreateManyAsync(IEnumerable<AssistantToolCallRecord> records, CancellationToken token = default)
        {
            if (records == null) return;
            List<string> queries = records.Where(record => record != null).Select(BuildInsert).ToList();
            if (queries.Count < 1) return;
            await _Driver.ExecuteQueriesAsync(queries, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AssistantToolCallRecord> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM assistant_tool_calls WHERE id = '" + _Driver.Sanitize(id) + "';";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return AssistantToolCallRecord.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<AssistantToolCallRecord>> EnumerateAsync(string tenantId, EnumerationQuery query, string assistantId = null, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            query ??= new EnumerationQuery();

            int skip = 0;
            if (!String.IsNullOrEmpty(query.ContinuationToken) && Int32.TryParse(query.ContinuationToken, out int parsedSkip))
                skip = parsedSkip;

            List<string> where = BuildWhere(tenantId, query, assistantId);
            string whereClause = "WHERE " + String.Join(" AND ", where);
            string order = query.Ordering == EnumerationOrderEnum.CreatedAscending ? "ASC" : "DESC";

            string countQuery = "SELECT COUNT(*) AS count FROM assistant_tool_calls " + whereClause + ";";
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            int totalRecords = countResult != null && countResult.Rows.Count > 0
                ? DataTableHelper.GetIntValue(countResult.Rows[0], "count")
                : 0;

            string listQuery =
                "SELECT * FROM assistant_tool_calls " + whereClause +
                " ORDER BY created_utc " + order + ", id " + order +
                " LIMIT " + query.MaxResults + " OFFSET " + skip + ";";

            DataTable result = await _Driver.ExecuteQueryAsync(listQuery, false, token).ConfigureAwait(false);
            List<AssistantToolCallRecord> records = new List<AssistantToolCallRecord>();
            if (result != null)
            {
                foreach (DataRow row in result.Rows)
                    records.Add(AssistantToolCallRecord.FromDataRow(row));
            }

            int nextSkip = skip + records.Count;
            bool endOfResults = nextSkip >= totalRecords;
            return new EnumerationResult<AssistantToolCallRecord>
            {
                Success = true,
                MaxResults = query.MaxResults,
                TotalRecords = totalRecords,
                RecordsRemaining = Math.Max(0, totalRecords - nextSkip),
                ContinuationToken = endOfResults ? null : nextSkip.ToString(),
                EndOfResults = endOfResults,
                Objects = records
            };
        }

        /// <inheritdoc />
        public async Task<List<AssistantToolCallRecord>> ListByChatHistoryIdAsync(string chatHistoryId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(chatHistoryId)) throw new ArgumentNullException(nameof(chatHistoryId));

            string query = "SELECT * FROM assistant_tool_calls WHERE chat_history_id = '" + _Driver.Sanitize(chatHistoryId) + "' ORDER BY sequence_number ASC, created_utc ASC;";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            List<AssistantToolCallRecord> records = new List<AssistantToolCallRecord>();
            if (result == null) return records;
            foreach (DataRow row in result.Rows) records.Add(AssistantToolCallRecord.FromDataRow(row));
            return records;
        }

        /// <inheritdoc />
        public async Task AttachChatHistoryIdByTraceIdAsync(string traceId, string chatHistoryId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(traceId) || String.IsNullOrEmpty(chatHistoryId)) return;

            string query =
                "UPDATE assistant_tool_calls SET chat_history_id = '" + _Driver.Sanitize(chatHistoryId) + "', " +
                "last_update_utc = '" + _Driver.FormatDateTime(DateTime.UtcNow) + "' " +
                "WHERE trace_id = '" + _Driver.Sanitize(traceId) + "';";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            AssistantToolCallRecord existing = await ReadAsync(id, token).ConfigureAwait(false);
            if (existing == null) return false;

            string query = "DELETE FROM assistant_tool_calls WHERE id = '" + _Driver.Sanitize(id) + "';";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return true;
        }

        /// <inheritdoc />
        public async Task<int> DeleteByFilterAsync(string tenantId, EnumerationQuery query, string assistantId = null, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            query ??= new EnumerationQuery();

            string whereClause = "WHERE " + String.Join(" AND ", BuildWhere(tenantId, query, assistantId));

            string countQuery = "SELECT COUNT(*) AS count FROM assistant_tool_calls " + whereClause + ";";
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            int count = countResult != null && countResult.Rows.Count > 0
                ? DataTableHelper.GetIntValue(countResult.Rows[0], "count")
                : 0;
            if (count < 1) return 0;

            string deleteQuery = "DELETE FROM assistant_tool_calls " + whereClause + ";";
            await _Driver.ExecuteQueryAsync(deleteQuery, true, token).ConfigureAwait(false);
            return count;
        }

        /// <inheritdoc />
        public async Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            string query = "DELETE FROM assistant_tool_calls WHERE created_utc < '" + _Driver.FormatDateTime(cutoff) + "';";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        private List<string> BuildWhere(string tenantId, EnumerationQuery query, string assistantId)
        {
            List<string> where = new List<string> { "tenant_id = '" + _Driver.Sanitize(tenantId) + "'" };
            string effectiveAssistantId = !String.IsNullOrWhiteSpace(assistantId) ? assistantId : query.AssistantIdFilter;
            if (!String.IsNullOrWhiteSpace(effectiveAssistantId))
                where.Add("assistant_id = '" + _Driver.Sanitize(effectiveAssistantId) + "'");
            if (!String.IsNullOrWhiteSpace(query.ThreadIdFilter))
                where.Add("thread_id = '" + _Driver.Sanitize(query.ThreadIdFilter) + "'");
            if (!String.IsNullOrWhiteSpace(query.RequestHistoryIdFilter))
                where.Add("request_history_id = '" + _Driver.Sanitize(query.RequestHistoryIdFilter) + "'");
            if (!String.IsNullOrWhiteSpace(query.ChatHistoryIdFilter))
                where.Add("chat_history_id = '" + _Driver.Sanitize(query.ChatHistoryIdFilter) + "'");
            if (!String.IsNullOrWhiteSpace(query.TraceIdFilter))
                where.Add("trace_id = '" + _Driver.Sanitize(query.TraceIdFilter) + "'");
            if (!String.IsNullOrWhiteSpace(query.ToolNameFilter))
                where.Add("tool_name = '" + _Driver.Sanitize(query.ToolNameFilter) + "'");
            if (query.SuccessFilter.HasValue)
                where.Add("success = " + _Driver.FormatBoolean(query.SuccessFilter.Value));
            if (query.DeniedFilter.HasValue)
                where.Add("denied = " + _Driver.FormatBoolean(query.DeniedFilter.Value));
            if (query.StartUtc.HasValue)
                where.Add("created_utc >= '" + _Driver.FormatDateTime(query.StartUtc.Value) + "'");
            if (query.EndUtc.HasValue)
                where.Add("created_utc <= '" + _Driver.FormatDateTime(query.EndUtc.Value) + "'");
            return where;
        }

        private string BuildInsert(AssistantToolCallRecord record)
        {
            if (String.IsNullOrEmpty(record.Id)) record.Id = IdGenerator.NewAssistantToolCallRecordId();
            if (record.CreatedUtc == default) record.CreatedUtc = DateTime.UtcNow;
            if (record.StartedUtc == default) record.StartedUtc = DateTime.UtcNow;
            if (record.FinishedUtc == default) record.FinishedUtc = record.StartedUtc;
            if (record.LastUpdateUtc == default) record.LastUpdateUtc = record.CreatedUtc;

            return
                "INSERT INTO assistant_tool_calls " +
                "(id, tenant_id, assistant_id, chat_history_id, request_history_id, trace_id, thread_id, origin, turn_index, iteration, sequence_number, provider_tool_call_id, tool_name, arguments_json, output_json, result_summary_json, success, denied, truncated, output_characters, input_bytes, output_bytes, duration_ms, error_type, error_message, provider, model, active, started_utc, finished_utc, created_utc, last_update_utc) VALUES (" +
                _Driver.FormatNullableString(record.Id) + ", " +
                _Driver.FormatNullableString(record.TenantId) + ", " +
                _Driver.FormatNullableString(record.AssistantId) + ", " +
                _Driver.FormatNullableString(record.ChatHistoryId) + ", " +
                _Driver.FormatNullableString(record.RequestHistoryId) + ", " +
                _Driver.FormatNullableString(record.TraceId) + ", " +
                _Driver.FormatNullableString(record.ThreadId) + ", " +
                _Driver.FormatNullableString(record.Origin) + ", " +
                record.TurnIndex + ", " +
                record.Iteration + ", " +
                record.SequenceNumber + ", " +
                _Driver.FormatNullableString(record.ProviderToolCallId) + ", " +
                _Driver.FormatNullableString(record.ToolName) + ", " +
                _Driver.FormatNullableString(record.ArgumentsJson) + ", " +
                _Driver.FormatNullableString(record.OutputJson) + ", " +
                _Driver.FormatNullableString(record.ResultSummaryJson) + ", " +
                _Driver.FormatBoolean(record.Success) + ", " +
                _Driver.FormatBoolean(record.Denied) + ", " +
                _Driver.FormatBoolean(record.Truncated) + ", " +
                record.OutputCharacters + ", " +
                record.InputBytes + ", " +
                record.OutputBytes + ", " +
                _Driver.FormatDouble(record.DurationMs) + ", " +
                _Driver.FormatNullableString(record.ErrorType) + ", " +
                _Driver.FormatNullableString(record.ErrorMessage) + ", " +
                _Driver.FormatNullableString(record.Provider) + ", " +
                _Driver.FormatNullableString(record.Model) + ", " +
                _Driver.FormatBoolean(record.Active) + ", " +
                _Driver.FormatNullableDateTime(record.StartedUtc) + ", " +
                _Driver.FormatNullableDateTime(record.FinishedUtc) + ", " +
                _Driver.FormatNullableDateTime(record.CreatedUtc) + ", " +
                _Driver.FormatNullableDateTime(record.LastUpdateUtc) +
                ");";
        }
    }
}

#pragma warning restore CS8625, CS8603
