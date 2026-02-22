namespace AssistantHub.Core.Database.Mysql
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Mysql.Implementations;
    using AssistantHub.Core.Database.Mysql.Queries;
    using AssistantHub.Core.Settings;
    using MySql.Data.MySqlClient;
    using SyslogLogging;

    /// <summary>
    /// MySQL database driver.
    /// </summary>
    public class MysqlDatabaseDriver : DatabaseDriverBase
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private DatabaseSettings _Settings = null;
        private LoggingModule _Logging = null;
        private string _ConnectionString = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public MysqlDatabaseDriver(DatabaseSettings settings, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));

            _ConnectionString =
                "Server=" + _Settings.Hostname + ";" +
                "Port=" + _Settings.Port + ";" +
                "Database=" + _Settings.DatabaseName + ";" +
                "Uid=" + _Settings.Username + ";" +
                "Pwd=" + _Settings.Password + ";" +
                "SslMode=" + (_Settings.RequireEncryption ? "Required" : "none");

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

        /// <inheritdoc />
        public override async Task InitializeAsync(CancellationToken token = default)
        {
            List<string> queries = new List<string>
            {
                TableQueries.CreateUsersTable,
                TableQueries.CreateCredentialsTable,
                TableQueries.CreateAssistantsTable,
                TableQueries.CreateAssistantSettingsTable,
                TableQueries.CreateAssistantDocumentsTable,
                TableQueries.CreateAssistantFeedbackTable,
                TableQueries.CreateIngestionRulesTable,
                TableQueries.CreateUsersEmailIndex,
                TableQueries.CreateCredentialsUserIdIndex,
                TableQueries.CreateCredentialsBearerTokenIndex,
                TableQueries.CreateAssistantsUserIdIndex,
                TableQueries.CreateAssistantSettingsAssistantIdIndex,
                TableQueries.CreateAssistantFeedbackAssistantIdIndex,
                TableQueries.CreateIngestionRulesNameIndex,
                TableQueries.CreateAssistantDocumentsIngestionRuleIdIndex,
                TableQueries.CreateChatHistoryTable,
                TableQueries.CreateChatHistoryAssistantIdIndex,
                TableQueries.CreateChatHistoryThreadIdIndex,
                TableQueries.CreateChatHistoryCreatedUtcIndex
            };

            await ExecuteQueriesAsync(queries, true, token).ConfigureAwait(false);

            // Auto-migration: add columns that may not exist in older databases
            string[] migrations = new string[]
            {
                "ALTER TABLE `chat_history` ADD COLUMN `completion_tokens` INT NOT NULL DEFAULT 0",
                "ALTER TABLE `chat_history` ADD COLUMN `tokens_per_second_overall` DOUBLE NOT NULL DEFAULT 0",
                "ALTER TABLE `chat_history` ADD COLUMN `tokens_per_second_generation` DOUBLE NOT NULL DEFAULT 0"
            };

            foreach (string migration in migrations)
            {
                try { await ExecuteQueryAsync(migration, false, token).ConfigureAwait(false); }
                catch (Exception) { /* Column already exists */ }
            }

            _Logging.Info("MySQL database initialized successfully");
        }

        /// <inheritdoc />
        public override async Task<DataTable> ExecuteQueryAsync(string query, bool isTransaction = false, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

            if (_Settings.LogQueries) _Logging.Debug("MySQL query: " + query);

            DataTable result = new DataTable();

            using (MySqlConnection conn = new MySqlConnection(_ConnectionString))
            {
                await conn.OpenAsync(token).ConfigureAwait(false);

                MySqlTransaction txn = null;

                try
                {
                    if (isTransaction) txn = await conn.BeginTransactionAsync(token).ConfigureAwait(false);

                    using (MySqlCommand cmd = new MySqlCommand(query, conn, txn))
                    {
                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                        {
                            result.Load(reader);
                        }
                    }

                    if (txn != null) await txn.CommitAsync(token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    if (txn != null) await txn.RollbackAsync(token).ConfigureAwait(false);
                    throw;
                }
                finally
                {
                    if (txn != null) txn.Dispose();
                }
            }

            return result;
        }

        /// <inheritdoc />
        public override async Task<DataTable> ExecuteQueriesAsync(IEnumerable<string> queries, bool isTransaction = false, CancellationToken token = default)
        {
            if (queries == null || !queries.Any()) throw new ArgumentNullException(nameof(queries));

            DataTable result = new DataTable();

            using (MySqlConnection conn = new MySqlConnection(_ConnectionString))
            {
                await conn.OpenAsync(token).ConfigureAwait(false);

                MySqlTransaction txn = null;

                try
                {
                    if (isTransaction) txn = await conn.BeginTransactionAsync(token).ConfigureAwait(false);

                    foreach (string query in queries)
                    {
                        if (String.IsNullOrEmpty(query)) continue;

                        if (_Settings.LogQueries) _Logging.Debug("MySQL query: " + query);

                        using (MySqlCommand cmd = new MySqlCommand(query, conn, txn))
                        {
                            using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                            {
                                result = new DataTable();
                                result.Load(reader);
                            }
                        }
                    }

                    if (txn != null) await txn.CommitAsync(token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    if (txn != null) await txn.RollbackAsync(token).ConfigureAwait(false);
                    throw;
                }
                finally
                {
                    if (txn != null) txn.Dispose();
                }
            }

            return result;
        }

        /// <inheritdoc />
        public override string FormatBoolean(bool value)
        {
            return value ? "1" : "0";
        }

        #endregion
    }
}
