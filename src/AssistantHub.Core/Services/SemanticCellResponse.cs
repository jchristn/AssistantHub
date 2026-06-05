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
    /// Semantic cell response from the Partio v0.4.0 service.
    /// </summary>
    internal class SemanticCellResponse
    {
        public Guid GUID { get; set; }
        public Guid? ParentGUID { get; set; }
        public string Type { get; set; } = null;
        public string Text { get; set; } = null;
        public List<ChunkResult> Chunks { get; set; } = null;
        public List<SemanticCellResponse> Children { get; set; } = null;
    }
}
