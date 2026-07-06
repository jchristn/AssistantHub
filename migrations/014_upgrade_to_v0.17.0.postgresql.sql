-- Migration script for AssistantHub v0.17.0
-- PostgreSQL
-- Retrieval telemetry, answerability settings, and eval chat-rail artifacts

ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS enable_answerability_check BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS answerability_inference_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS answerability_mode TEXT DEFAULT 'LogOnly';
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS answerability_prompt TEXT;

ALTER TABLE chat_history ADD COLUMN IF NOT EXISTS query_class TEXT;
ALTER TABLE chat_history ADD COLUMN IF NOT EXISTS answerability_decision TEXT;
ALTER TABLE chat_history ADD COLUMN IF NOT EXISTS answerability_reason TEXT;
ALTER TABLE chat_history ADD COLUMN IF NOT EXISTS dropped_candidate_count INTEGER;
ALTER TABLE chat_history ADD COLUMN IF NOT EXISTS dropped_candidate_summary_json TEXT;
ALTER TABLE chat_history ADD COLUMN IF NOT EXISTS final_citation_count INTEGER;
