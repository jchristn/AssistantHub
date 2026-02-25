namespace AssistantHub.Core.Database.Postgresql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// PostgreSQL tenant methods implementation.
    /// </summary>
    public class TenantMethods : ITenantMethods
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
        public TenantMethods(PostgresqlDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
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

            string labelsJson = tenant.Labels != null ? JsonSerializer.Serialize(tenant.Labels) : null;
            string tagsJson = tenant.Tags != null ? JsonSerializer.Serialize(tenant.Tags) : null;

            string query =
                "INSERT INTO tenants " +
                "(id, name, active, is_protected, labels_json, tags_json, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(tenant.Id) + "', " +
                "'" + _Driver.Sanitize(tenant.Name) + "', " +
                _Driver.FormatBoolean(tenant.Active) + ", " +
                _Driver.FormatBoolean(tenant.IsProtected) + ", " +
                _Driver.FormatNullableString(labelsJson) + ", " +
                _Driver.FormatNullableString(tagsJson) + ", " +
                "'" + _Driver.FormatDateTime(tenant.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(tenant.LastUpdateUtc) + "'" +
                ")";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return tenant;
        }

        /// <inheritdoc />
        public async Task<TenantMetadata> ReadByIdAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT * FROM tenants WHERE id = '" + _Driver.Sanitize(id) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return TenantMetadata.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<TenantMetadata> ReadByNameAsync(string name, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            string query = "SELECT * FROM tenants WHERE name = '" + _Driver.Sanitize(name) + "'";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;
            return TenantMetadata.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<TenantMetadata> UpdateAsync(TenantMetadata tenant, CancellationToken token = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            tenant.LastUpdateUtc = DateTime.UtcNow;

            string labelsJson = tenant.Labels != null ? JsonSerializer.Serialize(tenant.Labels) : null;
            string tagsJson = tenant.Tags != null ? JsonSerializer.Serialize(tenant.Tags) : null;

            string query =
                "UPDATE tenants SET " +
                "name = '" + _Driver.Sanitize(tenant.Name) + "', " +
                "active = " + _Driver.FormatBoolean(tenant.Active) + ", " +
                "is_protected = " + _Driver.FormatBoolean(tenant.IsProtected) + ", " +
                "labels_json = " + _Driver.FormatNullableString(labelsJson) + ", " +
                "tags_json = " + _Driver.FormatNullableString(tagsJson) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(tenant.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(tenant.Id) + "'";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return tenant;
        }

        /// <inheritdoc />
        public async Task DeleteByIdAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "DELETE FROM tenants WHERE id = '" + _Driver.Sanitize(id) + "'";
            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByIdAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query = "SELECT COUNT(*) AS cnt FROM tenants WHERE id = '" + _Driver.Sanitize(id) + "'";
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

            int offset = 0;
            if (!String.IsNullOrEmpty(query.ContinuationToken))
            {
                if (!Int32.TryParse(query.ContinuationToken, out offset)) offset = 0;
            }

            string orderBy = query.Ordering == EnumerationOrderEnum.CreatedDescending
                ? "ORDER BY created_utc DESC"
                : "ORDER BY created_utc ASC";

            string countQuery = "SELECT COUNT(*) AS cnt FROM tenants";
            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            long totalRecords = 0;
            if (countResult != null && countResult.Rows.Count > 0)
                totalRecords = Convert.ToInt64(countResult.Rows[0]["cnt"]);

            string selectQuery =
                "SELECT * FROM tenants " +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + offset;

            DataTable result = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);

            List<TenantMetadata> objects = new List<TenantMetadata>();
            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    TenantMetadata obj = TenantMetadata.FromDataRow(row);
                    if (obj != null) objects.Add(obj);
                }
            }

            long nextOffset = offset + objects.Count;
            long remaining = totalRecords - nextOffset;
            if (remaining < 0) remaining = 0;

            sw.Stop();

            EnumerationResult<TenantMetadata> enumerationResult = new EnumerationResult<TenantMetadata>();
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
            string query = "SELECT COUNT(*) AS cnt FROM tenants";
            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return 0;
            return Convert.ToInt64(result.Rows[0]["cnt"]);
        }

        #endregion
    }
}
