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
    /// A single chunk from the Partio process response.
    /// </summary>
    internal class ProcessChunk
    {
        public Guid CellGUID { get; set; }
        public string Text { get; set; } = null;
        public List<double> Embeddings { get; set; } = null;
    }
}
