-- Migration script for AssistantHub v0.17.0
-- SQL Server
-- Retrieval telemetry and answerability settings

IF COL_LENGTH('assistant_settings', 'enable_answerability_check') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD enable_answerability_check BIT NOT NULL DEFAULT 0;
END
GO

IF COL_LENGTH('assistant_settings', 'answerability_inference_endpoint_id') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD answerability_inference_endpoint_id NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('assistant_settings', 'answerability_mode') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD answerability_mode NVARCHAR(32) NULL DEFAULT 'LogOnly';
END
GO

IF COL_LENGTH('assistant_settings', 'answerability_prompt') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD answerability_prompt NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('chat_history', 'query_class') IS NULL
BEGIN
    ALTER TABLE chat_history ADD query_class NVARCHAR(64) NULL;
END
GO

IF COL_LENGTH('chat_history', 'answerability_decision') IS NULL
BEGIN
    ALTER TABLE chat_history ADD answerability_decision NVARCHAR(64) NULL;
END
GO

IF COL_LENGTH('chat_history', 'answerability_reason') IS NULL
BEGIN
    ALTER TABLE chat_history ADD answerability_reason NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('chat_history', 'dropped_candidate_count') IS NULL
BEGIN
    ALTER TABLE chat_history ADD dropped_candidate_count INT NULL;
END
GO

IF COL_LENGTH('chat_history', 'dropped_candidate_summary_json') IS NULL
BEGIN
    ALTER TABLE chat_history ADD dropped_candidate_summary_json NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('chat_history', 'final_citation_count') IS NULL
BEGIN
    ALTER TABLE chat_history ADD final_citation_count INT NULL;
END
GO

-- Hybrid fusion, recency and prompt context order settings
IF COL_LENGTH('assistant_settings', 'fusion_strategy') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD fusion_strategy NVARCHAR(32) NULL DEFAULT 'Rrf';
END
GO

IF COL_LENGTH('assistant_settings', 'rrf_k') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD rrf_k INT NOT NULL DEFAULT 60;
END
GO

IF COL_LENGTH('assistant_settings', 'fusion_candidate_pool') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD fusion_candidate_pool INT NULL;
END
GO

IF COL_LENGTH('assistant_settings', 'recency_weight') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD recency_weight FLOAT NOT NULL DEFAULT 0;
END
GO

IF COL_LENGTH('assistant_settings', 'context_order') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD context_order NVARCHAR(32) NULL DEFAULT 'Score';
END
GO

-- Reranking, conversation rewrite, supersession, duplicate detection and eval judge settings
IF COL_LENGTH('assistant_settings', 'eval_judge_inference_endpoint_id') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD eval_judge_inference_endpoint_id NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('assistant_settings', 'embedding_task_prefixes') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD embedding_task_prefixes BIT NOT NULL DEFAULT 0;
END
GO

IF COL_LENGTH('assistant_settings', 'enable_conversation_rewrite') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD enable_conversation_rewrite BIT NOT NULL DEFAULT 0;
END
GO

IF COL_LENGTH('assistant_settings', 'conversation_rewrite_prompt') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD conversation_rewrite_prompt NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('assistant_settings', 'reranker_type') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD reranker_type NVARCHAR(32) NULL DEFAULT 'Llm';
END
GO

IF COL_LENGTH('assistant_settings', 'rerank_endpoint_id') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD rerank_endpoint_id NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('assistant_settings', 'rerank_candidate_count') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD rerank_candidate_count INT NOT NULL DEFAULT 20;
END
GO

IF COL_LENGTH('assistant_settings', 'rerank_min_score') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD rerank_min_score FLOAT NULL;
END
GO

IF COL_LENGTH('assistant_settings', 'supersession_mode') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD supersession_mode NVARCHAR(32) NULL DEFAULT 'Demote';
END
GO

IF COL_LENGTH('assistant_documents', 'supersedes_json') IS NULL
BEGIN
    ALTER TABLE assistant_documents ADD supersedes_json NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('assistant_documents', 'superseded_by') IS NULL
BEGIN
    ALTER TABLE assistant_documents ADD superseded_by NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('assistant_documents', 'content_sha256') IS NULL
BEGIN
    ALTER TABLE assistant_documents ADD content_sha256 NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('assistant_documents', 'near_duplicates_json') IS NULL
BEGIN
    ALTER TABLE assistant_documents ADD near_duplicates_json NVARCHAR(MAX) NULL;
END
GO
