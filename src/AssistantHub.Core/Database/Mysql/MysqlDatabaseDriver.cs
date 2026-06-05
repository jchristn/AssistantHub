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

            Tenant = new TenantMethods(this, _Settings, _Logging);
            User = new UserMethods(this, _Settings, _Logging);
            Credential = new CredentialMethods(this, _Settings, _Logging);
            Assistant = new AssistantMethods(this, _Settings, _Logging);
            AssistantSettings = new AssistantSettingsMethods(this, _Settings, _Logging);
            AssistantDocument = new AssistantDocumentMethods(this, _Settings, _Logging);
            AssistantFeedback = new AssistantFeedbackMethods(this, _Settings, _Logging);
            IngestionRule = new IngestionRuleMethods(this, _Settings, _Logging);
            CrawlPlan = new CrawlPlanMethods(this, _Settings, _Logging);
            CrawlOperation = new CrawlOperationMethods(this, _Settings, _Logging);
            ChatHistory = new ChatHistoryMethods(this, _Settings, _Logging);
            ChatHistoryPerformanceEvent = new ChatHistoryPerformanceEventMethods(this, _Settings, _Logging);
            RequestHistory = new RequestHistoryMethods(this, _Settings, _Logging);
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override async Task InitializeAsync(CancellationToken token = default)
        {
            List<string> tableQueries = new List<string>
            {
                TableQueries.CreateTenantsTable,
                TableQueries.CreateUsersTable,
                TableQueries.CreateCredentialsTable,
                TableQueries.CreateAssistantsTable,
                TableQueries.CreateAssistantSettingsTable,
                TableQueries.CreateAssistantDocumentsTable,
                TableQueries.CreateAssistantFeedbackTable,
                TableQueries.CreateIngestionRulesTable,
                TableQueries.CreateCrawlPlansTable,
                TableQueries.CreateCrawlOperationsTable,
                TableQueries.CreateChatHistoryTable,
                TableQueries.CreateRequestHistoryTable,
                TableQueries.CreateChatHistoryPerformanceEventsTable
            };

            await ExecuteQueriesAsync(tableQueries, true, token).ConfigureAwait(false);
            await EnsureAssistantSettingsEndpointColumnsAsync(token).ConfigureAwait(false);
            await EnsureTelemetryColumnsAsync(token).ConfigureAwait(false);

            // MySQL does not support CREATE INDEX IF NOT EXISTS;
            // create indices individually and ignore duplicate key errors
            string[] indexQueries = new string[]
            {
                TableQueries.CreateTenantsNameIndex,
                TableQueries.CreateTenantsCreatedUtcIndex,
                TableQueries.CreateUsersEmailIndex,
                TableQueries.CreateUsersTenantIdIndex,
                TableQueries.CreateUsersTenantEmailIndex,
                TableQueries.CreateCredentialsUserIdIndex,
                TableQueries.CreateCredentialsBearerTokenIndex,
                TableQueries.CreateCredentialsTenantIdIndex,
                TableQueries.CreateAssistantsUserIdIndex,
                TableQueries.CreateAssistantsTenantIdIndex,
                TableQueries.CreateAssistantSettingsAssistantIdIndex,
                TableQueries.CreateAssistantFeedbackAssistantIdIndex,
                TableQueries.CreateAssistantFeedbackTenantIdIndex,
                TableQueries.CreateAssistantFeedbackTenantAssistantCreatedIndex,
                TableQueries.CreateIngestionRulesNameIndex,
                TableQueries.CreateIngestionRulesTenantIdIndex,
                TableQueries.CreateAssistantDocumentsIngestionRuleIdIndex,
                TableQueries.CreateAssistantDocumentsTenantIdIndex,
                TableQueries.CreateCrawlPlansTenantIdIndex,
                TableQueries.CreateCrawlPlansStateIndex,
                TableQueries.CreateCrawlOperationsTenantIdIndex,
                TableQueries.CreateCrawlOperationsCrawlPlanIdIndex,
                TableQueries.CreateCrawlOperationsCreatedUtcIndex,
                TableQueries.CreateAssistantDocumentsCrawlPlanIdIndex,
                TableQueries.CreateAssistantDocumentsCrawlOperationIdIndex,
                TableQueries.CreateChatHistoryAssistantIdIndex,
                TableQueries.CreateChatHistoryThreadIdIndex,
                TableQueries.CreateChatHistoryCreatedUtcIndex,
                TableQueries.CreateChatHistoryTenantIdIndex,
                TableQueries.CreateChatHistoryTraceIdIndex,
                TableQueries.CreateChatHistoryRequestHistoryIdIndex,
                TableQueries.CreateRequestHistoryTenantIdIndex,
                TableQueries.CreateRequestHistoryUserIdIndex,
                TableQueries.CreateRequestHistoryCredentialIdIndex,
                TableQueries.CreateRequestHistoryAssistantIdIndex,
                TableQueries.CreateRequestHistoryThreadIdIndex,
                TableQueries.CreateRequestHistoryStatusCodeIndex,
                TableQueries.CreateRequestHistorySuccessIndex,
                TableQueries.CreateRequestHistoryCreatedUtcIndex,
                TableQueries.CreateRequestHistoryPathIndex,
                TableQueries.CreateRequestHistoryTraceIdIndex,
                TableQueries.CreateRequestHistoryChatHistoryIdIndex,
                TableQueries.CreateRequestHistoryTenantAssistantCreatedIndex,
                TableQueries.CreateRequestHistoryTenantAssistantSuccessCreatedIndex,
                TableQueries.CreateChatHistoryPerformanceEventsChatHistoryIdIndex,
                TableQueries.CreateChatHistoryPerformanceEventsAssistantIdIndex,
                TableQueries.CreateChatHistoryPerformanceEventsRequestHistoryIdIndex,
                TableQueries.CreateChatHistoryPerformanceEventsTraceIdIndex,
                TableQueries.CreateChatHistoryPerformanceEventsStageIndex,
                TableQueries.CreateChatHistoryPerformanceEventsStartedUtcIndex,
                TableQueries.CreateChatHistoryPerformanceEventsTenantIdIndex,
                TableQueries.CreateChatHistoryPerformanceEventsEndpointIdIndex,
                TableQueries.CreateChatHistoryPerformanceEventsProviderModelIndex,
                TableQueries.CreateChatHistoryPerformanceEventsCreatedUtcIndex,
                TableQueries.CreateChatHistoryPerformanceEventsDurationMsIndex,
                TableQueries.CreateChatHistoryPerformanceEventsTenantCreatedIndex,
                TableQueries.CreateChatHistoryPerformanceEventsTenantAssistantCreatedIndex,
                TableQueries.CreateChatHistoryPerformanceEventsTenantAssistantStageCreatedIndex,
                TableQueries.CreateChatHistoryPerformanceEventsTenantAssistantEndpointCreatedIndex
            };

            foreach (string indexQuery in indexQueries)
            {
                try { await ExecuteQueryAsync(indexQuery, false, token).ConfigureAwait(false); }
                catch (Exception) { /* Index already exists */ }
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

        #region Private-Methods

        private async Task EnsureAssistantSettingsEndpointColumnsAsync(CancellationToken token)
        {
            await EnsureColumnAsync("assistant_settings", "retrieval_gate_inference_endpoint_id", TableQueries.AddAssistantSettingsRetrievalGateInferenceEndpointIdColumn, token).ConfigureAwait(false);
            await EnsureColumnAsync("assistant_settings", "query_rewrite_inference_endpoint_id", TableQueries.AddAssistantSettingsQueryRewriteInferenceEndpointIdColumn, token).ConfigureAwait(false);
            await EnsureColumnAsync("assistant_settings", "rerank_inference_endpoint_id", TableQueries.AddAssistantSettingsRerankInferenceEndpointIdColumn, token).ConfigureAwait(false);
        }

        private async Task EnsureTelemetryColumnsAsync(CancellationToken token)
        {
            await EnsureColumnAsync("chat_history", "trace_id", TableQueries.AddChatHistoryTraceIdColumn, token).ConfigureAwait(false);
            await EnsureColumnAsync("chat_history", "request_history_id", TableQueries.AddChatHistoryRequestHistoryIdColumn, token).ConfigureAwait(false);
            await EnsureColumnAsync("chat_history", "performance_schema_version", TableQueries.AddChatHistoryPerformanceSchemaVersionColumn, token).ConfigureAwait(false);
            await EnsureColumnAsync("chat_history", "performance_json", TableQueries.AddChatHistoryPerformanceJsonColumn, token).ConfigureAwait(false);
            await EnsureColumnAsync("request_history", "trace_id", TableQueries.AddRequestHistoryTraceIdColumn, token).ConfigureAwait(false);
            await EnsureColumnAsync("request_history", "chat_history_id", TableQueries.AddRequestHistoryChatHistoryIdColumn, token).ConfigureAwait(false);
            await EnsureColumnAsync("chat_history_performance_events", "assistant_id", TableQueries.AddChatHistoryPerformanceEventsAssistantIdColumn, token).ConfigureAwait(false);
            await ExecuteQueryAsync(TableQueries.BackfillChatHistoryPerformanceEventsAssistantId, true, token).ConfigureAwait(false);
        }

        private async Task EnsureColumnAsync(string tableName, string columnName, string alterQuery, CancellationToken token)
        {
            string query =
                "SELECT COUNT(*) AS column_count FROM INFORMATION_SCHEMA.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() " +
                "AND TABLE_NAME = '" + Sanitize(tableName) + "' " +
                "AND COLUMN_NAME = '" + Sanitize(columnName) + "'";

            DataTable result = await ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result != null
                && result.Rows.Count > 0
                && Int32.TryParse(result.Rows[0]["column_count"]?.ToString(), out int count)
                && count > 0)
            {
                return;
            }

            await ExecuteQueryAsync(alterQuery, false, token).ConfigureAwait(false);
        }

        #endregion
    }
}
