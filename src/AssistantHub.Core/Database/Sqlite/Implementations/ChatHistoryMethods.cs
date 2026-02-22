#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Database.Sqlite.Implementations
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
    /// SQLite chat history methods implementation.
    /// </summary>
    public class ChatHistoryMethods : IChatHistoryMethods
    {
        #region Private-Members

        private readonly string _Header = "[ChatHistoryMethods] ";
        private readonly SqliteDatabaseDriver _Driver;
        private readonly DatabaseSettings _Settings;
        private readonly LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="driver">SQLite database driver.</param>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public ChatHistoryMethods(SqliteDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
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

            if (String.IsNullOrEmpty(history.Id)) history.Id = IdGenerator.NewChatHistoryId();
            history.CreatedUtc = DateTime.UtcNow;
            history.LastUpdateUtc = history.CreatedUtc;

            string query =
                "INSERT INTO chat_history " +
                "(id, thread_id, assistant_id, collection_id, user_message_utc, user_message, " +
                "retrieval_start_utc, retrieval_duration_ms, retrieval_context, " +
                "prompt_sent_utc, prompt_tokens, " +
                "endpoint_resolution_duration_ms, compaction_duration_ms, inference_connection_duration_ms, " +
                "time_to_first_token_ms, time_to_last_token_ms, " +
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
                history.PromptTokens + ", " +
                _Driver.FormatDouble(history.EndpointResolutionDurationMs) + ", " +
                _Driver.FormatDouble(history.CompactionDurationMs) + ", " +
                _Driver.FormatDouble(history.InferenceConnectionDurationMs) + ", " +
                _Driver.FormatDouble(history.TimeToFirstTokenMs) + ", " +
                _Driver.FormatDouble(history.TimeToLastTokenMs) + ", " +
                _Driver.FormatNullableString(history.AssistantResponse) + ", " +
                "'" + _Driver.FormatDateTime(history.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(history.LastUpdateUtc) + "'" +
                ");";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return history;
        }

        /// <inheritdoc />
        public async Task<ChatHistory> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT * FROM chat_history WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return ChatHistory.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "DELETE FROM chat_history WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<ChatHistory>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            EnumerationResult<ChatHistory> ret = new EnumerationResult<ChatHistory>();
            ret.MaxResults = query.MaxResults;

            int skip = 0;
            if (!String.IsNullOrEmpty(query.ContinuationToken))
                Int32.TryParse(query.ContinuationToken, out skip);

            string orderBy;
            switch (query.Ordering)
            {
                case EnumerationOrderEnum.CreatedAscending:
                    orderBy = "ORDER BY created_utc ASC";
                    break;
                case EnumerationOrderEnum.CreatedDescending:
                default:
                    orderBy = "ORDER BY created_utc DESC";
                    break;
            }

            List<string> conditions = new List<string>();
            if (!String.IsNullOrEmpty(query.AssistantIdFilter))
                conditions.Add("assistant_id = '" + _Driver.Sanitize(query.AssistantIdFilter) + "'");
            if (!String.IsNullOrEmpty(query.ThreadIdFilter))
                conditions.Add("thread_id = '" + _Driver.Sanitize(query.ThreadIdFilter) + "'");

            string whereClause = conditions.Count > 0 ? "WHERE " + String.Join(" AND ", conditions) + " " : "";

            string selectQuery =
                "SELECT * FROM chat_history " +
                whereClause +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + skip + ";";

            string countQuery =
                "SELECT COUNT(*) AS cnt FROM chat_history " + whereClause + ";";

            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            if (countResult != null && countResult.Rows.Count > 0)
                ret.TotalRecords = DataTableHelper.GetLongValue(countResult.Rows[0], "cnt");

            DataTable selectResult = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);
            if (selectResult != null && selectResult.Rows.Count > 0)
            {
                foreach (DataRow row in selectResult.Rows)
                {
                    ret.Objects.Add(ChatHistory.FromDataRow(row));
                }
            }

            long nextOffset = skip + ret.Objects.Count;
            ret.RecordsRemaining = ret.TotalRecords - nextOffset;
            if (ret.RecordsRemaining < 0) ret.RecordsRemaining = 0;
            ret.EndOfResults = (nextOffset >= ret.TotalRecords);
            ret.ContinuationToken = ret.EndOfResults ? null : nextOffset.ToString();

            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(assistantId)) throw new ArgumentNullException(nameof(assistantId));

            string query =
                "DELETE FROM chat_history WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteExpiredAsync(int retentionDays, CancellationToken token = default)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            string query =
                "DELETE FROM chat_history WHERE created_utc < '" + _Driver.FormatDateTime(cutoff) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
