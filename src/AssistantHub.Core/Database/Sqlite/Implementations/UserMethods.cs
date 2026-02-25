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
    /// SQLite user methods implementation.
    /// </summary>
    public class UserMethods : IUserMethods
    {
        #region Private-Members

        private readonly string _Header = "[UserMethods] ";
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
        public UserMethods(SqliteDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
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

            if (String.IsNullOrEmpty(user.Id)) user.Id = IdGenerator.NewUserId();
            user.CreatedUtc = DateTime.UtcNow;
            user.LastUpdateUtc = user.CreatedUtc;

            string query =
                "INSERT INTO users " +
                "(id, tenant_id, email, password_sha256, first_name, last_name, is_admin, is_tenant_admin, active, is_protected, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(user.Id) + "', " +
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
                "'" + _Driver.FormatDateTime(user.LastUpdateUtc) + "'" +
                ");";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return user;
        }

        /// <inheritdoc />
        public async Task<UserMaster> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT * FROM users WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return UserMaster.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<UserMaster> ReadByEmailAsync(string tenantId, string email, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            string query =
                "SELECT * FROM users WHERE tenant_id = '" + _Driver.Sanitize(tenantId) + "' AND email = '" + _Driver.Sanitize(email) + "';";

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

            string query =
                "DELETE FROM users WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT COUNT(*) AS cnt FROM users WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return false;
            int count = DataTableHelper.GetIntValue(result.Rows[0], "cnt");
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<UserMaster>> EnumerateAsync(string tenantId, EnumerationQuery query, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (query == null) throw new ArgumentNullException(nameof(query));

            EnumerationResult<UserMaster> ret = new EnumerationResult<UserMaster>();
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

            string selectQuery =
                "SELECT * FROM users " +
                "WHERE tenant_id = '" + _Driver.Sanitize(tenantId) + "' " +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + skip + ";";

            string countQuery =
                "SELECT COUNT(*) AS cnt FROM users WHERE tenant_id = '" + _Driver.Sanitize(tenantId) + "';";

            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            if (countResult != null && countResult.Rows.Count > 0)
                ret.TotalRecords = DataTableHelper.GetLongValue(countResult.Rows[0], "cnt");

            DataTable selectResult = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);
            if (selectResult != null && selectResult.Rows.Count > 0)
            {
                foreach (DataRow row in selectResult.Rows)
                {
                    ret.Objects.Add(UserMaster.FromDataRow(row));
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
            string query = "SELECT COUNT(*) AS cnt FROM users;";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return 0;
            return DataTableHelper.GetLongValue(result.Rows[0], "cnt");
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
