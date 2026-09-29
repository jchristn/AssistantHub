namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json.Nodes;

    /// <summary>
    /// AssistantHub's own per-endpoint timeouts. Partio owns endpoint records, so the values are stored as endpoint
    /// tags and exposed to API callers as top-level fields (<c>RequestTimeoutMs</c>, <c>UtilityTimeoutMs</c>). They
    /// bound the calls AssistantHub itself makes: answer and utility calls to a completion endpoint, and query
    /// embeddings through an embedding endpoint. Partio's <c>MaximumTimeoutMs</c> is separate and bounds Partio's side.
    /// Endpoints read by AssistantHub are remembered here so the chat rail can apply their timeouts.
    /// </summary>
    public static class PartioEndpointTimeouts
    {
        #region Public-Members

        /// <summary>Tag key for the request timeout.</summary>
        public const string RequestTimeoutTag = "AssistantHub.RequestTimeoutMs";

        /// <summary>Tag key for the utility-step timeout (completion endpoints).</summary>
        public const string UtilityTimeoutTag = "AssistantHub.UtilityTimeoutMs";

        /// <summary>Top-level request field for the request timeout.</summary>
        public const string RequestTimeoutField = "RequestTimeoutMs";

        /// <summary>Top-level request field for the utility-step timeout.</summary>
        public const string UtilityTimeoutField = "UtilityTimeoutMs";

        /// <summary>Smallest accepted timeout in milliseconds.</summary>
        public const int MinimumTimeoutMs = 1000;

        /// <summary>Largest accepted timeout in milliseconds (one hour).</summary>
        public const int MaximumTimeoutMs = 3600000;

        /// <summary>Top-level field names that are AssistantHub-only and must not be sent to Partio.</summary>
        public static readonly string[] FieldNames = { RequestTimeoutField, UtilityTimeoutField };

        #endregion

        #region Private-Members

        private static readonly ConcurrentDictionary<string, (int? RequestTimeoutMs, int? UtilityTimeoutMs)> _Known =
            new ConcurrentDictionary<string, (int? RequestTimeoutMs, int? UtilityTimeoutMs)>(StringComparer.Ordinal);

        #endregion

        #region Public-Methods

        /// <summary>
        /// Validate a timeout value from an API request.
        /// </summary>
        /// <param name="value">Value (null or 0 clears it).</param>
        /// <returns>An error message, or null when valid.</returns>
        public static string Validate(int? value)
        {
            if (!value.HasValue || value.Value == 0) return null;
            if (value.Value < MinimumTimeoutMs || value.Value > MaximumTimeoutMs)
                return "Timeouts must be between " + MinimumTimeoutMs + " and " + MaximumTimeoutMs + " milliseconds, or 0 to use the server default.";
            return null;
        }

        /// <summary>
        /// Validate the timeout fields in a raw endpoint request body.
        /// </summary>
        /// <param name="json">Request body.</param>
        /// <returns>An error message, or null when valid.</returns>
        public static string ValidateRequestJson(string json)
        {
            JsonObject obj = ParseObject(json);
            if (obj == null) return null;
            foreach (string field in FieldNames)
            {
                JsonNode node = GetIgnoreCase(obj, field, out _);
                if (node == null) continue;
                if (!TryGetInt(node, out int value)) return field + " must be a whole number of milliseconds.";
                string error = Validate(value);
                if (error != null) return field + ": " + error;
            }

            return null;
        }

        /// <summary>
        /// Move the timeout fields of an incoming request into endpoint tags, and remove the top-level fields.
        /// A value of 0 or null removes the tag (server default). Absent fields leave the tags unchanged.
        /// </summary>
        /// <param name="incoming">Caller request body.</param>
        /// <param name="tags">Merged tags (modified).</param>
        public static void FoldIntoTags(JsonObject incoming, JsonObject tags)
        {
            if (incoming == null || tags == null) return;
            FoldOne(incoming, tags, RequestTimeoutField, RequestTimeoutTag);
            FoldOne(incoming, tags, UtilityTimeoutField, UtilityTimeoutTag);
        }

        /// <summary>
        /// Remove the AssistantHub-only timeout fields from an endpoint body bound for Partio.
        /// </summary>
        /// <param name="obj">Endpoint body (modified).</param>
        public static void StripFields(JsonObject obj)
        {
            if (obj == null) return;
            foreach (string field in FieldNames)
            {
                GetIgnoreCase(obj, field, out string key);
                if (key != null) obj.Remove(key);
            }
        }

        /// <summary>
        /// Read an endpoint's timeouts from its tags into its top-level fields, and remember them by endpoint id.
        /// </summary>
        /// <param name="endpoint">Endpoint (modified).</param>
        public static void Apply(PartioEndpointConfig endpoint)
        {
            if (endpoint == null) return;
            endpoint.RequestTimeoutMs = ReadTag(endpoint.Tags, RequestTimeoutTag);
            endpoint.UtilityTimeoutMs = ReadTag(endpoint.Tags, UtilityTimeoutTag);
            Remember(endpoint.Id, endpoint.RequestTimeoutMs, endpoint.UtilityTimeoutMs);
        }

        /// <summary>
        /// Expose the timeout tags of a raw endpoint JSON document (an endpoint, a Partio envelope with <c>Data</c>,
        /// or an enumeration result with <c>Objects</c>) as top-level fields, and remember them by endpoint id.
        /// </summary>
        /// <param name="json">Endpoint JSON.</param>
        /// <returns>JSON with the fields added, or the input when it is not an endpoint document.</returns>
        public static string ExposeInJson(string json)
        {
            JsonObject obj = ParseObject(json);
            if (obj == null) return json;

            JsonArray list = (GetIgnoreCase(obj, "Objects", out _) ?? GetIgnoreCase(obj, "Data", out _)) as JsonArray;
            if (list != null)
            {
                foreach (JsonNode item in list)
                    if (item is JsonObject endpoint) ExposeOne(endpoint);
            }
            else
            {
                ExposeOne(obj);
            }

            return obj.ToJsonString();
        }

        /// <summary>
        /// Remember an endpoint's timeouts.
        /// </summary>
        /// <param name="endpointId">Endpoint id.</param>
        /// <param name="requestTimeoutMs">Request timeout, or null.</param>
        /// <param name="utilityTimeoutMs">Utility timeout, or null.</param>
        public static void Remember(string endpointId, int? requestTimeoutMs, int? utilityTimeoutMs)
        {
            if (String.IsNullOrEmpty(endpointId)) return;
            _Known[endpointId] = (requestTimeoutMs, utilityTimeoutMs);
        }

        /// <summary>
        /// The request timeout configured on an endpoint AssistantHub has read, or null for the server default.
        /// </summary>
        /// <param name="endpointId">Endpoint id.</param>
        /// <returns>Timeout in milliseconds, or null.</returns>
        public static int? GetRequestTimeoutMs(string endpointId)
        {
            return !String.IsNullOrEmpty(endpointId) && _Known.TryGetValue(endpointId, out var known) ? known.RequestTimeoutMs : null;
        }

        /// <summary>
        /// The utility-step timeout configured on a completion endpoint AssistantHub has read, or null for the default.
        /// </summary>
        /// <param name="endpointId">Endpoint id.</param>
        /// <returns>Timeout in milliseconds, or null.</returns>
        public static int? GetUtilityTimeoutMs(string endpointId)
        {
            return !String.IsNullOrEmpty(endpointId) && _Known.TryGetValue(endpointId, out var known) ? known.UtilityTimeoutMs : null;
        }

        /// <summary>
        /// Forget every remembered endpoint (tests).
        /// </summary>
        public static void Reset()
        {
            _Known.Clear();
        }

        /// <summary>
        /// Parse a timeout tag value.
        /// </summary>
        /// <param name="tags">Endpoint tags.</param>
        /// <param name="key">Tag key.</param>
        /// <returns>Timeout in milliseconds, or null when absent or invalid.</returns>
        public static int? ReadTag(IDictionary<string, string> tags, string key)
        {
            if (tags == null) return null;
            string value = tags.FirstOrDefault(t => String.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
            if (!Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ms)) return null;
            return ms >= MinimumTimeoutMs && ms <= MaximumTimeoutMs ? ms : null;
        }

        #endregion

        #region Private-Methods

        private static void FoldOne(JsonObject incoming, JsonObject tags, string field, string tag)
        {
            JsonNode node = GetIgnoreCase(incoming, field, out string key);
            if (key == null) return;

            string existing = tags.Select(t => t.Key).FirstOrDefault(k => String.Equals(k, tag, StringComparison.OrdinalIgnoreCase));
            if (existing != null) tags.Remove(existing);

            if (node != null && TryGetInt(node, out int value) && value > 0)
                tags[tag] = Math.Clamp(value, MinimumTimeoutMs, MaximumTimeoutMs).ToString(CultureInfo.InvariantCulture);
        }

        private static void ExposeOne(JsonObject endpoint)
        {
            Dictionary<string, string> tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (GetIgnoreCase(endpoint, "Tags", out _) is JsonObject tagObject)
            {
                foreach (KeyValuePair<string, JsonNode?> tag in tagObject)
                    tags[tag.Key] = tag.Value?.ToString();
            }

            int? request = ReadTag(tags, RequestTimeoutTag);
            int? utility = ReadTag(tags, UtilityTimeoutTag);
            endpoint[RequestTimeoutField] = request.HasValue ? JsonValue.Create(request.Value) : null;
            endpoint[UtilityTimeoutField] = utility.HasValue ? JsonValue.Create(utility.Value) : null;

            string id = GetIgnoreCase(endpoint, "Id", out _)?.ToString();
            Remember(id, request, utility);
        }

        private static bool TryGetInt(JsonNode node, out int value)
        {
            value = 0;
            if (node is not JsonValue v) return false;
            if (v.TryGetValue(out int i)) { value = i; return true; }
            if (v.TryGetValue(out long l) && l >= Int32.MinValue && l <= Int32.MaxValue) { value = (int)l; return true; }
            if (v.TryGetValue(out double d) && d == Math.Floor(d) && d >= Int32.MinValue && d <= Int32.MaxValue) { value = (int)d; return true; }
            if (v.TryGetValue(out string s) && Int32.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)) { value = parsed; return true; }
            return false;
        }

        private static JsonNode GetIgnoreCase(JsonObject obj, string name, out string key)
        {
            key = null;
            foreach (KeyValuePair<string, JsonNode?> property in obj)
            {
                if (String.Equals(property.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    key = property.Key;
                    return property.Value;
                }
            }

            return null;
        }

        private static JsonObject ParseObject(string json)
        {
            if (String.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonNode.Parse(json) as JsonObject;
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }

        #endregion
    }
}
