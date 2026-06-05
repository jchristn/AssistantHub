namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    internal class OllamaModelEntry
    {
        /// <summary>
        /// Model name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Model size in bytes.
        /// </summary>
        public long Size { get; set; } = 0;

        /// <summary>
        /// Last modified timestamp.
        /// </summary>
        [JsonPropertyName("modified_at")]
        public DateTime? ModifiedAt { get; set; } = null;
    }
}
