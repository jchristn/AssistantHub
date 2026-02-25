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
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;

    /// <summary>
    /// SQL Server tenant methods.
    /// </summary>
    public class TenantMethods : ITenantMethods
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
        public TenantMethods(SqlServerDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<TenantMetadata> CreateAsync(TenantMetadata tenant, CancellationToken token = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            tenant.CreatedUtc = DateTime.UtcNow;
            tenant.LastUpdateUtc = tenant.CreatedUtc;

            string query =
                "INSERT INTO tenants " +
                "(id, name, active, is_protected, labels_json, tags_json, created_utc, last_update_utc) " +
                "VALUES " +
                "('" + _Driver.Sanitize(tenant.Id) + "', " +
                "'" + _Driver.Sanitize(tenant.Name) + "', " +
                _Driver.FormatBoolean(tenant.Active) + ", " +
                _Driver.FormatBoolean(tenant.IsProtected) + ", " +
                _Driver.FormatNullableString(Serializer.SerializeJson(tenant.Labels)) + ", " +
                _Driver.FormatNullableString(Serializer.SerializeJson(tenant.Tags)) + ", " +
                "'" + _Driver.FormatDateTime(tenant.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(tenant.LastUpdateUtc) + "');";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return tenant;
        }

        /// <inheritdoc />
        public async Task<TenantMetadata> ReadByIdAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM tenants WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return TenantMetadata.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<TenantMetadata> ReadByNameAsync(string name, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            string query = "SELECT * FROM tenants WHERE name = '" + _Driver.Sanitize(name) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return TenantMetadata.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<TenantMetadata> UpdateAsync(TenantMetadata tenant, CancellationToken token = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            tenant.LastUpdateUtc = DateTime.UtcNow;

            string query =
                "UPDATE tenants SET " +
                "name = '" + _Driver.Sanitize(tenant.Name) + "', " +
                "active = " + _Driver.FormatBoolean(tenant.Active) + ", " +
                "is_protected = " + _Driver.FormatBoolean(tenant.IsProtected) + ", " +
                "labels_json = " + _Driver.FormatNullableString(Serializer.SerializeJson(tenant.Labels)) + ", " +
                "tags_json = " + _Driver.FormatNullableString(Serializer.SerializeJson(tenant.Tags)) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(tenant.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(tenant.Id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return tenant;
        }

        /// <inheritdoc />
        public async Task DeleteByIdAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM tenants WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByIdAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT COUNT(*) AS cnt FROM tenants WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return false;
            int count = Convert.ToInt32(result.Rows[0]["cnt"]);
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<TenantMetadata>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
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

            string countQuery = "SELECT COUNT(*) AS cnt FROM tenants;";
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            long totalRecords = 0;
            if (countResult != null && countResult.Rows.Count > 0)
                totalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string selectQuery =
                "SELECT * FROM tenants " +
                orderBy + " " +
                "OFFSET " + skip + " ROWS FETCH NEXT " + maxResults + " ROWS ONLY;";

            DataTable result = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);

            EnumerationResult<TenantMetadata> ret = new EnumerationResult<TenantMetadata>();
            ret.MaxResults = maxResults;
            ret.TotalRecords = totalRecords;

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    TenantMetadata obj = TenantMetadata.FromDataRow(row);
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
            string query = "SELECT COUNT(*) AS cnt FROM tenants;";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return 0;
            return Convert.ToInt64(result.Rows[0]["cnt"]);
        }

        #endregion
    }
}
