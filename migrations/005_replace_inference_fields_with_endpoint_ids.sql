ALTER TABLE assistant_settings ADD COLUMN inference_endpoint_id TEXT;
ALTER TABLE assistant_settings ADD COLUMN embedding_endpoint_id TEXT;
ALTER TABLE assistant_settings DROP COLUMN inference_provider;
ALTER TABLE assistant_settings DROP COLUMN inference_endpoint;
ALTER TABLE assistant_settings DROP COLUMN inference_api_key;
