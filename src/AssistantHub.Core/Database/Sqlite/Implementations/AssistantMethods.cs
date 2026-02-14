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
    /// SQLite assistant methods implementation.
    /// </summary>
    public class AssistantMethods : IAssistantMethods
    {
        #region Private-Members

        private readonly string _Header = "[AssistantMethods] ";
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
        public AssistantMethods(SqliteDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Assistant> CreateAsync(Assistant assistant, CancellationToken token = default)
        {
            if (assistant == null) throw new ArgumentNullException(nameof(assistant));

            if (String.IsNullOrEmpty(assistant.Id)) assistant.Id = IdGenerator.NewAssistantId();
            assistant.CreatedUtc = DateTime.UtcNow;
            assistant.LastUpdateUtc = assistant.CreatedUtc;

            string query =
                "INSERT INTO assistants " +
                "(id, user_id, name, description, active, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(assistant.Id) + "', " +
                "'" + _Driver.Sanitize(assistant.UserId) + "', " +
                "'" + _Driver.Sanitize(assistant.Name) + "', " +
                _Driver.FormatNullableString(assistant.Description) + ", " +
                _Driver.FormatBoolean(assistant.Active) + ", " +
                "'" + _Driver.FormatDateTime(assistant.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(assistant.LastUpdateUtc) + "'" +
                ");";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return assistant;
        }

        /// <inheritdoc />
        public async Task<Assistant> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT * FROM assistants WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return Assistant.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<Assistant> UpdateAsync(Assistant assistant, CancellationToken token = default)
        {
            if (assistant == null) throw new ArgumentNullException(nameof(assistant));

            assistant.LastUpdateUtc = DateTime.UtcNow;

            string query =
                "UPDATE assistants SET " +
                "user_id = '" + _Driver.Sanitize(assistant.UserId) + "', " +
                "name = '" + _Driver.Sanitize(assistant.Name) + "', " +
                "description = " + _Driver.FormatNullableString(assistant.Description) + ", " +
                "active = " + _Driver.FormatBoolean(assistant.Active) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(assistant.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(assistant.Id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return assistant;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "DELETE FROM assistants WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT COUNT(*) AS cnt FROM assistants WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return false;
            int count = DataTableHelper.GetIntValue(result.Rows[0], "cnt");
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Assistant>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            EnumerationResult<Assistant> ret = new EnumerationResult<Assistant>();
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
                whereClause = "WHERE user_id = '" + _Driver.Sanitize(query.AssistantIdFilter) + "' ";
                whereClauseCount = whereClause;
            }

            string selectQuery =
                "SELECT * FROM assistants " +
                whereClause +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + skip + ";";

            string countQuery =
                "SELECT COUNT(*) AS cnt FROM assistants " + whereClauseCount + ";";

            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            if (countResult != null && countResult.Rows.Count > 0)
                ret.TotalRecords = DataTableHelper.GetLongValue(countResult.Rows[0], "cnt");

            DataTable selectResult = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);
            if (selectResult != null && selectResult.Rows.Count > 0)
            {
                foreach (DataRow row in selectResult.Rows)
                {
                    ret.Objects.Add(Assistant.FromDataRow(row));
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
        public async Task<long> GetCountAsync(CancellationToken token = default)
        {
            string query = "SELECT COUNT(*) AS cnt FROM assistants;";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return 0;
            return DataTableHelper.GetLongValue(result.Rows[0], "cnt");
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
