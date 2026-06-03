-- Migration script for AssistantHub v0.11.0
-- PostgreSQL
-- Assistant utility inference endpoint routing

ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS retrieval_gate_inference_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS query_rewrite_inference_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS rerank_inference_endpoint_id TEXT;
