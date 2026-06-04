namespace AssistantHub.Core.Database.Mysql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// MySQL request-history methods implementation.
    /// </summary>
    public class RequestHistoryMethods : IRequestHistoryMethods
    {
        #region Private-Members

        private readonly MysqlDatabaseDriver _Driver;

        private const string _SummaryColumns =
            "id, trace_id, chat_history_id, tenant_id, user_id, credential_id, assistant_id, thread_id, principal_name, " +
            "request_type, source_type, http_method, route_template, request_path, request_url, source_ip, " +
            "status_code, success, duration_ms, request_content_type, response_content_type, " +
            "request_size_bytes, response_size_bytes, request_body_truncated, response_body_truncated, " +
            "request_body_is_binary, response_body_is_binary, created_utc, last_update_utc";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistoryMethods(MysqlDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<RequestHistoryEntry> CreateAsync(RequestHistoryEntry entry, CancellationToken token = default)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            if (String.IsNullOrEmpty(entry.Id)) entry.Id = IdGenerator.NewRequestHistoryId();
            entry.CreatedUtc = DateTime.UtcNow;
            entry.LastUpdateUtc = entry.CreatedUtc;

            string query =
                "INSERT INTO request_history " +
                "(id, trace_id, chat_history_id, tenant_id, user_id, credential_id, assistant_id, thread_id, principal_name, " +
                "request_type, source_type, http_method, route_template, request_path, request_url, source_ip, " +
                "status_code, success, duration_ms, request_content_type, response_content_type, " +
                "request_size_bytes, response_size_bytes, request_body_truncated, response_body_truncated, " +
                "request_body_is_binary, response_body_is_binary, route_parameters_json, query_parameters_json, " +
                "request_headers_json, response_headers_json, request_body, response_body, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(entry.Id) + "', " +
                _Driver.FormatNullableString(entry.TraceId) + ", " +
                _Driver.FormatNullableString(entry.ChatHistoryId) + ", " +
                _Driver.FormatNullableString(entry.TenantId) + ", " +
                _Driver.FormatNullableString(entry.UserId) + ", " +
                _Driver.FormatNullableString(entry.CredentialId) + ", " +
                _Driver.FormatNullableString(entry.AssistantId) + ", " +
                _Driver.FormatNullableString(entry.ThreadId) + ", " +
                _Driver.FormatNullableString(entry.PrincipalName) + ", " +
                _Driver.FormatNullableString(entry.RequestType) + ", " +
                _Driver.FormatNullableString(entry.SourceType) + ", " +
                _Driver.FormatNullableString(entry.HttpMethod) + ", " +
                _Driver.FormatNullableString(entry.RouteTemplate) + ", " +
                _Driver.FormatNullableString(entry.RequestPath) + ", " +
                _Driver.FormatNullableString(entry.RequestUrl) + ", " +
                _Driver.FormatNullableString(entry.SourceIp) + ", " +
                entry.StatusCode + ", " +
                _Driver.FormatBoolean(entry.Success) + ", " +
                _Driver.FormatDouble(entry.DurationMs) + ", " +
                _Driver.FormatNullableString(entry.RequestContentType) + ", " +
                _Driver.FormatNullableString(entry.ResponseContentType) + ", " +
                entry.RequestSizeBytes + ", " +
                entry.ResponseSizeBytes + ", " +
                _Driver.FormatBoolean(entry.RequestBodyTruncated) + ", " +
                _Driver.FormatBoolean(entry.ResponseBodyTruncated) + ", " +
                _Driver.FormatBoolean(entry.RequestBodyIsBinary) + ", " +
                _Driver.FormatBoolean(entry.ResponseBodyIsBinary) + ", " +
                _Driver.FormatNullableString(SerializeDictionary(entry.RouteParameters)) + ", " +
                _Driver.FormatNullableString(SerializeDictionary(entry.QueryParameters)) + ", " +
                _Driver.FormatNullableString(SerializeDictionary(entry.RequestHeaders)) + ", " +
                _Driver.FormatNullableString(SerializeDictionary(entry.ResponseHeaders)) + ", " +
                _Driver.FormatNullableString(entry.RequestBody) + ", " +
                _Driver.FormatNullableString(entry.ResponseBody) + ", " +
                "'" + _Driver.FormatDateTime(entry.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(entry.LastUpdateUtc) + "'" +
                ");";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return entry;
        }

        /// <inheritdoc />
        public async Task<RequestHistoryEntry> ReadAsync(string id, bool includeDetails = true, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string columns = includeDetails ? "*" : _SummaryColumns;
            string query =
                "SELECT " + columns + " FROM request_history WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return RequestHistoryEntry.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<RequestHistoryEntry>> EnumerateAsync(RequestHistorySearchFilter filter, CancellationToken token = default)
        {
            filter ??= new RequestHistorySearchFilter();

            EnumerationResult<RequestHistoryEntry> ret = new EnumerationResult<RequestHistoryEntry>
            {
                MaxResults = filter.MaxResults
            };

            int skip = ParseOffset(filter.ContinuationToken);
            string whereClause = BuildWhereClause(filter);
            string orderBy = filter.Ordering == EnumerationOrderEnum.CreatedAscending
                ? "ORDER BY created_utc ASC"
                : "ORDER BY created_utc DESC";

            string selectQuery =
                "SELECT " + _SummaryColumns + " FROM request_history " +
                whereClause + " " +
                orderBy + " " +
                "LIMIT " + filter.MaxResults + " OFFSET " + skip + ";";

            string countQuery =
                "SELECT COUNT(*) AS cnt FROM request_history " + whereClause + ";";

            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            if (countResult != null && countResult.Rows.Count > 0)
                ret.TotalRecords = DataTableHelper.GetLongValue(countResult.Rows[0], "cnt");

            DataTable selectResult = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);
            if (selectResult != null)
            {
                foreach (DataRow row in selectResult.Rows)
                {
                    ret.Objects.Add(RequestHistoryEntry.FromDataRow(row));
                }
            }

            long nextOffset = skip + ret.Objects.Count;
            ret.RecordsRemaining = Math.Max(0, ret.TotalRecords - nextOffset);
            ret.EndOfResults = nextOffset >= ret.TotalRecords;
            ret.ContinuationToken = ret.EndOfResults ? null : nextOffset.ToString();
            return ret;
        }

        /// <inheritdoc />
        public async Task<RequestHistorySummaryResult> SummarizeAsync(RequestHistorySearchFilter filter, CancellationToken token = default)
        {
            filter ??= new RequestHistorySearchFilter();
            filter.StartUtc ??= DateTime.UtcNow.AddHours(-24);
            filter.EndUtc ??= DateTime.UtcNow;
            if (filter.EndUtc <= filter.StartUtc)
                filter.EndUtc = filter.StartUtc.Value.AddSeconds(filter.BucketSeconds);

            RequestHistorySummaryResult ret = InitializeSummary(filter.StartUtc.Value, filter.EndUtc.Value, filter.BucketSeconds);
            double[] durationSums = new double[ret.Buckets.Count];

            string query =
                "SELECT created_utc, success, duration_ms FROM request_history " +
                BuildWhereClause(filter) + " " +
                "ORDER BY created_utc ASC;";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null) return ret;

            foreach (DataRow row in result.Rows)
            {
                DateTime createdUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
                bool success = DataTableHelper.GetBooleanValue(row, "success");
                double durationMs = DataTableHelper.GetDoubleValue(row, "duration_ms");

                ret.TotalCount++;
                if (success) ret.TotalSuccess++;
                else ret.TotalFailure++;
                ret.AverageDurationMs += durationMs;

                int index = (int)Math.Floor((createdUtc - filter.StartUtc.Value).TotalSeconds / filter.BucketSeconds);
                if (index < 0 || index >= ret.Buckets.Count) continue;

                RequestHistorySummaryBucket bucket = ret.Buckets[index];
                bucket.RequestCount++;
                if (success) bucket.SuccessCount++;
                else bucket.FailureCount++;
                durationSums[index] += durationMs;
            }

            if (ret.TotalCount > 0)
                ret.AverageDurationMs = Math.Round(ret.AverageDurationMs / ret.TotalCount, 2);

            for (int i = 0; i < ret.Buckets.Count; i++)
            {
                if (ret.Buckets[i].RequestCount > 0)
                    ret.Buckets[i].AverageDurationMs = Math.Round(durationSums[i] / ret.Buckets[i].RequestCount, 2);
            }

            return ret;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (await ReadAsync(id, false, token).ConfigureAwait(false) == null) return false;

            string query = "DELETE FROM request_history WHERE id = '" + _Driver.Sanitize(id) + "';";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return true;
        }

        /// <inheritdoc />
        public async Task<int> DeleteByFilterAsync(RequestHistorySearchFilter filter, CancellationToken token = default)
        {
            filter ??= new RequestHistorySearchFilter();
            string whereClause = BuildWhereClause(filter);

            string countQuery = "SELECT COUNT(*) AS cnt FROM request_history " + whereClause + ";";
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            int count = countResult != null && countResult.Rows.Count > 0
                ? DataTableHelper.GetIntValue(countResult.Rows[0], "cnt")
                : 0;

            if (count < 1) return 0;

            string deleteQuery = "DELETE FROM request_history " + whereClause + ";";
            await _Driver.ExecuteQueryAsync(deleteQuery, true, token).ConfigureAwait(false);
            return count;
        }

        /// <inheritdoc />
        public async Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            string query =
                "DELETE FROM request_history WHERE created_utc < '" + _Driver.FormatDateTime(cutoff) + "';";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private string BuildWhereClause(RequestHistorySearchFilter filter)
        {
            List<string> conditions = new List<string>();

            if (!String.IsNullOrEmpty(filter.RequestType))
                conditions.Add("request_type = '" + _Driver.Sanitize(filter.RequestType) + "'");
            if (!String.IsNullOrEmpty(filter.HttpMethod))
                conditions.Add("http_method = '" + _Driver.Sanitize(filter.HttpMethod.ToUpperInvariant()) + "'");
            if (!String.IsNullOrEmpty(filter.PathContains))
                conditions.Add("request_path LIKE '%" + _Driver.Sanitize(filter.PathContains) + "%'");
            if (filter.StatusCode.HasValue)
                conditions.Add("status_code = " + filter.StatusCode.Value);
            if (filter.Success.HasValue)
                conditions.Add("success = " + _Driver.FormatBoolean(filter.Success.Value));
            if (!String.IsNullOrEmpty(filter.TenantId))
                conditions.Add("tenant_id = '" + _Driver.Sanitize(filter.TenantId) + "'");
            if (!String.IsNullOrEmpty(filter.UserId))
                conditions.Add("user_id = '" + _Driver.Sanitize(filter.UserId) + "'");
            if (!String.IsNullOrEmpty(filter.CredentialId))
                conditions.Add("credential_id = '" + _Driver.Sanitize(filter.CredentialId) + "'");
            if (!String.IsNullOrEmpty(filter.AssistantId))
                conditions.Add("assistant_id = '" + _Driver.Sanitize(filter.AssistantId) + "'");
            if (!String.IsNullOrEmpty(filter.ThreadId))
                conditions.Add("thread_id = '" + _Driver.Sanitize(filter.ThreadId) + "'");
            if (!String.IsNullOrEmpty(filter.SourceType))
                conditions.Add("source_type = '" + _Driver.Sanitize(filter.SourceType) + "'");
            if (filter.StartUtc.HasValue)
                conditions.Add("created_utc >= '" + _Driver.FormatDateTime(filter.StartUtc.Value) + "'");
            if (filter.EndUtc.HasValue)
                conditions.Add("created_utc <= '" + _Driver.FormatDateTime(filter.EndUtc.Value) + "'");
            if (!String.IsNullOrEmpty(filter.SearchText))
            {
                string search = _Driver.Sanitize(filter.SearchText);
                conditions.Add(
                    "(request_path LIKE '%" + search + "%' " +
                    "OR request_url LIKE '%" + search + "%' " +
                    "OR principal_name LIKE '%" + search + "%' " +
                    "OR request_body LIKE '%" + search + "%' " +
                    "OR response_body LIKE '%" + search + "%')");
            }

            if (conditions.Count < 1) return String.Empty;
            return "WHERE " + String.Join(" AND ", conditions);
        }

        private int ParseOffset(string continuationToken)
        {
            if (String.IsNullOrEmpty(continuationToken)) return 0;
            return Int32.TryParse(continuationToken, out int skip) && skip > 0 ? skip : 0;
        }

        private RequestHistorySummaryResult InitializeSummary(DateTime startUtc, DateTime endUtc, int bucketSeconds)
        {
            RequestHistorySummaryResult ret = new RequestHistorySummaryResult();
            DateTime cursor = startUtc;
            while (cursor < endUtc)
            {
                DateTime next = cursor.AddSeconds(bucketSeconds);
                ret.Buckets.Add(new RequestHistorySummaryBucket
                {
                    BucketStartUtc = cursor,
                    BucketEndUtc = next
                });
                cursor = next;
            }

            if (ret.Buckets.Count < 1)
            {
                ret.Buckets.Add(new RequestHistorySummaryBucket
                {
                    BucketStartUtc = startUtc,
                    BucketEndUtc = endUtc
                });
            }

            return ret;
        }

        private string SerializeDictionary(Dictionary<string, string> values)
        {
            if (values == null || values.Count < 1) return null;
            return JsonSerializer.Serialize(values);
        }

        #endregion
    }
}
