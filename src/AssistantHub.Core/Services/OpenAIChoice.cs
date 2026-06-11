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

    internal class OpenAIChoice
    {
        /// <summary>
        /// Message in the choice.
        /// </summary>
        public OpenAIMessage Message { get; set; } = null;

        /// <summary>
        /// Provider finish reason.
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; } = null;
    }
}
