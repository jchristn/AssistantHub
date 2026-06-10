namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;

    /// <summary>
    /// Resolves assistant tool policy into effective server-side tool availability.
    /// </summary>
    public class AssistantToolPolicyResolver
    {
        #region Private-Members

        private readonly AssistantHubSettings _Settings;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Server settings.</param>
        public AssistantToolPolicyResolver(AssistantHubSettings settings)
        {
            _Settings = settings ?? new AssistantHubSettings();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Resolve effective tools for the supplied assistant settings.
        /// </summary>
        /// <param name="assistant">Assistant.</param>
        /// <param name="settings">Assistant settings.</param>
        /// <param name="includeDisabled">Include tools disabled by policy.</param>
        /// <returns>Effective tool descriptors.</returns>
        public List<AssistantToolDescriptor> Resolve(
            Assistant assistant,
            AssistantSettings settings,
            bool includeDisabled = false)
        {
            AssistantToolPolicy policy = settings?.ToolPolicy ?? new AssistantToolPolicy();
            policy.Normalize();

            bool toolCallsEnabled = policy.EnableToolCalls;
            bool hasCollection = settings != null && !String.IsNullOrWhiteSpace(settings.CollectionId);
            bool hasVerbex = _Settings.Verbex != null && !String.IsNullOrWhiteSpace(_Settings.Verbex.Endpoint);
            bool hasS3 = _Settings.S3 != null && !String.IsNullOrWhiteSpace(_Settings.S3.BucketName);
            bool hasTavily = HasTavilyProvider(_Settings.ExternalSearch, policy);

            List<AssistantToolDescriptor> tools = new List<AssistantToolDescriptor>
            {
                Create("collection_search", "Collection Search", "Collection", toolCallsEnabled && policy.EnableCollectionSearchTool, hasCollection, "Assistant collection is not configured."),
                Create("collection_read_chunks", "Collection Chunk Read", "Collection", toolCallsEnabled && policy.EnableCollectionReadChunksTool, hasCollection, "Assistant collection is not configured."),
                Create("collection_enumerate_documents", "Collection Document Enumeration", "Collection", toolCallsEnabled && policy.EnableCollectionEnumerateDocumentsTool, hasCollection, "Assistant collection is not configured."),
                Create("verbex_full_text_search", "Verbex Full-Text Search", "Verbex", toolCallsEnabled && policy.EnableVerbexFullTextSearchTool, hasVerbex, "Verbex endpoint is not configured."),
                Create("index_enumerate_records", "Verbex Record Enumeration", "Verbex", toolCallsEnabled && policy.EnableIndexEnumerateRecordsTool, hasVerbex, "Verbex endpoint is not configured."),
                Create("s3_object_read", "S3 Object Read", "S3", toolCallsEnabled && policy.EnableS3ObjectReadTool, hasS3, "S3 bucket is not configured."),
                Create("bucket_enumerate_objects", "S3 Bucket Object Enumeration", "S3", toolCallsEnabled && policy.EnableBucketEnumerateObjectsTool, hasS3, "S3 bucket is not configured."),
                Create("web_search", "Web Search", "Web", toolCallsEnabled && policy.EnableWebSearchTool, hasTavily, "Tavily web search is not configured globally or on the assistant.")
            };

            if (includeDisabled) return tools;
            IEnumerable<AssistantToolDescriptor> availableTools = tools.Where(tool => tool.EnabledByPolicy && tool.Available);
            if (policy.AllowedToolNames != null && policy.AllowedToolNames.Count > 0)
            {
                availableTools = availableTools.Where(tool =>
                    policy.AllowedToolNames.Contains(tool.ToolName, StringComparer.OrdinalIgnoreCase));
            }

            return availableTools.ToList();
        }

        #endregion

        #region Private-Methods

        private static AssistantToolDescriptor Create(
            string toolName,
            string displayName,
            string category,
            bool enabledByPolicy,
            bool prerequisitesAvailable,
            string unavailableReason)
        {
            bool available = enabledByPolicy && prerequisitesAvailable;
            string reason = null;
            if (!enabledByPolicy) reason = "Disabled by assistant tool policy.";
            else if (!prerequisitesAvailable) reason = unavailableReason;
            else if (!AssistantToolRegistry.IsImplementedTool(toolName))
            {
                available = false;
                reason = "Tool executor is not implemented yet.";
            }

            return new AssistantToolDescriptor
            {
                ToolName = toolName,
                DisplayName = displayName,
                Category = category,
                EnabledByPolicy = enabledByPolicy,
                Available = available,
                UnavailableReason = available ? null : reason
            };
        }

        private static bool HasEnabledTavilyProvider(ExternalSearchSettings settings)
        {
            return ExternalSearchConfigurationHelper.ResolveDefaultTavilyProvider(settings) != null;
        }

        private static bool HasTavilyProvider(ExternalSearchSettings settings, AssistantToolPolicy policy)
        {
            if (policy != null
                && ExternalSearchConfigurationHelper.ResolveAssistantTavilyProvider(
                    policy.TavilyEndpoint,
                    policy.TavilyApiKey,
                    policy.ToolCallTimeoutMs) != null)
                return true;

            return HasEnabledTavilyProvider(settings);
        }

        #endregion
    }
}
