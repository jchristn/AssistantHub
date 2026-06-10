namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Builds policy-aware, redacted tool-call audit payloads.
    /// </summary>
    public static class AssistantToolAuditWriter
    {
        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Build persisted arguments according to policy.
        /// </summary>
        /// <param name="argumentsJson">Raw JSON arguments.</param>
        /// <param name="policy">Assistant tool policy.</param>
        /// <returns>Redacted or suppressed argument JSON.</returns>
        public static string BuildPersistedArguments(string argumentsJson, AssistantToolPolicy policy)
        {
            if (policy != null && !policy.PersistToolArguments)
                return "{\"suppressed\":true,\"reason\":\"PersistToolArguments is false\"}";

            return RedactToolJson(argumentsJson);
        }

        /// <summary>
        /// Build persisted output according to policy.
        /// </summary>
        /// <param name="toolResult">Tool result.</param>
        /// <param name="policy">Assistant tool policy.</param>
        /// <returns>Output summary or redacted full output.</returns>
        public static string BuildPersistedOutput(AssistantToolExecutionResult toolResult, AssistantToolPolicy policy)
        {
            if (policy != null && policy.PersistToolOutputs && !String.IsNullOrWhiteSpace(toolResult?.OutputJson))
                return RedactToolJson(toolResult.OutputJson);

            return BuildPersistedToolOutputSummary(toolResult);
        }

        /// <summary>
        /// Redact sensitive fields from arbitrary tool JSON.
        /// </summary>
        /// <param name="json">JSON payload.</param>
        /// <returns>Redacted JSON payload.</returns>
        public static string RedactToolJson(string json)
        {
            if (String.IsNullOrWhiteSpace(json)) return "{}";

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                object redacted = RedactJsonElement(document.RootElement);
                return JsonSerializer.Serialize(redacted, _JsonOptions);
            }
            catch (JsonException)
            {
                return "{\"redacted\":true,\"reason\":\"payload was not valid JSON\"}";
            }
        }

        private static string BuildPersistedToolOutputSummary(AssistantToolExecutionResult toolResult)
        {
            return JsonSerializer.Serialize(new
            {
                Success = toolResult?.Success == true,
                Tool = toolResult?.ToolName,
                Denied = toolResult?.Denied == true,
                Truncated = toolResult?.Truncated == true,
                OutputCharacters = toolResult?.OutputCharacters ?? 0,
                DurationMs = toolResult?.DurationMs ?? 0,
                Error = toolResult?.ErrorMessage
            }, _JsonOptions);
        }

        private static object RedactJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    Dictionary<string, object> obj = new Dictionary<string, object>(StringComparer.Ordinal);
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        obj[property.Name] = IsSensitiveToolField(property.Name)
                            ? "[redacted]"
                            : RedactJsonElement(property.Value);
                    }
                    return obj;

                case JsonValueKind.Array:
                    List<object> list = new List<object>();
                    foreach (JsonElement item in element.EnumerateArray())
                        list.Add(RedactJsonElement(item));
                    return list;

                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long integer)) return integer;
                    if (element.TryGetDouble(out double number)) return number;
                    return element.GetRawText();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                default:
                    return null;
            }
        }

        private static bool IsSensitiveToolField(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) return false;
            string normalized = name.Replace("_", String.Empty, StringComparison.Ordinal).Replace("-", String.Empty, StringComparison.Ordinal).ToLowerInvariant();
            return normalized.Contains("apikey", StringComparison.Ordinal)
                || normalized.Contains("password", StringComparison.Ordinal)
                || normalized.Contains("secret", StringComparison.Ordinal)
                || normalized.Contains("token", StringComparison.Ordinal)
                || normalized.Contains("credential", StringComparison.Ordinal)
                || normalized.Contains("bearer", StringComparison.Ordinal)
                || normalized.Contains("accesskey", StringComparison.Ordinal)
                || normalized.Contains("signedurl", StringComparison.Ordinal)
                || normalized.Contains("connectionstring", StringComparison.Ordinal);
        }
    }
}
