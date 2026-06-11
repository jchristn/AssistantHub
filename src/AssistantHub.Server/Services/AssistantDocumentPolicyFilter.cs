namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Applies assistant document visibility filters to document-backed surfaces.
    /// </summary>
    internal static class AssistantDocumentPolicyFilter
    {
        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        internal static ChatMetadataFilter BuildAssistantMetadataFilter(AssistantSettings settings)
        {
            if (settings == null) return null;

            ChatMetadataFilter filter = null;

            if (!String.IsNullOrWhiteSpace(settings.RetrievalLabelFilter))
            {
                Dictionary<string, List<string>> labelFilter =
                    JsonSerializer.Deserialize<Dictionary<string, List<string>>>(settings.RetrievalLabelFilter, _JsonOptions);

                if (labelFilter != null)
                {
                    filter ??= new ChatMetadataFilter();
                    labelFilter.TryGetValue("Required", out List<string> requiredLabels);
                    labelFilter.TryGetValue("Excluded", out List<string> excludedLabels);
                    filter.RequiredLabels = NormalizeList(requiredLabels);
                    filter.ExcludedLabels = NormalizeList(excludedLabels);
                }
            }

            if (!String.IsNullOrWhiteSpace(settings.RetrievalTagFilter))
            {
                Dictionary<string, List<ChatTagCondition>> tagFilter =
                    JsonSerializer.Deserialize<Dictionary<string, List<ChatTagCondition>>>(settings.RetrievalTagFilter, _JsonOptions);

                if (tagFilter != null)
                {
                    filter ??= new ChatMetadataFilter();
                    tagFilter.TryGetValue("Required", out List<ChatTagCondition> requiredTags);
                    tagFilter.TryGetValue("Excluded", out List<ChatTagCondition> excludedTags);
                    filter.RequiredTags = NormalizeTags(requiredTags);
                    filter.ExcludedTags = NormalizeTags(excludedTags);
                }
            }

            return filter != null && !filter.IsEmpty ? filter : null;
        }

        internal static bool MatchesAssistantMetadataFilters(AssistantDocument document, AssistantSettings settings)
        {
            ChatMetadataFilter filter = BuildAssistantMetadataFilter(settings);
            if (filter == null) return true;

            return MatchesMetadataFilter(document, filter);
        }

        internal static bool MatchesMetadataFilter(AssistantDocument document, ChatMetadataFilter filter)
        {
            if (document == null) return false;
            if (filter == null || filter.IsEmpty) return true;

            HashSet<string> labels = ParseLabels(document.Labels);
            Dictionary<string, string> tags = ParseTags(document.Tags);

            if (filter.RequiredLabels != null)
            {
                foreach (string label in filter.RequiredLabels)
                    if (!labels.Contains(label))
                        return false;
            }

            if (filter.ExcludedLabels != null)
            {
                foreach (string label in filter.ExcludedLabels)
                    if (labels.Contains(label))
                        return false;
            }

            if (filter.RequiredTags != null)
            {
                foreach (ChatTagCondition condition in filter.RequiredTags)
                    if (!MatchesTagCondition(tags, condition))
                        return false;
            }

            if (filter.ExcludedTags != null)
            {
                foreach (ChatTagCondition condition in filter.ExcludedTags)
                    if (MatchesTagCondition(tags, condition))
                        return false;
            }

            return true;
        }

        private static List<string> NormalizeList(List<string> values)
        {
            if (values == null) return null;

            List<string> normalized = values
                .Where(value => !String.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return normalized.Count > 0 ? normalized : null;
        }

        private static List<ChatTagCondition> NormalizeTags(List<ChatTagCondition> values)
        {
            if (values == null) return null;

            List<ChatTagCondition> normalized = values
                .Where(value => value != null && !String.IsNullOrWhiteSpace(value.Key))
                .Select(value => new ChatTagCondition
                {
                    Key = value.Key.Trim(),
                    Condition = String.IsNullOrWhiteSpace(value.Condition) ? "Equals" : value.Condition.Trim(),
                    Value = value.Value?.Trim()
                })
                .ToList();

            return normalized.Count > 0 ? normalized : null;
        }

        internal static HashSet<string> ParseLabels(string labelsJson)
        {
            HashSet<string> labels = new HashSet<string>(StringComparer.Ordinal);
            if (String.IsNullOrWhiteSpace(labelsJson)) return labels;

            try
            {
                List<string> parsed = JsonSerializer.Deserialize<List<string>>(labelsJson, _JsonOptions);
                if (parsed == null) return labels;

                foreach (string label in parsed)
                    if (!String.IsNullOrWhiteSpace(label))
                        labels.Add(label.Trim());
            }
            catch (JsonException)
            {
                return labels;
            }

            return labels;
        }

        internal static Dictionary<string, string> ParseTags(string tagsJson)
        {
            Dictionary<string, string> tags = new Dictionary<string, string>(StringComparer.Ordinal);
            if (String.IsNullOrWhiteSpace(tagsJson)) return tags;

            try
            {
                Dictionary<string, string> parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(tagsJson, _JsonOptions);
                if (parsed == null) return tags;

                foreach (KeyValuePair<string, string> tag in parsed)
                    if (!String.IsNullOrWhiteSpace(tag.Key))
                        tags[tag.Key.Trim()] = tag.Value;
            }
            catch (JsonException)
            {
                return tags;
            }

            return tags;
        }

        private static bool MatchesTagCondition(Dictionary<string, string> tags, ChatTagCondition condition)
        {
            if (tags == null || condition == null || String.IsNullOrWhiteSpace(condition.Key)) return false;

            string key = condition.Key.Trim();
            string operation = String.IsNullOrWhiteSpace(condition.Condition) ? "Equals" : condition.Condition.Trim();
            bool found = tags.TryGetValue(key, out string actual);
            string expected = condition.Value ?? String.Empty;

            if (String.Equals(operation, "IsNull", StringComparison.OrdinalIgnoreCase)
                || String.Equals(operation, "NotExists", StringComparison.OrdinalIgnoreCase))
                return !found || String.IsNullOrEmpty(actual);

            if (String.Equals(operation, "IsNotNull", StringComparison.OrdinalIgnoreCase)
                || String.Equals(operation, "Exists", StringComparison.OrdinalIgnoreCase))
                return found && !String.IsNullOrEmpty(actual);

            if (!found) return String.Equals(operation, "NotEquals", StringComparison.OrdinalIgnoreCase)
                || String.Equals(operation, "ContainsNot", StringComparison.OrdinalIgnoreCase);

            if (String.Equals(operation, "Equals", StringComparison.OrdinalIgnoreCase))
                return String.Equals(actual, expected, StringComparison.Ordinal);

            if (String.Equals(operation, "NotEquals", StringComparison.OrdinalIgnoreCase))
                return !String.Equals(actual, expected, StringComparison.Ordinal);

            if (String.Equals(operation, "Contains", StringComparison.OrdinalIgnoreCase))
                return actual != null && actual.IndexOf(expected, StringComparison.Ordinal) >= 0;

            if (String.Equals(operation, "ContainsNot", StringComparison.OrdinalIgnoreCase))
                return actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0;

            if (String.Equals(operation, "StartsWith", StringComparison.OrdinalIgnoreCase))
                return actual != null && actual.StartsWith(expected, StringComparison.Ordinal);

            if (String.Equals(operation, "EndsWith", StringComparison.OrdinalIgnoreCase))
                return actual != null && actual.EndsWith(expected, StringComparison.Ordinal);

            if (String.Equals(operation, "GreaterThan", StringComparison.OrdinalIgnoreCase))
            {
                return TryParseDouble(actual, out double actualNumber)
                    && TryParseDouble(expected, out double expectedNumber)
                    && actualNumber > expectedNumber;
            }

            if (String.Equals(operation, "LessThan", StringComparison.OrdinalIgnoreCase))
            {
                return TryParseDouble(actual, out double actualNumber)
                    && TryParseDouble(expected, out double expectedNumber)
                    && actualNumber < expectedNumber;
            }

            return String.Equals(actual, expected, StringComparison.Ordinal);
        }

        private static bool TryParseDouble(string value, out double result)
        {
            return Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }
}
