-- Migration script for AssistantHub v0.16.0
-- SQLite
-- Optional assistant tool-routing inference endpoint and thinking exposure setting

ALTER TABLE assistant_settings ADD COLUMN tool_routing_inference_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN expose_thinking INTEGER NOT NULL DEFAULT 0;
