namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Maps AssistantHub tool-call capability fields to Partio endpoint labels and tags.
    /// </summary>
    public static class PartioEndpointToolMetadata
    {
        /// <summary>
        /// Label indicating that AssistantHub tool-call metadata is present.
        /// </summary>
        public const string ToolCallingLabel = "assistanthub:tool-calling";

        /// <summary>
        /// Tag key for whether the endpoint supports tool calls.
        /// </summary>
        public const string SupportsToolCallingTag = "AssistantHub.SupportsToolCalling";

        /// <summary>
        /// Tag key for the tool-calling wire format.
        /// </summary>
        public const string ToolCallingApiFormatTag = "AssistantHub.ToolCallingApiFormat";

        /// <summary>
        /// Tag key for parallel tool-call support.
        /// </summary>
        public const string SupportsParallelToolCallsTag = "AssistantHub.SupportsParallelToolCalls";

        /// <summary>
        /// Tag key for streaming tool-call support.
        /// </summary>
        public const string SupportsStreamingToolCallsTag = "AssistantHub.SupportsStreamingToolCalls";

        private static readonly string[] RequestToolFieldNames =
        {
            nameof(PartioEndpointRequest.SupportsToolCalling),
            nameof(PartioEndpointRequest.ToolCallingApiFormat),
            nameof(PartioEndpointRequest.SupportsParallelToolCalls),
            nameof(PartioEndpointRequest.SupportsStreamingToolCalls)
        };

        /// <summary>
        /// Return true when a raw endpoint request includes AssistantHub tool-call fields.
        /// </summary>
        public static bool RequestContainsToolCapabilityFields(string json)
        {
            return RequestToolFieldNames.Any(name => JsonObjectContainsProperty(json, name));
        }

        /// <summary>
        /// Return true when a raw endpoint request includes a property.
        /// </summary>
        public static bool JsonObjectContainsProperty(string json, string propertyName)
        {
            if (String.IsNullOrWhiteSpace(json) || String.IsNullOrWhiteSpace(propertyName)) return false;

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Object) return false;

                foreach (JsonProperty property in document.RootElement.EnumerateObject())
                {
                    if (String.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Write AssistantHub tool-call request fields into Partio endpoint labels/tags.
        /// </summary>
        public static void WriteRequestToolFieldsToTags(PartioEndpointRequest request)
        {
            if (request == null) return;

            request.Labels = NormalizeLabels(request.Labels);
            request.Tags = NormalizeTags(request.Tags);

            if (request.SupportsToolCalling)
            {
                AddLabel(request.Labels, ToolCallingLabel);
                SetTag(request.Tags, SupportsToolCallingTag, "true");
                SetTag(request.Tags, ToolCallingApiFormatTag, request.ToolCallingApiFormat);
                SetTag(request.Tags, SupportsParallelToolCallsTag, request.SupportsParallelToolCalls ? "true" : "false");
                SetTag(request.Tags, SupportsStreamingToolCallsTag, request.SupportsStreamingToolCalls ? "true" : "false");
            }
            else
            {
                RemoveLabel(request.Labels, ToolCallingLabel);
                SetTag(request.Tags, SupportsToolCallingTag, "false");
                RemoveTag(request.Tags, ToolCallingApiFormatTag);
                RemoveTag(request.Tags, SupportsParallelToolCallsTag);
                RemoveTag(request.Tags, SupportsStreamingToolCallsTag);
            }
        }

        /// <summary>
        /// Read AssistantHub tool-call fields from Partio endpoint labels/tags.
        /// </summary>
        public static void ReadTagsToToolFields(PartioEndpointConfig endpoint)
        {
            if (endpoint == null) return;

            bool hasMetadata = HasLabel(endpoint.Labels, ToolCallingLabel)
                || TryGetTag(endpoint.Tags, SupportsToolCallingTag, out _)
                || TryGetTag(endpoint.Tags, ToolCallingApiFormatTag, out _)
                || TryGetTag(endpoint.Tags, SupportsParallelToolCallsTag, out _)
                || TryGetTag(endpoint.Tags, SupportsStreamingToolCallsTag, out _);

            if (!hasMetadata) return;

            bool supports = TryGetBoolTag(endpoint.Tags, SupportsToolCallingTag, out bool supportsValue)
                ? supportsValue
                : HasLabel(endpoint.Labels, ToolCallingLabel);

            endpoint.SupportsToolCalling = supports;
            endpoint.ToolCallingApiFormat = supports && TryGetTag(endpoint.Tags, ToolCallingApiFormatTag, out string format)
                ? format
                : null;
            endpoint.SupportsParallelToolCalls = supports && TryGetBoolTag(endpoint.Tags, SupportsParallelToolCallsTag, out bool parallel) && parallel;
            endpoint.SupportsStreamingToolCalls = supports && TryGetBoolTag(endpoint.Tags, SupportsStreamingToolCallsTag, out bool streaming) && streaming;
        }

        /// <summary>
        /// Read AssistantHub tool-call fields from Partio endpoint labels/tags.
        /// </summary>
        public static void ReadTagsToToolFields(IEnumerable<PartioEndpointConfig> endpoints)
        {
            if (endpoints == null) return;
            foreach (PartioEndpointConfig endpoint in endpoints)
                ReadTagsToToolFields(endpoint);
        }

        /// <summary>
        /// Serialize an endpoint request for Partio, removing AssistantHub-only top-level tool fields.
        /// </summary>
        public static string SerializePartioRequest(PartioEndpointRequest request)
        {
            if (request == null) return null;

            JsonNode node = JsonNode.Parse(JsonSerializer.Serialize(request));
            if (node is JsonObject obj)
            {
                foreach (string name in RequestToolFieldNames)
                    RemovePropertyIgnoreCase(obj, name);
            }

            return node?.ToJsonString() ?? "{}";
        }

        private static List<string> NormalizeLabels(List<string> labels)
        {
            if (labels == null) return new List<string>();

            return labels
                .Where(label => !String.IsNullOrWhiteSpace(label))
                .Select(label => label.Trim())
                .GroupBy(label => label, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        private static Dictionary<string, string> NormalizeTags(Dictionary<string, string> tags)
        {
            Dictionary<string, string> normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (tags == null) return normalized;

            foreach (KeyValuePair<string, string> tag in tags)
            {
                if (String.IsNullOrWhiteSpace(tag.Key)) continue;
                normalized[tag.Key.Trim()] = tag.Value;
            }

            return normalized;
        }

        private static void AddLabel(List<string> labels, string label)
        {
            if (labels == null || String.IsNullOrWhiteSpace(label)) return;
            if (!labels.Any(existing => String.Equals(existing, label, StringComparison.OrdinalIgnoreCase)))
                labels.Add(label);
        }

        private static void RemoveLabel(List<string> labels, string label)
        {
            if (labels == null || String.IsNullOrWhiteSpace(label)) return;
            labels.RemoveAll(existing => String.Equals(existing, label, StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasLabel(List<string> labels, string label)
        {
            return labels != null
                && labels.Any(existing => String.Equals(existing, label, StringComparison.OrdinalIgnoreCase));
        }

        private static void SetTag(Dictionary<string, string> tags, string key, string value)
        {
            if (tags == null || String.IsNullOrWhiteSpace(key)) return;

            RemoveTag(tags, key);
            if (value != null)
                tags[key] = value;
        }

        private static void RemoveTag(Dictionary<string, string> tags, string key)
        {
            if (tags == null || String.IsNullOrWhiteSpace(key)) return;

            string existingKey = tags.Keys.FirstOrDefault(candidate => String.Equals(candidate, key, StringComparison.OrdinalIgnoreCase));
            if (existingKey != null)
                tags.Remove(existingKey);
        }

        private static bool TryGetTag(Dictionary<string, string> tags, string key, out string value)
        {
            value = null;
            if (tags == null || String.IsNullOrWhiteSpace(key)) return false;

            foreach (KeyValuePair<string, string> tag in tags)
            {
                if (String.Equals(tag.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = tag.Value;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetBoolTag(Dictionary<string, string> tags, string key, out bool value)
        {
            value = false;
            if (!TryGetTag(tags, key, out string raw)) return false;

            if (Boolean.TryParse(raw, out bool parsed))
            {
                value = parsed;
                return true;
            }

            if (String.Equals(raw, "1", StringComparison.OrdinalIgnoreCase)
                || String.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (String.Equals(raw, "0", StringComparison.OrdinalIgnoreCase)
                || String.Equals(raw, "no", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            return false;
        }

        private static void RemovePropertyIgnoreCase(JsonObject obj, string propertyName)
        {
            string key = obj.Select(property => property.Key)
                .FirstOrDefault(key => String.Equals(key, propertyName, StringComparison.OrdinalIgnoreCase));
            if (key != null)
                obj.Remove(key);
        }
    }
}
