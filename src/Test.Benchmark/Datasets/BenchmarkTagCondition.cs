namespace Test.Benchmark.Datasets
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A tag condition.
    /// </summary>
    public class BenchmarkTagCondition
    {
        #region Public-Members

        /// <summary>
        /// Tag key.
        /// </summary>
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Condition: Equals, NotEquals, Contains, StartsWith, EndsWith, GreaterThan, LessThan, IsNull, IsNotNull.
        /// </summary>
        [JsonPropertyName("condition")]
        public string Condition { get; set; } = "Equals";

        /// <summary>
        /// Comparison value.
        /// </summary>
        [JsonPropertyName("value")]
        public string? Value { get; set; } = null;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Whether a tag set satisfies the condition.
        /// </summary>
        /// <param name="tags">Document tags.</param>
        /// <returns>True when satisfied.</returns>
        public bool Matches(Dictionary<string, string> tags)
        {
            string? actual = null;
            foreach (KeyValuePair<string, string> tag in tags)
            {
                if (string.Equals(tag.Key, Key, StringComparison.OrdinalIgnoreCase)) actual = tag.Value;
            }

            string expected = Value ?? string.Empty;
            switch ((Condition ?? "Equals").ToLowerInvariant())
            {
                case "isnull": return actual == null;
                case "isnotnull": return actual != null;
                case "notequals": return actual == null || !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
            }

            if (actual == null) return false;
            switch ((Condition ?? "Equals").ToLowerInvariant())
            {
                case "contains": return actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
                case "startswith": return actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase);
                case "endswith": return actual.EndsWith(expected, StringComparison.OrdinalIgnoreCase);
                case "greaterthan": return Compare(actual, expected) > 0;
                case "lessthan": return Compare(actual, expected) < 0;
                default: return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
            }
        }

        #endregion

        #region Private-Methods

        private static int Compare(string actual, string expected)
        {
            if (double.TryParse(actual, NumberStyles.Float, CultureInfo.InvariantCulture, out double a)
                && double.TryParse(expected, NumberStyles.Float, CultureInfo.InvariantCulture, out double b))
                return a.CompareTo(b);
            return string.Compare(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
