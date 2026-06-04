-- Migration script for AssistantHub v0.12.0
-- SQLite
-- Provider-agnostic assistant performance telemetry
--
-- Run this script once against an existing SQLite database before starting
-- v0.12.0 when startup migrations are not used.

ALTER TABLE chat_history ADD COLUMN trace_id TEXT;
ALTER TABLE chat_history ADD COLUMN request_history_id TEXT;
ALTER TABLE chat_history ADD COLUMN performance_schema_version INTEGER NOT NULL DEFAULT 1;
ALTER TABLE chat_history ADD COLUMN performance_json TEXT;

ALTER TABLE request_history ADD COLUMN trace_id TEXT;
ALTER TABLE request_history ADD COLUMN chat_history_id TEXT;

CREATE TABLE IF NOT EXISTS chat_history_performance_events (
  id TEXT PRIMARY KEY,
  tenant_id TEXT NOT NULL DEFAULT 'default',
  chat_history_id TEXT NOT NULL,
  request_history_id TEXT,
  trace_id TEXT,
  sequence_number INTEGER NOT NULL DEFAULT 0,
  stage TEXT NOT NULL,
  phase TEXT,
  kind TEXT,
  endpoint_id TEXT,
  endpoint_name TEXT,
  endpoint_type TEXT,
  provider TEXT,
  api_format TEXT,
  model TEXT,
  started_utc TEXT,
  finished_utc TEXT,
  duration_ms REAL NOT NULL DEFAULT 0,
  success INTEGER NOT NULL DEFAULT 1,
  http_status_code INTEGER,
  error_type TEXT,
  error_message TEXT,
  input_tokens INTEGER,
  output_tokens INTEGER,
  total_tokens INTEGER,
  chunks_input INTEGER,
  chunks_output INTEGER,
  retrieval_query_count INTEGER,
  endpoint_limiter_wait_ms REAL,
  request_to_headers_ms REAL,
  headers_to_first_token_ms REAL,
  first_token_to_last_token_ms REAL,
  client_total_ms REAL,
  provider_queue_ms REAL,
  provider_load_ms REAL,
  provider_prompt_eval_ms REAL,
  provider_generation_ms REAL,
  provider_total_ms REAL,
  provider_tokens_per_second REAL,
  provider_request_id TEXT,
  metadata_json TEXT,
  provider_metrics_json TEXT,
  provider_raw_json TEXT,
  created_utc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_chat_history_trace_id ON chat_history(trace_id);
CREATE INDEX IF NOT EXISTS idx_chat_history_request_history_id ON chat_history(request_history_id);
CREATE INDEX IF NOT EXISTS idx_request_history_trace_id ON request_history(trace_id);
CREATE INDEX IF NOT EXISTS idx_request_history_chat_history_id ON request_history(chat_history_id);
CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_chat_history_id ON chat_history_performance_events(chat_history_id);
CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_request_history_id ON chat_history_performance_events(request_history_id);
CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_trace_id ON chat_history_performance_events(trace_id);
CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_stage ON chat_history_performance_events(stage);
CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_started_utc ON chat_history_performance_events(started_utc);
CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_tenant_id ON chat_history_performance_events(tenant_id);
CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_endpoint_id ON chat_history_performance_events(endpoint_id);
CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_provider_model ON chat_history_performance_events(provider, model);
CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_created_utc ON chat_history_performance_events(created_utc);
CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_duration_ms ON chat_history_performance_events(duration_ms);
CREATE INDEX IF NOT EXISTS idx_chat_history_performance_events_tenant_created ON chat_history_performance_events(tenant_id, created_utc);
