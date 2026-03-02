-- Migration 004: Upgrade existing v0.5.0 database to v0.6.0
-- Adds re-ranking columns to assistant_settings and chat_history
-- Back up your database before running this migration.

------------------------------------------------------------------------
-- SQLite
------------------------------------------------------------------------
ALTER TABLE assistant_settings ADD COLUMN enable_reranking INTEGER NOT NULL DEFAULT 0;
ALTER TABLE assistant_settings ADD COLUMN reranker_top_k INTEGER NOT NULL DEFAULT 5;
ALTER TABLE assistant_settings ADD COLUMN reranker_score_threshold REAL NOT NULL DEFAULT 3.0;
ALTER TABLE assistant_settings ADD COLUMN rerank_prompt TEXT;

ALTER TABLE chat_history ADD COLUMN rerank_duration_ms REAL NOT NULL DEFAULT 0;
ALTER TABLE chat_history ADD COLUMN rerank_input_count INTEGER NOT NULL DEFAULT 0;
ALTER TABLE chat_history ADD COLUMN rerank_output_count INTEGER NOT NULL DEFAULT 0;

------------------------------------------------------------------------
-- PostgreSQL
------------------------------------------------------------------------
-- ALTER TABLE assistant_settings ADD COLUMN enable_reranking BOOLEAN NOT NULL DEFAULT FALSE;
-- ALTER TABLE assistant_settings ADD COLUMN reranker_top_k INTEGER NOT NULL DEFAULT 5;
-- ALTER TABLE assistant_settings ADD COLUMN reranker_score_threshold DOUBLE PRECISION NOT NULL DEFAULT 3.0;
-- ALTER TABLE assistant_settings ADD COLUMN rerank_prompt TEXT;

-- ALTER TABLE chat_history ADD COLUMN rerank_duration_ms DOUBLE PRECISION NOT NULL DEFAULT 0;
-- ALTER TABLE chat_history ADD COLUMN rerank_input_count INTEGER NOT NULL DEFAULT 0;
-- ALTER TABLE chat_history ADD COLUMN rerank_output_count INTEGER NOT NULL DEFAULT 0;

------------------------------------------------------------------------
-- MySQL
------------------------------------------------------------------------
-- ALTER TABLE assistant_settings ADD COLUMN enable_reranking TINYINT(1) NOT NULL DEFAULT 0;
-- ALTER TABLE assistant_settings ADD COLUMN reranker_top_k INT NOT NULL DEFAULT 5;
-- ALTER TABLE assistant_settings ADD COLUMN reranker_score_threshold DOUBLE NOT NULL DEFAULT 3.0;
-- ALTER TABLE assistant_settings ADD COLUMN rerank_prompt TEXT;

-- ALTER TABLE chat_history ADD COLUMN rerank_duration_ms DOUBLE NOT NULL DEFAULT 0;
-- ALTER TABLE chat_history ADD COLUMN rerank_input_count INT NOT NULL DEFAULT 0;
-- ALTER TABLE chat_history ADD COLUMN rerank_output_count INT NOT NULL DEFAULT 0;

------------------------------------------------------------------------
-- SQL Server
------------------------------------------------------------------------
-- ALTER TABLE assistant_settings ADD enable_reranking BIT NOT NULL DEFAULT 0;
-- ALTER TABLE assistant_settings ADD reranker_top_k INT NOT NULL DEFAULT 5;
-- ALTER TABLE assistant_settings ADD reranker_score_threshold FLOAT NOT NULL DEFAULT 3.0;
-- ALTER TABLE assistant_settings ADD rerank_prompt NVARCHAR(MAX);

-- ALTER TABLE chat_history ADD rerank_duration_ms FLOAT NOT NULL DEFAULT 0;
-- ALTER TABLE chat_history ADD rerank_input_count INT NOT NULL DEFAULT 0;
-- ALTER TABLE chat_history ADD rerank_output_count INT NOT NULL DEFAULT 0;
