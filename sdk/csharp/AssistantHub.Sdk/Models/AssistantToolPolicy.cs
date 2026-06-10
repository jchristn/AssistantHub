namespace AssistantHub.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Administrator-controlled policy for server-side tools exposed to a model.
    /// </summary>
    public class AssistantToolPolicy
    {
        /// <summary>
        /// Master switch for model-directed server-side tool calls.
        /// </summary>
        [JsonPropertyName("EnableToolCalls")]
        public bool EnableToolCalls { get; set; }

        /// <summary>
        /// Maximum tool-call loop iterations allowed for one chat turn.
        /// </summary>
        [JsonPropertyName("MaxToolIterations")]
        public int MaxToolIterations { get; set; } = 6;

        /// <summary>
        /// Maximum individual tool calls allowed for one chat turn.
        /// </summary>
        [JsonPropertyName("MaxToolCallsPerTurn")]
        public int MaxToolCallsPerTurn { get; set; } = 12;

        /// <summary>
        /// Tool-choice mode for compatible providers.
        /// </summary>
        [JsonPropertyName("ToolChoiceMode")]
        public string ToolChoiceMode { get; set; } = "Auto";

        /// <summary>
        /// Maximum parallel tool calls accepted from one model response.
        /// </summary>
        [JsonPropertyName("MaxParallelToolCalls")]
        public int MaxParallelToolCalls { get; set; } = 1;

        /// <summary>
        /// Allow parallel tool call execution.
        /// </summary>
        [JsonPropertyName("AllowParallelToolCalls")]
        public bool AllowParallelToolCalls { get; set; }

        /// <summary>
        /// Per-tool timeout in milliseconds.
        /// </summary>
        [JsonPropertyName("ToolCallTimeoutMs")]
        public int ToolCallTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Maximum characters returned to the model from a single tool call.
        /// </summary>
        [JsonPropertyName("MaxToolOutputChars")]
        public int MaxToolOutputChars { get; set; } = 12000;

        /// <summary>
        /// Maximum aggregate tool-output characters per chat turn.
        /// </summary>
        [JsonPropertyName("MaxToolOutputCharactersPerTurn")]
        public int MaxToolOutputCharactersPerTurn { get; set; } = 50000;

        /// <summary>
        /// Maximum model-visible result items returned by supported tools.
        /// </summary>
        [JsonPropertyName("MaxToolResultItems")]
        public int MaxToolResultItems { get; set; } = 20;

        /// <summary>
        /// Expose safe tool traces to public users.
        /// </summary>
        [JsonPropertyName("ExposeToolTraceToUser")]
        public bool ExposeToolTraceToUser { get; set; }

        /// <summary>
        /// Persist redacted tool arguments for diagnostics.
        /// </summary>
        [JsonPropertyName("PersistToolArguments")]
        public bool PersistToolArguments { get; set; } = true;

        /// <summary>
        /// Persist full tool outputs.
        /// </summary>
        [JsonPropertyName("PersistToolOutputs")]
        public bool PersistToolOutputs { get; set; }

        /// <summary>
        /// Require citations for tool-derived evidence where supported.
        /// </summary>
        [JsonPropertyName("RequireCitationsForToolEvidence")]
        public bool RequireCitationsForToolEvidence { get; set; } = true;

        /// <summary>
        /// Optional final allow-list of model-visible tool names.
        /// </summary>
        [JsonPropertyName("AllowedToolNames")]
        public List<string> AllowedToolNames { get; set; } = new List<string>();

        /// <summary>
        /// Whether streaming chat clients may receive safe tool-progress events.
        /// </summary>
        [JsonPropertyName("EnableToolFeedbackEvents")]
        public bool EnableToolFeedbackEvents { get; set; } = true;

        /// <summary>
        /// Whether Slack channels receive safe tool-progress lifecycle messages.
        /// </summary>
        [JsonPropertyName("EnableSlackToolProgressMessages")]
        public bool EnableSlackToolProgressMessages { get; set; } = true;

        /// <summary>
        /// Enable exhaustive or bounded collection search.
        /// </summary>
        [JsonPropertyName("EnableCollectionSearchTool")]
        public bool EnableCollectionSearchTool { get; set; }

        /// <summary>
        /// Enable exact collection chunk reads by document ID and position.
        /// </summary>
        [JsonPropertyName("EnableCollectionReadChunksTool")]
        public bool EnableCollectionReadChunksTool { get; set; }

        /// <summary>
        /// Enable Verbex full-text search.
        /// </summary>
        [JsonPropertyName("EnableVerbexFullTextSearchTool")]
        public bool EnableVerbexFullTextSearchTool { get; set; }

        /// <summary>
        /// Enable safe S3 object reads.
        /// </summary>
        [JsonPropertyName("EnableS3ObjectReadTool")]
        public bool EnableS3ObjectReadTool { get; set; }

        /// <summary>
        /// Enable assistant collection document enumeration.
        /// </summary>
        [JsonPropertyName("EnableCollectionEnumerateDocumentsTool")]
        public bool EnableCollectionEnumerateDocumentsTool { get; set; }

        /// <summary>
        /// Alias for enabling assistant collection document enumeration.
        /// </summary>
        [JsonPropertyName("EnableCollectionEnumerationTool")]
        public bool EnableCollectionEnumerationTool { get; set; }

        /// <summary>
        /// Enable Verbex index record enumeration.
        /// </summary>
        [JsonPropertyName("EnableIndexEnumerateRecordsTool")]
        public bool EnableIndexEnumerateRecordsTool { get; set; }

        /// <summary>
        /// Enable S3 bucket object enumeration.
        /// </summary>
        [JsonPropertyName("EnableBucketEnumerateObjectsTool")]
        public bool EnableBucketEnumerateObjectsTool { get; set; }

        /// <summary>
        /// Enable Tavily-backed web search.
        /// </summary>
        [JsonPropertyName("EnableWebSearchTool")]
        public bool EnableWebSearchTool { get; set; }

        /// <summary>
        /// Assistant-level Tavily endpoint override. Empty uses the system-level provider.
        /// </summary>
        [JsonPropertyName("TavilyEndpoint")]
        public string TavilyEndpoint { get; set; }

        /// <summary>
        /// Assistant-level Tavily API key override. Empty uses the system-level provider.
        /// </summary>
        [JsonPropertyName("TavilyApiKey")]
        public string TavilyApiKey { get; set; }

        /// <summary>
        /// Maximum search results a model may request in one tool call.
        /// </summary>
        [JsonPropertyName("MaxSearchResultsPerCall")]
        public int MaxSearchResultsPerCall { get; set; } = 10;

        /// <summary>
        /// Maximum collection search top-k.
        /// </summary>
        [JsonPropertyName("MaxSearchTopK")]
        public int MaxSearchTopK { get; set; } = 50;

        /// <summary>
        /// Maximum search queries a model may submit in one collection search call.
        /// </summary>
        [JsonPropertyName("MaxSearchQueriesPerCall")]
        public int MaxSearchQueriesPerCall { get; set; } = 3;

        /// <summary>
        /// Maximum assistant-visible documents a collection search may consider.
        /// </summary>
        [JsonPropertyName("MaxDocumentsConsideredPerSearch")]
        public int MaxDocumentsConsideredPerSearch { get; set; } = 1000;

        /// <summary>
        /// Maximum raw retrieval results a collection search may consider across all search passes.
        /// </summary>
        [JsonPropertyName("MaxResultsConsideredPerSearch")]
        public int MaxResultsConsideredPerSearch { get; set; } = 1000;

        /// <summary>
        /// Allow the server to add deterministic query variants for collection search.
        /// </summary>
        [JsonPropertyName("EnableServerGeneratedQueryVariants")]
        public bool EnableServerGeneratedQueryVariants { get; set; } = false;

        /// <summary>
        /// Maximum exact collection chunks returned by one read call.
        /// </summary>
        [JsonPropertyName("MaxChunksPerRead")]
        public int MaxChunksPerRead { get; set; } = 20;

        /// <summary>
        /// Maximum chunk ranges accepted by one read request.
        /// </summary>
        [JsonPropertyName("MaxReadRangesPerCall")]
        public int MaxReadRangesPerCall { get; set; } = 5;

        /// <summary>
        /// Maximum neighbor window for chunk reads and retrieval searches.
        /// </summary>
        [JsonPropertyName("MaxNeighborWindow")]
        public int MaxNeighborWindow { get; set; } = 2;

        /// <summary>
        /// Allowed collection search modes.
        /// </summary>
        [JsonPropertyName("AllowedSearchModes")]
        public List<string> AllowedSearchModes { get; set; } = new List<string> { "Vector", "FullText", "Hybrid" };

        /// <summary>
        /// Optional default collection search mode.
        /// </summary>
        [JsonPropertyName("DefaultSearchMode")]
        public string DefaultSearchMode { get; set; }

        /// <summary>
        /// Allow model-supplied assistant document ID filters.
        /// </summary>
        [JsonPropertyName("AllowModelDocumentIdFilter")]
        public bool AllowModelDocumentIdFilter { get; set; } = true;

        /// <summary>
        /// Return labels in model-visible tool output.
        /// </summary>
        [JsonPropertyName("ReturnLabels")]
        public bool ReturnLabels { get; set; }

        /// <summary>
        /// Return tags in model-visible tool output.
        /// </summary>
        [JsonPropertyName("ReturnTags")]
        public bool ReturnTags { get; set; }

        /// <summary>
        /// Return full collection search chunk content. Defaults to excerpts only.
        /// </summary>
        [JsonPropertyName("ReturnFullSearchContent")]
        public bool ReturnFullSearchContent { get; set; }

        /// <summary>
        /// Permit non-completed document metadata visibility.
        /// </summary>
        [JsonPropertyName("AllowNonCompletedDocumentMetadata")]
        public bool AllowNonCompletedDocumentMetadata { get; set; }

        /// <summary>
        /// Alias for enabling Verbex full-text search.
        /// </summary>
        [JsonPropertyName("EnableVerbexSearchTool")]
        public bool EnableVerbexSearchTool { get; set; }

        /// <summary>
        /// Alias for enabling Verbex index enumeration.
        /// </summary>
        [JsonPropertyName("EnableIndexEnumerationTool")]
        public bool EnableIndexEnumerationTool { get; set; }

        /// <summary>
        /// Optional default Verbex index identifier.
        /// </summary>
        [JsonPropertyName("DefaultIndexId")]
        public string DefaultIndexId { get; set; }

        /// <summary>
        /// Maximum Verbex results.
        /// </summary>
        [JsonPropertyName("MaxVerbexResults")]
        public int MaxVerbexResults { get; set; } = 20;

        /// <summary>
        /// Allow raw Verbex record details in responses.
        /// </summary>
        [JsonPropertyName("AllowRawIndexRecords")]
        public bool AllowRawIndexRecords { get; set; }

        /// <summary>
        /// Require Verbex records to map to assistant documents.
        /// </summary>
        [JsonPropertyName("RequireDocumentMapping")]
        public bool RequireDocumentMapping { get; set; } = true;

        /// <summary>
        /// Return Verbex record metadata.
        /// </summary>
        [JsonPropertyName("ReturnVerbexRecordMetadata")]
        public bool ReturnVerbexRecordMetadata { get; set; }

        /// <summary>
        /// Maximum bytes returned by one S3 object read call.
        /// </summary>
        [JsonPropertyName("MaxObjectReadBytes")]
        public int MaxObjectReadBytes { get; set; } = 131072;

        /// <summary>
        /// Maximum aggregate S3 object bytes per chat turn.
        /// </summary>
        [JsonPropertyName("MaxObjectBytesPerTurn")]
        public int MaxObjectBytesPerTurn { get; set; } = 524288;

        /// <summary>
        /// Maximum bucket enumeration results.
        /// </summary>
        [JsonPropertyName("MaxBucketEnumerationResults")]
        public int MaxBucketEnumerationResults { get; set; } = 50;

        /// <summary>
        /// Permit object reads outside document-backed objects.
        /// </summary>
        [JsonPropertyName("AllowBucketWideObjectRead")]
        public bool AllowBucketWideObjectRead { get; set; }

        /// <summary>
        /// Restrict reads to document-backed objects.
        /// </summary>
        [JsonPropertyName("DocumentBackedObjectsOnly")]
        public bool DocumentBackedObjectsOnly { get; set; } = true;

        /// <summary>
        /// Redact object keys in model-visible output.
        /// </summary>
        [JsonPropertyName("RedactObjectKeys")]
        public bool RedactObjectKeys { get; set; } = true;

        /// <summary>
        /// Permit base64 output for binary object reads.
        /// </summary>
        [JsonPropertyName("AllowBinaryObjectOutput")]
        public bool AllowBinaryObjectOutput { get; set; }

        /// <summary>
        /// Permit raw web content in Tavily responses.
        /// </summary>
        [JsonPropertyName("AllowRawWebContent")]
        public bool AllowRawWebContent { get; set; }

        /// <summary>
        /// Permit image URLs in Tavily responses.
        /// </summary>
        [JsonPropertyName("AllowWebImages")]
        public bool AllowWebImages { get; set; }

        /// <summary>
        /// Permit direct ungoverned URL retrieval tools when implemented.
        /// </summary>
        [JsonPropertyName("AllowUngovernedWebAccess")]
        public bool AllowUngovernedWebAccess { get; set; }

        /// <summary>
        /// Permit source URLs in model-visible document enumeration output.
        /// </summary>
        [JsonPropertyName("AllowDocumentSourceUrls")]
        public bool AllowDocumentSourceUrls { get; set; }

        /// <summary>
        /// Permit labels and tags in model-visible enumeration/search output.
        /// </summary>
        [JsonPropertyName("AllowDocumentMetadataDetails")]
        public bool AllowDocumentMetadataDetails { get; set; }

        /// <summary>
        /// Allowed Verbex index IDs.
        /// </summary>
        [JsonPropertyName("AllowedVerbexIndexIds")]
        public List<string> AllowedVerbexIndexIds { get; set; } = new List<string>();

        /// <summary>
        /// Allowed S3 bucket names for non-default bucket access.
        /// </summary>
        [JsonPropertyName("AllowedBucketNames")]
        public List<string> AllowedBucketNames { get; set; } = new List<string>();

        /// <summary>
        /// Allowed S3 object key prefixes.
        /// </summary>
        [JsonPropertyName("AllowedBucketPrefixes")]
        public List<string> AllowedBucketPrefixes { get; set; } = new List<string>();

        /// <summary>
        /// Allowed S3 object suffixes.
        /// </summary>
        [JsonPropertyName("AllowedObjectSuffixes")]
        public List<string> AllowedObjectSuffixes { get; set; } = new List<string>();

        /// <summary>
        /// Allowed S3/document content types.
        /// </summary>
        [JsonPropertyName("AllowedContentTypes")]
        public List<string> AllowedContentTypes { get; set; } = new List<string>();

        /// <summary>
        /// Allowed web-search domains.
        /// </summary>
        [JsonPropertyName("AllowedWebDomains")]
        public List<string> AllowedWebDomains { get; set; } = new List<string>();

        /// <summary>
        /// Blocked web-search domains.
        /// </summary>
        [JsonPropertyName("BlockedWebDomains")]
        public List<string> BlockedWebDomains { get; set; } = new List<string>();

        /// <summary>
        /// Allowed external search providers.
        /// </summary>
        [JsonPropertyName("AllowedProviders")]
        public List<string> AllowedProviders { get; set; } = new List<string>();

        /// <summary>
        /// Maximum web-search results.
        /// </summary>
        [JsonPropertyName("MaxWebResults")]
        public int MaxWebResults { get; set; } = 5;

        /// <summary>
        /// Default web-search depth.
        /// </summary>
        [JsonPropertyName("SearchDepth")]
        public string SearchDepth { get; set; } = "basic";

        /// <summary>
        /// Allow advanced web-search depth.
        /// </summary>
        [JsonPropertyName("AllowAdvancedSearchDepth")]
        public bool AllowAdvancedSearchDepth { get; set; }

        /// <summary>
        /// Allow Tavily news topic.
        /// </summary>
        [JsonPropertyName("AllowNewsTopic")]
        public bool AllowNewsTopic { get; set; } = true;

        /// <summary>
        /// Require safe-search behavior.
        /// </summary>
        [JsonPropertyName("RequireSafeSearch")]
        public bool RequireSafeSearch { get; set; } = true;

        /// <summary>
        /// Maximum web-search calls per chat turn.
        /// </summary>
        [JsonPropertyName("MaxWebSearchesPerTurn")]
        public int MaxWebSearchesPerTurn { get; set; } = 3;
    }
}
