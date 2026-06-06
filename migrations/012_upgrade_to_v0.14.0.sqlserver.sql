-- Migration script for AssistantHub v0.14.0
-- SQL Server
-- Assistant chat-open model loading setting

IF COL_LENGTH('assistant_settings', 'load_models_on_chat_open') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD load_models_on_chat_open BIT NOT NULL DEFAULT 0;
END
GO
