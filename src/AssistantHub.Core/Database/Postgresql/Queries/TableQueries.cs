namespace AssistantHub.Core.Database.Postgresql.Queries
{
    /// <summary>
    /// PostgreSQL table creation and index queries.
    /// </summary>
    internal static class TableQueries
    {
        #region Tables

        internal static string CreateTenantsTable =
            "CREATE TABLE IF NOT EXISTS tenants (" +
            "  id TEXT PRIMARY KEY, " +
            "  name TEXT NOT NULL, " +
            "  active INTEGER NOT NULL DEFAULT 1, " +
            "  is_protected INTEGER NOT NULL DEFAULT 0, " +
            "  labels_json TEXT, " +
            "  tags_json TEXT, " +
            "  created_utc TEXT NOT NULL, " +
            "  last_update_utc TEXT NOT NULL " +
            ")";

        internal static string CreateUsersTable =
            "CREATE TABLE IF NOT EXISTS users (" +
            "  id TEXT PRIMARY KEY, " +
            "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
            "  email TEXT NOT NULL, " +
            "  password_sha256 TEXT, " +
            "  first_name TEXT, " +
            "  last_name TEXT, " +
            "  is_admin INTEGER NOT NULL DEFAULT 0, " +
            "  is_tenant_admin INTEGER NOT NULL DEFAULT 0, " +
            "  active INTEGER NOT NULL DEFAULT 1, " +
            "  is_protected INTEGER NOT NULL DEFAULT 0, " +
            "  created_utc TEXT NOT NULL, " +
            "  last_update_utc TEXT NOT NULL " +
            ")";

        internal static string CreateCredentialsTable =
            "CREATE TABLE IF NOT EXISTS credentials (" +
            "  id TEXT PRIMARY KEY, " +
            "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
            "  user_id TEXT NOT NULL, " +
            "  name TEXT, " +
            "  bearer_token TEXT NOT NULL, " +
            "  active INTEGER NOT NULL DEFAULT 1, " +
            "  is_protected INTEGER NOT NULL DEFAULT 0, " +
            "  created_utc TEXT NOT NULL, " +
            "  last_update_utc TEXT NOT NULL " +
            ")";

        internal static string CreateAssistantsTable =
            "CREATE TABLE IF NOT EXISTS assistants (" +
            "  id TEXT PRIMARY KEY, " +
            "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
            "  user_id TEXT NOT NULL, " +
            "  name TEXT NOT NULL, " +
            "  description TEXT, " +
            "  active INTEGER NOT NULL DEFAULT 1, " +
            "  created_utc TEXT NOT NULL, " +
            "  last_update_utc TEXT NOT NULL " +
            ")";

        internal static string CreateAssistantSettingsTable =
            "CREATE TABLE IF NOT EXISTS assistant_settings (" +
            "  id TEXT PRIMARY KEY, " +
            "  assistant_id TEXT NOT NULL, " +
            "  temperature DOUBLE PRECISION NOT NULL DEFAULT 0.7, " +
            "  top_p DOUBLE PRECISION NOT NULL DEFAULT 1.0, " +
            "  system_prompt TEXT, " +
            "  max_tokens INTEGER NOT NULL DEFAULT 4096, " +
            "  context_window INTEGER NOT NULL DEFAULT 8192, " +
            "  model TEXT NOT NULL DEFAULT 'gemma3:4b', " +
            "  enable_rag INTEGER NOT NULL DEFAULT 0, " +
            "  enable_retrieval_gate INTEGER NOT NULL DEFAULT 0, " +
            "  enable_citations INTEGER NOT NULL DEFAULT 0, " +
            "  citation_link_mode TEXT DEFAULT 'None', " +
            "  collection_id TEXT, " +
            "  retrieval_top_k INTEGER NOT NULL DEFAULT 10, " +
            "  retrieval_score_threshold DOUBLE PRECISION NOT NULL DEFAULT 0.3, " +
            "  search_mode TEXT DEFAULT 'Vector', " +
            "  text_weight DOUBLE PRECISION DEFAULT 0.3, " +
            "  fulltext_search_type TEXT DEFAULT 'TsRank', " +
            "  fulltext_language TEXT DEFAULT 'english', " +
            "  fulltext_normalization INTEGER DEFAULT 32, " +
            "  fulltext_minimum_score DOUBLE PRECISION DEFAULT NULL, " +
            "  retrieval_include_neighbors INTEGER NOT NULL DEFAULT 0, " +
            "  inference_endpoint_id TEXT, " +
            "  embedding_endpoint_id TEXT, " +
            "  title TEXT, " +
            "  logo_url TEXT, " +
            "  favicon_url TEXT, " +
            "  streaming INTEGER NOT NULL DEFAULT 1, " +
            "  created_utc TEXT NOT NULL, " +
            "  last_update_utc TEXT NOT NULL " +
            ")";

        internal static string CreateAssistantDocumentsTable =
            "CREATE TABLE IF NOT EXISTS assistant_documents (" +
            "  id TEXT PRIMARY KEY, " +
            "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
            "  name TEXT NOT NULL, " +
            "  original_filename TEXT, " +
            "  content_type TEXT DEFAULT 'application/octet-stream', " +
            "  size_bytes INTEGER NOT NULL DEFAULT 0, " +
            "  s3_key TEXT, " +
            "  status TEXT NOT NULL DEFAULT 'Pending', " +
            "  status_message TEXT, " +
            "  ingestion_rule_id TEXT, " +
            "  bucket_name TEXT, " +
            "  collection_id TEXT, " +
            "  labels_json TEXT, " +
            "  tags_json TEXT, " +
            "  chunk_record_ids TEXT, " +
            "  created_utc TEXT NOT NULL, " +
            "  last_update_utc TEXT NOT NULL " +
            ")";

        internal static string CreateAssistantFeedbackTable =
            "CREATE TABLE IF NOT EXISTS assistant_feedback (" +
            "  id TEXT PRIMARY KEY, " +
            "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
            "  assistant_id TEXT NOT NULL, " +
            "  user_message TEXT, " +
            "  assistant_response TEXT, " +
            "  rating TEXT NOT NULL DEFAULT 'ThumbsUp', " +
            "  feedback_text TEXT, " +
            "  message_history TEXT, " +
            "  created_utc TEXT NOT NULL, " +
            "  last_update_utc TEXT NOT NULL " +
            ")";

        internal static string CreateIngestionRulesTable =
            "CREATE TABLE IF NOT EXISTS ingestion_rules (" +
            "  id TEXT PRIMARY KEY, " +
            "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
            "  name TEXT NOT NULL, " +
            "  description TEXT, " +
            "  bucket TEXT NOT NULL, " +
            "  collection_name TEXT NOT NULL, " +
            "  collection_id TEXT, " +
            "  labels_json TEXT, " +
            "  tags_json TEXT, " +
            "  atomization_json TEXT, " +
            "  summarization_json TEXT, " +
            "  chunking_json TEXT, " +
            "  embedding_json TEXT, " +
            "  created_utc TEXT NOT NULL, " +
            "  last_update_utc TEXT NOT NULL " +
            ")";

        internal static string CreateChatHistoryTable =
            "CREATE TABLE IF NOT EXISTS chat_history (" +
            "  id TEXT PRIMARY KEY, " +
            "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
            "  thread_id TEXT NOT NULL, " +
            "  assistant_id TEXT NOT NULL, " +
            "  collection_id TEXT, " +
            "  user_message_utc TEXT NOT NULL, " +
            "  user_message TEXT, " +
            "  retrieval_start_utc TEXT, " +
            "  retrieval_duration_ms DOUBLE PRECISION NOT NULL DEFAULT 0, " +
            "  retrieval_gate_decision TEXT, " +
            "  retrieval_gate_duration_ms DOUBLE PRECISION NOT NULL DEFAULT 0, " +
            "  retrieval_context TEXT, " +
            "  prompt_sent_utc TEXT, " +
            "  prompt_tokens INTEGER NOT NULL DEFAULT 0, " +
            "  endpoint_resolution_duration_ms DOUBLE PRECISION NOT NULL DEFAULT 0, " +
            "  compaction_duration_ms DOUBLE PRECISION NOT NULL DEFAULT 0, " +
            "  inference_connection_duration_ms DOUBLE PRECISION NOT NULL DEFAULT 0, " +
            "  time_to_first_token_ms DOUBLE PRECISION NOT NULL DEFAULT 0, " +
            "  time_to_last_token_ms DOUBLE PRECISION NOT NULL DEFAULT 0, " +
            "  completion_tokens INTEGER NOT NULL DEFAULT 0, " +
            "  tokens_per_second_overall DOUBLE PRECISION NOT NULL DEFAULT 0, " +
            "  tokens_per_second_generation DOUBLE PRECISION NOT NULL DEFAULT 0, " +
            "  assistant_response TEXT, " +
            "  created_utc TEXT NOT NULL, " +
            "  last_update_utc TEXT NOT NULL " +
            ")";

        #endregion

        #region Indices

        internal static string CreateTenantsNameIndex =
            "CREATE INDEX IF NOT EXISTS idx_tenants_name ON tenants (name)";

        internal static string CreateTenantsCreatedUtcIndex =
            "CREATE INDEX IF NOT EXISTS idx_tenants_created_utc ON tenants (created_utc)";

        internal static string CreateUsersTenantIdIndex =
            "CREATE INDEX IF NOT EXISTS idx_users_tenant_id ON users (tenant_id)";

        internal static string CreateUsersTenantEmailIndex =
            "CREATE UNIQUE INDEX IF NOT EXISTS idx_users_tenant_email ON users (tenant_id, email)";

        internal static string CreateUsersEmailIndex =
            "CREATE INDEX IF NOT EXISTS idx_users_email ON users (email)";

        internal static string CreateCredentialsTenantIdIndex =
            "CREATE INDEX IF NOT EXISTS idx_credentials_tenant_id ON credentials (tenant_id)";

        internal static string CreateAssistantsTenantIdIndex =
            "CREATE INDEX IF NOT EXISTS idx_assistants_tenant_id ON assistants (tenant_id)";

        internal static string CreateAssistantDocumentsTenantIdIndex =
            "CREATE INDEX IF NOT EXISTS idx_assistant_documents_tenant_id ON assistant_documents (tenant_id)";

        internal static string CreateAssistantFeedbackTenantIdIndex =
            "CREATE INDEX IF NOT EXISTS idx_assistant_feedback_tenant_id ON assistant_feedback (tenant_id)";

        internal static string CreateIngestionRulesTenantIdIndex =
            "CREATE INDEX IF NOT EXISTS idx_ingestion_rules_tenant_id ON ingestion_rules (tenant_id)";

        internal static string CreateChatHistoryTenantIdIndex =
            "CREATE INDEX IF NOT EXISTS idx_chat_history_tenant_id ON chat_history (tenant_id)";

        internal static string CreateCredentialsUserIdIndex =
            "CREATE INDEX IF NOT EXISTS idx_credentials_user_id ON credentials (user_id)";

        internal static string CreateCredentialsBearerTokenIndex =
            "CREATE INDEX IF NOT EXISTS idx_credentials_bearer_token ON credentials (bearer_token)";

        internal static string CreateAssistantsUserIdIndex =
            "CREATE INDEX IF NOT EXISTS idx_assistants_user_id ON assistants (user_id)";

        internal static string CreateAssistantSettingsAssistantIdIndex =
            "CREATE INDEX IF NOT EXISTS idx_assistant_settings_assistant_id ON assistant_settings (assistant_id)";

        internal static string CreateAssistantFeedbackAssistantIdIndex =
            "CREATE INDEX IF NOT EXISTS idx_assistant_feedback_assistant_id ON assistant_feedback (assistant_id)";

        internal static string CreateIngestionRulesNameIndex =
            "CREATE INDEX IF NOT EXISTS idx_ingestion_rules_name ON ingestion_rules (name)";

        internal static string CreateAssistantDocumentsIngestionRuleIdIndex =
            "CREATE INDEX IF NOT EXISTS idx_assistant_documents_ingestion_rule_id ON assistant_documents (ingestion_rule_id)";

        internal static string CreateChatHistoryAssistantIdIndex =
            "CREATE INDEX IF NOT EXISTS idx_chat_history_assistant_id ON chat_history (assistant_id)";

        internal static string CreateChatHistoryThreadIdIndex =
            "CREATE INDEX IF NOT EXISTS idx_chat_history_thread_id ON chat_history (thread_id)";

        internal static string CreateChatHistoryCreatedUtcIndex =
            "CREATE INDEX IF NOT EXISTS idx_chat_history_created_utc ON chat_history (created_utc)";

        #endregion
    }
}
