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

-- Hybrid fusion, recency and prompt context order settings
ALTER TABLE assistant_settings ADD COLUMN fusion_strategy TEXT DEFAULT 'Rrf';
ALTER TABLE assistant_settings ADD COLUMN rrf_k INTEGER NOT NULL DEFAULT 60;
ALTER TABLE assistant_settings ADD COLUMN fusion_candidate_pool INTEGER;
ALTER TABLE assistant_settings ADD COLUMN recency_weight REAL NOT NULL DEFAULT 0;
ALTER TABLE assistant_settings ADD COLUMN context_order TEXT DEFAULT 'Score';

-- Reranking, conversation rewrite, supersession, duplicate detection and eval judge settings
ALTER TABLE assistant_settings ADD COLUMN eval_judge_inference_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN embedding_task_prefixes INTEGER NOT NULL DEFAULT 0;
ALTER TABLE assistant_settings ADD COLUMN enable_conversation_rewrite INTEGER NOT NULL DEFAULT 0;
ALTER TABLE assistant_settings ADD COLUMN conversation_rewrite_prompt TEXT;
ALTER TABLE assistant_settings ADD COLUMN reranker_type TEXT DEFAULT 'Llm';
ALTER TABLE assistant_settings ADD COLUMN rerank_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN rerank_candidate_count INTEGER NOT NULL DEFAULT 20;
ALTER TABLE assistant_settings ADD COLUMN rerank_min_score REAL;
ALTER TABLE assistant_settings ADD COLUMN supersession_mode TEXT DEFAULT 'Demote';
ALTER TABLE assistant_documents ADD COLUMN supersedes_json TEXT;
ALTER TABLE assistant_documents ADD COLUMN superseded_by TEXT;
ALTER TABLE assistant_documents ADD COLUMN content_sha256 TEXT;
ALTER TABLE assistant_documents ADD COLUMN near_duplicates_json TEXT;
