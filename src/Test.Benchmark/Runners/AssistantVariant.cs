namespace Test.Benchmark.Runners
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json.Nodes;
    using Test.Benchmark.Datasets;

    /// <summary>
    /// The retrieval and chat configuration of one benchmark assistant. Defaults are AssistantHub's own defaults for a
    /// new assistant, except that responses are non-streaming (the harness scores JSON responses). Each distinct
    /// configuration becomes its own assistant pointing at the same collection, so a sweep never re-ingests.
    /// </summary>
    public class AssistantVariant
    {
        #region Public-Members

        /// <summary>Search mode: Vector, FullText or Hybrid.</summary>
        public string SearchMode { get; set; } = "Vector";

        /// <summary>Candidates retrieved.</summary>
        public int TopK { get; set; } = 10;

        /// <summary>Raw-score threshold.</summary>
        public double ScoreThreshold { get; set; } = 0.3;

        /// <summary>Hybrid text weight.</summary>
        public double TextWeight { get; set; } = 0.3;

        /// <summary>Hybrid fusion strategy (Rrf or Linear).</summary>
        public string FusionStrategy { get; set; } = "Rrf";

        /// <summary>RRF constant k.</summary>
        public int RrfK { get; set; } = 60;

        /// <summary>Candidates per hybrid leg before fusion (null: store default).</summary>
        public int? CandidatePool { get; set; } = null;

        /// <summary>Recency weight in RRF fusion.</summary>
        public double RecencyWeight { get; set; } = 0;

        /// <summary>Prompt context order (Score or ReadingOrder).</summary>
        public string ContextOrder { get; set; } = "Score";

        /// <summary>Full-text ranking function.</summary>
        public string FullTextSearchType { get; set; } = "TsRank";

        /// <summary>Neighbor chunks to include.</summary>
        public int IncludeNeighbors { get; set; } = 0;

        /// <summary>Query rewrite on.</summary>
        public bool QueryRewrite { get; set; } = false;

        /// <summary>LLM rerank on.</summary>
        public bool Rerank { get; set; } = false;

        /// <summary>Rerank output size.</summary>
        public int RerankTopK { get; set; } = 5;

        /// <summary>Rerank score threshold (0–10).</summary>
        public double RerankThreshold { get; set; } = 3;

        /// <summary>Retrieval gate on.</summary>
        public bool Gate { get; set; } = false;

        /// <summary>Answerability check on.</summary>
        public bool Answerability { get; set; } = false;

        /// <summary>Answerability mode.</summary>
        public string AnswerabilityMode { get; set; } = "LogOnly";

        /// <summary>Citations on.</summary>
        public bool Citations { get; set; } = false;

        /// <summary>Completion endpoint for answers.</summary>
        public string InferenceEndpointId { get; set; } = "default";

        /// <summary>Completion endpoint for utility calls (rewrite, rerank, gate, answerability), or null for the answer endpoint.</summary>
        public string? UtilityEndpointId { get; set; } = null;

        /// <summary>Embedding endpoint for queries.</summary>
        public string EmbeddingEndpointId { get; set; } = "default";

        /// <summary>Answer temperature.</summary>
        public double Temperature { get; set; } = 0.0;

        /// <summary>Reranker type: Llm or CrossEncoder.</summary>
        public string RerankerType { get; set; } = "Llm";

        /// <summary>Cross-encoder reranker id from the server's Rerankers settings, or null for the first one.</summary>
        public string? RerankEndpointId { get; set; } = null;

        /// <summary>Candidates retrieved and scored when rerank is on.</summary>
        public int RerankCandidates { get; set; } = 20;

        /// <summary>Cross-encoder minimum score (0–1), or null for none.</summary>
        public double? RerankMinScore { get; set; } = null;

        /// <summary>Rewrite follow-up questions into standalone questions before retrieval.</summary>
        public bool ConversationRewrite { get; set; } = false;

        /// <summary>Supersession mode: Demote, Hide or Include.</summary>
        public string Supersession { get; set; } = "Demote";

        /// <summary>Send the embedding model's query prefix.</summary>
        public bool TaskPrefixes { get; set; } = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Build from arguments.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <param name="mode">Search mode override.</param>
        /// <returns>Variant.</returns>
        public static AssistantVariant From(BenchmarkArguments args, string? mode = null)
        {
            return new AssistantVariant
            {
                SearchMode = mode ?? args.Get("mode", "Vector"),
                TopK = args.GetInt("k", 10),
                ScoreThreshold = args.GetDouble("threshold", 0.3),
                TextWeight = args.GetDouble("text-weight", 0.3),
                FusionStrategy = args.Get("fusion", "Rrf"),
                RrfK = args.GetInt("rrf-k", 60),
                CandidatePool = args.GetOptional("candidate-pool") == null ? null : args.GetInt("candidate-pool", 0),
                RecencyWeight = args.GetDouble("recency-weight", 0),
                ContextOrder = args.Get("context-order", "Score"),
                FullTextSearchType = args.Get("fulltext-type", "TsRank"),
                IncludeNeighbors = args.GetInt("neighbors", 0),
                QueryRewrite = args.GetFlag("rewrite"),
                Rerank = args.GetFlag("rerank"),
                RerankTopK = args.GetInt("rerank-k", 5),
                RerankThreshold = args.GetDouble("rerank-threshold", 3),
                Gate = args.GetFlag("gate"),
                Answerability = args.GetFlag("answerability"),
                AnswerabilityMode = args.Get("answerability-mode", "LogOnly"),
                Citations = args.GetFlag("citations"),
                InferenceEndpointId = args.Get("inference-endpoint", "default"),
                UtilityEndpointId = args.GetOptional("utility-endpoint"),
                EmbeddingEndpointId = args.Get("embedding-endpoint", "default"),
                Temperature = args.GetDouble("temperature", 0.0),
                RerankerType = args.Get("rerank-type", "Llm"),
                RerankEndpointId = args.GetOptional("rerank-endpoint"),
                RerankCandidates = args.GetInt("rerank-candidates", 20),
                RerankMinScore = args.GetOptional("rerank-min-score") == null ? null : args.GetDouble("rerank-min-score", 0),
                ConversationRewrite = args.GetFlag("conversation-rewrite"),
                Supersession = args.Get("supersession", "Demote"),
                TaskPrefixes = args.GetFlag("task-prefixes")
            };
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Clone with a different search mode.
        /// </summary>
        /// <param name="mode">Mode.</param>
        /// <returns>Copy.</returns>
        public AssistantVariant WithMode(string mode)
        {
            AssistantVariant copy = (AssistantVariant)MemberwiseClone();
            copy.SearchMode = mode;
            return copy;
        }

        /// <summary>
        /// Short stable hash of the configuration.
        /// </summary>
        /// <returns>8 hex characters.</returns>
        public string Hash()
        {
            return DatasetStore.Sha256(string.Join("|", Describe().Select(kv => kv.Key + "=" + kv.Value))).Substring(0, 8);
        }

        /// <summary>
        /// Configuration for reports.
        /// </summary>
        /// <returns>Name to value.</returns>
        public SortedDictionary<string, string> Describe()
        {
            SortedDictionary<string, string> d = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["searchMode"] = SearchMode,
                ["topK"] = TopK.ToString(CultureInfo.InvariantCulture),
                ["scoreThreshold"] = ScoreThreshold.ToString(CultureInfo.InvariantCulture),
                ["textWeight"] = TextWeight.ToString(CultureInfo.InvariantCulture),
                ["fullTextSearchType"] = FullTextSearchType,
                ["neighbors"] = IncludeNeighbors.ToString(CultureInfo.InvariantCulture),
                ["queryRewrite"] = QueryRewrite ? "on" : "off",
                ["rerank"] = Rerank ? "on (k " + RerankTopK + ", threshold " + RerankThreshold.ToString(CultureInfo.InvariantCulture) + ")" : "off",
                ["gate"] = Gate ? "on" : "off",
                ["answerability"] = Answerability ? AnswerabilityMode : "off",
                ["citations"] = Citations ? "on" : "off",
                ["inferenceEndpoint"] = InferenceEndpointId,
                ["utilityEndpoint"] = UtilityEndpointId ?? "(answer endpoint)",
                ["embeddingEndpoint"] = EmbeddingEndpointId,
                ["temperature"] = Temperature.ToString(CultureInfo.InvariantCulture)
            };

            // Settings added after the run ledger started appear only when they differ from the product default, so
            // default configurations keep the fingerprint (and assistant name) they had before the settings existed.
            if (!String.Equals(FusionStrategy, "Rrf", StringComparison.OrdinalIgnoreCase)) d["fusionStrategy"] = FusionStrategy;
            if (RrfK != 60) d["rrfK"] = RrfK.ToString(CultureInfo.InvariantCulture);
            if (CandidatePool.HasValue) d["candidatePool"] = CandidatePool.Value.ToString(CultureInfo.InvariantCulture);
            if (RecencyWeight != 0) d["recencyWeight"] = RecencyWeight.ToString(CultureInfo.InvariantCulture);
            if (!String.Equals(ContextOrder, "Score", StringComparison.OrdinalIgnoreCase)) d["contextOrder"] = ContextOrder;
            if (Rerank && !String.Equals(RerankerType, "Llm", StringComparison.OrdinalIgnoreCase)) d["rerankType"] = RerankerType + (RerankEndpointId != null ? " (" + RerankEndpointId + ")" : "");
            if (Rerank && RerankCandidates != 20) d["rerankCandidates"] = RerankCandidates.ToString(CultureInfo.InvariantCulture);
            if (Rerank && RerankMinScore.HasValue) d["rerankMinScore"] = RerankMinScore.Value.ToString(CultureInfo.InvariantCulture);
            if (ConversationRewrite) d["conversationRewrite"] = "on";
            if (!String.Equals(Supersession, "Demote", StringComparison.OrdinalIgnoreCase)) d["supersession"] = Supersession;
            if (TaskPrefixes) d["taskPrefixes"] = "on";
            return d;
        }

        /// <summary>
        /// Apply this variant to an assistant settings object read from the API.
        /// </summary>
        /// <param name="settings">Settings JSON (modified in place).</param>
        /// <param name="collectionId">Collection id.</param>
        public void ApplyTo(JsonObject settings, string collectionId)
        {
            settings["EnableRag"] = true;
            settings["CollectionId"] = collectionId;
            settings["SearchMode"] = SearchMode;
            settings["RetrievalTopK"] = TopK;
            settings["RetrievalScoreThreshold"] = ScoreThreshold;
            settings["TextWeight"] = TextWeight;
            settings["FusionStrategy"] = FusionStrategy;
            settings["RrfK"] = RrfK;
            settings["FusionCandidatePool"] = CandidatePool;
            settings["RecencyWeight"] = RecencyWeight;
            settings["ContextOrder"] = ContextOrder;
            settings["FullTextSearchType"] = FullTextSearchType;
            settings["RetrievalIncludeNeighbors"] = IncludeNeighbors;
            settings["EnableQueryRewrite"] = QueryRewrite;
            settings["EnableReranking"] = Rerank;
            settings["RerankerTopK"] = RerankTopK;
            settings["RerankerScoreThreshold"] = RerankThreshold;
            settings["EnableRetrievalGate"] = Gate;
            settings["EnableAnswerabilityCheck"] = Answerability;
            settings["AnswerabilityMode"] = AnswerabilityMode;
            settings["EnableCitations"] = Citations;
            settings["InferenceEndpointId"] = InferenceEndpointId;
            settings["EmbeddingEndpointId"] = EmbeddingEndpointId;
            settings["Temperature"] = Temperature;
            settings["RerankerType"] = RerankerType;
            settings["RerankEndpointId"] = RerankEndpointId;
            settings["RerankCandidateCount"] = RerankCandidates;
            settings["RerankMinScore"] = RerankMinScore;
            settings["EnableConversationRewrite"] = ConversationRewrite;
            settings["SupersessionMode"] = Supersession;
            settings["EmbeddingTaskPrefixes"] = TaskPrefixes;
            settings["Streaming"] = false;
            settings["EnableDocumentAttachments"] = true;
            settings["DocumentAttachmentMaxCount"] = 20;
            if (UtilityEndpointId != null)
            {
                settings["QueryRewriteInferenceEndpointId"] = UtilityEndpointId;
                settings["RerankInferenceEndpointId"] = UtilityEndpointId;
                settings["RetrievalGateInferenceEndpointId"] = UtilityEndpointId;
                settings["AnswerabilityInferenceEndpointId"] = UtilityEndpointId;
            }
        }

        #endregion
    }
}
