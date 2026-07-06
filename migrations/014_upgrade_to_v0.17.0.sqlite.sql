-- Migration script for AssistantHub v0.17.0
-- SQLite
-- Retrieval telemetry, answerability settings, and eval chat-rail artifacts

ALTER TABLE assistant_settings ADD COLUMN enable_answerability_check INTEGER NOT NULL DEFAULT 0;
ALTER TABLE assistant_settings ADD COLUMN answerability_inference_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN answerability_mode TEXT DEFAULT 'LogOnly';
ALTER TABLE assistant_settings ADD COLUMN answerability_prompt TEXT;

ALTER TABLE chat_history ADD COLUMN query_class TEXT;
ALTER TABLE chat_history ADD COLUMN answerability_decision TEXT;
ALTER TABLE chat_history ADD COLUMN answerability_reason TEXT;
ALTER TABLE chat_history ADD COLUMN dropped_candidate_count INTEGER;
ALTER TABLE chat_history ADD COLUMN dropped_candidate_summary_json TEXT;
ALTER TABLE chat_history ADD COLUMN final_citation_count INTEGER;

ALTER TABLE eval_runs ADD COLUMN execution_mode TEXT DEFAULT 'ChatRail';
ALTER TABLE eval_runs ADD COLUMN category_filter_json TEXT;

ALTER TABLE eval_results ADD COLUMN chat_history_id TEXT;
ALTER TABLE eval_results ADD COLUMN trace_id TEXT;
ALTER TABLE eval_results ADD COLUMN retrieval_json TEXT;
ALTER TABLE eval_results ADD COLUMN citations_json TEXT;
ALTER TABLE eval_results ADD COLUMN tool_calls_json TEXT;
ALTER TABLE eval_results ADD COLUMN query_class TEXT;
ALTER TABLE eval_results ADD COLUMN answerability_decision TEXT;
