namespace AssistantHub.McpServer.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Helper methods for the AssistantHub MCP Server.
    /// </summary>
    internal static class AssistantHubMcpServerHelpers
    {
        /// <summary>
        /// Serialize an object as JSON.
        /// </summary>
        public static string Serialize(object? value)
        {
            return AssistantHub.Core.Helpers.Serializer.SerializeJson(value, true) ?? "null";
        }

        /// <summary>
        /// Serialize and conditionally redact an object.
        /// </summary>
        public static string Serialize(AssistantHubMcpContext context, object? value, bool includeSecrets = false)
        {
            string json = Serialize(value);
            return includeSecrets ? json : context.Redactor.RedactJson(json);
        }

        /// <summary>
        /// Conditionally redact an existing JSON string.
        /// </summary>
        public static string SerializeJsonString(AssistantHubMcpContext context, string? json, bool includeSecrets = false)
        {
            string materialized = json ?? "null";
            return includeSecrets ? materialized : context.Redactor.RedactJson(materialized);
        }

        /// <summary>
        /// Get a required string property.
        /// </summary>
        public static string GetStringRequired(JsonElement? args, string propertyName)
        {
            if (!TryGetProperty(args, propertyName, out JsonElement prop))
                throw new ArgumentException("Required parameter '" + propertyName + "' is missing");

            if (prop.ValueKind == JsonValueKind.String)
            {
                string? value = prop.GetString();
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Parameter '" + propertyName + "' cannot be empty");
                return value;
            }

            string raw = prop.GetRawText();
            if (string.IsNullOrWhiteSpace(raw))
                throw new ArgumentException("Parameter '" + propertyName + "' cannot be empty");
            return raw;
        }

        /// <summary>
        /// Get an optional string property.
        /// </summary>
        public static string? GetStringOptional(JsonElement? args, string propertyName)
        {
            if (!TryGetProperty(args, propertyName, out JsonElement prop))
                return null;

            return prop.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String => prop.GetString(),
                _ => prop.GetRawText()
            };
        }

        /// <summary>
        /// Get an optional integer.
        /// </summary>
        public static int? GetIntOptional(JsonElement? args, string propertyName)
        {
            if (!TryGetProperty(args, propertyName, out JsonElement prop))
                return null;
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out int value))
                return value;
            if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out int stringValue))
                return stringValue;
            return null;
        }

        /// <summary>
        /// Get an optional boolean with default.
        /// </summary>
        public static bool GetBoolOrDefault(JsonElement? args, string propertyName, bool defaultValue = false)
        {
            if (!TryGetProperty(args, propertyName, out JsonElement prop))
                return defaultValue;
            if (prop.ValueKind == JsonValueKind.True)
                return true;
            if (prop.ValueKind == JsonValueKind.False)
                return false;
            if (prop.ValueKind == JsonValueKind.String && bool.TryParse(prop.GetString(), out bool value))
                return value;
            return defaultValue;
        }

        /// <summary>
        /// Get a required JSON parameter as raw JSON.
        /// </summary>
        public static string GetJsonRequired(JsonElement? args, string propertyName)
        {
            if (!TryGetProperty(args, propertyName, out JsonElement prop))
                throw new ArgumentException("Required parameter '" + propertyName + "' is missing");
            return NormalizeJsonParameter(prop, propertyName, required: true)!;
        }

        /// <summary>
        /// Get an optional JSON parameter as raw JSON.
        /// </summary>
        public static string? GetJsonOptional(JsonElement? args, string propertyName)
        {
            if (!TryGetProperty(args, propertyName, out JsonElement prop))
                return null;
            return NormalizeJsonParameter(prop, propertyName, required: false);
        }

        /// <summary>
        /// Deserialize a required JSON parameter.
        /// </summary>
        public static T DeserializeRequired<T>(JsonElement? args, string propertyName)
        {
            string json = GetJsonRequired(args, propertyName);
            T? ret = AssistantHub.Core.Helpers.Serializer.DeserializeJson<T>(json);
            if (ret == null)
                throw new ArgumentException("Parameter '" + propertyName + "' could not be deserialized");
            return ret;
        }

        /// <summary>
        /// Deserialize an optional JSON parameter.
        /// </summary>
        public static T? DeserializeOptional<T>(JsonElement? args, string propertyName) where T : class
        {
            string? json = GetJsonOptional(args, propertyName);
            if (string.IsNullOrWhiteSpace(json))
                return null;
            return AssistantHub.Core.Helpers.Serializer.DeserializeJson<T>(json);
        }

        /// <summary>
        /// Append query string parameters to a path.
        /// </summary>
        public static string AppendQueryString(string path, IEnumerable<KeyValuePair<string, string?>> parameters)
        {
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, string?> kvp in parameters)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value))
                    parts.Add(kvp.Key + "=" + Uri.EscapeDataString(kvp.Value));
            }

            if (parts.Count < 1)
                return path;

            return path + "?" + string.Join("&", parts);
        }

        /// <summary>
        /// Convert a binary download to a JSON envelope.
        /// </summary>
        public static string SerializeBinaryEnvelope(BinaryResponse response, string source)
        {
            return Serialize(new
            {
                FileName = response.FileName,
                ContentType = response.ContentType ?? "application/octet-stream",
                Size = response.ContentLength ?? response.Bytes.LongLength,
                ContentBase64 = Convert.ToBase64String(response.Bytes),
                Source = source
            });
        }

        /// <summary>
        /// Decode required base64 content and enforce the configured limit.
        /// </summary>
        public static byte[] GetBase64BytesRequired(JsonElement? args, string propertyName, long maxInlineBinaryBytes)
        {
            string base64 = GetStringRequired(args, propertyName);

            byte[] data;
            try
            {
                data = Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Parameter '" + propertyName + "' must be valid base64 content");
            }

            EnsureBinaryWithinLimit(data.LongLength, maxInlineBinaryBytes, propertyName);
            return data;
        }

        /// <summary>
        /// Enforce the configured binary size limit.
        /// </summary>
        public static void EnsureBinaryWithinLimit(long sizeBytes, long maxInlineBinaryBytes, string operationName)
        {
            if (sizeBytes > maxInlineBinaryBytes)
            {
                throw new ArgumentException(
                    "Binary payload for '"
                    + operationName
                    + "' exceeds the configured inline limit of "
                    + maxInlineBinaryBytes
                    + " bytes");
            }
        }

        private static bool TryGetProperty(JsonElement? args, string propertyName, out JsonElement prop)
        {
            prop = default;
            if (!args.HasValue)
                return false;
            return args.Value.TryGetProperty(propertyName, out prop);
        }

        private static string? NormalizeJsonParameter(JsonElement prop, string propertyName, bool required)
        {
            if (prop.ValueKind == JsonValueKind.Null)
            {
                if (required)
                    throw new ArgumentException("Parameter '" + propertyName + "' cannot be null");
                return null;
            }

            if (prop.ValueKind == JsonValueKind.String)
            {
                string? str = prop.GetString();
                if (required && string.IsNullOrWhiteSpace(str))
                    throw new ArgumentException("Parameter '" + propertyName + "' cannot be empty");
                return str;
            }

            return prop.GetRawText();
        }
    }
}
