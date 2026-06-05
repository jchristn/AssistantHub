namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// A single search result from RecallDB.
    /// </summary>
    internal class SearchResult
    {
        /// <summary>
        /// Document identifier (maps to AssistantDocument.Id).
        /// </summary>
        public string DocumentId { get; set; } = null;

        /// <summary>
        /// Similarity score.
        /// </summary>
        public double Score { get; set; } = 0;

        /// <summary>
        /// Full-text relevance score (null in vector-only mode).
        /// </summary>
        public double? TextScore { get; set; }

        /// <summary>
        /// Text content of the matching chunk.
        /// </summary>
        public string Content { get; set; } = null;

        /// <summary>
        /// Positional index of this chunk within its source document.
        /// </summary>
        public int? Position { get; set; } = null;

        /// <summary>
        /// Neighboring chunks surrounding this match in positional order.
        /// Populated when IncludeNeighbors is specified on the search query.
        /// </summary>
        public List<SearchResult> Neighbors { get; set; } = null;
    }
}
