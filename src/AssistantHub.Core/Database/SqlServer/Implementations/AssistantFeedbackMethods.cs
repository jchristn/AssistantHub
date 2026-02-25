namespace AssistantHub.Core.Database.SqlServer.Implementations
{
    using System;
    using System.Data;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using SyslogLogging;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;

    /// <summary>
    /// SQL Server assistant feedback methods.
    /// </summary>
    public class AssistantFeedbackMethods : IAssistantFeedbackMethods
    {
        #region Private-Members

        private SqlServerDatabaseDriver _Driver;
        private DatabaseSettings _Settings;
        private LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="driver">SQL Server database driver.</param>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public AssistantFeedbackMethods(SqlServerDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
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
                "(id, tenant_id, assistant_id, user_message, assistant_response, rating, feedback_text, message_history, created_utc, last_update_utc) " +
                "VALUES " +
                "('" + _Driver.Sanitize(feedback.Id) + "', " +
                "'" + _Driver.Sanitize(feedback.TenantId) + "', " +
                "'" + _Driver.Sanitize(feedback.AssistantId) + "', " +
                _Driver.FormatNullableString(feedback.UserMessage) + ", " +
                _Driver.FormatNullableString(feedback.AssistantResponse) + ", " +
                "'" + _Driver.Sanitize(feedback.Rating.ToString()) + "', " +
                _Driver.FormatNullableString(feedback.FeedbackText) + ", " +
                _Driver.FormatNullableString(feedback.MessageHistory) + ", " +
                "'" + _Driver.FormatDateTime(feedback.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(feedback.LastUpdateUtc) + "');";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return feedback;
        }

        /// <inheritdoc />
        public async Task<AssistantFeedback> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM assistant_feedback WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return AssistantFeedback.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM assistant_feedback WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<AssistantFeedback>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (query == null) throw new ArgumentNullException(nameof(query));

            Stopwatch sw = Stopwatch.StartNew();

            int maxResults = query.MaxResults;
            int skip = 0;
            if (!String.IsNullOrEmpty(query.ContinuationToken))
            {
                if (!Int32.TryParse(query.ContinuationToken, out skip)) skip = 0;
            }

            string orderBy = query.Ordering == EnumerationOrderEnum.CreatedAscending
                ? "ORDER BY created_utc ASC"
                : "ORDER BY created_utc DESC";

            string whereClause = "WHERE tenant_id = '" + _Driver.Sanitize(tenantId) + "' ";
            if (!String.IsNullOrEmpty(query.AssistantIdFilter))
                whereClause += "AND assistant_id = '" + _Driver.Sanitize(query.AssistantIdFilter) + "' ";

            string countQuery = "SELECT COUNT(*) AS cnt FROM assistant_feedback " + whereClause + ";";
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            long totalRecords = 0;
            if (countResult != null && countResult.Rows.Count > 0)
                totalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string selectQuery =
                "SELECT * FROM assistant_feedback " +
                whereClause +
                orderBy + " " +
                "OFFSET " + skip + " ROWS FETCH NEXT " + maxResults + " ROWS ONLY;";

            DataTable result = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);

            EnumerationResult<AssistantFeedback> ret = new EnumerationResult<AssistantFeedback>();
            ret.MaxResults = maxResults;
            ret.TotalRecords = totalRecords;

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    AssistantFeedback obj = AssistantFeedback.FromDataRow(row);
                    if (obj != null) ret.Objects.Add(obj);
                }
            }

            long nextSkip = skip + maxResults;
            ret.RecordsRemaining = Math.Max(0, totalRecords - nextSkip);

            if (nextSkip < totalRecords)
            {
                ret.ContinuationToken = nextSkip.ToString();
                ret.EndOfResults = false;
            }
            else
            {
                ret.ContinuationToken = null;
                ret.EndOfResults = true;
            }

            sw.Stop();
            ret.TotalMs = sw.Elapsed.TotalMilliseconds;
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteByAssistantIdAsync(string assistantId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(assistantId)) throw new ArgumentNullException(nameof(assistantId));

            string query = "DELETE FROM assistant_feedback WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion
    }
}
