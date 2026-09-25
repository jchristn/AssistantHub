namespace AssistantHub.Server.OpenApi
{
    using System.Collections.Generic;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for server configuration routes.
    /// </summary>
    public static class ConfigurationApiDocs
    {
        #region Private-Members

        private const string _Tag = "Configuration";

        private static AssistantHubSettings ExampleSettings(string externalSearchApiKey)
        {
            AssistantHubSettings settings = new AssistantHubSettings();
            settings.AdminApiKeys = new List<string> { "example-admin-api-key" };

            settings.S3.AccessKey = "EXAMPLEACCESSKEY";
            settings.S3.SecretKey = "example-secret-key";
            settings.S3.BucketName = "assistanthub";

            settings.Inference.Endpoint = "http://ollama:11434";
            settings.Inference.DefaultModel = "gemma3:4b";

            settings.RequestHistory.Enabled = true;
            settings.RequestHistory.RetentionDays = 30;

            settings.ExternalSearch = new ExternalSearchSettings
            {
                Enabled = true,
                AllowFallback = true,
                MaxResults = 5,
                TimeoutMs = 30000,
                SafeSearch = true,
                AllowRawContent = false,
                IncludeDomains = new List<string>(),
                ExcludeDomains = new List<string> { "example-spam.test" },
                Providers = new List<ExternalSearchProviderSettings>
                {
                    new ExternalSearchProviderSettings
                    {
                        Name = "tavily",
                        ProviderType = "Tavily",
                        Endpoint = "https://api.tavily.com/search",
                        ApiKey = externalSearchApiKey,
                        Enabled = true,
                        IsDefault = true,
                        TimeoutMs = 30000
                    }
                }
            };

            return settings;
        }

        #endregion

        #region Public-Members

        /// <summary>GET /v1.0/configuration.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get server configuration", _Tag)
            .Describe("Returns the running server settings (the contents of assistanthub.json). Global administrators only. External-search provider API keys are replaced with [REDACTED]; other values, including admin API keys, storage keys, and database credentials, are returned as configured, so treat the response as sensitive.")
            .Returns(200, "Current server settings.", ExampleSettings(ExternalSearchConfigurationHelper.RedactedSecret))
            .Errors(401, 403, 500);

        /// <summary>GET /v1.0/configuration/external-search/status.</summary>
        public static OpenApiRouteMetadata ExternalSearchStatus => ApiDoc.Create("Get external search status", _Tag)
            .Describe("Returns a secret-free summary of the external web-search configuration: whether it is globally enabled and how many enabled providers are fully configured versus misconfigured (for example, missing an API key). Provider counts are zero when external search is disabled. Global administrators only.")
            .Returns(200, "External search configuration status.", new ExternalSearchConfigurationStatus
            {
                Enabled = true,
                EnabledProviders = 1,
                ConfiguredProviders = 1,
                MisconfiguredProviders = 0
            })
            .Errors(401, 403, 500);

        /// <summary>PUT /v1.0/configuration.</summary>
        public static OpenApiRouteMetadata Update => ApiDoc.Create("Update server configuration", _Tag)
            .Describe("Replaces the server settings and persists them to assistanthub.json. Global administrators only. The body is a complete settings object (typically the result of GET /v1.0/configuration with edits); sections that are omitted or null are reset to their default values rather than preserved. The AdminApiKeys, DefaultTenant, Webserver, Database, S3, DocumentAtom, Chunking, Embeddings, Inference, RecallDb, Verbex, ExternalSearch, Logging, ProcessingLog, ChatHistory, RequestHistory, and Crawl sections are applied; Telemetry is not updated by this call. External-search provider API keys submitted as [REDACTED] retain their stored value. The external-search section is validated and a 400 is returned describing any problems. Some settings, such as the webserver and database, only take effect after a restart.")
            .Body("Complete server settings.", ExampleSettings("tvly-example-api-key"))
            .Returns(200, "Updated server settings, with external-search API keys redacted.", ExampleSettings(ExternalSearchConfigurationHelper.RedactedSecret))
            .Errors(400, 401, 403, 500);

        #endregion
    }
}
