#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Core.Database.Sqlite
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Sqlite.Implementations;
    using AssistantHub.Core.Database.Sqlite.Queries;
    using AssistantHub.Core.Settings;
    using Microsoft.Data.Sqlite;
    using SyslogLogging;

    /// <summary>
    /// SQLite database driver.
    /// </summary>
    public class SqliteDatabaseDriver : DatabaseDriverBase, IDisposable
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[SqliteDatabaseDriver] ";
        private readonly DatabaseSettings _Settings;
        private readonly LoggingModule _Logging;
        private readonly string _ConnectionString;
        private readonly SemaphoreSlim _Semaphore = new SemaphoreSlim(1, 1);
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public SqliteDatabaseDriver(DatabaseSettings settings, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _ConnectionString = "Data Source=" + _Settings.Filename + ";";

            User = new UserMethods(this, _Settings, _Logging);
            Credential = new CredentialMethods(this, _Settings, _Logging);
            Assistant = new AssistantMethods(this, _Settings, _Logging);
            AssistantSettings = new AssistantSettingsMethods(this, _Settings, _Logging);
            AssistantDocument = new AssistantDocumentMethods(this, _Settings, _Logging);
            AssistantFeedback = new AssistantFeedbackMethods(this, _Settings, _Logging);
            IngestionRule = new IngestionRuleMethods(this, _Settings, _Logging);
            ChatHistory = new ChatHistoryMethods(this, _Settings, _Logging);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Initialize the database (create tables and indices).
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public override async Task InitializeAsync(CancellationToken token = default)
        {
            string pragmas =
                "PRAGMA journal_mode = WAL; " +
                "PRAGMA synchronous = NORMAL; " +
                "PRAGMA temp_store = MEMORY; " +
                "PRAGMA mmap_size = 134217728; " +
                "PRAGMA cache_size = -65536; ";

            await ExecuteQueryAsync(pragmas, false, token).ConfigureAwait(false);
            await ExecuteQueryAsync(TableQueries.CreateTables(), false, token).ConfigureAwait(false);
            await ExecuteQueryAsync(TableQueries.CreateIndices(), false, token).ConfigureAwait(false);

            _Logging.Info(_Header + "initialized SQLite database at " + _Settings.Filename);
        }

        /// <summary>
        /// Execute a query.
        /// </summary>
        /// <param name="query">SQL query.</param>
        /// <param name="isTransaction">Execute within a transaction.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>DataTable result.</returns>
        public override async Task<DataTable> ExecuteQueryAsync(string query, bool isTransaction = false, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

            DataTable result = new DataTable();
            SqliteConnection conn = null;
            SqliteTransaction txn = null;

            await _Semaphore.WaitAsync(token).ConfigureAwait(false);

            try
            {
                conn = new SqliteConnection(_ConnectionString);
                await conn.OpenAsync(token).ConfigureAwait(false);

                if (isTransaction) txn = conn.BeginTransaction();

                if (_Settings.LogQueries)
                    _Logging.Debug(_Header + "query: " + query);

                using (SqliteCommand cmd = new SqliteCommand(query, conn, txn))
                {
                    using (SqliteDataReader reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            result.Columns.Add(new DataColumn(reader.GetName(i), typeof(string)));
                        }

                        while (await reader.ReadAsync(token).ConfigureAwait(false))
                        {
                            DataRow row = result.NewRow();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                if (reader.IsDBNull(i))
                                    row[i] = DBNull.Value;
                                else
                                    row[i] = reader.GetValue(i).ToString();
                            }
                            result.Rows.Add(row);
                        }
                    }
                }

                if (txn != null) txn.Commit();
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception executing query: " + e.Message);
                if (txn != null)
                {
                    try { txn.Rollback(); }
                    catch (Exception) { }
                }
                throw;
            }
            finally
            {
                if (txn != null) txn.Dispose();
                if (conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
                _Semaphore.Release();
            }

            return result;
        }

        /// <summary>
        /// Execute multiple queries.
        /// </summary>
        /// <param name="queries">SQL queries.</param>
        /// <param name="isTransaction">Execute within a transaction.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>DataTable result.</returns>
        public override async Task<DataTable> ExecuteQueriesAsync(IEnumerable<string> queries, bool isTransaction = false, CancellationToken token = default)
        {
            if (queries == null) throw new ArgumentNullException(nameof(queries));
            string combined = String.Join(Environment.NewLine, queries);
            return await ExecuteQueryAsync(combined, isTransaction, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed) return;

            if (disposing)
            {
                _Semaphore?.Dispose();
            }

            _Disposed = true;
        }

        #endregion
    }
}

#pragma warning restore CS8625, CS8603, CS8600
