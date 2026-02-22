namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
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
        /// Model name or identifier.
        /// </summary>
        public string Model { get; set; } = "gemma3:4b";

        /// <summary>
        /// Whether RAG retrieval is enabled.
        /// </summary>
        public bool EnableRag { get; set; } = false;

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
        /// Completion endpoint identifier (references a managed Partio completion endpoint).
        /// </summary>
        public string InferenceEndpointId { get; set; } = null;

        /// <summary>
        /// Embedding endpoint identifier (overrides server-wide default for per-assistant RAG queries).
        /// </summary>
        public string EmbeddingEndpointId { get; set; } = null;

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
        /// Whether to enable SSE streaming for chat responses.
        /// </summary>
        public bool Streaming { get; set; } = true;

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
            obj.Model = DataTableHelper.GetStringValue(row, "model");
            obj.EnableRag = DataTableHelper.GetBooleanValue(row, "enable_rag", false);
            obj.CollectionId = DataTableHelper.GetStringValue(row, "collection_id");
            obj.RetrievalTopK = DataTableHelper.GetIntValue(row, "retrieval_top_k", 10);
            obj.RetrievalScoreThreshold = DataTableHelper.GetDoubleValue(row, "retrieval_score_threshold", 0.3);
            obj.SearchMode = DataTableHelper.GetStringValue(row, "search_mode") ?? "Vector";
            obj.TextWeight = DataTableHelper.GetDoubleValue(row, "text_weight", 0.3);
            obj.FullTextSearchType = DataTableHelper.GetStringValue(row, "fulltext_search_type") ?? "TsRank";
            obj.FullTextLanguage = DataTableHelper.GetStringValue(row, "fulltext_language") ?? "english";
            obj.FullTextNormalization = DataTableHelper.GetIntValue(row, "fulltext_normalization", 32);
            obj.FullTextMinimumScore = DataTableHelper.GetNullableDoubleValue(row, "fulltext_minimum_score");
            obj.InferenceEndpointId = DataTableHelper.GetStringValue(row, "inference_endpoint_id");
            obj.EmbeddingEndpointId = DataTableHelper.GetStringValue(row, "embedding_endpoint_id");
            obj.Title = DataTableHelper.GetStringValue(row, "title");
            obj.LogoUrl = DataTableHelper.GetStringValue(row, "logo_url");
            obj.FaviconUrl = DataTableHelper.GetStringValue(row, "favicon_url");
            obj.Streaming = DataTableHelper.GetBooleanValue(row, "streaming", true);
            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            obj.LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc");
            return obj;
        }

        #endregion
    }
}
