-- Migration script for AssistantHub v0.14.0
-- SQLite
-- Assistant chat-open model loading setting

ALTER TABLE assistant_settings ADD COLUMN load_models_on_chat_open INTEGER NOT NULL DEFAULT 0;
