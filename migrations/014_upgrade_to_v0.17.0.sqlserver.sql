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
