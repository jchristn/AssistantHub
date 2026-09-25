namespace Test.Benchmark.Runners
{
    using System.Collections.Generic;
    using System.Text.Json.Nodes;
    using Test.Benchmark.Datasets;

    /// <summary>
    /// The ingestion configuration a collection is built with. Defaults match what the dashboard creates for a new
    /// ingestion rule (FixedTokenCount, 256 tokens, no overlap, no summarization). Every field goes into the
    /// collection name's hash, so a changed configuration gets its own collection and never reuses stale chunks.
    /// </summary>
    public class IngestionVariant
    {
        #region Public-Members

        /// <summary>Chunking strategy.</summary>
        public string Strategy { get; set; } = "FixedTokenCount";

        /// <summary>Tokens per chunk.</summary>
        public int ChunkTokens { get; set; } = 256;

        /// <summary>Overlap tokens.</summary>
        public int Overlap { get; set; } = 0;

        /// <summary>Static context prefix, or null.</summary>
        public string? ContextPrefix { get; set; } = null;

        /// <summary>Completion endpoint for summarization, or null for none.</summary>
        public string? SummarizationEndpointId { get; set; } = null;

        /// <summary>Summarization order.</summary>
        public string SummarizationOrder { get; set; } = "BottomUp";

        /// <summary>Embedding endpoint id.</summary>
        public string EmbeddingEndpointId { get; set; } = "default";

        /// <summary>Embedding dimensionality of the collection.</summary>
        public int Dimensions { get; set; } = 384;

        /// <summary>L2-normalize embeddings.</summary>
        public bool L2Normalization { get; set; } = false;

        /// <summary>Free-form suffix to keep variants apart.</summary>
        public string Suffix { get; set; } = string.Empty;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Build from arguments.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>Variant.</returns>
        public static IngestionVariant From(BenchmarkArguments args)
        {
            return new IngestionVariant
            {
                Strategy = args.Get("chunk-strategy", "FixedTokenCount"),
                ChunkTokens = args.GetInt("chunk-tokens", 256),
                Overlap = args.GetInt("chunk-overlap", 0),
                ContextPrefix = args.GetOptional("context-prefix"),
                SummarizationEndpointId = args.GetOptional("summarize-endpoint"),
                SummarizationOrder = args.Get("summarize-order", "BottomUp"),
                EmbeddingEndpointId = args.Get("embedding-endpoint", "default"),
                Dimensions = args.GetInt("dimensions", 384),
                L2Normalization = args.GetFlag("l2-normalize"),
                Suffix = args.Get("scope-suffix", string.Empty)
            };
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Short stable hash of the configuration.
        /// </summary>
        /// <returns>8 hex characters.</returns>
        public string Hash()
        {
            string key = Strategy + "|" + ChunkTokens + "|" + Overlap + "|" + (ContextPrefix ?? "") + "|" + (SummarizationEndpointId ?? "") + "|"
                + SummarizationOrder + "|" + EmbeddingEndpointId + "|" + Dimensions + "|" + L2Normalization;
            return DatasetStore.Sha256(key).Substring(0, 8);
        }

        /// <summary>
        /// Configuration for reports.
        /// </summary>
        /// <returns>Name to value.</returns>
        public Dictionary<string, string> Describe()
        {
            Dictionary<string, string> d = new Dictionary<string, string>
            {
                ["chunking"] = Strategy + " " + ChunkTokens + " tokens, overlap " + Overlap,
                ["embeddingEndpoint"] = EmbeddingEndpointId + " (" + Dimensions + " dims" + (L2Normalization ? ", L2" : "") + ")",
                ["summarization"] = SummarizationEndpointId == null ? "off" : SummarizationEndpointId + " " + SummarizationOrder,
                ["ingestionHash"] = Hash()
            };
            if (ContextPrefix != null) d["contextPrefix"] = ContextPrefix;
            return d;
        }

        /// <summary>
        /// Ingestion rule body for the AssistantHub API.
        /// </summary>
        /// <param name="name">Rule name.</param>
        /// <param name="bucket">Bucket.</param>
        /// <param name="collectionName">Collection name.</param>
        /// <param name="collectionId">Collection id.</param>
        /// <returns>JSON body.</returns>
        public JsonObject ToRuleBody(string name, string bucket, string collectionName, string collectionId)
        {
            JsonObject chunking = new JsonObject
            {
                ["Strategy"] = Strategy,
                ["FixedTokenCount"] = ChunkTokens,
                ["OverlapCount"] = Overlap
            };
            if (!string.IsNullOrEmpty(ContextPrefix)) chunking["ContextPrefix"] = ContextPrefix;

            JsonObject body = new JsonObject
            {
                ["Name"] = name,
                ["Description"] = "Benchmark ingestion rule (managed by src/Test.Benchmark).",
                ["Bucket"] = bucket,
                ["CollectionName"] = collectionName,
                ["CollectionId"] = collectionId,
                ["Chunking"] = chunking,
                ["Embedding"] = new JsonObject { ["EmbeddingEndpointId"] = EmbeddingEndpointId, ["L2Normalization"] = L2Normalization }
            };
            if (!string.IsNullOrEmpty(SummarizationEndpointId))
                body["Summarization"] = new JsonObject { ["CompletionEndpointId"] = SummarizationEndpointId, ["Order"] = SummarizationOrder };
            return body;
        }

        #endregion
    }
}
