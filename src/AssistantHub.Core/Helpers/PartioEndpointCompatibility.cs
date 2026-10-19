namespace AssistantHub.Core.Helpers
{
    using System;

    /// <summary>Diagnoses endpoint format rejection by an outdated configured Partio service.</summary>
    public static class PartioEndpointCompatibility
    {
        /// <summary>Return an actionable dependency error for a rejected cloud format, or null for other failures.</summary>
        public static string GetVersionMismatchMessage(string responseBody)
        {
            if (String.IsNullOrEmpty(responseBody)
                || !responseBody.Contains("enum type ApiFormatEnum", StringComparison.OrdinalIgnoreCase)) return null;
            if (!responseBody.Contains("OllamaCloud", StringComparison.OrdinalIgnoreCase)
                && !responseBody.Contains("xAI", StringComparison.OrdinalIgnoreCase)) return null;

            return "The configured Partio service does not support this cloud endpoint format. "
                + "Deploy Partio v0.5.1 or later at the configured Chunking.Endpoint, then retry. "
                + "For Docker Compose, pull and recreate partio-server; rebuilding AssistantHub does not replace an already-running Partio container.";
        }
    }
}
