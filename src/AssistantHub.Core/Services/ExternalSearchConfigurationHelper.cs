namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AssistantHub.Core.Settings;

    /// <summary>
    /// Normalization, validation, and provider resolution helpers for external search settings.
    /// </summary>
    public static class ExternalSearchConfigurationHelper
    {
        /// <summary>
        /// Redacted secret marker accepted by configuration update flows.
        /// </summary>
        public const string RedactedSecret = "[REDACTED]";

        /// <summary>
        /// Normalize external-search settings in place.
        /// </summary>
        public static void Normalize(ExternalSearchSettings settings)
        {
            if (settings == null) return;

            settings.MaxResults = Math.Clamp(settings.MaxResults, 1, 20);
            settings.TimeoutMs = Math.Clamp(settings.TimeoutMs, 1000, 300000);
            settings.IncludeDomains = NormalizeList(settings.IncludeDomains);
            settings.ExcludeDomains = NormalizeList(settings.ExcludeDomains);

            if (settings.Providers == null)
                settings.Providers = new List<ExternalSearchProviderSettings>();

            settings.Providers = settings.Providers
                .Where(provider => provider != null)
                .ToList();

            foreach (ExternalSearchProviderSettings provider in settings.Providers)
                NormalizeProvider(provider, settings.TimeoutMs);

            EnsureSingleDefaultProvider(settings.Providers);
        }

        /// <summary>
        /// Validate external-search settings.
        /// </summary>
        public static List<string> Validate(ExternalSearchSettings settings)
        {
            List<string> errors = new List<string>();
            if (settings == null) return errors;

            Normalize(settings);

            for (int i = 0; i < settings.Providers.Count; i++)
            {
                ExternalSearchProviderSettings provider = settings.Providers[i];
                string prefix = "ExternalSearch.Providers[" + i + "]";

                if (provider.Enabled && !String.Equals(provider.ProviderType, "Tavily", StringComparison.OrdinalIgnoreCase))
                    errors.Add(prefix + ".ProviderType must be Tavily for the first release.");

                if (provider.Enabled && !HasValidHttpEndpoint(provider.Endpoint, allowMissingEnvironmentVariable: true))
                    errors.Add(prefix + ".Endpoint must resolve to an absolute http or https URL.");

                if (provider.TimeoutMs < 1000 || provider.TimeoutMs > 300000)
                    errors.Add(prefix + ".TimeoutMs must be between 1000 and 300000.");
            }

            return errors;
        }

        /// <summary>
        /// Resolve a configured value that may contain an environment-variable reference.
        /// </summary>
        public static string ResolveConfiguredValue(string value, bool throwIfMissingEnvironmentVariable = true)
        {
            if (String.IsNullOrWhiteSpace(value)) return null;

            string trimmed = value.Trim();
            string variableName = TryExtractEnvironmentVariableName(trimmed);
            if (!String.IsNullOrWhiteSpace(variableName))
            {
                string envValue = Environment.GetEnvironmentVariable(variableName);
                if (String.IsNullOrWhiteSpace(envValue))
                {
                    if (throwIfMissingEnvironmentVariable)
                        throw new InvalidOperationException("Environment variable " + variableName + " is not set.");

                    return null;
                }

                return envValue.Trim();
            }

            return Environment.ExpandEnvironmentVariables(trimmed);
        }

        /// <summary>
        /// Determine whether a provider is an enabled, fully configured Tavily provider.
        /// </summary>
        public static bool IsConfiguredTavilyProvider(ExternalSearchProviderSettings provider)
        {
            return IsConfiguredTavilyProvider(provider, out _);
        }

        /// <summary>
        /// Determine whether a provider is an enabled, fully configured Tavily provider.
        /// </summary>
        public static bool IsConfiguredTavilyProvider(ExternalSearchProviderSettings provider, out string reason)
        {
            reason = null;
            if (provider == null)
            {
                reason = "provider is missing";
                return false;
            }

            if (!provider.Enabled)
            {
                reason = "provider is disabled";
                return false;
            }

            if (!String.Equals(provider.ProviderType, "Tavily", StringComparison.OrdinalIgnoreCase))
            {
                reason = "provider type is not Tavily";
                return false;
            }

            string endpoint;
            string apiKey;
            try
            {
                endpoint = ResolveConfiguredValue(String.IsNullOrWhiteSpace(provider.Endpoint) ? TavilySearchClient.DefaultEndpoint : provider.Endpoint);
                apiKey = ResolveConfiguredValue(provider.ApiKey);
            }
            catch (Exception e)
            {
                reason = e.Message;
                return false;
            }

            if (!HasValidHttpEndpoint(endpoint, allowMissingEnvironmentVariable: false))
            {
                reason = "endpoint is invalid";
                return false;
            }

            if (String.IsNullOrWhiteSpace(apiKey))
            {
                reason = "API key is missing";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Resolve an assistant-level Tavily override into a provider.
        /// </summary>
        public static ExternalSearchProviderSettings ResolveAssistantTavilyProvider(string endpoint, string apiKey, int timeoutMs)
        {
            ExternalSearchProviderSettings provider = new ExternalSearchProviderSettings
            {
                Name = "assistant-tavily",
                ProviderType = "Tavily",
                Endpoint = endpoint,
                ApiKey = apiKey,
                Enabled = true,
                IsDefault = true,
                TimeoutMs = Math.Clamp(timeoutMs, 1000, 300000)
            };

            return IsConfiguredTavilyProvider(provider) ? CloneResolvedProvider(provider, provider.TimeoutMs) : null;
        }

        /// <summary>
        /// Resolve the default configured Tavily provider from system settings.
        /// </summary>
        public static ExternalSearchProviderSettings ResolveDefaultTavilyProvider(ExternalSearchSettings settings)
        {
            if (settings == null || !settings.Enabled || settings.Providers == null)
                return null;

            Normalize(settings);

            ExternalSearchProviderSettings provider = settings.Providers
                .Where(IsConfiguredTavilyProvider)
                .OrderByDescending(candidate => candidate.IsDefault)
                .FirstOrDefault();

            return provider == null ? null : CloneResolvedProvider(provider, settings.TimeoutMs);
        }

        /// <summary>
        /// Build a safe summary of external-search configuration status.
        /// </summary>
        public static ExternalSearchConfigurationStatus GetStatus(ExternalSearchSettings settings)
        {
            ExternalSearchConfigurationStatus status = new ExternalSearchConfigurationStatus();
            if (settings == null) return status;

            ExternalSearchSettings normalized = CloneSettings(settings);
            Normalize(normalized);
            status.Enabled = normalized.Enabled;
            if (!normalized.Enabled) return status;

            foreach (ExternalSearchProviderSettings provider in normalized.Providers)
            {
                if (provider == null || !provider.Enabled) continue;
                status.EnabledProviders++;
                if (IsConfiguredTavilyProvider(provider))
                    status.ConfiguredProviders++;
                else
                    status.MisconfiguredProviders++;
            }

            return status;
        }

        private static ExternalSearchSettings CloneSettings(ExternalSearchSettings settings)
        {
            if (settings == null) return null;

            return new ExternalSearchSettings
            {
                Enabled = settings.Enabled,
                AllowFallback = settings.AllowFallback,
                MaxResults = settings.MaxResults,
                TimeoutMs = settings.TimeoutMs,
                SafeSearch = settings.SafeSearch,
                AllowRawContent = settings.AllowRawContent,
                IncludeDomains = settings.IncludeDomains == null ? new List<string>() : new List<string>(settings.IncludeDomains),
                ExcludeDomains = settings.ExcludeDomains == null ? new List<string>() : new List<string>(settings.ExcludeDomains),
                Providers = settings.Providers == null
                    ? new List<ExternalSearchProviderSettings>()
                    : settings.Providers
                        .Where(provider => provider != null)
                        .Select(provider => new ExternalSearchProviderSettings
                        {
                            Name = provider.Name,
                            ProviderType = provider.ProviderType,
                            Endpoint = provider.Endpoint,
                            ApiKey = provider.ApiKey,
                            Enabled = provider.Enabled,
                            IsDefault = provider.IsDefault,
                            TimeoutMs = provider.TimeoutMs
                        })
                        .ToList()
            };
        }

        private static ExternalSearchProviderSettings CloneResolvedProvider(ExternalSearchProviderSettings provider, int fallbackTimeoutMs)
        {
            int timeoutMs = provider.TimeoutMs > 0 ? provider.TimeoutMs : fallbackTimeoutMs;
            return new ExternalSearchProviderSettings
            {
                Name = provider.Name,
                ProviderType = "Tavily",
                Endpoint = ResolveConfiguredValue(String.IsNullOrWhiteSpace(provider.Endpoint) ? TavilySearchClient.DefaultEndpoint : provider.Endpoint),
                ApiKey = ResolveConfiguredValue(provider.ApiKey),
                Enabled = true,
                IsDefault = provider.IsDefault,
                TimeoutMs = Math.Clamp(timeoutMs, 1000, 300000)
            };
        }

        private static void NormalizeProvider(ExternalSearchProviderSettings provider, int fallbackTimeoutMs)
        {
            if (provider == null) return;

            provider.Name = String.IsNullOrWhiteSpace(provider.Name) ? "tavily" : provider.Name.Trim();
            provider.ProviderType = String.IsNullOrWhiteSpace(provider.ProviderType) ? "Tavily" : provider.ProviderType.Trim();
            if (String.Equals(provider.ProviderType, "tavily", StringComparison.OrdinalIgnoreCase))
                provider.ProviderType = "Tavily";

            provider.Endpoint = String.IsNullOrWhiteSpace(provider.Endpoint) ? TavilySearchClient.DefaultEndpoint : provider.Endpoint.Trim();
            provider.ApiKey = String.IsNullOrWhiteSpace(provider.ApiKey) ? null : provider.ApiKey.Trim();

            if (provider.TimeoutMs <= 0)
                provider.TimeoutMs = fallbackTimeoutMs;
        }

        private static void EnsureSingleDefaultProvider(List<ExternalSearchProviderSettings> providers)
        {
            if (providers == null || providers.Count == 0) return;

            ExternalSearchProviderSettings firstEnabled = providers.FirstOrDefault(provider => provider.Enabled);
            if (firstEnabled == null) return;

            bool foundDefault = false;
            foreach (ExternalSearchProviderSettings provider in providers)
            {
                if (!provider.Enabled) continue;

                if (provider.IsDefault && !foundDefault)
                {
                    foundDefault = true;
                    continue;
                }

                if (provider.IsDefault)
                    provider.IsDefault = false;
            }

            if (!foundDefault)
                firstEnabled.IsDefault = true;
        }

        private static bool HasValidHttpEndpoint(string value, bool allowMissingEnvironmentVariable)
        {
            string endpoint;
            try
            {
                endpoint = ResolveConfiguredValue(value, !allowMissingEnvironmentVariable);
            }
            catch
            {
                return false;
            }

            if (String.IsNullOrWhiteSpace(endpoint))
                return allowMissingEnvironmentVariable;

            return Uri.TryCreate(endpoint, UriKind.Absolute, out Uri uri)
                && (String.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    || String.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
        }

        private static List<string> NormalizeList(List<string> values)
        {
            if (values == null) return new List<string>();

            return values
                .Where(value => !String.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(100)
                .ToList();
        }

        private static string TryExtractEnvironmentVariableName(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return null;

            string trimmed = value.Trim();
            if (trimmed.StartsWith("${", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal) && trimmed.Length > 3)
                return trimmed.Substring(2, trimmed.Length - 3);

            if (trimmed.StartsWith("$env:", StringComparison.OrdinalIgnoreCase) && trimmed.Length > 5)
                return trimmed.Substring(5);

            if (trimmed.StartsWith("$", StringComparison.Ordinal) && trimmed.Length > 1)
                return trimmed.Substring(1);

            if (trimmed.StartsWith("%", StringComparison.Ordinal) && trimmed.EndsWith("%", StringComparison.Ordinal) && trimmed.Length > 2)
                return trimmed.Substring(1, trimmed.Length - 2);

            return null;
        }
    }

    /// <summary>
    /// Safe status summary for external-search configuration.
    /// </summary>
    public class ExternalSearchConfigurationStatus
    {
        /// <summary>
        /// Whether external search is globally enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Enabled provider count.
        /// </summary>
        public int EnabledProviders { get; set; } = 0;

        /// <summary>
        /// Fully configured provider count.
        /// </summary>
        public int ConfiguredProviders { get; set; } = 0;

        /// <summary>
        /// Enabled but incomplete provider count.
        /// </summary>
        public int MisconfiguredProviders { get; set; } = 0;
    }
}
