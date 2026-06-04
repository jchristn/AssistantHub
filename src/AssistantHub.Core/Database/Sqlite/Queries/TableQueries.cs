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
                "  enable_reranking INTEGER NOT NULL DEFAULT 0, " +
                "  reranker_top_k INTEGER NOT NULL DEFAULT 5, " +
                "  reranker_score_threshold REAL NOT NULL DEFAULT 3.0, " +
                "  rerank_prompt TEXT, " +
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
                "  retrieval_gate_inference_endpoint_id TEXT, " +
                "  query_rewrite_inference_endpoint_id TEXT, " +
                "  rerank_inference_endpoint_id TEXT, " +
                "  embedding_endpoint_id TEXT, " +
                "  title TEXT, " +
                "  logo_url TEXT, " +
                "  favicon_url TEXT, " +
                "  retrieval_label_filter TEXT, " +
                "  retrieval_tag_filter TEXT, " +
                "  eval_judge_prompt TEXT, " +
                "  streaming INTEGER NOT NULL DEFAULT 1, " +
                "  enable_slack INTEGER NOT NULL DEFAULT 0, " +
                "  slack_app_token TEXT, " +
                "  slack_bot_token TEXT, " +
                "  slack_channel_id TEXT, " +
                "  slack_message_prefix TEXT, " +
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
                "  crawl_plan_id TEXT, " +
                "  crawl_operation_id TEXT, " +
                "  source_url TEXT, " +
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
                "CREATE TABLE IF NOT EXISTS crawl_plans (" +
                "  id TEXT PRIMARY KEY, " +
                "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
                "  name TEXT NOT NULL, " +
                "  repository_type TEXT NOT NULL DEFAULT 'Web', " +
                "  ingestion_settings_json TEXT, " +
                "  repository_settings_json TEXT, " +
                "  schedule_json TEXT, " +
                "  filter_json TEXT, " +
                "  process_additions INTEGER NOT NULL DEFAULT 1, " +
                "  process_updates INTEGER NOT NULL DEFAULT 1, " +
                "  process_deletions INTEGER NOT NULL DEFAULT 0, " +
                "  max_drain_tasks INTEGER NOT NULL DEFAULT 8, " +
                "  retention_days INTEGER NOT NULL DEFAULT 7, " +
                "  state TEXT NOT NULL DEFAULT 'Stopped', " +
                "  last_crawl_start_utc TEXT, " +
                "  last_crawl_finish_utc TEXT, " +
                "  last_crawl_success INTEGER, " +
                "  created_utc TEXT NOT NULL, " +
                "  last_update_utc TEXT NOT NULL" +
                "); " +
                "CREATE TABLE IF NOT EXISTS crawl_operations (" +
                "  id TEXT PRIMARY KEY, " +
                "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
                "  crawl_plan_id TEXT NOT NULL, " +
                "  state TEXT NOT NULL DEFAULT 'NotStarted', " +
                "  status_message TEXT, " +
                "  objects_enumerated INTEGER NOT NULL DEFAULT 0, " +
                "  bytes_enumerated INTEGER NOT NULL DEFAULT 0, " +
                "  objects_added INTEGER NOT NULL DEFAULT 0, " +
                "  bytes_added INTEGER NOT NULL DEFAULT 0, " +
                "  objects_updated INTEGER NOT NULL DEFAULT 0, " +
                "  bytes_updated INTEGER NOT NULL DEFAULT 0, " +
                "  objects_deleted INTEGER NOT NULL DEFAULT 0, " +
                "  bytes_deleted INTEGER NOT NULL DEFAULT 0, " +
                "  objects_success INTEGER NOT NULL DEFAULT 0, " +
                "  bytes_success INTEGER NOT NULL DEFAULT 0, " +
                "  objects_failed INTEGER NOT NULL DEFAULT 0, " +
                "  bytes_failed INTEGER NOT NULL DEFAULT 0, " +
                "  enumeration_file TEXT, " +
                "  start_utc TEXT, " +
                "  start_enumeration_utc TEXT, " +
                "  finish_enumeration_utc TEXT, " +
                "  start_retrieval_utc TEXT, " +
                "  finish_retrieval_utc TEXT, " +
                "  finish_utc TEXT, " +
                "  created_utc TEXT NOT NULL, " +
                "  last_update_utc TEXT NOT NULL" +
                "); " +
                "CREATE TABLE IF NOT EXISTS chat_history (" +
                "  id TEXT PRIMARY KEY, " +
                "  trace_id TEXT, " +
                "  request_history_id TEXT, " +
                "  performance_schema_version INTEGER NOT NULL DEFAULT 1, " +
                "  performance_json TEXT, " +
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
                "  rerank_duration_ms REAL NOT NULL DEFAULT 0, " +
                "  rerank_input_count INTEGER NOT NULL DEFAULT 0, " +
                "  rerank_output_count INTEGER NOT NULL DEFAULT 0, " +
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
                "  metadata_filter TEXT, " +
                "  origin TEXT, " +
                "  assistant_response TEXT, " +
                "  created_utc TEXT NOT NULL, " +
                "  last_update_utc TEXT NOT NULL" +
                "); " +
                "CREATE TABLE IF NOT EXISTS request_history (" +
                "  id TEXT PRIMARY KEY, " +
                "  trace_id TEXT, " +
                "  chat_history_id TEXT, " +
                "  tenant_id TEXT, " +
                "  user_id TEXT, " +
                "  credential_id TEXT, " +
                "  assistant_id TEXT, " +
                "  thread_id TEXT, " +
                "  principal_name TEXT, " +
                "  request_type TEXT NOT NULL DEFAULT 'SystemApi', " +
                "  source_type TEXT NOT NULL DEFAULT 'api', " +
                "  http_method TEXT NOT NULL, " +
                "  route_template TEXT, " +
                "  request_path TEXT NOT NULL, " +
                "  request_url TEXT NOT NULL, " +
                "  source_ip TEXT, " +
                "  status_code INTEGER NOT NULL DEFAULT 0, " +
                "  success INTEGER NOT NULL DEFAULT 0, " +
                "  duration_ms REAL NOT NULL DEFAULT 0, " +
                "  request_content_type TEXT, " +
                "  response_content_type TEXT, " +
                "  request_size_bytes INTEGER NOT NULL DEFAULT 0, " +
                "  response_size_bytes INTEGER NOT NULL DEFAULT 0, " +
                "  request_body_truncated INTEGER NOT NULL DEFAULT 0, " +
                "  response_body_truncated INTEGER NOT NULL DEFAULT 0, " +
                "  request_body_is_binary INTEGER NOT NULL DEFAULT 0, " +
                "  response_body_is_binary INTEGER NOT NULL DEFAULT 0, " +
                "  route_parameters_json TEXT, " +
                "  query_parameters_json TEXT, " +
                "  request_headers_json TEXT, " +
                "  response_headers_json TEXT, " +
                "  request_body TEXT, " +
                "  response_body TEXT, " +
                "  created_utc TEXT NOT NULL, " +
                "  last_update_utc TEXT NOT NULL" +
                "); " +
                "CREATE TABLE IF NOT EXISTS chat_history_performance_events (" +
                "  id TEXT PRIMARY KEY, " +
                "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
                "  chat_history_id TEXT NOT NULL, " +
                "  request_history_id TEXT, " +
                "  trace_id TEXT, " +
                "  sequence_number INTEGER NOT NULL DEFAULT 0, " +
                "  stage TEXT NOT NULL, " +
                "  phase TEXT, " +
                "  kind TEXT, " +
                "  endpoint_id TEXT, " +
                "  endpoint_name TEXT, " +
                "  endpoint_type TEXT, " +
                "  provider TEXT, " +
                "  api_format TEXT, " +
                "  model TEXT, " +
                "  started_utc TEXT, " +
                "  finished_utc TEXT, " +
                "  duration_ms REAL NOT NULL DEFAULT 0, " +
                "  success INTEGER NOT NULL DEFAULT 1, " +
                "  http_status_code INTEGER, " +
                "  error_type TEXT, " +
                "  error_message TEXT, " +
                "  input_tokens INTEGER, " +
                "  output_tokens INTEGER, " +
                "  total_tokens INTEGER, " +
                "  chunks_input INTEGER, " +
                "  chunks_output INTEGER, " +
                "  retrieval_query_count INTEGER, " +
                "  endpoint_limiter_wait_ms REAL, " +
                "  request_to_headers_ms REAL, " +
                "  headers_to_first_token_ms REAL, " +
                "  first_token_to_last_token_ms REAL, " +
                "  client_total_ms REAL, " +
                "  provider_queue_ms REAL, " +
                "  provider_load_ms REAL, " +
                "  provider_prompt_eval_ms REAL, " +
                "  provider_generation_ms REAL, " +
                "  provider_total_ms REAL, " +
                "  provider_tokens_per_second REAL, " +
                "  provider_request_id TEXT, " +
                "  metadata_json TEXT, " +
                "  provider_metrics_json TEXT, " +
                "  provider_raw_json TEXT, " +
                "  created_utc TEXT NOT NULL" +
                "); " +
                "CREATE TABLE IF NOT EXISTS eval_facts (" +
                "  id TEXT PRIMARY KEY, " +
                "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
                "  assistant_id TEXT NOT NULL, " +
                "  category TEXT, " +
                "  question TEXT, " +
                "  expected_facts TEXT, " +
                "  created_utc TEXT NOT NULL, " +
                "  last_update_utc TEXT NOT NULL" +
                "); " +
                "CREATE TABLE IF NOT EXISTS eval_runs (" +
                "  id TEXT PRIMARY KEY, " +
                "  tenant_id TEXT NOT NULL DEFAULT 'default', " +
                "  assistant_id TEXT NOT NULL, " +
                "  status TEXT NOT NULL DEFAULT 'Pending', " +
                "  total_facts INTEGER NOT NULL DEFAULT 0, " +
                "  facts_evaluated INTEGER NOT NULL DEFAULT 0, " +
                "  facts_passed INTEGER NOT NULL DEFAULT 0, " +
                "  facts_failed INTEGER NOT NULL DEFAULT 0, " +
                "  pass_rate REAL NOT NULL DEFAULT 0, " +
                "  judge_prompt TEXT, " +
                "  started_utc TEXT, " +
                "  completed_utc TEXT, " +
                "  created_utc TEXT NOT NULL" +
                "); " +
                "CREATE TABLE IF NOT EXISTS eval_results (" +
                "  id TEXT PRIMARY KEY, " +
                "  run_id TEXT NOT NULL, " +
                "  fact_id TEXT NOT NULL, " +
                "  question TEXT, " +
                "  expected_facts TEXT, " +
                "  llm_response TEXT, " +
                "  fact_verdicts TEXT, " +
                "  overall_pass INTEGER NOT NULL DEFAULT 0, " +
                "  duration_ms INTEGER NOT NULL DEFAULT 0, " +
                "  created_utc TEXT NOT NULL" +
                "); ";
        }

        /// <summary>
        /// Add the retrieval gate inference endpoint ID column.
        /// </summary>
        public static string AddAssistantSettingsRetrievalGateInferenceEndpointIdColumn =
            "ALTER TABLE assistant_settings ADD COLUMN retrieval_gate_inference_endpoint_id TEXT;";

        /// <summary>
        /// Add the query rewrite inference endpoint ID column.
        /// </summary>
        public static string AddAssistantSettingsQueryRewriteInferenceEndpointIdColumn =
            "ALTER TABLE assistant_settings ADD COLUMN query_rewrite_inference_endpoint_id TEXT;";

        /// <summary>
        /// Add the rerank inference endpoint ID column.
        /// </summary>
        public static string AddAssistantSettingsRerankInferenceEndpointIdColumn =
            "ALTER TABLE assistant_settings ADD COLUMN rerank_inference_endpoint_id TEXT;";

        /// <summary>
        /// Add the trace ID column to chat history.
        /// </summary>
        public static string AddChatHistoryTraceIdColumn =
            "ALTER TABLE chat_history ADD COLUMN trace_id TEXT;";

        /// <summary>
        /// Add the request-history ID column to chat history.
        /// </summary>
        public static string AddChatHistoryRequestHistoryIdColumn =
            "ALTER TABLE chat_history ADD COLUMN request_history_id TEXT;";

        /// <summary>
        /// Add the performance schema-version column to chat history.
        /// </summary>
        public static string AddChatHistoryPerformanceSchemaVersionColumn =
            "ALTER TABLE chat_history ADD COLUMN performance_schema_version INTEGER NOT NULL DEFAULT 1;";

        /// <summary>
        /// Add the performance JSON column to chat history.
        /// </summary>
        public static string AddChatHistoryPerformanceJsonColumn =
            "ALTER TABLE chat_history ADD COLUMN performance_json TEXT;";

        /// <summary>
        /// Add the trace ID column to request history.
        /// </summary>
        public static string AddRequestHistoryTraceIdColumn =
            "ALTER TABLE request_history ADD COLUMN trace_id TEXT;";

        /// <summary>
        /// Add the chat-history ID column to request history.
        /// </summary>
        public static string AddRequestHistoryChatHistoryIdColumn =
            "ALTER TABLE request_history ADD COLUMN chat_history_id TEXT;";

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
                "CREATE INDEX IF NOT EXISTS idx_chat_history_tenant_id ON chat_history(tenant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_trace_id ON chat_history(trace_id); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_request_history_id ON chat_history(request_history_id); " +
                "CREATE INDEX IF NOT EXISTS idx_request_history_tenant_id ON request_history(tenant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_request_history_user_id ON request_history(user_id); " +
                "CREATE INDEX IF NOT EXISTS idx_request_history_credential_id ON request_history(credential_id); " +
                "CREATE INDEX IF NOT EXISTS idx_request_history_assistant_id ON request_history(assistant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_request_history_thread_id ON request_history(thread_id); " +
                "CREATE INDEX IF NOT EXISTS idx_request_history_status_code ON request_history(status_code); " +
                "CREATE INDEX IF NOT EXISTS idx_request_history_success ON request_history(success); " +
                "CREATE INDEX IF NOT EXISTS idx_request_history_created_utc ON request_history(created_utc); " +
                "CREATE INDEX IF NOT EXISTS idx_request_history_request_path ON request_history(request_path); " +
                "CREATE INDEX IF NOT EXISTS idx_request_history_trace_id ON request_history(trace_id); " +
                "CREATE INDEX IF NOT EXISTS idx_request_history_chat_history_id ON request_history(chat_history_id); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_chat_history_id ON chat_history_performance_events(chat_history_id); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_request_history_id ON chat_history_performance_events(request_history_id); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_trace_id ON chat_history_performance_events(trace_id); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_stage ON chat_history_performance_events(stage); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_started_utc ON chat_history_performance_events(started_utc); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_tenant_id ON chat_history_performance_events(tenant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_endpoint_id ON chat_history_performance_events(endpoint_id); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_provider_model ON chat_history_performance_events(provider, model); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_created_utc ON chat_history_performance_events(created_utc); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_duration_ms ON chat_history_performance_events(duration_ms); " +
                "CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_tenant_created ON chat_history_performance_events(tenant_id, created_utc); " +
                "CREATE INDEX IF NOT EXISTS idx_crawl_plans_tenant_id ON crawl_plans(tenant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_crawl_plans_state ON crawl_plans(state); " +
                "CREATE INDEX IF NOT EXISTS idx_crawl_operations_tenant_id ON crawl_operations(tenant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_crawl_operations_crawl_plan_id ON crawl_operations(crawl_plan_id); " +
                "CREATE INDEX IF NOT EXISTS idx_crawl_operations_created_utc ON crawl_operations(created_utc); " +
                "CREATE INDEX IF NOT EXISTS idx_assistant_documents_crawl_plan_id ON assistant_documents(crawl_plan_id); " +
                "CREATE INDEX IF NOT EXISTS idx_assistant_documents_crawl_operation_id ON assistant_documents(crawl_operation_id); " +
                "CREATE INDEX IF NOT EXISTS idx_eval_facts_tenant_id ON eval_facts(tenant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_eval_facts_assistant_id ON eval_facts(assistant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_eval_facts_category ON eval_facts(category); " +
                "CREATE INDEX IF NOT EXISTS idx_eval_facts_created_utc ON eval_facts(created_utc); " +
                "CREATE INDEX IF NOT EXISTS idx_eval_runs_tenant_id ON eval_runs(tenant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_eval_runs_assistant_id ON eval_runs(assistant_id); " +
                "CREATE INDEX IF NOT EXISTS idx_eval_runs_status ON eval_runs(status); " +
                "CREATE INDEX IF NOT EXISTS idx_eval_runs_created_utc ON eval_runs(created_utc); " +
                "CREATE INDEX IF NOT EXISTS idx_eval_results_run_id ON eval_results(run_id); " +
                "CREATE INDEX IF NOT EXISTS idx_eval_results_fact_id ON eval_results(fact_id); ";
        }

    }
}
