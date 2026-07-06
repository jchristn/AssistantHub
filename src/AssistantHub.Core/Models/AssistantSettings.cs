namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using System.Text.Json;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Assistant settings record.
    /// </summary>
    public class AssistantSettings
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix aset_.
        /// </summary>
        public string Id
        {
            get => _Id;
            set => _Id = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Id));
        }

        /// <summary>
        /// Assistant identifier to which these settings belong.
        /// </summary>
        public string AssistantId
        {
            get => _AssistantId;
            set => _AssistantId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(AssistantId));
        }

        /// <summary>
        /// Sampling temperature (0.0 to 2.0).
        /// </summary>
        public double Temperature
        {
            get => _Temperature;
            set => _Temperature = (value >= 0.0 && value <= 2.0) ? value : throw new ArgumentOutOfRangeException(nameof(Temperature));
        }

        /// <summary>
        /// Top-p nucleus sampling (0.0 to 1.0).
        /// </summary>
        public double TopP
        {
            get => _TopP;
            set => _TopP = (value >= 0.0 && value <= 1.0) ? value : throw new ArgumentOutOfRangeException(nameof(TopP));
        }

        /// <summary>
        /// System prompt sent to the inference provider.
        /// </summary>
        public string SystemPrompt { get; set; } = "You are a helpful assistant. Use the provided context to answer questions accurately.";

        /// <summary>
        /// Maximum number of tokens to generate.
        /// </summary>
        public int MaxTokens
        {
            get => _MaxTokens;
            set => _MaxTokens = (value >= 1) ? value : throw new ArgumentOutOfRangeException(nameof(MaxTokens));
        }

        /// <summary>
        /// Context window size in tokens.
        /// </summary>
        public int ContextWindow
        {
            get => _ContextWindow;
            set => _ContextWindow = (value >= 1) ? value : throw new ArgumentOutOfRangeException(nameof(ContextWindow));
        }

        /// <summary>
        /// Whether RAG retrieval is enabled.
        /// </summary>
        public bool EnableRag { get; set; } = false;

        /// <summary>
        /// Whether the LLM-based retrieval gate is enabled.
        /// When enabled, an LLM call classifies whether each user message requires
        /// new retrieval or can be answered from existing conversation context.
        /// </summary>
        public bool EnableRetrievalGate { get; set; } = false;

        /// <summary>
        /// Whether LLM-based query rewrite is enabled.
        /// When enabled, the user's prompt is rewritten into multiple semantically
        /// varied queries before retrieval to improve recall.
        /// </summary>
        public bool EnableQueryRewrite { get; set; } = false;

        /// <summary>
        /// The prompt template used for query rewriting.
        /// Must contain the {prompt} placeholder which is replaced with the user's message.
        /// When null or empty, a built-in default prompt is used.
        /// </summary>
        public string QueryRewritePrompt { get; set; } = null;

        /// <summary>
        /// Whether LLM-based re-ranking of retrieved chunks is enabled.
        /// When enabled, retrieved chunks are scored by an LLM for relevance
        /// and low-scoring chunks are filtered out before context injection.
        /// </summary>
        public bool EnableReranking { get; set; } = false;

        /// <summary>
        /// Maximum number of chunks to keep after re-ranking (min 1).
        /// </summary>
        public int RerankerTopK
        {
            get => _RerankerTopK;
            set => _RerankerTopK = (value >= 1) ? value : throw new ArgumentOutOfRangeException(nameof(RerankerTopK));
        }

        /// <summary>
        /// Minimum LLM relevance score (0.0–10.0) for a chunk to survive re-ranking.
        /// </summary>
        public double RerankerScoreThreshold
        {
            get => _RerankerScoreThreshold;
            set => _RerankerScoreThreshold = (value >= 0.0 && value <= 10.0) ? value : throw new ArgumentOutOfRangeException(nameof(RerankerScoreThreshold));
        }

        /// <summary>
        /// Custom re-ranking prompt template. Must contain {query} and {chunks} placeholders.
        /// When null, a built-in default prompt is used.
        /// </summary>
        public string RerankPrompt { get; set; } = null;

        /// <summary>
        /// Whether to include citation metadata in chat completion responses.
        /// When enabled, retrieved context chunks are indexed in the system prompt
        /// and the model is instructed to cite sources using bracket notation [1], [2], etc.
        /// </summary>
        public bool EnableCitations { get; set; } = false;

        /// <summary>
        /// Controls document download linking in citation cards.
        /// Values: "None" (display-only), "Authenticated" (requires bearer token),
        /// "Public" (unauthenticated server-proxied download).
        /// </summary>
        public string CitationLinkMode { get; set; } = "None";

        /// <summary>
        /// Whether public assistant chat users may attach completed documents from the assistant collection.
        /// </summary>
        public bool EnableDocumentAttachments { get; set; } = false;

        /// <summary>
        /// Maximum number of documents that may be attached to one chat request.
        /// </summary>
        public int DocumentAttachmentMaxCount
        {
            get => _DocumentAttachmentMaxCount;
            set => _DocumentAttachmentMaxCount = (value >= 1 && value <= 100) ? value : throw new ArgumentOutOfRangeException(nameof(DocumentAttachmentMaxCount));
        }

        /// <summary>
        /// Whether public document-selection responses may include source URLs.
        /// </summary>
        public bool ExposeDocumentSourceUrls { get; set; } = false;

        /// <summary>
        /// Collection identifier for document retrieval.
        /// </summary>
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// Number of top results to retrieve.
        /// </summary>
        public int RetrievalTopK
        {
            get => _RetrievalTopK;
            set => _RetrievalTopK = (value >= 1) ? value : throw new ArgumentOutOfRangeException(nameof(RetrievalTopK));
        }

        /// <summary>
        /// Minimum score threshold for retrieval results (0.0 to 1.0).
        /// </summary>
        public double RetrievalScoreThreshold
        {
            get => _RetrievalScoreThreshold;
            set => _RetrievalScoreThreshold = (value >= 0.0 && value <= 1.0) ? value : throw new ArgumentOutOfRangeException(nameof(RetrievalScoreThreshold));
        }

        /// <summary>
        /// Search mode for retrieval: Vector, FullText, or Hybrid.
        /// </summary>
        public string SearchMode { get; set; } = "Vector";

        /// <summary>
        /// Weight of full-text score in hybrid mode (0.0 to 1.0).
        /// Formula: Score = (1.0 - TextWeight) * vectorScore + TextWeight * textScore.
        /// Only applies when SearchMode is "Hybrid".
        /// </summary>
        public double TextWeight { get; set; } = 0.3;

        /// <summary>
        /// Full-text ranking function: "TsRank" (term frequency) or "TsRankCd" (cover density, rewards proximity).
        /// </summary>
        public string FullTextSearchType { get; set; } = "TsRank";

        /// <summary>
        /// PostgreSQL text search language configuration.
        /// Controls stemming and stop words.
        /// </summary>
        public string FullTextLanguage { get; set; } = "english";

        /// <summary>
        /// Full-text score normalization bitmask. 32 = normalized 0-1 (recommended for hybrid).
        /// </summary>
        public int FullTextNormalization { get; set; } = 32;

        /// <summary>
        /// Minimum full-text score threshold. Documents with TextScore below this are excluded.
        /// Null means no threshold.
        /// </summary>
        public double? FullTextMinimumScore { get; set; } = null;

        /// <summary>
        /// Number of neighboring chunks to retrieve before and after each matched chunk (0-10).
        /// When set, each search result from RecallDB includes up to N chunks before and N chunks
        /// after the matched position within the same document. 0 means no neighbors.
        /// </summary>
        public int RetrievalIncludeNeighbors
        {
            get => _RetrievalIncludeNeighbors;
            set => _RetrievalIncludeNeighbors = Math.Clamp(value, 0, 10);
        }

        /// <summary>
        /// Completion endpoint identifier (references a managed Partio completion endpoint).
        /// </summary>
        public string InferenceEndpointId { get; set; } = null;

        /// <summary>
        /// Completion endpoint identifier used only for model-directed tool routing.
        /// When null or empty, the primary inference endpoint is used.
        /// </summary>
        public string ToolRoutingInferenceEndpointId { get; set; } = null;

        /// <summary>
        /// Completion endpoint identifier used for retrieval gate decisions.
        /// When null or empty, the primary inference endpoint is used.
        /// </summary>
        public string RetrievalGateInferenceEndpointId { get; set; } = null;

        /// <summary>
        /// Completion endpoint identifier used for query rewriting.
        /// When null or empty, the primary inference endpoint is used.
        /// </summary>
        public string QueryRewriteInferenceEndpointId { get; set; } = null;

        /// <summary>
        /// Completion endpoint identifier used for LLM re-ranking.
        /// When null or empty, the primary inference endpoint is used.
        /// </summary>
        public string RerankInferenceEndpointId { get; set; } = null;

        /// <summary>
        /// Whether to run an LLM-based answerability check after retrieval/rerank and before final generation.
        /// </summary>
        public bool EnableAnswerabilityCheck { get; set; } = false;

        /// <summary>
        /// Completion endpoint identifier used for answerability checks.
        /// When null or empty, the primary inference endpoint is used.
        /// </summary>
        public string AnswerabilityInferenceEndpointId { get; set; } = null;

        /// <summary>
        /// Answerability behavior mode. Values: LogOnly, AskClarifyingQuestion, ReturnUnsupported.
        /// </summary>
        public string AnswerabilityMode { get; set; } = "LogOnly";

        /// <summary>
        /// Custom answerability prompt template. When null, a built-in default prompt is used.
        /// </summary>
        public string AnswerabilityPrompt { get; set; } = null;

        /// <summary>
        /// Embedding endpoint identifier (overrides server-wide default for per-assistant RAG queries).
        /// </summary>
        public string EmbeddingEndpointId { get; set; } = null;

        /// <summary>
        /// Whether to load or warm configured endpoint models when a chat window is opened.
        /// </summary>
        public bool LoadModelsOnChatOpen { get; set; } = false;

        /// <summary>
        /// Whether provider thinking/reasoning text may be exposed in public assistant chat.
        /// </summary>
        public bool ExposeThinking { get; set; } = false;

        /// <summary>
        /// Title displayed as the heading on the chat window.
        /// </summary>
        public string Title { get; set; } = null;

        /// <summary>
        /// URL for the logo image shown in the chat window upper-left (max 192x192).
        /// </summary>
        public string LogoUrl { get; set; } = null;

        /// <summary>
        /// URL for the favicon shown in the browser tab.
        /// </summary>
        public string FaviconUrl { get; set; } = null;

        /// <summary>
        /// JSON-serialized label filter for retrieval (e.g. {"Required":["a"],"Excluded":["b"]}).
        /// </summary>
        public string RetrievalLabelFilter { get; set; } = null;

        /// <summary>
        /// JSON-serialized tag filter for retrieval (e.g. {"Required":[{"Key":"k","Condition":"Equals","Value":"v"}],"Excluded":[...]}).
        /// </summary>
        public string RetrievalTagFilter { get; set; } = null;

        /// <summary>
        /// Custom evaluation judge prompt template for RAG evaluation.
        /// Must contain {QUESTION}, {RESPONSE}, and {EXPECTED_FACT} placeholders.
        /// When null, a built-in default prompt is used.
        /// </summary>
        public string EvalJudgePrompt { get; set; } = null;

        /// <summary>
        /// Whether to enable SSE streaming for chat responses.
        /// </summary>
        public bool Streaming { get; set; } = true;

        /// <summary>
        /// Whether Slack integration is enabled for this assistant.
        /// </summary>
        public bool EnableSlack { get; set; } = false;

        /// <summary>
        /// Slack app-level token used for Socket Mode.
        /// </summary>
        public string SlackAppToken { get; set; } = null;

        /// <summary>
        /// Slack bot token used for chat posting and metadata lookup.
        /// </summary>
        public string SlackBotToken { get; set; } = null;

        /// <summary>
        /// Slack channel identifier for configured channel traffic.
        /// </summary>
        public string SlackChannelId { get; set; } = null;

        /// <summary>
        /// Start-of-message indicator required for configured channel traffic.
        /// </summary>
        public string SlackMessagePrefix { get; set; } = null;

        /// <summary>
        /// JSON-serialized AssistantToolPolicy controlling model-directed server-side tools.
        /// </summary>
        public string ToolPolicyJson
        {
            get => _ToolPolicyJson;
            set
            {
                _ToolPolicyJson = String.IsNullOrWhiteSpace(value) ? null : value.Trim();
                _ToolPolicy = null;
            }
        }

        /// <summary>
        /// Parsed AssistantToolPolicy controlling model-directed server-side tools.
        /// </summary>
        public AssistantToolPolicy ToolPolicy
        {
            get
            {
                if (_ToolPolicy != null) return _ToolPolicy;

                AssistantToolPolicy policy = ParseToolPolicyJson(_ToolPolicyJson) ?? new AssistantToolPolicy();
                policy.Normalize();
                _ToolPolicy = policy;
                return _ToolPolicy;
            }
            set
            {
                _ToolPolicy = value;
                if (_ToolPolicy == null)
                {
                    _ToolPolicyJson = null;
                    return;
                }

                _ToolPolicy.Normalize();
                _ToolPolicyJson = JsonSerializer.Serialize(_ToolPolicy, _ToolPolicyJsonOptions);
            }
        }

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the record was last updated in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.NewAssistantSettingsId();
        private string _AssistantId = "asst_placeholder";
        private double _Temperature = 0.7;
        private double _TopP = 1.0;
        private int _MaxTokens = 4096;
        private int _ContextWindow = 8192;
        private int _RetrievalTopK = 10;
        private double _RetrievalScoreThreshold = 0.3;
        private int _RerankerTopK = 5;
        private double _RerankerScoreThreshold = 3.0;
        private int _RetrievalIncludeNeighbors = 0;
        private int _DocumentAttachmentMaxCount = 10;
        private string _ToolPolicyJson = null;
        private AssistantToolPolicy _ToolPolicy = null;
        private static readonly JsonSerializerOptions _ToolPolicyJsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AssistantSettings()
        {
        }

        /// <summary>
        /// Create an AssistantSettings from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>AssistantSettings instance or null.</returns>
        public static AssistantSettings FromDataRow(DataRow row)
        {
            if (row == null) return null;
            AssistantSettings obj = new AssistantSettings();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.AssistantId = DataTableHelper.GetStringValue(row, "assistant_id");
            obj.Temperature = DataTableHelper.GetDoubleValue(row, "temperature", 0.7);
            obj.TopP = DataTableHelper.GetDoubleValue(row, "top_p", 1.0);
            obj.SystemPrompt = DataTableHelper.GetStringValue(row, "system_prompt");
            obj.MaxTokens = DataTableHelper.GetIntValue(row, "max_tokens", 4096);
            obj.ContextWindow = DataTableHelper.GetIntValue(row, "context_window", 8192);
            obj.EnableRag = DataTableHelper.GetBooleanValue(row, "enable_rag", false);
            obj.EnableRetrievalGate = DataTableHelper.GetBooleanValue(row, "enable_retrieval_gate", false);
            obj.EnableQueryRewrite = DataTableHelper.GetBooleanValue(row, "enable_query_rewrite", false);
            obj.QueryRewritePrompt = DataTableHelper.GetStringValue(row, "query_rewrite_prompt");
            obj.EnableReranking = DataTableHelper.GetBooleanValue(row, "enable_reranking", false);
            obj.RerankerTopK = DataTableHelper.GetIntValue(row, "reranker_top_k", 5);
            obj.RerankerScoreThreshold = DataTableHelper.GetDoubleValue(row, "reranker_score_threshold", 3.0);
            obj.RerankPrompt = DataTableHelper.GetStringValue(row, "rerank_prompt");
            obj.EnableCitations = DataTableHelper.GetBooleanValue(row, "enable_citations", false);
            obj.CitationLinkMode = DataTableHelper.GetStringValue(row, "citation_link_mode") ?? "None";
            obj.EnableDocumentAttachments = DataTableHelper.GetBooleanValue(row, "enable_document_attachments", false);
            obj.DocumentAttachmentMaxCount = DataTableHelper.GetIntValue(row, "document_attachment_max_count", 10);
            obj.ExposeDocumentSourceUrls = DataTableHelper.GetBooleanValue(row, "expose_document_source_urls", false);
            obj.CollectionId = DataTableHelper.GetStringValue(row, "collection_id");
            obj.RetrievalTopK = DataTableHelper.GetIntValue(row, "retrieval_top_k", 10);
            obj.RetrievalScoreThreshold = DataTableHelper.GetDoubleValue(row, "retrieval_score_threshold", 0.3);
            obj.SearchMode = DataTableHelper.GetStringValue(row, "search_mode") ?? "Vector";
            obj.TextWeight = DataTableHelper.GetDoubleValue(row, "text_weight", 0.3);
            obj.FullTextSearchType = DataTableHelper.GetStringValue(row, "fulltext_search_type") ?? "TsRank";
            obj.FullTextLanguage = DataTableHelper.GetStringValue(row, "fulltext_language") ?? "english";
            obj.FullTextNormalization = DataTableHelper.GetIntValue(row, "fulltext_normalization", 32);
            obj.FullTextMinimumScore = DataTableHelper.GetNullableDoubleValue(row, "fulltext_minimum_score");
            obj.RetrievalIncludeNeighbors = DataTableHelper.GetIntValue(row, "retrieval_include_neighbors", 0);
            obj.InferenceEndpointId = DataTableHelper.GetStringValue(row, "inference_endpoint_id");
            obj.ToolRoutingInferenceEndpointId = DataTableHelper.GetStringValue(row, "tool_routing_inference_endpoint_id");
            obj.RetrievalGateInferenceEndpointId = DataTableHelper.GetStringValue(row, "retrieval_gate_inference_endpoint_id");
            obj.QueryRewriteInferenceEndpointId = DataTableHelper.GetStringValue(row, "query_rewrite_inference_endpoint_id");
            obj.RerankInferenceEndpointId = DataTableHelper.GetStringValue(row, "rerank_inference_endpoint_id");
            obj.EnableAnswerabilityCheck = DataTableHelper.GetBooleanValue(row, "enable_answerability_check", false);
            obj.AnswerabilityInferenceEndpointId = DataTableHelper.GetStringValue(row, "answerability_inference_endpoint_id");
            obj.AnswerabilityMode = DataTableHelper.GetStringValue(row, "answerability_mode") ?? "LogOnly";
            obj.AnswerabilityPrompt = DataTableHelper.GetStringValue(row, "answerability_prompt");
            obj.EmbeddingEndpointId = DataTableHelper.GetStringValue(row, "embedding_endpoint_id");
            obj.LoadModelsOnChatOpen = DataTableHelper.GetBooleanValue(row, "load_models_on_chat_open", false);
            obj.ExposeThinking = DataTableHelper.GetBooleanValue(row, "expose_thinking", false);
            obj.Title = DataTableHelper.GetStringValue(row, "title");
            obj.LogoUrl = DataTableHelper.GetStringValue(row, "logo_url");
            obj.FaviconUrl = DataTableHelper.GetStringValue(row, "favicon_url");
            obj.RetrievalLabelFilter = DataTableHelper.GetStringValue(row, "retrieval_label_filter");
            obj.RetrievalTagFilter = DataTableHelper.GetStringValue(row, "retrieval_tag_filter");
            obj.EvalJudgePrompt = DataTableHelper.GetStringValue(row, "eval_judge_prompt");
            obj.Streaming = DataTableHelper.GetBooleanValue(row, "streaming", true);
            obj.EnableSlack = DataTableHelper.GetBooleanValue(row, "enable_slack", false);
            obj.SlackAppToken = DataTableHelper.GetStringValue(row, "slack_app_token");
            obj.SlackBotToken = DataTableHelper.GetStringValue(row, "slack_bot_token");
            obj.SlackChannelId = DataTableHelper.GetStringValue(row, "slack_channel_id");
            obj.SlackMessagePrefix = DataTableHelper.GetStringValue(row, "slack_message_prefix");
            obj.ToolPolicyJson = DataTableHelper.GetStringValue(row, "tool_policy_json");
            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            obj.LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc");
            return obj;
        }

        private static AssistantToolPolicy ParseToolPolicyJson(string json)
        {
            if (String.IsNullOrWhiteSpace(json)) return null;

            try
            {
                return JsonSerializer.Deserialize<AssistantToolPolicy>(json, _ToolPolicyJsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        #endregion
    }
}
