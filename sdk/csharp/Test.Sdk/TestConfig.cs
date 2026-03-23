namespace Test.Sdk
{
    using System;

    /// <summary>
    /// Configuration for the C# SDK test suite.
    /// </summary>
    public class TestConfig
    {
        /// <summary>
        /// Base URL of the AssistantHub server.
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:6600";

        /// <summary>
        /// API key for authentication.
        /// </summary>
        public string ApiKey { get; set; } = "default";

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// Loads configuration from environment variables and command line arguments.
        /// Command line arguments take precedence over environment variables.
        /// </summary>
        /// <param name="args">Command line arguments in the form key=value.</param>
        /// <returns>A populated TestConfig instance.</returns>
        public static TestConfig Load(string[] args)
        {
            TestConfig config = new TestConfig();

            string envBaseUrl = Environment.GetEnvironmentVariable("ASSISTANTHUB_BASE_URL");
            if (!string.IsNullOrWhiteSpace(envBaseUrl))
            {
                config.BaseUrl = envBaseUrl;
            }

            string envApiKey = Environment.GetEnvironmentVariable("ASSISTANTHUB_API_KEY");
            if (!string.IsNullOrWhiteSpace(envApiKey))
            {
                config.ApiKey = envApiKey;
            }

            string envTenantId = Environment.GetEnvironmentVariable("ASSISTANTHUB_TENANT_ID");
            if (!string.IsNullOrWhiteSpace(envTenantId))
            {
                config.TenantId = envTenantId;
            }

            if (args != null)
            {
                foreach (string arg in args)
                {
                    int eqIndex = arg.IndexOf('=');
                    if (eqIndex <= 0) continue;

                    string key = arg.Substring(0, eqIndex).ToLowerInvariant();
                    string value = arg.Substring(eqIndex + 1);

                    switch (key)
                    {
                        case "baseurl":
                            config.BaseUrl = value;
                            break;
                        case "apikey":
                            config.ApiKey = value;
                            break;
                        case "tenantid":
                            config.TenantId = value;
                            break;
                    }
                }
            }

            return config;
        }
    }
}
