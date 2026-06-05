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
    /// Type detection response from DocumentAtom.
    /// </summary>
    internal class TypeDetectResponse
    {
        /// <summary>
        /// Detected MIME type.
        /// </summary>
        public string MimeType { get; set; } = null;

        /// <summary>
        /// Detected document type.
        /// </summary>
        public string Type { get; set; } = null;
    }
}
