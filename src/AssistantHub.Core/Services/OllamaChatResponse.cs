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

    internal class OllamaChatResponse
    {
        /// <summary>
        /// Response message.
        /// </summary>
        public OllamaMessage Message { get; set; } = null;

        [JsonPropertyName("total_duration")]
        public long? TotalDuration { get; set; } = null;

        [JsonPropertyName("load_duration")]
        public long? LoadDuration { get; set; } = null;

        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; set; } = null;

        [JsonPropertyName("prompt_eval_duration")]
        public long? PromptEvalDuration { get; set; } = null;

        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; } = null;

        [JsonPropertyName("eval_duration")]
        public long? EvalDuration { get; set; } = null;
    }
}
