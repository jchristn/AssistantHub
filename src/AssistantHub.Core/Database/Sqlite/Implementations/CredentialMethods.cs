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
    /// SQLite credential methods implementation.
    /// </summary>
    public class CredentialMethods : ICredentialMethods
    {
        #region Private-Members

        private readonly string _Header = "[CredentialMethods] ";
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
        public CredentialMethods(SqliteDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Credential> CreateAsync(Credential credential, CancellationToken token = default)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential));

            if (String.IsNullOrEmpty(credential.Id)) credential.Id = IdGenerator.NewCredentialId();
            credential.CreatedUtc = DateTime.UtcNow;
            credential.LastUpdateUtc = credential.CreatedUtc;

            string query =
                "INSERT INTO credentials " +
                "(id, user_id, name, bearer_token, active, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(credential.Id) + "', " +
                "'" + _Driver.Sanitize(credential.UserId) + "', " +
                _Driver.FormatNullableString(credential.Name) + ", " +
                "'" + _Driver.Sanitize(credential.BearerToken) + "', " +
                _Driver.FormatBoolean(credential.Active) + ", " +
                "'" + _Driver.FormatDateTime(credential.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(credential.LastUpdateUtc) + "'" +
                ");";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return credential;
        }

        /// <inheritdoc />
        public async Task<Credential> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT * FROM credentials WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return Credential.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<Credential> ReadByBearerTokenAsync(string bearerToken, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bearerToken)) throw new ArgumentNullException(nameof(bearerToken));

            string query =
                "SELECT * FROM credentials WHERE bearer_token = '" + _Driver.Sanitize(bearerToken) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return Credential.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<Credential> UpdateAsync(Credential credential, CancellationToken token = default)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential));

            credential.LastUpdateUtc = DateTime.UtcNow;

            string query =
                "UPDATE credentials SET " +
                "user_id = '" + _Driver.Sanitize(credential.UserId) + "', " +
                "name = " + _Driver.FormatNullableString(credential.Name) + ", " +
                "bearer_token = '" + _Driver.Sanitize(credential.BearerToken) + "', " +
                "active = " + _Driver.FormatBoolean(credential.Active) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(credential.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(credential.Id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return credential;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "DELETE FROM credentials WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT COUNT(*) AS cnt FROM credentials WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return false;
            int count = DataTableHelper.GetIntValue(result.Rows[0], "cnt");
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Credential>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            EnumerationResult<Credential> ret = new EnumerationResult<Credential>();
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
                "SELECT * FROM credentials " +
                whereClause +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + skip + ";";

            string countQuery =
                "SELECT COUNT(*) AS cnt FROM credentials " + whereClauseCount + ";";

            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            if (countResult != null && countResult.Rows.Count > 0)
                ret.TotalRecords = DataTableHelper.GetLongValue(countResult.Rows[0], "cnt");

            DataTable selectResult = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);
            if (selectResult != null && selectResult.Rows.Count > 0)
            {
                foreach (DataRow row in selectResult.Rows)
                {
                    ret.Objects.Add(Credential.FromDataRow(row));
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
        public async Task DeleteByUserIdAsync(string userId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

            string query =
                "DELETE FROM credentials WHERE user_id = '" + _Driver.Sanitize(userId) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
