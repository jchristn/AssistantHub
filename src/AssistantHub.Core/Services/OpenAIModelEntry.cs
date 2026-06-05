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

    internal class OpenAIModelEntry
    {
        /// <summary>
        /// Model identifier.
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Creation timestamp (Unix seconds).
        /// </summary>
        public long Created { get; set; } = 0;

        /// <summary>
        /// Model owner.
        /// </summary>
        [JsonPropertyName("owned_by")]
        public string OwnedBy { get; set; } = null;
    }
}
