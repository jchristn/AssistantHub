namespace AssistantHub.Server.Services
{
    using System;
    using System.Linq;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;

    /// <summary>
    /// Redacts secret-bearing server configuration values for API responses.
    /// </summary>
    public static class ConfigurationSettingsRedactor
    {
        /// <summary>
        /// Create a response-safe configuration copy.
        /// </summary>
        public static AssistantHubSettings CreateRedactedCopy(AssistantHubSettings settings)
        {
            if (settings == null) return null;

            AssistantHubSettings clone = Serializer.DeserializeJson<AssistantHubSettings>(Serializer.SerializeJson(settings));
            RedactExternalSearchProviderApiKeys(clone);
            return clone;
        }

        /// <summary>
        /// Preserve existing provider API keys when a redacted configuration response is submitted back.
        /// </summary>
        public static void PreserveRedactedExternalSearchSecrets(AssistantHubSettings updated, AssistantHubSettings current)
        {
            if (updated?.ExternalSearch?.Providers == null || current?.ExternalSearch?.Providers == null)
                return;

            for (int i = 0; i < updated.ExternalSearch.Providers.Count; i++)
            {
                ExternalSearchProviderSettings provider = updated.ExternalSearch.Providers[i];
                if (provider == null || !IsRedacted(provider.ApiKey)) continue;

                ExternalSearchProviderSettings existing = FindExistingProvider(provider, current.ExternalSearch, i);
                if (existing != null)
                    provider.ApiKey = existing.ApiKey;
            }
        }

        private static void RedactExternalSearchProviderApiKeys(AssistantHubSettings settings)
        {
            if (settings?.ExternalSearch?.Providers == null) return;

            foreach (ExternalSearchProviderSettings provider in settings.ExternalSearch.Providers)
            {
                if (provider != null && !String.IsNullOrWhiteSpace(provider.ApiKey))
                    provider.ApiKey = ExternalSearchConfigurationHelper.RedactedSecret;
            }
        }

        private static ExternalSearchProviderSettings FindExistingProvider(
            ExternalSearchProviderSettings updated,
            ExternalSearchSettings current,
            int index)
        {
            if (updated == null || current?.Providers == null) return null;

            ExternalSearchProviderSettings byName = current.Providers.FirstOrDefault(provider =>
                provider != null
                && !String.IsNullOrWhiteSpace(updated.Name)
                && String.Equals(provider.Name, updated.Name, StringComparison.OrdinalIgnoreCase));
            if (byName != null) return byName;

            if (index >= 0 && index < current.Providers.Count)
                return current.Providers[index];

            return null;
        }

        private static bool IsRedacted(string value)
        {
            return String.Equals(value, ExternalSearchConfigurationHelper.RedactedSecret, StringComparison.OrdinalIgnoreCase)
                || String.Equals(value, "[redacted]", StringComparison.OrdinalIgnoreCase);
        }
    }
}
