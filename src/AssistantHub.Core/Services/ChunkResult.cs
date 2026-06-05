namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// A single chunk result from Partio v0.4.0.
    /// </summary>
    internal class ChunkResult
    {
        public Guid CellGUID { get; set; }
        public string Text { get; set; } = null;
        public List<string> Labels { get; set; } = null;
        public Dictionary<string, string> Tags { get; set; } = null;
        public List<float> Embeddings { get; set; } = null;
    }
}
