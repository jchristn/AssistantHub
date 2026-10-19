namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
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
            if (!String.IsNullOrEmpty(clone.Inference.ApiKey)) clone.Inference.ApiKey = ExternalSearchConfigurationHelper.RedactedSecret;
            if (!String.IsNullOrEmpty(clone.Inference.ProviderSettings?.Bedrock?.SecretAccessKey)) clone.Inference.ProviderSettings.Bedrock.SecretAccessKey = ExternalSearchConfigurationHelper.RedactedSecret;
            if (!String.IsNullOrEmpty(clone.Inference.ProviderSettings?.Bedrock?.SessionToken)) clone.Inference.ProviderSettings.Bedrock.SessionToken = ExternalSearchConfigurationHelper.RedactedSecret;
            if (!String.IsNullOrEmpty(clone.Inference.ProviderSettings?.VertexAI?.ServiceAccountJson)) clone.Inference.ProviderSettings.VertexAI.ServiceAccountJson = ExternalSearchConfigurationHelper.RedactedSecret;
            foreach (RerankerSettings reranker in clone.Rerankers ?? new List<RerankerSettings>())
            {
                if (reranker != null && !String.IsNullOrWhiteSpace(reranker.ApiKey))
                    reranker.ApiKey = ExternalSearchConfigurationHelper.RedactedSecret;
            }

            return clone;
        }

        /// <summary>
        /// Preserve existing provider API keys when a redacted configuration response is submitted back.
        /// </summary>
        public static void PreserveRedactedExternalSearchSecrets(AssistantHubSettings updated, AssistantHubSettings current)
        {
            if (updated?.Inference != null && current?.Inference != null)
            {
                if (IsRedacted(updated.Inference.ApiKey)) updated.Inference.ApiKey = current.Inference.ApiKey;
                if (IsRedacted(updated.Inference.ProviderSettings?.Bedrock?.SecretAccessKey)) updated.Inference.ProviderSettings.Bedrock.SecretAccessKey = current.Inference.ProviderSettings?.Bedrock?.SecretAccessKey;
                if (IsRedacted(updated.Inference.ProviderSettings?.Bedrock?.SessionToken)) updated.Inference.ProviderSettings.Bedrock.SessionToken = current.Inference.ProviderSettings?.Bedrock?.SessionToken;
                if (IsRedacted(updated.Inference.ProviderSettings?.VertexAI?.ServiceAccountJson)) updated.Inference.ProviderSettings.VertexAI.ServiceAccountJson = current.Inference.ProviderSettings?.VertexAI?.ServiceAccountJson;
            }
            // Reranker keys are redacted the same way; restore them by reranker Id.
            foreach (RerankerSettings reranker in updated?.Rerankers ?? new List<RerankerSettings>())
            {
                if (reranker == null || !IsRedacted(reranker.ApiKey)) continue;
                RerankerSettings existing = current?.Rerankers?.FirstOrDefault(r => r != null && String.Equals(r.Id, reranker.Id, StringComparison.OrdinalIgnoreCase));
                reranker.ApiKey = existing?.ApiKey;
            }

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
