namespace AssistantHub.Core.Database.Postgresql
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database.Postgresql.Implementations;
    using AssistantHub.Core.Database.Postgresql.Queries;
    using AssistantHub.Core.Settings;
    using Npgsql;
    using SyslogLogging;

    /// <summary>
    /// PostgreSQL database driver.
    /// </summary>
    public class PostgresqlDatabaseDriver : DatabaseDriverBase
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private readonly DatabaseSettings _Settings;
        private readonly LoggingModule _Logging;
        private readonly string _ConnectionString;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public PostgresqlDatabaseDriver(DatabaseSettings settings, LoggingModule logging) : base()
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _ConnectionString = _Settings.GetConnectionString();

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
                "ALTER TABLE chat_history ADD COLUMN completion_tokens INTEGER NOT NULL DEFAULT 0",
                "ALTER TABLE chat_history ADD COLUMN tokens_per_second_overall DOUBLE PRECISION NOT NULL DEFAULT 0",
                "ALTER TABLE chat_history ADD COLUMN tokens_per_second_generation DOUBLE PRECISION NOT NULL DEFAULT 0"
            };

            foreach (string migration in migrations)
            {
                try { await ExecuteQueryAsync(migration, false, token).ConfigureAwait(false); }
                catch (Exception) { /* Column already exists */ }
            }

            _Logging.Info("PostgreSQL database initialized successfully");
        }

        /// <inheritdoc />
        public override async Task<DataTable> ExecuteQueryAsync(string query, bool isTransaction = false, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

            if (_Settings.LogQueries) _Logging.Debug("PostgreSQL query: " + query);

            DataTable result = new DataTable();

            using (NpgsqlConnection conn = new NpgsqlConnection(_ConnectionString))
            {
                await conn.OpenAsync(token).ConfigureAwait(false);

                NpgsqlTransaction txn = null;

                try
                {
                    if (isTransaction) txn = await conn.BeginTransactionAsync(token).ConfigureAwait(false);

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn, txn))
                    {
                        using (NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
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

            using (NpgsqlConnection conn = new NpgsqlConnection(_ConnectionString))
            {
                await conn.OpenAsync(token).ConfigureAwait(false);

                NpgsqlTransaction txn = null;

                try
                {
                    if (isTransaction) txn = await conn.BeginTransactionAsync(token).ConfigureAwait(false);

                    foreach (string query in queries)
                    {
                        if (String.IsNullOrEmpty(query)) continue;

                        if (_Settings.LogQueries) _Logging.Debug("PostgreSQL query: " + query);

                        using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn, txn))
                        {
                            using (NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
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
