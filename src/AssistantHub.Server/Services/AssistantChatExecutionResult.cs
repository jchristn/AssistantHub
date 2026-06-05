namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using Enums = AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Chat execution result.
    /// </summary>
    public class AssistantChatExecutionResult
    {
        /// <summary>
        /// Indicates whether execution succeeded.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Error message when execution fails.
        /// </summary>
        public string ErrorMessage { get; set; } = null;

        /// <summary>
        /// Assistant record used for execution.
        /// </summary>
        public Assistant Assistant { get; set; } = null;

        /// <summary>
        /// Assistant settings used for execution.
        /// </summary>
        public AssistantSettings AssistantSettings { get; set; } = null;

        /// <summary>
        /// OpenAI-compatible completion response.
        /// </summary>
        public ChatCompletionResponse Response { get; set; } = null;

        /// <summary>
        /// Canonical assistant response text after transport-agnostic cleanup.
        /// </summary>
        public string CanonicalResponseText { get; set; } = null;

        /// <summary>
        /// Persisted chat-history identifier when history was written.
        /// </summary>
        public string ChatHistoryId { get; set; } = null;
    }
}
