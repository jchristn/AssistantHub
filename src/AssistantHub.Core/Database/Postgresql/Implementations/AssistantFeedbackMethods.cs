namespace AssistantHub.Core.Database.Postgresql.Implementations
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
    /// PostgreSQL assistant feedback methods implementation.
    /// </summary>
    public class AssistantFeedbackMethods : IAssistantFeedbackMethods
    {
        #region Private-Members

        private PostgresqlDatabaseDriver _Driver;
        private DatabaseSettings _Settings;
        private LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="driver">PostgreSQL database driver.</param>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public AssistantFeedbackMethods(PostgresqlDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<AssistantFeedback> CreateAsync(AssistantFeedback feedback, CancellationToken token = default)
        {
            if (feedback == null) throw new ArgumentNullException(nameof(feedback));

            feedback.CreatedUtc = DateTime.UtcNow;
            feedback.LastUpdateUtc = feedback.CreatedUtc;

            string query =
                "INSERT INTO assistant_feedback " +
                "(id, tenant_id, assistant_id, user_message, assistant_response, rating, feedback_text, message_history, " +
                "created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(feedback.Id) + "', " +
                "'" + _Driver.Sanitize(feedback.TenantId) + "', " +
                "'" + _Driver.Sanitize(feedback.AssistantId) + "', " +
                _Driver.FormatNullableString(feedback.UserMessage) + ", " +
                _Driver.FormatNullableString(feedback.AssistantResponse) + ", " +
                "'" + _Driver.Sanitize(feedback.Rating.ToString()) + "', " +
                _Driver.FormatNullableString(feedback.FeedbackText) + ", " +
                _Driver.FormatNullableString(feedback.MessageHistory) + ", " +
                "'" + _Driver.FormatDateTime(feedback.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(feedback.LastUpdateUtc) + "'" +
                ")";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return feedback;
        }

        /// <inheritdoc />
        public async Task<AssistantFeedback> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM assistant_feedback WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return AssistantFeedback.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM assistant_feedback WHERE id = '" + _Driver.Sanitize(id) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<AssistantFeedback>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
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
            conditions.Add("tenant_id = '" + _Driver.Sanitize(tenantId) + "'");
            if (!String.IsNullOrEmpty(query.AssistantIdFilter))
                conditions.Add("assistant_id = '" + _Driver.Sanitize(query.AssistantIdFilter) + "'");
            string whereClause = "WHERE " + String.Join(" AND ", conditions) + " ";

            string countQuery = "SELECT COUNT(*) AS cnt FROM assistant_feedback " + whereClause;
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            long totalRecords = 0;
            if (countResult != null && countResult.Rows.Count > 0)
                totalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string selectQuery =
                "SELECT * FROM assistant_feedback " +
                whereClause +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + offset;

            DataTable result = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);

            List<AssistantFeedback> objects = new List<AssistantFeedback>();
            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    AssistantFeedback obj = AssistantFeedback.FromDataRow(row);
                    if (obj != null) objects.Add(obj);
                }
            }

            long nextOffset = offset + objects.Count;
            long remaining = totalRecords - nextOffset;
            if (remaining < 0) remaining = 0;

            sw.Stop();

            EnumerationResult<AssistantFeedback> enumerationResult = new EnumerationResult<AssistantFeedback>();
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

            string query = "DELETE FROM assistant_feedback WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion
    }
}
