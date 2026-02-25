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
    /// SQL Server user methods.
    /// </summary>
    public class UserMethods : IUserMethods
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
        public UserMethods(SqlServerDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
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
                "(id, tenant_id, email, password_sha256, first_name, last_name, is_admin, is_tenant_admin, active, is_protected, created_utc, last_update_utc) " +
                "VALUES " +
                "('" + _Driver.Sanitize(user.Id) + "', " +
                "'" + _Driver.Sanitize(user.TenantId) + "', " +
                "'" + _Driver.Sanitize(user.Email) + "', " +
                _Driver.FormatNullableString(user.PasswordSha256) + ", " +
                _Driver.FormatNullableString(user.FirstName) + ", " +
                _Driver.FormatNullableString(user.LastName) + ", " +
                _Driver.FormatBoolean(user.IsAdmin) + ", " +
                _Driver.FormatBoolean(user.IsTenantAdmin) + ", " +
                _Driver.FormatBoolean(user.Active) + ", " +
                _Driver.FormatBoolean(user.IsProtected) + ", " +
                "'" + _Driver.FormatDateTime(user.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(user.LastUpdateUtc) + "');";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return user;
        }

        /// <inheritdoc />
        public async Task<UserMaster> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM users WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return UserMaster.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<UserMaster> ReadByEmailAsync(string tenantId, string email, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            string query = "SELECT * FROM users WHERE tenant_id = '" + _Driver.Sanitize(tenantId) + "' AND email = '" + _Driver.Sanitize(email) + "';";

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
                "tenant_id = '" + _Driver.Sanitize(user.TenantId) + "', " +
                "email = '" + _Driver.Sanitize(user.Email) + "', " +
                "password_sha256 = " + _Driver.FormatNullableString(user.PasswordSha256) + ", " +
                "first_name = " + _Driver.FormatNullableString(user.FirstName) + ", " +
                "last_name = " + _Driver.FormatNullableString(user.LastName) + ", " +
                "is_admin = " + _Driver.FormatBoolean(user.IsAdmin) + ", " +
                "is_tenant_admin = " + _Driver.FormatBoolean(user.IsTenantAdmin) + ", " +
                "active = " + _Driver.FormatBoolean(user.Active) + ", " +
                "is_protected = " + _Driver.FormatBoolean(user.IsProtected) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(user.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(user.Id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return user;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM users WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT COUNT(*) AS cnt FROM users WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return false;
            int count = Convert.ToInt32(result.Rows[0]["cnt"]);
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<UserMaster>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
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

            string countQuery = "SELECT COUNT(*) AS cnt FROM users " + whereClause + ";";
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            long totalRecords = 0;
            if (countResult != null && countResult.Rows.Count > 0)
                totalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string selectQuery =
                "SELECT * FROM users " +
                whereClause +
                orderBy + " " +
                "OFFSET " + skip + " ROWS FETCH NEXT " + maxResults + " ROWS ONLY;";

            DataTable result = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);

            EnumerationResult<UserMaster> ret = new EnumerationResult<UserMaster>();
            ret.MaxResults = maxResults;
            ret.TotalRecords = totalRecords;

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    UserMaster obj = UserMaster.FromDataRow(row);
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
        public async Task<long> GetCountAsync(CancellationToken token = default)
        {
            string query = "SELECT COUNT(*) AS cnt FROM users;";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return 0;
            return Convert.ToInt64(result.Rows[0]["cnt"]);
        }

        #endregion
    }
}
