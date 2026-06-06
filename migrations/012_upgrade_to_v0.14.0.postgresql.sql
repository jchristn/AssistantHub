-- Migration script for AssistantHub v0.14.0
-- PostgreSQL
-- Assistant chat-open model loading setting

ALTER TABLE assistant_settings ADD COLUMN IF NOT EXISTS load_models_on_chat_open BOOLEAN NOT NULL DEFAULT FALSE;
