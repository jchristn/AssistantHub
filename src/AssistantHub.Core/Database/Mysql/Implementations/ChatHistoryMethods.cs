namespace AssistantHub.Core.Database.Mysql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// MySQL chat history methods implementation.
    /// </summary>
    public class ChatHistoryMethods : IChatHistoryMethods
    {
        #region Private-Members

        private MysqlDatabaseDriver _Driver;
        private DatabaseSettings _Settings;
        private LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="driver">MySQL database driver.</param>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public ChatHistoryMethods(MysqlDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ChatHistory> CreateAsync(ChatHistory history, CancellationToken token = default)
        {
            if (history == null) throw new ArgumentNullException(nameof(history));

            history.CreatedUtc = DateTime.UtcNow;
            history.LastUpdateUtc = history.CreatedUtc;

            string query =
                "INSERT INTO chat_history " +
                "(id, thread_id, assistant_id, collection_id, user_message_utc, user_message, " +
                "retrieval_start_utc, retrieval_duration_ms, retrieval_context, " +
                "prompt_sent_utc, time_to_first_token_ms, time_to_last_token_ms, " +
                "assistant_response, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(history.Id) + "', " +
                "'" + _Driver.Sanitize(history.ThreadId) + "', " +
                "'" + _Driver.Sanitize(history.AssistantId) + "', " +
                _Driver.FormatNullableString(history.CollectionId) + ", " +
                "'" + _Driver.FormatDateTime(history.UserMessageUtc) + "', " +
                _Driver.FormatNullableString(history.UserMessage) + ", " +
                _Driver.FormatNullableDateTime(history.RetrievalStartUtc) + ", " +
                _Driver.FormatDouble(history.RetrievalDurationMs) + ", " +
                _Driver.FormatNullableString(history.RetrievalContext) + ", " +
                _Driver.FormatNullableDateTime(history.PromptSentUtc) + ", " +
                _Driver.FormatDouble(history.TimeToFirstTokenMs) + ", " +
                _Driver.FormatDouble(history.TimeToLastTokenMs) + ", " +
                _Driver.FormatNullableString(history.AssistantResponse) + ", " +
                "'" + _Driver.FormatDateTime(history.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(history.LastUpdateUtc) + "'" +
                ")";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return history;
        }

        /// <inheritdoc />
        public async Task<ChatHistory> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM chat_history WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return ChatHistory.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM chat_history WHERE id = '" + _Driver.Sanitize(id) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<ChatHistory>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            Stopwatch sw = Stopwatch.StartNew();

            int offset = 0;
            if (!String.IsNullOrEmpty(query.ContinuationToken))
            {
                if (!Int32.TryParse(query.ContinuationToken, out offset)) offset = 0;
            }

            string orderBy = query.Ordering == EnumerationOrderEnum.CreatedDescending
                ? "ORDER BY created_utc DESC"
                : "ORDER BY created_utc ASC";

            List<string> conditions = new List<string>();
            if (!String.IsNullOrEmpty(query.AssistantIdFilter))
                conditions.Add("assistant_id = '" + _Driver.Sanitize(query.AssistantIdFilter) + "'");
            if (!String.IsNullOrEmpty(query.ThreadIdFilter))
                conditions.Add("thread_id = '" + _Driver.Sanitize(query.ThreadIdFilter) + "'");

            string whereClause = conditions.Count > 0 ? "WHERE " + String.Join(" AND ", conditions) + " " : "";

            string countQuery = "SELECT COUNT(*) AS cnt FROM chat_history " + whereClause;
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            long totalRecords = 0;
            if (countResult != null && countResult.Rows.Count > 0)
                totalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string selectQuery =
                "SELECT * FROM chat_history " +
                whereClause +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + offset;

            DataTable result = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);

            List<ChatHistory> objects = new List<ChatHistory>();
            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    ChatHistory obj = ChatHistory.FromDataRow(row);
                    if (obj != null) objects.Add(obj);
                }
            }

            long nextOffset = offset + objects.Count;
            long remaining = totalRecords - nextOffset;
            if (remaining < 0) remaining = 0;

            sw.Stop();

            EnumerationResult<ChatHistory> enumerationResult = new EnumerationResult<ChatHistory>();
            enumerationResult.MaxResults = query.MaxResults;
            enumerationResult.TotalRecords = totalRecords;
            enumerationResult.RecordsRemaining = remaining;
            enumerationResult.Objects = objects;
            enumerationResult.ContinuationToken = remaining > 0 ? nextOffset.ToString() : null;
            enumerationResult.EndOfResults = remaining <= 0;
            enumerationResult.TotalMs = sw.Elapsed.TotalMilliseconds;
            return enumerationResult;
        }

        /// <inheritdoc />
        public async Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(assistantId)) throw new ArgumentNullException(nameof(assistantId));

            string query = "DELETE FROM chat_history WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            string query = "DELETE FROM chat_history WHERE created_utc < '" + _Driver.FormatDateTime(cutoff) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion
    }
}
