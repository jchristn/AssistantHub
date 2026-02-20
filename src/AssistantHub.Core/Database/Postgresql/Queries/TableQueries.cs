namespace AssistantHub.Core.Database.Postgresql.Queries
{
    /// <summary>
    /// PostgreSQL table creation and index queries.
    /// </summary>
    internal static class TableQueries
    {
        #region Tables

        internal static string CreateUsersTable =
            "CREATE TABLE IF NOT EXISTS users (" +
            "  id TEXT PRIMARY KEY, " +
            "  email TEXT NOT NULL, " +
            "  password_sha256 TEXT, " +
            "  first_name TEXT, " +
            "  last_name TEXT, " +
            "  is_admin INTEGER NOT NULL DEFAULT 0, " +
            "  active INTEGER NOT NULL DEFAULT 1, " +
            "  created_utc TEXT NOT NULL, " +
            "  last_update_utc TEXT NOT NULL " +
            ")";

        internal static string CreateCredentialsTable =
            "CREATE TABLE IF NOT EXISTS credentials (" +
            "  id TEXT PRIMARY KEY, " +
            "  user_id TEXT NOT NULL, " +
            "  name TEXT, " +
            "  bearer_token TEXT NOT NULL, " +
            "  active INTEGER NOT NULL DEFAULT 1, " +
            "  created_utc TEXT NOT NULL, " +
            "  last_update_utc TEXT NOT NULL " +
            ")";

        internal static string CreateAssistantsTable =
            "CREATE TABLE IF NOT EXISTS assistants (" +
            "  id TEXT PRIMARY KEY, " +
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
            "  collection_id TEXT, " +
            "  retrieval_top_k INTEGER NOT NULL DEFAULT 10, " +
            "  retrieval_score_threshold DOUBLE PRECISION NOT NULL DEFAULT 0.3, " +
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
            "  thread_id TEXT NOT NULL, " +
            "  assistant_id TEXT NOT NULL, " +
            "  collection_id TEXT, " +
            "  user_message_utc TEXT NOT NULL, " +
            "  user_message TEXT, " +
            "  retrieval_start_utc TEXT, " +
            "  retrieval_duration_ms DOUBLE PRECISION NOT NULL DEFAULT 0, " +
            "  retrieval_context TEXT, " +
            "  prompt_sent_utc TEXT, " +
            "  prompt_tokens INTEGER NOT NULL DEFAULT 0, " +
            "  time_to_first_token_ms DOUBLE PRECISION NOT NULL DEFAULT 0, " +
            "  time_to_last_token_ms DOUBLE PRECISION NOT NULL DEFAULT 0, " +
            "  assistant_response TEXT, " +
            "  created_utc TEXT NOT NULL, " +
            "  last_update_utc TEXT NOT NULL " +
            ")";

        #endregion

        #region Indices

        internal static string CreateUsersEmailIndex =
            "CREATE INDEX IF NOT EXISTS idx_users_email ON users (email)";

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
