namespace AssistantHub.Core.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request body for <c>POST /v1.0/assistants/{assistantId}/retrieve</c>: run an assistant's retrieval pipeline
    /// (gate, rewrite, search, fusion, rerank, answerability) without final inference.
    /// </summary>
    public class AssistantRetrieveRequest
    {
        /// <summary>
        /// Conversation messages, as for chat. The last user message is the retrieval query. Optional when
        /// <see cref="Query"/> is set.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<ChatCompletionMessage> Messages { get; set; } = null;

        /// <summary>
        /// Single query text, shorthand for one user message.
        /// </summary>
        [JsonPropertyName("query")]
        public string Query { get; set; } = null;

        /// <summary>
        /// Optional request-level metadata filter, merged with the assistant's default filters as in chat.
        /// </summary>
        [JsonPropertyName("metadata_filter")]
        public ChatMetadataFilter MetadataFilter { get; set; } = null;

        /// <summary>
        /// Optional attached document identifiers that constrain retrieval, as in chat.
        /// </summary>
        [JsonPropertyName("attached_document_ids")]
        public List<string> AttachedDocumentIds { get; set; } = null;

        /// <summary>
        /// Include per-stage ranked lists in the response.
        /// </summary>
        [JsonPropertyName("include_stages")]
        public bool IncludeStages { get; set; } = true;

        /// <summary>
        /// Run the assistant's answerability check (when enabled in its settings) on the final chunks.
        /// </summary>
        [JsonPropertyName("include_answerability")]
        public bool IncludeAnswerability { get; set; } = true;

        /// <summary>
        /// Optional assistant settings to use for this call instead of the saved ones, for trying changes before
        /// saving them. The assistant's saved collection is used when the override has none. Nothing is persisted.
        /// </summary>
        [JsonPropertyName("settings_override")]
        public AssistantSettings SettingsOverride { get; set; } = null;
    }
}
