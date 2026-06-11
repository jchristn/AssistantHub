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
        /// Run only local SDK contract tests that do not require a server.
        /// </summary>
        public bool LocalOnly { get; set; } = false;

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

            string envLocalOnly = Environment.GetEnvironmentVariable("ASSISTANTHUB_SDK_LOCAL_ONLY");
            if (!string.IsNullOrWhiteSpace(envLocalOnly))
            {
                config.LocalOnly = IsTruthy(envLocalOnly);
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
                        case "local":
                        case "localonly":
                        case "local-only":
                            config.LocalOnly = IsTruthy(value);
                            break;
                    }
                }
            }

            return config;
        }

        private static bool IsTruthy(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            value = value.Trim();
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "y", StringComparison.OrdinalIgnoreCase);
        }
    }
}
