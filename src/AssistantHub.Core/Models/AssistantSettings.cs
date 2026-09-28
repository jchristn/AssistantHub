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
        /// Minimum vector similarity for retrieval results (0.0 to 1.0). In Hybrid mode it drops only chunks that the
        /// vector leg found on its own with a similarity below it; chunks the full-text leg found are kept. FullText
        /// results are not held to it; FullTextMinimumScore is their cutoff.
        /// </summary>
        public double RetrievalScoreThreshold
        {
            get => _RetrievalScoreThreshold;
            set => _RetrievalScoreThreshold = (value >= 0.0 && value <= 1.0) ? value : throw new ArgumentOutOfRangeException(nameof(RetrievalScoreThreshold));
        }

        /// <summary>
        /// Search mode for retrieval: Vector, FullText, or Hybrid.
        /// </summary>
        public string SearchMode { get; set; } = "Hybrid";

        /// <summary>
        /// Share of the full-text leg in hybrid mode (0.0 to 1.0); the vector leg gets 1.0 - TextWeight.
        /// With the default Rrf fusion the fused score is ((1 - w) / (k + vectorRank) + w / (k + textRank)) * (k + 1),
        /// so a chunk ranked first by both legs scores 1.0 and a chunk found only by the text leg scores at most w.
        /// With Linear fusion the score is a blend of the two legs' normalized scores. Only applies when SearchMode is "Hybrid".
        /// </summary>
        public double TextWeight { get; set; } = 0.5;

        /// <summary>
        /// How hybrid search combines its vector and full-text legs: "Rrf" (weighted reciprocal rank fusion, the
        /// default) or "Linear" (blend of the legs' normalized scores). Only applies when SearchMode is "Hybrid".
        /// </summary>
        public string FusionStrategy
        {
            get => _FusionStrategy;
            set => _FusionStrategy = NormalizeFusionStrategy(value);
        }

        /// <summary>
        /// The RRF constant k (1 to 100,000). Smaller values reward top ranks more strongly; larger values flatten
        /// the difference between adjacent ranks. Only applies to Rrf fusion. Default 60.
        /// </summary>
        public int RrfK
        {
            get => _RrfK;
            set => _RrfK = (value >= 1 && value <= 100000) ? value : throw new ArgumentOutOfRangeException(nameof(RrfK));
        }

        /// <summary>
        /// Number of candidates each hybrid leg retrieves before fusion (1 to 10,000). Null uses RecallDB's default,
        /// max(RetrievalTopK x 4, 100) capped at 1,000.
        /// </summary>
        public int? FusionCandidatePool
        {
            get => _FusionCandidatePool;
            set => _FusionCandidatePool = (value == null || (value.Value >= 1 && value.Value <= 10000)) ? value : throw new ArgumentOutOfRangeException(nameof(FusionCandidatePool));
        }

        /// <summary>
        /// Weight of a recency signal added to Rrf fusion (0.0 to 1.0): newer documents rank higher among otherwise
        /// similar candidates. 0 disables it. Only applies to Rrf fusion in hybrid mode.
        /// </summary>
        public double RecencyWeight
        {
            get => _RecencyWeight;
            set => _RecencyWeight = (value >= 0.0 && value <= 1.0) ? value : throw new ArgumentOutOfRangeException(nameof(RecencyWeight));
        }

        /// <summary>
        /// Order of retrieved context in the prompt: "Score" (most relevant first, the default) or "ReadingOrder"
        /// (chunks grouped by document, documents ordered by their best chunk, chunks in document order, with
        /// adjacent chunks merged). Does not change which chunks are retrieved or how they are ranked.
        /// </summary>
        public string ContextOrder
        {
            get => _ContextOrder;
            set => _ContextOrder = NormalizeContextOrder(value);
        }

        /// <summary>
        /// Prepend the embedding model's task prefix (for example "search_query: " for nomic-embed-text) to queries.
        /// Turn it on only for collections whose ingestion rule also uses task prefixes, so queries and documents match.
        /// </summary>
        public bool EmbeddingTaskPrefixes { get; set; } = false;

        /// <summary>
        /// Rewrite a follow-up question into a standalone question from the recent conversation, and search the
        /// rewrite alongside the original message. Runs only when there is earlier conversation.
        /// </summary>
        public bool EnableConversationRewrite { get; set; } = false;

        /// <summary>
        /// Optional prompt for the conversation rewrite. Must contain {question}; {conversation} is replaced with the recent turns. Null uses the
        /// built-in prompt.
        /// </summary>
        public string ConversationRewritePrompt { get; set; } = null;

        /// <summary>
        /// Reranker used when EnableReranking is true: "Llm" (the rerank inference endpoint scores candidates 0-10)
        /// or "CrossEncoder" (a cross-encoder rerank service from server settings, scores 0-1).
        /// </summary>
        public string RerankerType
        {
            get => _RerankerType;
            set => _RerankerType = NormalizeRerankerType(value);
        }

        /// <summary>
        /// Identifier of the cross-encoder reranker (from the server's Rerankers settings) used when RerankerType is
        /// "CrossEncoder". Null uses the first configured reranker.
        /// </summary>
        public string RerankEndpointId { get; set; } = null;

        /// <summary>
        /// Number of candidates retrieved for reranking (1 to 200). Retrieval fetches max(RetrievalTopK, this value)
        /// candidates when reranking is on, and the reranker keeps RerankerTopK of them. Default 20.
        /// </summary>
        public int RerankCandidateCount
        {
            get => _RerankCandidateCount;
            set => _RerankCandidateCount = (value >= 1 && value <= 200) ? value : throw new ArgumentOutOfRangeException(nameof(RerankCandidateCount));
        }

        /// <summary>
        /// Minimum cross-encoder score (0.0 to 1.0) for a candidate to count as relevant. When every candidate scores
        /// below it, no context is injected and the answer model is told nothing relevant was found. Null disables it.
        /// </summary>
        public double? RerankMinScore
        {
            get => _RerankMinScore;
            set => _RerankMinScore = (value == null || (value.Value >= 0.0 && value.Value <= 1.0)) ? value : throw new ArgumentOutOfRangeException(nameof(RerankMinScore));
        }

        /// <summary>
        /// How retrieval treats a document that another document supersedes: "Demote" (drop it and put its
        /// replacement in its place, the default), "Hide" (drop it) or "Include" (keep it, marked as outdated).
        /// </summary>
        public string SupersessionMode
        {
            get => _SupersessionMode;
            set => _SupersessionMode = NormalizeSupersessionMode(value);
        }

        /// <summary>
        /// Optional completion endpoint used to judge in-product Eval runs. Null or empty uses InferenceEndpointId,
        /// so the assistant grades its own answers.
        /// </summary>
        public string EvalJudgeInferenceEndpointId { get; set; } = null;

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
        private int _RerankerTopK = 10;
        private double _RerankerScoreThreshold = 3.0;
        private int _RetrievalIncludeNeighbors = 1;
        private string _FusionStrategy = "Rrf";
        private int _RrfK = 60;
        private int? _FusionCandidatePool = null;
        private double _RecencyWeight = 0.0;
        private string _ContextOrder = "Score";
        private string _RerankerType = "Llm";
        private int _RerankCandidateCount = 20;
        private double? _RerankMinScore = null;
        private string _SupersessionMode = "Demote";
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
            obj.FusionStrategy = DataTableHelper.GetStringValue(row, "fusion_strategy");
            obj.RrfK = Math.Clamp(DataTableHelper.GetIntValue(row, "rrf_k", 60), 1, 100000);
            int? fusionCandidatePool = DataTableHelper.GetNullableIntValue(row, "fusion_candidate_pool");
            obj.FusionCandidatePool = fusionCandidatePool.HasValue ? Math.Clamp(fusionCandidatePool.Value, 1, 10000) : null;
            obj.RecencyWeight = Math.Clamp(DataTableHelper.GetDoubleValue(row, "recency_weight", 0.0), 0.0, 1.0);
            obj.ContextOrder = DataTableHelper.GetStringValue(row, "context_order");
            obj.EvalJudgeInferenceEndpointId = DataTableHelper.GetStringValue(row, "eval_judge_inference_endpoint_id");
            obj.EmbeddingTaskPrefixes = DataTableHelper.GetBooleanValue(row, "embedding_task_prefixes", false);
            obj.EnableConversationRewrite = DataTableHelper.GetBooleanValue(row, "enable_conversation_rewrite", false);
            obj.ConversationRewritePrompt = DataTableHelper.GetStringValue(row, "conversation_rewrite_prompt");
            obj.RerankerType = DataTableHelper.GetStringValue(row, "reranker_type");
            obj.RerankEndpointId = DataTableHelper.GetStringValue(row, "rerank_endpoint_id");
            obj.RerankCandidateCount = Math.Clamp(DataTableHelper.GetIntValue(row, "rerank_candidate_count", 20), 1, 200);
            double? rerankMinScore = DataTableHelper.GetNullableDoubleValue(row, "rerank_min_score");
            obj.RerankMinScore = rerankMinScore;
            obj.SupersessionMode = DataTableHelper.GetStringValue(row, "supersession_mode");
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

        private static string NormalizeFusionStrategy(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "Rrf";
            if (value.Trim().Equals("Rrf", StringComparison.OrdinalIgnoreCase)) return "Rrf";
            if (value.Trim().Equals("Linear", StringComparison.OrdinalIgnoreCase)) return "Linear";
            throw new ArgumentOutOfRangeException(nameof(FusionStrategy), "FusionStrategy must be Rrf or Linear.");
        }

        private static string NormalizeRerankerType(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "Llm";
            if (value.Trim().Equals("Llm", StringComparison.OrdinalIgnoreCase)) return "Llm";
            if (value.Trim().Equals("CrossEncoder", StringComparison.OrdinalIgnoreCase)) return "CrossEncoder";
            throw new ArgumentOutOfRangeException(nameof(RerankerType), "RerankerType must be Llm or CrossEncoder.");
        }

        private static string NormalizeSupersessionMode(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "Demote";
            foreach (string mode in new[] { "Demote", "Hide", "Include" })
            {
                if (value.Trim().Equals(mode, StringComparison.OrdinalIgnoreCase)) return mode;
            }
            throw new ArgumentOutOfRangeException(nameof(SupersessionMode), "SupersessionMode must be Demote, Hide or Include.");
        }

        private static string NormalizeContextOrder(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "Score";
            if (value.Trim().Equals("Score", StringComparison.OrdinalIgnoreCase)) return "Score";
            if (value.Trim().Equals("ReadingOrder", StringComparison.OrdinalIgnoreCase)) return "ReadingOrder";
            throw new ArgumentOutOfRangeException(nameof(ContextOrder), "ContextOrder must be Score or ReadingOrder.");
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
