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
        private static readonly Regex CitationPattern = new Regex(
            @"\\?\[(\d+)\\?\]",
            RegexOptions.Compiled);

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

            int maxIndex = sources.Count;
            HashSet<int> referenced = new HashSet<int>();

            foreach (Match match in CitationPattern.Matches(responseText))
            {
                if (int.TryParse(match.Groups[1].Value, out int index)
                    && index >= 1
                    && index <= maxIndex)
                {
                    referenced.Add(index);
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
    }
}
