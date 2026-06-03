-- Migration script for AssistantHub v0.11.0
-- Assistant utility inference endpoint routing
--
-- This script adds:
--   1. retrieval_gate_inference_endpoint_id for retrieval gate prompts
--   2. query_rewrite_inference_endpoint_id for query rewrite prompts
--   3. rerank_inference_endpoint_id for reranking prompts
--
-- Run this script against your existing SQLite database before starting
-- v0.11.0. Provider-specific runnable scripts are also available:
--   migrations/009_upgrade_to_v0.11.0.sqlite.sql
--   migrations/009_upgrade_to_v0.11.0.postgresql.sql
--   migrations/009_upgrade_to_v0.11.0.mysql.sql
--   migrations/009_upgrade_to_v0.11.0.sqlserver.sql

-- ============================================================================
-- SQLite
-- ============================================================================

ALTER TABLE assistant_settings ADD COLUMN retrieval_gate_inference_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN query_rewrite_inference_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN rerank_inference_endpoint_id TEXT;

-- ============================================================================
-- PostgreSQL (uncomment to use)
-- ============================================================================
-- ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS retrieval_gate_inference_endpoint_id TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS query_rewrite_inference_endpoint_id TEXT;
-- ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS rerank_inference_endpoint_id TEXT;

-- ============================================================================
-- MySQL (uncomment to use)
-- ============================================================================
-- Note: MySQL does not support ALTER TABLE ADD COLUMN IF NOT EXISTS on all supported versions.
-- Run each statement once, or check INFORMATION_SCHEMA.COLUMNS before applying.
-- ALTER TABLE `assistant_settings` ADD COLUMN `retrieval_gate_inference_endpoint_id` TEXT;
-- ALTER TABLE `assistant_settings` ADD COLUMN `query_rewrite_inference_endpoint_id` TEXT;
-- ALTER TABLE `assistant_settings` ADD COLUMN `rerank_inference_endpoint_id` TEXT;

-- ============================================================================
-- SQL Server (uncomment to use)
-- ============================================================================
-- IF COL_LENGTH('assistant_settings', 'retrieval_gate_inference_endpoint_id') IS NULL
--   ALTER TABLE assistant_settings ADD retrieval_gate_inference_endpoint_id NVARCHAR(MAX) NULL;
--
-- IF COL_LENGTH('assistant_settings', 'query_rewrite_inference_endpoint_id') IS NULL
--   ALTER TABLE assistant_settings ADD query_rewrite_inference_endpoint_id NVARCHAR(MAX) NULL;
--
-- IF COL_LENGTH('assistant_settings', 'rerank_inference_endpoint_id') IS NULL
--   ALTER TABLE assistant_settings ADD rerank_inference_endpoint_id NVARCHAR(MAX) NULL;
