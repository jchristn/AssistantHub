-- Migration script for AssistantHub v0.16.0
-- SQL Server
-- Optional assistant tool-routing inference endpoint and thinking exposure setting

IF COL_LENGTH('assistant_settings', 'tool_routing_inference_endpoint_id') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD tool_routing_inference_endpoint_id NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('assistant_settings', 'expose_thinking') IS NULL
BEGIN
    ALTER TABLE assistant_settings ADD expose_thinking BIT NOT NULL DEFAULT 0;
END
GO
