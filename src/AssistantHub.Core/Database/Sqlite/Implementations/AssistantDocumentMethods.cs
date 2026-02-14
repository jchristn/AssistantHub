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
    /// SQLite assistant document methods implementation.
    /// </summary>
    public class AssistantDocumentMethods : IAssistantDocumentMethods
    {
        #region Private-Members

        private readonly string _Header = "[AssistantDocumentMethods] ";
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
        public AssistantDocumentMethods(SqliteDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
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

            if (String.IsNullOrEmpty(document.Id)) document.Id = IdGenerator.NewAssistantDocumentId();
            document.CreatedUtc = DateTime.UtcNow;
            document.LastUpdateUtc = document.CreatedUtc;

            string query =
                "INSERT INTO assistant_documents " +
                "(id, assistant_id, name, original_filename, content_type, size_bytes, s3_key, " +
                "status, status_message, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(document.Id) + "', " +
                "'" + _Driver.Sanitize(document.AssistantId) + "', " +
                "'" + _Driver.Sanitize(document.Name) + "', " +
                _Driver.FormatNullableString(document.OriginalFilename) + ", " +
                _Driver.FormatNullableString(document.ContentType) + ", " +
                document.SizeBytes + ", " +
                _Driver.FormatNullableString(document.S3Key) + ", " +
                "'" + _Driver.Sanitize(document.Status.ToString()) + "', " +
                _Driver.FormatNullableString(document.StatusMessage) + ", " +
                "'" + _Driver.FormatDateTime(document.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(document.LastUpdateUtc) + "'" +
                ");";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return document;
        }

        /// <inheritdoc />
        public async Task<AssistantDocument> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT * FROM assistant_documents WHERE id = '" + _Driver.Sanitize(id) + "';";

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

            DateTime now = DateTime.UtcNow;

            string query =
                "UPDATE assistant_documents SET " +
                "status = '" + _Driver.Sanitize(status.ToString()) + "', " +
                "status_message = " + _Driver.FormatNullableString(statusMessage) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(now) + "' " +
                "WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "DELETE FROM assistant_documents WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT COUNT(*) AS cnt FROM assistant_documents WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return false;
            int count = DataTableHelper.GetIntValue(result.Rows[0], "cnt");
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<AssistantDocument>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            EnumerationResult<AssistantDocument> ret = new EnumerationResult<AssistantDocument>();
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

            string whereClause = "";
            string whereClauseCount = "";
            if (!String.IsNullOrEmpty(query.AssistantIdFilter))
            {
                whereClause = "WHERE assistant_id = '" + _Driver.Sanitize(query.AssistantIdFilter) + "' ";
                whereClauseCount = whereClause;
            }

            string selectQuery =
                "SELECT * FROM assistant_documents " +
                whereClause +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + skip + ";";

            string countQuery =
                "SELECT COUNT(*) AS cnt FROM assistant_documents " + whereClauseCount + ";";

            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            if (countResult != null && countResult.Rows.Count > 0)
                ret.TotalRecords = DataTableHelper.GetLongValue(countResult.Rows[0], "cnt");

            DataTable selectResult = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);
            if (selectResult != null && selectResult.Rows.Count > 0)
            {
                foreach (DataRow row in selectResult.Rows)
                {
                    ret.Objects.Add(AssistantDocument.FromDataRow(row));
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
                "DELETE FROM assistant_documents WHERE assistant_id = '" + _Driver.Sanitize(assistantId) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
