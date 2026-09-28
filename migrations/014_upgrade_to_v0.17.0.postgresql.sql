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

-- Hybrid fusion, recency and prompt context order settings
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS fusion_strategy TEXT DEFAULT 'Rrf';
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS rrf_k INTEGER NOT NULL DEFAULT 60;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS fusion_candidate_pool INTEGER;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS recency_weight DOUBLE PRECISION NOT NULL DEFAULT 0;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS context_order TEXT DEFAULT 'Score';

-- Reranking, conversation rewrite, supersession, duplicate detection and eval judge settings
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS eval_judge_inference_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS embedding_task_prefixes BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS enable_conversation_rewrite BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS conversation_rewrite_prompt TEXT;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS reranker_type TEXT DEFAULT 'Llm';
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS rerank_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS rerank_candidate_count INTEGER NOT NULL DEFAULT 20;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS rerank_min_score DOUBLE PRECISION;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS supersession_mode TEXT DEFAULT 'Demote';
ALTER TABLE assistant_documents ADD COLUMN IF NOT EXISTS supersedes_json TEXT;
ALTER TABLE assistant_documents ADD COLUMN IF NOT EXISTS superseded_by TEXT;
ALTER TABLE assistant_documents ADD COLUMN IF NOT EXISTS content_sha256 TEXT;
ALTER TABLE assistant_documents ADD COLUMN IF NOT EXISTS near_duplicates_json TEXT;
