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
    /// SQL Server assistant document methods.
    /// </summary>
    public class AssistantDocumentMethods : IAssistantDocumentMethods
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
        public AssistantDocumentMethods(SqlServerDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<AssistantDocument> CreateAsync(AssistantDocument document, CancellationToken token = default)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            document.CreatedUtc = DateTime.UtcNow;
            document.LastUpdateUtc = document.CreatedUtc;

            string query =
                "INSERT INTO assistant_documents " +
                "(id, assistant_id, name, original_filename, content_type, size_bytes, s3_key, " +
                "status, status_message, created_utc, last_update_utc) " +
                "VALUES " +
                "('" + _Driver.Sanitize(document.Id) + "', " +
                "'" + _Driver.Sanitize(document.AssistantId) + "', " +
                "'" + _Driver.Sanitize(document.Name) + "', " +
                _Driver.FormatNullableString(document.OriginalFilename) + ", " +
                _Driver.FormatNullableString(document.ContentType) + ", " +
                document.SizeBytes + ", " +
                _Driver.FormatNullableString(document.S3Key) + ", " +
                "'" + _Driver.Sanitize(document.Status.ToString()) + "', " +
                _Driver.FormatNullableString(document.StatusMessage) + ", " +
                "'" + _Driver.FormatDateTime(document.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(document.LastUpdateUtc) + "');";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return document;
        }

        /// <inheritdoc />
        public async Task<AssistantDocument> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM assistant_documents WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return AssistantDocument.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<AssistantDocument> UpdateAsync(AssistantDocument document, CancellationToken token = default)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            document.LastUpdateUtc = DateTime.UtcNow;

            string query =
                "UPDATE assistant_documents SET " +
                "assistant_id = '" + _Driver.Sanitize(document.AssistantId) + "', " +
                "name = '" + _Driver.Sanitize(document.Name) + "', " +
                "original_filename = " + _Driver.FormatNullableString(document.OriginalFilename) + ", " +
                "content_type = " + _Driver.FormatNullableString(document.ContentType) + ", " +
                "size_bytes = " + document.SizeBytes + ", " +
                "s3_key = " + _Driver.FormatNullableString(document.S3Key) + ", " +
                "status = '" + _Driver.Sanitize(document.Status.ToString()) + "', " +
                "status_message = " + _Driver.FormatNullableString(document.StatusMessage) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(document.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(document.Id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return document;
        }

        /// <inheritdoc />
        public async Task UpdateStatusAsync(string id, DocumentStatusEnum status, string statusMessage, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "UPDATE assistant_documents SET " +
                "status = '" + _Driver.Sanitize(status.ToString()) + "', " +
                "status_message = " + _Driver.FormatNullableString(statusMessage) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(DateTime.UtcNow) + "' " +
                "WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM assistant_documents WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT COUNT(*) AS cnt FROM assistant_documents WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return false;
            int count = Convert.ToInt32(result.Rows[0]["cnt"]);
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<AssistantDocument>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
        {
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

            string whereClause = "";
            if (!String.IsNullOrEmpty(query.AssistantIdFilter))
                whereClause = "WHERE assistant_id = '" + _Driver.Sanitize(query.AssistantIdFilter) + "' ";

            string countQuery = "SELECT COUNT(*) AS cnt FROM assistant_documents " + whereClause + ";";
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            long totalRecords = 0;
            if (countResult != null && countResult.Rows.Count > 0)
                totalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string selectQuery =
                "SELECT * FROM assistant_documents " +
                whereClause +
                orderBy + " " +
                "OFFSET " + skip + " ROWS FETCH NEXT " + maxResults + " ROWS ONLY;";

            DataTable result = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);

            EnumerationResult<AssistantDocument> ret = new EnumerationResult<AssistantDocument>();
            ret.MaxResults = maxResults;
            ret.TotalRecords = totalRecords;

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    AssistantDocument obj = AssistantDocument.FromDataRow(row);
                    if (obj != null) ret.Objects.Add(obj);
                }
            }

            long nextSkip = skip + maxResults;
            ret.RecordsRemaining = totalRecords - nextSkip;
            if (ret.RecordsRemaining < 0) ret.RecordsRemaining = 0;

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

            string query = "DELETE FROM assistant_documents WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion
    }
}
