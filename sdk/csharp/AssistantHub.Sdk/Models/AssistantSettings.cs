namespace AssistantHub.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Assistant settings record.
    /// </summary>
    public class AssistantSettings
    {
        /// <summary>
        /// Unique identifier with prefix aset_.
        /// </summary>
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Assistant identifier to which these settings belong.
        /// </summary>
        [JsonPropertyName("AssistantId")]
        public string AssistantId { get; set; }

        /// <summary>
        /// Sampling temperature (0.0 to 2.0).
        /// </summary>
        [JsonPropertyName("Temperature")]
        public double Temperature { get; set; }

        /// <summary>
        /// Top-p nucleus sampling (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("TopP")]
        public double TopP { get; set; }

        /// <summary>
        /// System prompt sent to the inference provider.
        /// </summary>
        [JsonPropertyName("SystemPrompt")]
        public string SystemPrompt { get; set; }

        /// <summary>
        /// Maximum number of tokens to generate.
        /// </summary>
        [JsonPropertyName("MaxTokens")]
        public int MaxTokens { get; set; }

        /// <summary>
        /// Context window size in tokens.
        /// </summary>
        [JsonPropertyName("ContextWindow")]
        public int ContextWindow { get; set; }

        /// <summary>
        /// Whether RAG retrieval is enabled.
        /// </summary>
        [JsonPropertyName("EnableRag")]
        public bool EnableRag { get; set; }

        /// <summary>
        /// Whether the LLM-based retrieval gate is enabled.
        /// </summary>
        [JsonPropertyName("EnableRetrievalGate")]
        public bool EnableRetrievalGate { get; set; }

        /// <summary>
        /// Whether LLM-based query rewrite is enabled.
        /// </summary>
        [JsonPropertyName("EnableQueryRewrite")]
        public bool EnableQueryRewrite { get; set; }

        /// <summary>
        /// The prompt template used for query rewriting.
        /// </summary>
        [JsonPropertyName("QueryRewritePrompt")]
        public string QueryRewritePrompt { get; set; }

        /// <summary>
        /// Whether LLM-based re-ranking of retrieved chunks is enabled.
        /// </summary>
        [JsonPropertyName("EnableReranking")]
        public bool EnableReranking { get; set; }

        /// <summary>
        /// Maximum number of chunks to keep after re-ranking.
        /// </summary>
        [JsonPropertyName("RerankerTopK")]
        public int RerankerTopK { get; set; }

        /// <summary>
        /// Minimum LLM relevance score (0.0-10.0) for a chunk to survive re-ranking.
        /// </summary>
        [JsonPropertyName("RerankerScoreThreshold")]
        public double RerankerScoreThreshold { get; set; }

        /// <summary>
        /// Custom re-ranking prompt template.
        /// </summary>
        [JsonPropertyName("RerankPrompt")]
        public string RerankPrompt { get; set; }

        /// <summary>
        /// Whether to include citation metadata in chat completion responses.
        /// </summary>
        [JsonPropertyName("EnableCitations")]
        public bool EnableCitations { get; set; }

        /// <summary>
        /// Controls document download linking in citation cards.
        /// </summary>
        [JsonPropertyName("CitationLinkMode")]
        public string CitationLinkMode { get; set; }

        /// <summary>
        /// Collection identifier for document retrieval.
        /// </summary>
        [JsonPropertyName("CollectionId")]
        public string CollectionId { get; set; }

        /// <summary>
        /// Number of top results to retrieve.
        /// </summary>
        [JsonPropertyName("RetrievalTopK")]
        public int RetrievalTopK { get; set; }

        /// <summary>
        /// Minimum score threshold for retrieval results (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("RetrievalScoreThreshold")]
        public double RetrievalScoreThreshold { get; set; }

        /// <summary>
        /// Search mode for retrieval: Vector, FullText, or Hybrid.
        /// </summary>
        [JsonPropertyName("SearchMode")]
        public string SearchMode { get; set; }

        /// <summary>
        /// Weight of full-text score in hybrid mode (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("TextWeight")]
        public double TextWeight { get; set; }

        /// <summary>
        /// Full-text ranking function.
        /// </summary>
        [JsonPropertyName("FullTextSearchType")]
        public string FullTextSearchType { get; set; }

        /// <summary>
        /// PostgreSQL text search language configuration.
        /// </summary>
        [JsonPropertyName("FullTextLanguage")]
        public string FullTextLanguage { get; set; }

        /// <summary>
        /// Full-text score normalization bitmask.
        /// </summary>
        [JsonPropertyName("FullTextNormalization")]
        public int FullTextNormalization { get; set; }

        /// <summary>
        /// Minimum full-text score threshold.
        /// </summary>
        [JsonPropertyName("FullTextMinimumScore")]
        public double? FullTextMinimumScore { get; set; }

        /// <summary>
        /// Number of neighboring chunks to retrieve before and after each matched chunk (0-10).
        /// </summary>
        [JsonPropertyName("RetrievalIncludeNeighbors")]
        public int RetrievalIncludeNeighbors { get; set; }

        /// <summary>
        /// Completion endpoint identifier.
        /// </summary>
        [JsonPropertyName("InferenceEndpointId")]
        public string InferenceEndpointId { get; set; }

        /// <summary>
        /// Completion endpoint identifier used for retrieval gate calls. Falls back to InferenceEndpointId when unset.
        /// </summary>
        [JsonPropertyName("RetrievalGateInferenceEndpointId")]
        public string RetrievalGateInferenceEndpointId { get; set; }

        /// <summary>
        /// Completion endpoint identifier used for query rewrite calls. Falls back to InferenceEndpointId when unset.
        /// </summary>
        [JsonPropertyName("QueryRewriteInferenceEndpointId")]
        public string QueryRewriteInferenceEndpointId { get; set; }

        /// <summary>
        /// Completion endpoint identifier used for reranking calls. Falls back to InferenceEndpointId when unset.
        /// </summary>
        [JsonPropertyName("RerankInferenceEndpointId")]
        public string RerankInferenceEndpointId { get; set; }

        /// <summary>
        /// Embedding endpoint identifier.
        /// </summary>
        [JsonPropertyName("EmbeddingEndpointId")]
        public string EmbeddingEndpointId { get; set; }

        /// <summary>
        /// Whether to load or warm configured endpoint models when a chat window is opened.
        /// </summary>
        [JsonPropertyName("LoadModelsOnChatOpen")]
        public bool LoadModelsOnChatOpen { get; set; }

        /// <summary>
        /// Title displayed as the heading on the chat window.
        /// </summary>
        [JsonPropertyName("Title")]
        public string Title { get; set; }

        /// <summary>
        /// URL for the logo image shown in the chat window.
        /// </summary>
        [JsonPropertyName("LogoUrl")]
        public string LogoUrl { get; set; }

        /// <summary>
        /// URL for the favicon shown in the browser tab.
        /// </summary>
        [JsonPropertyName("FaviconUrl")]
        public string FaviconUrl { get; set; }

        /// <summary>
        /// JSON-serialized label filter for retrieval.
        /// </summary>
        [JsonPropertyName("RetrievalLabelFilter")]
        public string RetrievalLabelFilter { get; set; }

        /// <summary>
        /// JSON-serialized tag filter for retrieval.
        /// </summary>
        [JsonPropertyName("RetrievalTagFilter")]
        public string RetrievalTagFilter { get; set; }

        /// <summary>
        /// Custom evaluation judge prompt template for RAG evaluation.
        /// </summary>
        [JsonPropertyName("EvalJudgePrompt")]
        public string EvalJudgePrompt { get; set; }

        /// <summary>
        /// Whether to enable SSE streaming for chat responses.
        /// </summary>
        [JsonPropertyName("Streaming")]
        public bool Streaming { get; set; }

        /// <summary>
        /// Whether Slack integration is enabled for this assistant.
        /// </summary>
        [JsonPropertyName("EnableSlack")]
        public bool EnableSlack { get; set; }

        /// <summary>
        /// Slack app-level token used for Socket Mode.
        /// </summary>
        [JsonPropertyName("SlackAppToken")]
        public string SlackAppToken { get; set; }

        /// <summary>
        /// Slack bot token used for chat posting and metadata lookup.
        /// </summary>
        [JsonPropertyName("SlackBotToken")]
        public string SlackBotToken { get; set; }

        /// <summary>
        /// Slack channel identifier for configured channel traffic.
        /// </summary>
        [JsonPropertyName("SlackChannelId")]
        public string SlackChannelId { get; set; }

        /// <summary>
        /// Start-of-message indicator required for configured channel traffic.
        /// </summary>
        [JsonPropertyName("SlackMessagePrefix")]
        public string SlackMessagePrefix { get; set; }

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        [JsonPropertyName("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated in UTC.
        /// </summary>
        [JsonPropertyName("LastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; }
    }
}
