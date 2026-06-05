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
    /// Search response from RecallDB.
    /// </summary>
    internal class SearchResponse
    {
        /// <summary>
        /// List of matching documents.
        /// </summary>
        public List<SearchResult> Documents { get; set; } = null;
    }
}
