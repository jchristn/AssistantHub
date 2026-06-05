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
    /// Response from RecallDB when storing a document/embedding.
    /// </summary>
    internal class RecallDbStoreResponse
    {
        /// <summary>
        /// Document key (unique identifier within the collection).
        /// </summary>
        public string DocumentKey { get; set; } = null;
    }
}
