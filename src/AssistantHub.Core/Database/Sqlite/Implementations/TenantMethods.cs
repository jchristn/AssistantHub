#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Database.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Interfaces;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// SQLite tenant methods implementation.
    /// </summary>
    public class TenantMethods : ITenantMethods
    {
        #region Private-Members

        private readonly string _Header = "[TenantMethods] ";
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
        public TenantMethods(SqliteDatabaseDriver driver, DatabaseSettings settings, LoggingModule logging)
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

            if (String.IsNullOrEmpty(tenant.Id)) tenant.Id = IdGenerator.NewTenantId();
            tenant.CreatedUtc = DateTime.UtcNow;
            tenant.LastUpdateUtc = tenant.CreatedUtc;

            string query =
                "INSERT INTO tenants " +
                "(id, name, active, is_protected, labels_json, tags_json, created_utc, last_update_utc) " +
                "VALUES (" +
                "'" + _Driver.Sanitize(tenant.Id) + "', " +
                "'" + _Driver.Sanitize(tenant.Name) + "', " +
                _Driver.FormatBoolean(tenant.Active) + ", " +
                _Driver.FormatBoolean(tenant.IsProtected) + ", " +
                _Driver.FormatNullableString(tenant.Labels != null ? JsonSerializer.Serialize(tenant.Labels) : null) + ", " +
                _Driver.FormatNullableString(tenant.Tags != null ? JsonSerializer.Serialize(tenant.Tags) : null) + ", " +
                "'" + _Driver.FormatDateTime(tenant.CreatedUtc) + "', " +
                "'" + _Driver.FormatDateTime(tenant.LastUpdateUtc) + "'" +
                ");";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return tenant;
        }

        /// <inheritdoc />
        public async Task<TenantMetadata> ReadByIdAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT * FROM tenants WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return TenantMetadata.FromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<TenantMetadata> ReadByNameAsync(string name, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            string query =
                "SELECT * FROM tenants WHERE name = '" + _Driver.Sanitize(name) + "';";

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
                "labels_json = " + _Driver.FormatNullableString(tenant.Labels != null ? JsonSerializer.Serialize(tenant.Labels) : null) + ", " +
                "tags_json = " + _Driver.FormatNullableString(tenant.Tags != null ? JsonSerializer.Serialize(tenant.Tags) : null) + ", " +
                "last_update_utc = '" + _Driver.FormatDateTime(tenant.LastUpdateUtc) + "' " +
                "WHERE id = '" + _Driver.Sanitize(tenant.Id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
            return tenant;
        }

        /// <inheritdoc />
        public async Task DeleteByIdAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "DELETE FROM tenants WHERE id = '" + _Driver.Sanitize(id) + "';";

            await _Driver.ExecuteQueryAsync(query, true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByIdAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            string query =
                "SELECT COUNT(*) AS cnt FROM tenants WHERE id = '" + _Driver.Sanitize(id) + "';";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return false;
            int count = DataTableHelper.GetIntValue(result.Rows[0], "cnt");
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<TenantMetadata>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            EnumerationResult<TenantMetadata> ret = new EnumerationResult<TenantMetadata>();
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
                "SELECT * FROM tenants " +
                orderBy + " " +
                "LIMIT " + query.MaxResults + " OFFSET " + skip + ";";

            string countQuery =
                "SELECT COUNT(*) AS cnt FROM tenants;";

            DataTable countResult = await _Driver.ExecuteQueryAsync(countQuery, false, token).ConfigureAwait(false);
            if (countResult != null && countResult.Rows.Count > 0)
                ret.TotalRecords = DataTableHelper.GetLongValue(countResult.Rows[0], "cnt");

            DataTable selectResult = await _Driver.ExecuteQueryAsync(selectQuery, false, token).ConfigureAwait(false);
            if (selectResult != null && selectResult.Rows.Count > 0)
            {
                foreach (DataRow row in selectResult.Rows)
                {
                    ret.Objects.Add(TenantMetadata.FromDataRow(row));
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
            string query = "SELECT COUNT(*) AS cnt FROM tenants;";

            DataTable result = await _Driver.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return 0;
            return DataTableHelper.GetLongValue(result.Rows[0], "cnt");
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
