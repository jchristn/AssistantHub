-- Migration script for AssistantHub v0.13.0
-- PostgreSQL
-- Assistant analytics indexes and performance-event assistant backfill

ALTER TABLE chat_history_performance_events ADD COLUMN IF NOT EXISTS assistant_id TEXT;

UPDATE chat_history_performance_events e
SET assistant_id = h.assistant_id
FROM chat_history h
WHERE e.assistant_id IS NULL
  AND h.id = e.chat_history_id;

CREATE INDEX IF NOT EXISTS idx_assistant_feedback_tenant_assistant_created ON assistant_feedback(tenant_id, assistant_id, created_utc);
CREATE INDEX IF NOT EXISTS idx_request_history_tenant_assistant_created ON request_history(tenant_id, assistant_id, created_utc);
CREATE INDEX IF NOT EXISTS idx_request_history_tenant_assistant_success_created ON request_history(tenant_id, assistant_id, success, created_utc);
CREATE INDEX IF NOT EXISTS idx_chpe_assistant_id ON chat_history_performance_events(assistant_id);
CREATE INDEX IF NOT EXISTS idx_chpe_tenant_assistant_created ON chat_history_performance_events(tenant_id, assistant_id, created_utc);
CREATE INDEX IF NOT EXISTS idx_chpe_tenant_assistant_stage_created ON chat_history_performance_events(tenant_id, assistant_id, stage, created_utc);
CREATE INDEX IF NOT EXISTS idx_chpe_tenant_assistant_endpoint_created ON chat_history_performance_events(tenant_id, assistant_id, endpoint_id, created_utc);
