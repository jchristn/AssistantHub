-- Migration script for AssistantHub v0.11.0
-- SQLite
-- Assistant utility inference endpoint routing
--
-- Run this script once against an existing SQLite database before starting
-- v0.11.0 when startup migrations are not used.

ALTER TABLE assistant_settings ADD COLUMN retrieval_gate_inference_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN query_rewrite_inference_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN rerank_inference_endpoint_id TEXT;
