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
    /// MySQL credential methods implementation.
    /// </summary>
    public class CredentialMethods : ICredentialMethods
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
        public CredentialMethods(MysqlDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
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
                ")";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return credential;
        }

        /// <inheritdoc />
        public async Task<Credential> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM credentials WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return Credential.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<Credential> ReadByBearerTokenAsync(string bearerToken, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bearerToken)) throw new ArgumentNullException(nameof(bearerToken));

            string query = "SELECT * FROM credentials WHERE bearer_token = '" + _Driver.Sanitize(bearerToken) + "'";
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
                "WHERE id = '" + _Driver.Sanitize(credential.Id) + "'";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return credential;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM credentials WHERE id = '" + _Driver.Sanitize(id) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT COUNT(*) AS cnt FROM credentials WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return false;
            int count = Convert.ToInt32(result.Rows[0]["cnt"]);
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Credential>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
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

            string countQuery = "SELECT COUNT(*) AS cnt FROM credentials";
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            long totalRecords = 0;
            if (countResult != null && countResult.Rows.Count > 0)
                totalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string selectQuery =
                "SELECT * FROM credentials " +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + offset;

            DataTable result = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);

            List<Credential> objects = new List<Credential>();
            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    Credential obj = Credential.FromDataRow(row);
                    if (obj != null) objects.Add(obj);
                }
            }

            long nextOffset = offset + objects.Count;
            long remaining = totalRecords - nextOffset;
            if (remaining < 0) remaining = 0;

            sw.Stop();

            EnumerationResult<Credential> enumerationResult = new EnumerationResult<Credential>();
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
        public async Task DeleteByUserIdAsync(string userId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

            string query = "DELETE FROM credentials WHERE user_id = '" + _Driver.Sanitize(userId) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        #endregion
    }
}
