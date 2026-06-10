#pragma warning disable CS1591

namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Typed facade for assistant collection tools.
    /// </summary>
    public class CollectionToolService
    {
        private readonly IAssistantToolExecutor _Executor;

        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="executor">Assistant tool executor.</param>
        public CollectionToolService(IAssistantToolExecutor executor)
        {
            _Executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        /// <summary>
        /// Search the assistant collection through the policy-scoped collection_search tool.
        /// </summary>
        public Task<AssistantToolExecutionResult> SearchCollectionAsync(
            AssistantToolExecutionContext context,
            CollectionToolSearchRequest request,
            CancellationToken token = default)
        {
            return ExecuteAsync(context, "collection_search", request, token);
        }

        /// <summary>
        /// Read exact chunks from a completed assistant document.
        /// </summary>
        public Task<AssistantToolExecutionResult> ReadChunksAsync(
            AssistantToolExecutionContext context,
            CollectionToolReadChunksRequest request,
            CancellationToken token = default)
        {
            return ExecuteAsync(context, "collection_read_chunks", request, token);
        }

        /// <summary>
        /// Enumerate assistant-visible collection documents.
        /// </summary>
        public Task<AssistantToolExecutionResult> EnumerateDocumentsAsync(
            AssistantToolExecutionContext context,
            CollectionToolEnumerateDocumentsRequest request,
            CancellationToken token = default)
        {
            return ExecuteAsync(context, "collection_enumerate_documents", request, token);
        }

        private Task<AssistantToolExecutionResult> ExecuteAsync(
            AssistantToolExecutionContext context,
            string toolName,
            object request,
            CancellationToken token)
        {
            return _Executor.ExecuteAsync(
                context,
                new AssistantToolExecutionRequest
                {
                    ToolName = toolName,
                    ArgumentsJson = JsonSerializer.Serialize(request ?? new object(), _JsonOptions)
                },
                token);
        }
    }

    /// <summary>
    /// Strongly typed collection_search request.
    /// </summary>
    public class CollectionToolSearchRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; }

        [JsonPropertyName("queries")]
        public List<string> Queries { get; set; }

        [JsonPropertyName("max_results")]
        public int? MaxResults { get; set; }

        [JsonPropertyName("top_k")]
        public int? TopK { get; set; }

        [JsonPropertyName("include_neighbors")]
        public int? IncludeNeighbors { get; set; }

        [JsonPropertyName("strategy")]
        public string Strategy { get; set; }

        [JsonPropertyName("search_mode")]
        public string SearchMode { get; set; }

        [JsonPropertyName("score_threshold")]
        public double? ScoreThreshold { get; set; }

        [JsonPropertyName("document_ids")]
        public List<string> DocumentIds { get; set; }

        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; }

        [JsonPropertyName("required_labels")]
        public List<string> RequiredLabels { get; set; }

        [JsonPropertyName("excluded_labels")]
        public List<string> ExcludedLabels { get; set; }

        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; }

        [JsonPropertyName("required_tags")]
        public Dictionary<string, string> RequiredTags { get; set; }

        [JsonPropertyName("excluded_tags")]
        public Dictionary<string, string> ExcludedTags { get; set; }

        [JsonPropertyName("source_url_contains")]
        public string SourceUrlContains { get; set; }

        [JsonPropertyName("fulltext_search_type")]
        public string FullTextSearchType { get; set; }

        [JsonPropertyName("fulltext_language")]
        public string FullTextLanguage { get; set; }

        [JsonPropertyName("fulltext_normalization")]
        public int? FullTextNormalization { get; set; }

        [JsonPropertyName("fulltext_minimum_score")]
        public double? FullTextMinimumScore { get; set; }
    }

    /// <summary>
    /// Strongly typed collection_read_chunks request.
    /// </summary>
    public class CollectionToolReadChunksRequest
    {
        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; }

        [JsonPropertyName("positions")]
        public List<int> Positions { get; set; }

        [JsonPropertyName("ranges")]
        public List<CollectionToolChunkRange> Ranges { get; set; }

        [JsonPropertyName("max_chunks")]
        public int? MaxChunks { get; set; }

        [JsonPropertyName("neighbor_window")]
        public int? NeighborWindow { get; set; }
    }

    /// <summary>
    /// Strongly typed chunk range.
    /// </summary>
    public class CollectionToolChunkRange
    {
        [JsonPropertyName("start_position")]
        public int StartPosition { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    /// <summary>
    /// Strongly typed collection_enumerate_documents request.
    /// </summary>
    public class CollectionToolEnumerateDocumentsRequest
    {
        [JsonPropertyName("max_results")]
        public int? MaxResults { get; set; }

        [JsonPropertyName("continuation_token")]
        public string ContinuationToken { get; set; }

        [JsonPropertyName("query")]
        public string Query { get; set; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; }

        [JsonPropertyName("required_labels")]
        public List<string> RequiredLabels { get; set; }

        [JsonPropertyName("excluded_labels")]
        public List<string> ExcludedLabels { get; set; }

        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; }

        [JsonPropertyName("required_tags")]
        public Dictionary<string, string> RequiredTags { get; set; }

        [JsonPropertyName("excluded_tags")]
        public Dictionary<string, string> ExcludedTags { get; set; }

        [JsonPropertyName("source_url_contains")]
        public string SourceUrlContains { get; set; }
    }
}

#pragma warning restore CS1591
