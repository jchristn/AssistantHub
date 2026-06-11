-- Migration script for AssistantHub v0.16.0
-- PostgreSQL
-- Optional assistant tool-routing inference endpoint and thinking exposure setting

ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS tool_routing_inference_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS expose_thinking BOOLEAN NOT NULL DEFAULT FALSE;
