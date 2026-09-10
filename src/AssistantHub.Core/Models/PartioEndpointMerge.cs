namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Builds Partio endpoint create/update request bodies using a raw-JSON merge.
    ///
    /// Partio's update is a full replace, not a merge: any field omitted from the PUT body is reset to its
    /// default (health-check config, MaximumTimeoutMs, ContextSize, Tokenization) or wiped (ApiKey, other
    /// services' Tags/Labels). To avoid clobbering data AssistantHub does not manage, an update GETs the current
    /// endpoint and overlays ONLY the fields the caller actually sent, leaving everything else intact.
    /// Tags and labels are unioned so entries owned by other services (or future keys) survive.
    /// </summary>
    public static class PartioEndpointMerge
    {
        private static readonly string[] ToolFieldNames =
        {
            nameof(PartioEndpointRequest.SupportsToolCalling),
            nameof(PartioEndpointRequest.ToolCallingApiFormat),
            nameof(PartioEndpointRequest.SupportsParallelToolCalls),
            nameof(PartioEndpointRequest.SupportsStreamingToolCalls)
        };

        /// <summary>
        /// Build a create body: normalize the caller payload (fold tool-calling capabilities into tags/labels,
        /// strip AssistantHub-only tool fields, default the tenant). There is no existing endpoint to preserve.
        /// </summary>
        public static string BuildCreateBody(string callerBody, string defaultTenantId = "default")
        {
            return Merge("{}", callerBody, defaultTenantId);
        }

        /// <summary>
        /// Build an update body: overlay the caller's fields onto the existing endpoint JSON, preserving every
        /// field the caller did not send. A blank/absent ApiKey preserves the stored key.
        /// </summary>
        public static string BuildUpdateBody(string existingRawJson, string callerBody, string defaultTenantId = "default")
        {
            return Merge(existingRawJson, callerBody, defaultTenantId);
        }

        private static string Merge(string existingRawJson, string callerBody, string defaultTenantId)
        {
            JsonObject baseObj = ParseObject(existingRawJson) ?? new JsonObject();
            JsonObject incoming = ParseObject(callerBody) ?? new JsonObject();

            // Union tags/labels first so entries owned by other services (or future keys) are not wiped.
            JsonObject tags = MergeTags(GetObject(baseObj, "Tags"), GetObject(incoming, "Tags"));
            JsonArray labels = MergeLabels(GetArray(baseObj, "Labels"), GetArray(incoming, "Labels"));

            foreach (KeyValuePair<string, JsonNode?> prop in incoming.ToList())
            {
                string name = prop.Key;
                if (EqualsIgnoreCase(name, "Tags") || EqualsIgnoreCase(name, "Labels")) continue;
                if (IsToolField(name)) continue; // folded into tags/labels below
                if (EqualsIgnoreCase(name, "ApiKey") && IsBlank(prop.Value)) continue; // blank key preserves stored key
                SetProperty(baseObj, name, prop.Value?.DeepClone());
            }

            // Fold AssistantHub tool-calling capabilities (from the caller) into the merged tags/labels.
            FoldToolFields(incoming, tags, labels);

            SetProperty(baseObj, "Tags", tags);
            SetProperty(baseObj, "Labels", labels);
            StripToolFields(baseObj);

            if (!HasNonEmptyString(baseObj, "TenantId"))
                SetProperty(baseObj, "TenantId", JsonValue.Create(defaultTenantId));

            return baseObj.ToJsonString();
        }

        private static void FoldToolFields(JsonObject incoming, JsonObject tags, JsonArray labels)
        {
            if (!ToolFieldNames.Any(name => ContainsKey(incoming, name))) return;

            bool supports = GetBool(incoming, nameof(PartioEndpointRequest.SupportsToolCalling));
            if (supports)
            {
                AddLabel(labels, PartioEndpointToolMetadata.ToolCallingLabel);
                SetTag(tags, PartioEndpointToolMetadata.SupportsToolCallingTag, "true");
                SetTag(tags, PartioEndpointToolMetadata.ToolCallingApiFormatTag, GetString(incoming, nameof(PartioEndpointRequest.ToolCallingApiFormat)));
                SetTag(tags, PartioEndpointToolMetadata.SupportsParallelToolCallsTag, GetBool(incoming, nameof(PartioEndpointRequest.SupportsParallelToolCalls)) ? "true" : "false");
                SetTag(tags, PartioEndpointToolMetadata.SupportsStreamingToolCallsTag, GetBool(incoming, nameof(PartioEndpointRequest.SupportsStreamingToolCalls)) ? "true" : "false");
            }
            else
            {
                RemoveLabel(labels, PartioEndpointToolMetadata.ToolCallingLabel);
                SetTag(tags, PartioEndpointToolMetadata.SupportsToolCallingTag, "false");
                RemoveTag(tags, PartioEndpointToolMetadata.ToolCallingApiFormatTag);
                RemoveTag(tags, PartioEndpointToolMetadata.SupportsParallelToolCallsTag);
                RemoveTag(tags, PartioEndpointToolMetadata.SupportsStreamingToolCallsTag);
            }
        }

        private static void StripToolFields(JsonObject obj)
        {
            foreach (string name in ToolFieldNames)
                RemoveKeyIgnoreCase(obj, name);
        }

        private static JsonObject MergeTags(JsonObject baseTags, JsonObject incomingTags)
        {
            JsonObject result = new JsonObject();
            if (baseTags != null)
                foreach (KeyValuePair<string, JsonNode?> tag in baseTags)
                    result[tag.Key] = tag.Value?.DeepClone();
            if (incomingTags != null)
                foreach (KeyValuePair<string, JsonNode?> tag in incomingTags)
                    result[tag.Key] = tag.Value?.DeepClone(); // caller wins on conflicts
            return result;
        }

        private static JsonArray MergeLabels(JsonArray baseLabels, JsonArray incomingLabels)
        {
            JsonArray result = new JsonArray();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (JsonArray source in new[] { baseLabels, incomingLabels })
            {
                if (source == null) continue;
                foreach (JsonNode? node in source)
                {
                    string label = node?.GetValue<string>();
                    if (String.IsNullOrWhiteSpace(label)) continue;
                    if (seen.Add(label.Trim()))
                        result.Add(label.Trim());
                }
            }
            return result;
        }

        private static void AddLabel(JsonArray labels, string label)
        {
            if (labels == null || String.IsNullOrWhiteSpace(label)) return;
            if (!labels.Any(n => String.Equals(n?.GetValue<string>(), label, StringComparison.OrdinalIgnoreCase)))
                labels.Add(label);
        }

        private static void RemoveLabel(JsonArray labels, string label)
        {
            if (labels == null || String.IsNullOrWhiteSpace(label)) return;
            for (int i = labels.Count - 1; i >= 0; i--)
            {
                if (String.Equals(labels[i]?.GetValue<string>(), label, StringComparison.OrdinalIgnoreCase))
                    labels.RemoveAt(i);
            }
        }

        private static void SetTag(JsonObject tags, string key, string value)
        {
            if (tags == null || String.IsNullOrWhiteSpace(key)) return;
            RemoveTag(tags, key);
            if (value != null) tags[key] = value;
        }

        private static void RemoveTag(JsonObject tags, string key)
        {
            if (tags == null || String.IsNullOrWhiteSpace(key)) return;
            string existing = tags.Select(t => t.Key).FirstOrDefault(k => String.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            if (existing != null) tags.Remove(existing);
        }

        private static JsonObject ParseObject(string json)
        {
            if (String.IsNullOrWhiteSpace(json)) return null;
            try { return JsonNode.Parse(json) as JsonObject; }
            catch { return null; }
        }

        private static JsonObject GetObject(JsonObject obj, string name)
        {
            foreach (KeyValuePair<string, JsonNode?> prop in obj)
                if (EqualsIgnoreCase(prop.Key, name) && prop.Value is JsonObject child) return child;
            return null;
        }

        private static JsonArray GetArray(JsonObject obj, string name)
        {
            foreach (KeyValuePair<string, JsonNode?> prop in obj)
                if (EqualsIgnoreCase(prop.Key, name) && prop.Value is JsonArray child) return child;
            return null;
        }

        private static void SetProperty(JsonObject obj, string name, JsonNode value)
        {
            RemoveKeyIgnoreCase(obj, name);
            obj[name] = value;
        }

        private static void RemoveKeyIgnoreCase(JsonObject obj, string name)
        {
            string existing = obj.Select(p => p.Key).FirstOrDefault(k => EqualsIgnoreCase(k, name));
            if (existing != null) obj.Remove(existing);
        }

        private static bool ContainsKey(JsonObject obj, string name)
        {
            return obj.Any(p => EqualsIgnoreCase(p.Key, name));
        }

        private static bool HasNonEmptyString(JsonObject obj, string name)
        {
            foreach (KeyValuePair<string, JsonNode?> prop in obj)
            {
                if (EqualsIgnoreCase(prop.Key, name) && prop.Value is JsonValue value
                    && value.TryGetValue(out string str) && !String.IsNullOrWhiteSpace(str))
                    return true;
            }
            return false;
        }

        private static bool GetBool(JsonObject obj, string name)
        {
            foreach (KeyValuePair<string, JsonNode?> prop in obj)
            {
                if (!EqualsIgnoreCase(prop.Key, name)) continue;
                if (prop.Value is JsonValue value)
                {
                    if (value.TryGetValue(out bool b)) return b;
                    if (value.TryGetValue(out string s) && Boolean.TryParse(s, out bool parsed)) return parsed;
                }
                return false;
            }
            return false;
        }

        private static string GetString(JsonObject obj, string name)
        {
            foreach (KeyValuePair<string, JsonNode?> prop in obj)
            {
                if (EqualsIgnoreCase(prop.Key, name) && prop.Value is JsonValue value && value.TryGetValue(out string s))
                    return s;
            }
            return null;
        }

        private static bool IsBlank(JsonNode node)
        {
            if (node is JsonValue value)
            {
                if (value.TryGetValue(out string s)) return String.IsNullOrWhiteSpace(s);
                return false;
            }
            return node == null;
        }

        private static bool IsToolField(string name)
        {
            return ToolFieldNames.Any(n => EqualsIgnoreCase(n, name));
        }

        private static bool EqualsIgnoreCase(string a, string b)
        {
            return String.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
