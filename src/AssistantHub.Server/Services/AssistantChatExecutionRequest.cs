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
    /// Chat execution request.
    /// </summary>
    public class AssistantChatExecutionRequest
    {
        /// <summary>
        /// Assistant identifier.
        /// </summary>
        public string AssistantId { get; set; } = null;

        /// <summary>
        /// Optional preloaded assistant record.
        /// </summary>
        public Assistant Assistant { get; set; } = null;

        /// <summary>
        /// Optional preloaded assistant settings record.
        /// </summary>
        public AssistantSettings AssistantSettings { get; set; } = null;

        /// <summary>
        /// Conversation messages to execute.
        /// </summary>
        public List<ChatCompletionMessage> Messages { get; set; } = new List<ChatCompletionMessage>();

        /// <summary>
        /// Conversation thread identifier used for history persistence.
        /// </summary>
        public string ThreadId { get; set; } = null;

        /// <summary>
        /// Trace identifier used to correlate chat, request history, and performance events.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// Request-history identifier assigned by the HTTP pipeline when available.
        /// </summary>
        public string RequestHistoryId { get; set; } = null;

        /// <summary>
        /// Callback invoked after chat history is persisted.
        /// </summary>
        public Action<string> ChatHistoryPersisted { get; set; } = null;

        /// <summary>
        /// Optional model override.
        /// </summary>
        public string Model { get; set; } = null;

        /// <summary>
        /// Optional temperature override.
        /// </summary>
        public double? Temperature { get; set; } = null;

        /// <summary>
        /// Optional top-p override.
        /// </summary>
        public double? TopP { get; set; } = null;

        /// <summary>
        /// Optional max token override.
        /// </summary>
        public int? MaxTokens { get; set; } = null;

        /// <summary>
        /// Optional metadata filter override.
        /// </summary>
        public ChatMetadataFilter MetadataFilter { get; set; } = null;

        /// <summary>
        /// Optional AssistantDocument.Id values used to constrain retrieval for this chat turn.
        /// </summary>
        public List<string> AttachedDocumentIds { get; set; } = null;

        /// <summary>
        /// Optional user message timestamp override.
        /// </summary>
        public DateTime? UserMessageUtc { get; set; } = null;

        /// <summary>
        /// Request origin label persisted to history.
        /// </summary>
        public string Origin { get; set; } = "api";

        /// <summary>
        /// Optional callback invoked with safe tool lifecycle progress events.
        /// </summary>
        public Func<AssistantToolProgressEvent, Task> ToolProgress { get; set; } = null;
    }
}
