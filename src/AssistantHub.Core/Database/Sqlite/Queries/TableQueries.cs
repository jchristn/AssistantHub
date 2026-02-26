namespace AssistantHub.Core.Database.Sqlite.Queries
{
    /// <summary>
    /// SQLite table creation queries.
    /// </summary>
    public static class TableQueries
    {
        /// <summary>
        /// Get the CREATE TABLE statements.
        /// </summary>
        public static string CreateTables()
        {
            return
                "CREATE TABLE IF NOT EXISTS tenants (" +
                "  id TEXT PRIMARY KEY, " +
                "  name TEXT NOT NULL, " +
                "  active INTEGER NOT NULL DEFAULT 1, " +
                "  is_protected INTEGER NOT NULL DEFAULT 0, " +
                "  labels_json TEXT, " +
                "  tags_json TEXT, " +
                "  created_utc TEXT NOT NULL, " +
                "  last_update_utc TEXT NOT NULL" +
                "); " +
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
                "  last_update_utc TEXT NOT NULL" +
                "); " +
                "CREATE TABLE IF NOT EXISTS credentials (" +
                "  id TEXT PRIMARY KEY, " +
                "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
                "  user_id TEXT NOT NULL, " +
                "  name TEXT, " +
                "  bearer_token TEXT NOT NULL, " +
                "  active INTEGER NOT NULL DEFAULT 1, " +
                "  is_protected INTEGER NOT NULL DEFAULT 0, " +
                "  created_utc TEXT NOT NULL, " +
                "  last_update_utc TEXT NOT NULL" +
                "); " +
                "CREATE TABLE IF NOT EXISTS assistants (" +
                "  id TEXT PRIMARY KEY, " +
                "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
                "  user_id TEXT NOT NULL, " +
                "  name TEXT NOT NULL, " +
                "  description TEXT, " +
                "  active INTEGER NOT NULL DEFAULT 1, " +
                "  created_utc TEXT NOT NULL, " +
                "  last_update_utc TEXT NOT NULL" +
                "); " +
                "CREATE TABLE IF NOT EXISTS assistant_settings (" +
                "  id TEXT PRIMARY KEY, " +
                "  assistant_id TEXT NOT NULL, " +
                "  temperature REAL NOT NULL DEFAULT 0.7, " +
                "  top_p REAL NOT NULL DEFAULT 1.0, " +
                "  system_prompt TEXT, " +
                "  max_tokens INTEGER NOT NULL DEFAULT 4096, " +
                "  context_window INTEGER NOT NULL DEFAULT 8192, " +
                "  model TEXT NOT NULL DEFAULT 'gemma3:4b', " +
                "  enable_rag INTEGER NOT NULL DEFAULT 0, " +
                "  enable_retrieval_gate INTEGER NOT NULL DEFAULT 0, " +
                "  enable_query_rewrite INTEGER NOT NULL DEFAULT 0, " +
                "  query_rewrite_prompt TEXT, " +
                "  enable_citations INTEGER NOT NULL DEFAULT 0, " +
                "  citation_link_mode TEXT DEFAULT 'None', " +
                "  collection_id TEXT, " +
                "  retrieval_top_k INTEGER NOT NULL DEFAULT 10, " +
                "  retrieval_score_threshold REAL NOT NULL DEFAULT 0.3, " +
                "  search_mode TEXT DEFAULT 'Vector', " +
                "  text_weight REAL DEFAULT 0.3, " +
                "  fulltext_search_type TEXT DEFAULT 'TsRank', " +
                "  fulltext_language TEXT DEFAULT 'english', " +
                "  fulltext_normalization INTEGER DEFAULT 32, " +
                "  fulltext_minimum_score REAL DEFAULT NULL, " +
                "  retrieval_include_neighbors INTEGER NOT NULL DEFAULT 0, " +
                "  inference_endpoint_id TEXT, " +
                "  embedding_endpoint_id TEXT, " +
                "  title TEXT, " +
                "  logo_url TEXT, " +
                "  favicon_url TEXT, " +
                "  streaming INTEGER NOT NULL DEFAULT 1, " +
                "  created_utc TEXT NOT NULL, " +
                "  last_update_utc TEXT NOT NULL" +
                "); " +
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
                "  last_update_utc TEXT NOT NULL" +
                "); " +
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
                "  last_update_utc TEXT NOT NULL" +
                "); " +
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
                "  last_update_utc TEXT NOT NULL" +
                "); " +
                "CREATE TABLE IF NOT EXISTS chat_history (" +
                "  id TEXT PRIMARY KEY, " +
                "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
                "  thread_id TEXT NOT NULL, " +
                "  assistant_id TEXT NOT NULL, " +
                "  collection_id TEXT, " +
                "  user_message_utc TEXT NOT NULL, " +
                "  user_message TEXT, " +
                "  retrieval_start_utc TEXT, " +
                "  retrieval_duration_ms REAL NOT NULL DEFAULT 0, " +
                "  retrieval_gate_decision TEXT, " +
                "  retrieval_gate_duration_ms REAL NOT NULL DEFAULT 0, " +
                "  query_rewrite_result TEXT, " +
                "  query_rewrite_duration_ms REAL NOT NULL DEFAULT 0, " +
                "  retrieval_context TEXT, " +
                "  prompt_sent_utc TEXT, " +
                "  prompt_tokens INTEGER NOT NULL DEFAULT 0, " +
                "  endpoint_resolution_duration_ms REAL NOT NULL DEFAULT 0, " +
                "  compaction_duration_ms REAL NOT NULL DEFAULT 0, " +
                "  inference_connection_duration_ms REAL NOT NULL DEFAULT 0, " +
                "  time_to_first_token_ms REAL NOT NULL DEFAULT 0, " +
                "  time_to_last_token_ms REAL NOT NULL DEFAULT 0, " +
                "  completion_tokens INTEGER NOT NULL DEFAULT 0, " +
                "  tokens_per_second_overall REAL NOT NULL DEFAULT 0, " +
                "  tokens_per_second_generation REAL NOT NULL DEFAULT 0, " +
                "  assistant_response TEXT, " +
                "  created_utc TEXT NOT NULL, " +
                "  last_update_utc TEXT NOT NULL" +
                "); ";
        }

        /// <summary>
        /// Get the CREATE INDEX statements.
        /// </summary>
        public static string CreateIndices()
        {
            return
                "CREATE INDEX IF NOT EXISTS idx_tenants_name ON tenants(name); " +
                "CREATE INDEX IF NOT EXISTS idx_tenants_created_utc ON tenants(created_utc); " +
                "CREATE INDEX IF NOT EXISTS idx_users_email ON users (email); " +
                "CREATE INDEX IF NOT EXISTS idx_users_tenant_id ON users(tenant_id); " +
                "CREATE UNIQUE INDEX IF NOT EXISTS idx_users_tenant_email ON users(tenant_id, email); " +
                "CREATE UNIQUE INDEX IF NOT EXISTS idx_credentials_bearer_token ON credentials (bearer_token); " +
                "CREATE INDEX IF NOT EXISTS idx_credentials_user_id ON credentials (user_id); " +
                "CREATE INDEX IF NOT EXISTS idx_credentials_tenant_id ON credentials(tenant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_assistants_user_id ON assistants (user_id); " +
                "CREATE INDEX IF NOT EXISTS idx_assistants_tenant_id ON assistants(tenant_id); " +
                "CREATE UNIQUE INDEX IF NOT EXISTS idx_assistant_settings_assistant_id ON assistant_settings (assistant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_assistant_documents_status ON assistant_documents (status); " +
                "CREATE INDEX IF NOT EXISTS idx_assistant_documents_tenant_id ON assistant_documents(tenant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_assistant_feedback_assistant_id ON assistant_feedback (assistant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_assistant_feedback_tenant_id ON assistant_feedback(tenant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_ingestion_rules_name ON ingestion_rules (name); " +
                "CREATE INDEX IF NOT EXISTS idx_ingestion_rules_tenant_id ON ingestion_rules(tenant_id); " +
                "CREATE UNIQUE INDEX IF NOT EXISTS idx_ingestion_rules_tenant_name ON ingestion_rules(tenant_id, name); " +
                "CREATE INDEX IF NOT EXISTS idx_assistant_documents_ingestion_rule_id ON assistant_documents (ingestion_rule_id); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_assistant_id ON chat_history (assistant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_thread_id ON chat_history (thread_id); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_created_utc ON chat_history (created_utc); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_tenant_id ON chat_history(tenant_id); ";
        }

    }
}
