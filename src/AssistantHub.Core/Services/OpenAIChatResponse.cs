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

    internal class OpenAIChatResponse
    {
        /// <summary>
        /// Response choices.
        /// </summary>
        public List<OpenAIChoice> Choices { get; set; } = null;

        /// <summary>
        /// Token usage reported by OpenAI-compatible providers.
        /// </summary>
        public ChatCompletionUsage Usage { get; set; } = null;
    }
}
