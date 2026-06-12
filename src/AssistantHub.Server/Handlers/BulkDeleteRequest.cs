namespace AssistantHub.Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    using WatsonWebserver.Core;
    using ApiErrorResponse = AssistantHub.Core.Models.ApiErrorResponse;

    internal class BulkDeleteRequest
    {
        public List<string> DocumentIds { get; set; } = null;
    }

    internal static class BulkDeleteRequestParser
    {
        public static List<string> ParseRecordIds(string body)
        {
            if (String.IsNullOrWhiteSpace(body)) return new List<string>();

            try
            {
                JsonNode node = JsonNode.Parse(body);
                if (node is JsonArray array)
                    return NormalizeIds(ReadStringArray(array));

                if (node is JsonObject obj)
                {
                    foreach (string propertyName in new[] { "RecordIds", "Ids", "DocumentIds" })
                    {
                        JsonArray ids = GetArrayProperty(obj, propertyName);
                        if (ids != null)
                            return NormalizeIds(ReadStringArray(ids));
                    }
                }
            }
            catch
            {
            }

            return new List<string>();
        }

        private static JsonArray GetArrayProperty(JsonObject obj, string propertyName)
        {
            if (obj == null) return null;

            foreach (KeyValuePair<string, JsonNode?> kvp in obj)
            {
                if (String.Equals(kvp.Key, propertyName, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value as JsonArray;
            }

            return null;
        }

        private static IEnumerable<string> ReadStringArray(JsonArray array)
        {
            foreach (JsonNode node in array)
            {
                if (node is JsonValue value && value.TryGetValue(out string text))
                    yield return text;
            }
        }

        private static List<string> NormalizeIds(IEnumerable<string> ids)
        {
            return ids
                .Where(id => !String.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }
    }
}
