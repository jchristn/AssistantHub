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
    /// PostgreSQL user methods implementation.
    /// </summary>
    public class UserMethods : IUserMethods
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
        public UserMethods(PostgresqlDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<UserMaster> CreateAsync(UserMaster user, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            user.CreatedUtc = DateTime.UtcNow;
            user.LastUpdateUtc = user.CreatedUtc;

            string query =
                "INSERT INTO users " +
                "(id, email, password_sha256, first_name, last_name, is_admin, active, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(user.Id) + "', " +
                "'" + _Driver.Sanitize(user.Email) + "', " +
                _Driver.FormatNullableString(user.PasswordSha256) + ", " +
                _Driver.FormatNullableString(user.FirstName) + ", " +
                _Driver.FormatNullableString(user.LastName) + ", " +
                _Driver.FormatBoolean(user.IsAdmin) + ", " +
                _Driver.FormatBoolean(user.Active) + ", " +
                "'" + _Driver.FormatDateTime(user.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(user.LastUpdateUtc) + "'" +
                ")";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return user;
        }

        /// <inheritdoc />
        public async Task<UserMaster> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM users WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return UserMaster.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<UserMaster> ReadByEmailAsync(string email, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            string query = "SELECT * FROM users WHERE email = '" + _Driver.Sanitize(email) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return UserMaster.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<UserMaster> UpdateAsync(UserMaster user, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            user.LastUpdateUtc = DateTime.UtcNow;

            string query =
                "UPDATE users SET " +
                "email = '" + _Driver.Sanitize(user.Email) + "', " +
                "password_sha256 = " + _Driver.FormatNullableString(user.PasswordSha256) + ", " +
                "first_name = " + _Driver.FormatNullableString(user.FirstName) + ", " +
                "last_name = " + _Driver.FormatNullableString(user.LastName) + ", " +
                "is_admin = " + _Driver.FormatBoolean(user.IsAdmin) + ", " +
                "active = " + _Driver.FormatBoolean(user.Active) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(user.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(user.Id) + "'";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return user;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM users WHERE id = '" + _Driver.Sanitize(id) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT COUNT(*) AS cnt FROM users WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return false;
            int count = Convert.ToInt32(result.Rows[0]["cnt"]);
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<UserMaster>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
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

            string countQuery = "SELECT COUNT(*) AS cnt FROM users";
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            long totalRecords = 0;
            if (countResult != null && countResult.Rows.Count > 0)
                totalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string selectQuery =
                "SELECT * FROM users " +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + offset;

            DataTable result = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);

            List<UserMaster> objects = new List<UserMaster>();
            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    UserMaster obj = UserMaster.FromDataRow(row);
                    if (obj != null) objects.Add(obj);
                }
            }

            long nextOffset = offset + objects.Count;
            long remaining = totalRecords - nextOffset;
            if (remaining < 0) remaining = 0;

            sw.Stop();

            EnumerationResult<UserMaster> enumerationResult = new EnumerationResult<UserMaster>();
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
        public async Task<long> GetCountAsync(CancellationToken token = default)
        {
            string query = "SELECT COUNT(*) AS cnt FROM users";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return 0;
            return Convert.ToInt64(result.Rows[0]["cnt"]);
        }

        #endregion
    }
}
