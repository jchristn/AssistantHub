-- Migration script for AssistantHub v0.13.0
-- SQLite
-- Assistant analytics indexes and performance-event assistant backfill

ALTER TABLE chat_history_performance_events ADD COLUMN assistant_id TEXT;

UPDATE chat_history_performance_events
SET assistant_id = (
  SELECT assistant_id
  FROM chat_history
  WHERE chat_history.id = chat_history_performance_events.chat_history_id
)
WHERE assistant_id IS NULL
  AND chat_history_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_assistant_feedback_tenant_assistant_created ON assistant_feedback(tenant_id, assistant_id, created_utc);
CREATE INDEX IF NOT EXISTS idx_request_history_tenant_assistant_created ON request_history(tenant_id, assistant_id, created_utc);
CREATE INDEX IF NOT EXISTS idx_request_history_tenant_assistant_success_created ON request_history(tenant_id, assistant_id, success, created_utc);
CREATE INDEX IF NOT EXISTS idx_chpe_assistant_id ON chat_history_performance_events(assistant_id);
CREATE INDEX IF NOT EXISTS idx_chpe_tenant_assistant_created ON chat_history_performance_events(tenant_id, assistant_id, created_utc);
CREATE INDEX IF NOT EXISTS idx_chpe_tenant_assistant_stage_created ON chat_history_performance_events(tenant_id, assistant_id, stage, created_utc);
CREATE INDEX IF NOT EXISTS idx_chpe_tenant_assistant_endpoint_created ON chat_history_performance_events(tenant_id, assistant_id, endpoint_id, created_utc);
