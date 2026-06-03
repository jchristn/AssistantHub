-- Migration script for AssistantHub v0.11.0
-- SQL Server
-- Assistant utility inference endpoint routing

IF COL_LENGTH('assistant_settings', 'retrieval_gate_inference_endpoint_id') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD retrieval_gate_inference_endpoint_id NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('assistant_settings', 'query_rewrite_inference_endpoint_id') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD query_rewrite_inference_endpoint_id NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('assistant_settings', 'rerank_inference_endpoint_id') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD rerank_inference_endpoint_id NVARCHAR(MAX) NULL;
END
GO
