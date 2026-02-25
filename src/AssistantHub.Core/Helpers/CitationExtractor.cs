namespace AssistantHub.Core.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Extracts bracket-notation citation references from model response text.
    /// </summary>
    public static class CitationExtractor
    {
        /// <summary>
        /// Matches bracket-notation citations including comma-separated forms.
        /// Handles: [1], [2, 3], [1, 2, 3], \[1\], [8, 9, 10], etc.
        /// </summary>
        private static readonly Regex CitationPattern = new Regex(
            @"\\?\[(\d+(?:\s*,\s*\d+)*)\\?\]",
            RegexOptions.Compiled);

        /// <summary>
        /// Extracts individual numbers from a comma-separated capture group.
        /// </summary>
        private static readonly Regex NumberPattern = new Regex(
            @"\d+",
            RegexOptions.Compiled);

        /// <summary>
        /// Matches a trailing bibliography/reference list that some models generate.
        /// Detects blocks like:
        ///   [1] Document Name (Page 20)
        ///   [2] Document Name (Page 54)
        /// at the end of the response, optionally preceded by a heading.
        /// </summary>
        private static readonly Regex TrailingBibliographyPattern = new Regex(
            @"(?:\n[ \t]*(?:References|Sources|Bibliography|Citations|Works Cited)[:\s]*\n)?(?:[ \t]*\[(\d+)\][ \t]+\S[^\n]*\n?){2,}\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Build a ChatCompletionCitations object from the source manifest
        /// and the model's response text.
        /// </summary>
        /// <param name="sources">The citation source manifest built during context injection.</param>
        /// <param name="responseText">The completed model response text.</param>
        /// <returns>A populated ChatCompletionCitations object.</returns>
        public static ChatCompletionCitations Extract(
            List<CitationSource> sources,
            string responseText)
        {
            ChatCompletionCitations citations = new ChatCompletionCitations
            {
                Sources = sources ?? new List<CitationSource>()
            };

            if (string.IsNullOrEmpty(responseText) || sources == null || sources.Count == 0)
                return citations;

            // Strip any trailing bibliography the model generated so that only
            // genuine inline references are counted.
            string textForParsing = TrailingBibliographyPattern.Replace(responseText, "");

            int maxIndex = sources.Count;
            HashSet<int> referenced = new HashSet<int>();

            foreach (Match match in CitationPattern.Matches(textForParsing))
            {
                // The capture group may contain comma-separated numbers like "8, 9, 10"
                string captured = match.Groups[1].Value;
                foreach (Match numMatch in NumberPattern.Matches(captured))
                {
                    if (int.TryParse(numMatch.Value, out int index)
                        && index >= 1
                        && index <= maxIndex)
                    {
                        referenced.Add(index);
                    }
                }
            }

            if (referenced.Count > 0)
            {
                citations.ReferencedIndices = referenced.OrderBy(i => i).ToList();
            }
            else
            {
                // Model did not produce any inline citation markers.
                // Fall back to referencing one source per unique document so the
                // UI can still display them without duplicates, and flag this as
                // auto-populated for diagnostics.
                citations.ReferencedIndices = sources
                    .GroupBy(s => s.DocumentId)
                    .Select(g => g.OrderByDescending(s => s.Score).First().Index)
                    .OrderBy(i => i)
                    .ToList();
                citations.AutoPopulated = true;
            }

            return citations;
        }

        /// <summary>
        /// Remove any trailing model-generated bibliography/reference list from the response text.
        /// This is used to clean the response before sending it to the client.
        /// </summary>
        /// <param name="responseText">The raw model response text.</param>
        /// <returns>The response text with any trailing bibliography removed.</returns>
        public static string StripBibliography(string responseText)
        {
            if (string.IsNullOrEmpty(responseText))
                return responseText;

            return TrailingBibliographyPattern.Replace(responseText, "").TrimEnd();
        }
    }
}
