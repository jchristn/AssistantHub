namespace AssistantHub.McpServer.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Response redaction helper for secret-bearing payloads.
    /// </summary>
    public class AssistantHubMcpRedactor
    {
        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private static readonly HashSet<string> _SecretPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BearerToken",
            "Password",
            "AdminPassword",
            "DefaultAdminPassword",
            "AdminApiKeys",
            "ApiKey",
            "ApiKeyValue",
            "TavilyApiKey",
            "AccessKey",
            "SecretKey",
            "SlackAppToken",
            "SlackBotToken"
        };

        /// <summary>
        /// Redact a JSON string.
        /// </summary>
        public string RedactJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json ?? "null";

            JsonNode? node;
            try
            {
                node = JsonNode.Parse(json);
            }
            catch
            {
                return json;
            }

            if (node == null)
                return json;

            RedactNode(node);
            return node.ToJsonString(_JsonOptions);
        }

        /// <summary>
        /// Serialize and redact an object.
        /// </summary>
        public string SerializeAndRedact(object? value)
        {
            return RedactJson(AssistantHub.Core.Helpers.Serializer.SerializeJson(value, true));
        }

        private static void RedactNode(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                List<string> propertyNames = new List<string>();
                foreach (KeyValuePair<string, JsonNode?> kvp in obj)
                    propertyNames.Add(kvp.Key);

                foreach (string propertyName in propertyNames)
                {
                    JsonNode? child = obj[propertyName];
                    if (_SecretPropertyNames.Contains(propertyName))
                    {
                        obj[propertyName] = BuildRedactedNode(child);
                    }
                    else if (String.Equals(propertyName, "ToolPolicyJson", StringComparison.OrdinalIgnoreCase))
                    {
                        obj[propertyName] = BuildRedactedJsonStringNode(child);
                    }
                    else if (child != null)
                    {
                        RedactNode(child);
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                for (int i = 0; i < arr.Count; i++)
                {
                    if (arr[i] != null)
                        RedactNode(arr[i]!);
                }
            }
        }

        private static JsonNode BuildRedactedNode(JsonNode? original)
        {
            if (original is JsonArray arr)
            {
                JsonArray ret = new JsonArray();
                for (int i = 0; i < Math.Max(1, arr.Count); i++)
                    ret.Add("[REDACTED]");
                return ret;
            }

            return JsonValue.Create("[REDACTED]")!;
        }

        private static JsonNode? BuildRedactedJsonStringNode(JsonNode? original)
        {
            string? json = original?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(json))
                return original;

            JsonNode? parsed;
            try
            {
                parsed = JsonNode.Parse(json);
            }
            catch
            {
                return original;
            }

            if (parsed == null)
                return original;

            RedactNode(parsed);
            return JsonValue.Create(parsed.ToJsonString())!;
        }
    }
}
