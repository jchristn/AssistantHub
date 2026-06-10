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

    internal class OpenAIMessage
    {
        /// <summary>
        /// Message role.
        /// </summary>
        public string Role { get; set; } = null;

        /// <summary>
        /// Message content.
        /// </summary>
        public string Content { get; set; } = null;

        /// <summary>
        /// Model-requested tool calls.
        /// </summary>
        [JsonPropertyName("tool_calls")]
        public List<AssistantModelToolCall> ToolCalls { get; set; } = null;

        /// <summary>
        /// Tool call identifier for tool-role messages.
        /// </summary>
        [JsonPropertyName("tool_call_id")]
        public string ToolCallId { get; set; } = null;

        /// <summary>
        /// Optional tool/function name.
        /// </summary>
        public string Name { get; set; } = null;
    }
}
