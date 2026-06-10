namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Administrator-controlled policy for server-side tools exposed to a model.
    /// </summary>
    public class AssistantToolPolicy
    {
        /// <summary>
        /// Master switch for model-directed server-side tool calls.
        /// </summary>
        public bool EnableToolCalls { get; set; } = false;

        /// <summary>
        /// Maximum tool-call loop iterations allowed for one chat turn.
        /// </summary>
        public int MaxToolIterations { get; set; } = 6;

        /// <summary>
        /// Maximum individual tool calls allowed for one chat turn.
        /// </summary>
        public int MaxToolCallsPerTurn { get; set; } = 12;

        /// <summary>
        /// Model tool-choice behavior requested from compatible endpoints.
        /// </summary>
        public string ToolChoiceMode { get; set; } = "Auto";

        /// <summary>
        /// Maximum parallel tool calls allowed in one model response.
        /// </summary>
        public int MaxParallelToolCalls { get; set; } = 1;

        /// <summary>
        /// Allow parallel tool calls. First release defaults to sequential execution.
        /// </summary>
        public bool AllowParallelToolCalls { get; set; } = false;

        /// <summary>
        /// Per-tool timeout in milliseconds.
        /// </summary>
        public int ToolCallTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Maximum characters returned to the model from a single tool call.
        /// </summary>
        public int MaxToolOutputChars { get; set; } = 12000;

        /// <summary>
        /// Maximum aggregate tool-output characters returned to the model in one chat turn.
        /// </summary>
        public int MaxToolOutputCharactersPerTurn { get; set; } = 50000;

        /// <summary>
        /// Maximum model-visible result items returned by tools when a tool supports item limits.
        /// </summary>
        public int MaxToolResultItems { get; set; } = 20;

        /// <summary>
        /// Expose safe tool traces to non-admin users.
        /// </summary>
        public bool ExposeToolTraceToUser { get; set; } = false;

        /// <summary>
        /// Persist redacted tool arguments for administrator diagnostics.
        /// </summary>
        public bool PersistToolArguments { get; set; } = true;

        /// <summary>
        /// Persist full tool outputs. First release stores metadata/summaries by default.
        /// </summary>
        public bool PersistToolOutputs { get; set; } = false;

        /// <summary>
        /// Require citations for tool-derived evidence where the tool can provide them.
        /// </summary>
        public bool RequireCitationsForToolEvidence { get; set; } = true;

        /// <summary>
        /// Optional final allow-list of model-visible tool names.
        /// </summary>
        public List<string> AllowedToolNames { get; set; } = new List<string>();

        /// <summary>
        /// Whether streaming chat clients may receive safe tool-progress events.
        /// </summary>
        public bool EnableToolFeedbackEvents { get; set; } = true;

        /// <summary>
        /// Whether Slack channels receive safe tool-progress lifecycle messages.
        /// </summary>
        public bool EnableSlackToolProgressMessages { get; set; } = true;

        /// <summary>
        /// Enable exhaustive or bounded collection search.
        /// </summary>
        public bool EnableCollectionSearchTool { get; set; } = false;

        /// <summary>
        /// Enable exact collection chunk reads by document ID and position.
        /// </summary>
        public bool EnableCollectionReadChunksTool { get; set; } = false;

        /// <summary>
        /// Enable Verbex full-text search.
        /// </summary>
        public bool EnableVerbexFullTextSearchTool { get; set; } = false;

        /// <summary>
        /// Enable safe S3 object reads.
        /// </summary>
        public bool EnableS3ObjectReadTool { get; set; } = false;

        /// <summary>
        /// Enable assistant collection document enumeration.
        /// </summary>
        public bool EnableCollectionEnumerateDocumentsTool { get; set; } = false;

        /// <summary>
        /// Alias for enabling assistant collection document enumeration.
        /// </summary>
        public bool EnableCollectionEnumerationTool { get; set; } = false;

        /// <summary>
        /// Enable Verbex index record enumeration.
        /// </summary>
        public bool EnableIndexEnumerateRecordsTool { get; set; } = false;

        /// <summary>
        /// Enable S3 bucket object enumeration.
        /// </summary>
        public bool EnableBucketEnumerateObjectsTool { get; set; } = false;

        /// <summary>
        /// Enable Tavily-backed web search.
        /// </summary>
        public bool EnableWebSearchTool { get; set; } = false;

        /// <summary>
        /// Assistant-level Tavily endpoint override. Empty uses the system-level provider.
        /// </summary>
        public string TavilyEndpoint { get; set; } = null;

        /// <summary>
        /// Assistant-level Tavily API key override. Empty uses the system-level provider.
        /// </summary>
        public string TavilyApiKey { get; set; } = null;

        /// <summary>
        /// Maximum search results a model may request in one tool call.
        /// </summary>
        public int MaxSearchResultsPerCall { get; set; } = 10;

        /// <summary>
        /// Maximum search top-k allowed for collection search.
        /// </summary>
        public int MaxSearchTopK { get; set; } = 50;

        /// <summary>
        /// Maximum search queries a model may submit in one collection search call.
        /// </summary>
        public int MaxSearchQueriesPerCall { get; set; } = 3;

        /// <summary>
        /// Maximum assistant-visible documents a collection search may consider.
        /// </summary>
        public int MaxDocumentsConsideredPerSearch { get; set; } = 1000;

        /// <summary>
        /// Maximum raw retrieval results a collection search may consider across all search passes.
        /// </summary>
        public int MaxResultsConsideredPerSearch { get; set; } = 1000;

        /// <summary>
        /// Allow the server to add deterministic query variants for collection search.
        /// </summary>
        public bool EnableServerGeneratedQueryVariants { get; set; } = false;

        /// <summary>
        /// Maximum exact collection chunks returned by one read call.
        /// </summary>
        public int MaxChunksPerRead { get; set; } = 20;

        /// <summary>
        /// Maximum chunk ranges accepted in one collection read request.
        /// </summary>
        public int MaxReadRangesPerCall { get; set; } = 5;

        /// <summary>
        /// Maximum neighbor window for chunk reads and retrieval searches.
        /// </summary>
        public int MaxNeighborWindow { get; set; } = 2;

        /// <summary>
        /// Search modes that collection search may use.
        /// </summary>
        public List<string> AllowedSearchModes { get; set; } = new List<string> { "Vector", "FullText", "Hybrid" };

        /// <summary>
        /// Optional default search mode override for collection search.
        /// </summary>
        public string DefaultSearchMode { get; set; } = null;

        /// <summary>
        /// Allow the model to narrow collection searches by assistant document ID.
        /// </summary>
        public bool AllowModelDocumentIdFilter { get; set; } = true;

        /// <summary>
        /// Return document labels in model-visible tool output.
        /// </summary>
        public bool ReturnLabels { get; set; } = false;

        /// <summary>
        /// Return document tags in model-visible tool output.
        /// </summary>
        public bool ReturnTags { get; set; } = false;

        /// <summary>
        /// Return full collection search chunk content. Defaults to excerpts only.
        /// </summary>
        public bool ReturnFullSearchContent { get; set; } = false;

        /// <summary>
        /// Permit non-completed document metadata to be visible in enumeration tools.
        /// </summary>
        public bool AllowNonCompletedDocumentMetadata { get; set; } = false;

        /// <summary>
        /// Alias for enabling Verbex full-text search.
        /// </summary>
        public bool EnableVerbexSearchTool { get; set; } = false;

        /// <summary>
        /// Alias for enabling Verbex index enumeration.
        /// </summary>
        public bool EnableIndexEnumerationTool { get; set; } = false;

        /// <summary>
        /// Optional default Verbex index identifier.
        /// </summary>
        public string DefaultIndexId { get; set; } = null;

        /// <summary>
        /// Maximum Verbex search/enumeration results.
        /// </summary>
        public int MaxVerbexResults { get; set; } = 20;

        /// <summary>
        /// Permit raw Verbex record details in model-visible responses.
        /// </summary>
        public bool AllowRawIndexRecords { get; set; } = false;

        /// <summary>
        /// Require Verbex records to map back to assistant documents.
        /// </summary>
        public bool RequireDocumentMapping { get; set; } = true;

        /// <summary>
        /// Return Verbex record metadata in model-visible responses.
        /// </summary>
        public bool ReturnVerbexRecordMetadata { get; set; } = false;

        /// <summary>
        /// Maximum bytes returned by one S3 object read call.
        /// </summary>
        public int MaxObjectReadBytes { get; set; } = 131072;

        /// <summary>
        /// Maximum aggregate object bytes returned in one chat turn.
        /// </summary>
        public int MaxObjectBytesPerTurn { get; set; } = 524288;

        /// <summary>
        /// Maximum S3 bucket enumeration results.
        /// </summary>
        public int MaxBucketEnumerationResults { get; set; } = 50;

        /// <summary>
        /// Permit object reads outside document-backed objects.
        /// </summary>
        public bool AllowBucketWideObjectRead { get; set; } = false;

        /// <summary>
        /// Restrict object reads to document-backed objects.
        /// </summary>
        public bool DocumentBackedObjectsOnly { get; set; } = true;

        /// <summary>
        /// Redact S3 object keys in model-visible output.
        /// </summary>
        public bool RedactObjectKeys { get; set; } = true;

        /// <summary>
        /// Permit base64 output for binary object reads.
        /// </summary>
        public bool AllowBinaryObjectOutput { get; set; } = false;

        /// <summary>
        /// Permit raw web content in Tavily responses.
        /// </summary>
        public bool AllowRawWebContent { get; set; } = false;

        /// <summary>
        /// Permit image URLs in Tavily responses.
        /// </summary>
        public bool AllowWebImages { get; set; } = false;

        /// <summary>
        /// Permit direct ungoverned URL retrieval tools when implemented.
        /// </summary>
        public bool AllowUngovernedWebAccess { get; set; } = false;

        /// <summary>
        /// Permit source URLs in model-visible document enumeration output.
        /// </summary>
        public bool AllowDocumentSourceUrls { get; set; } = false;

        /// <summary>
        /// Permit labels and tags in model-visible enumeration/search output.
        /// </summary>
        public bool AllowDocumentMetadataDetails { get; set; } = false;

        /// <summary>
        /// Allowed Verbex index IDs.
        /// </summary>
        public List<string> AllowedVerbexIndexIds { get; set; } = new List<string>();

        /// <summary>
        /// Allowed S3 bucket names for non-default bucket access.
        /// </summary>
        public List<string> AllowedBucketNames { get; set; } = new List<string>();

        /// <summary>
        /// Allowed S3 object key prefixes.
        /// </summary>
        public List<string> AllowedBucketPrefixes { get; set; } = new List<string>();

        /// <summary>
        /// Allowed S3 object suffixes.
        /// </summary>
        public List<string> AllowedObjectSuffixes { get; set; } = new List<string>();

        /// <summary>
        /// Allowed S3/document content types.
        /// </summary>
        public List<string> AllowedContentTypes { get; set; } = new List<string>();

        /// <summary>
        /// Allowed web-search domains.
        /// </summary>
        public List<string> AllowedWebDomains { get; set; } = new List<string>();

        /// <summary>
        /// Blocked web-search domains.
        /// </summary>
        public List<string> BlockedWebDomains { get; set; } = new List<string>();

        /// <summary>
        /// Allowed web-search providers.
        /// </summary>
        public List<string> AllowedProviders { get; set; } = new List<string>();

        /// <summary>
        /// Maximum web-search results.
        /// </summary>
        public int MaxWebResults { get; set; } = 5;

        /// <summary>
        /// Default Tavily search depth.
        /// </summary>
        public string SearchDepth { get; set; } = "basic";

        /// <summary>
        /// Permit Tavily advanced search depth.
        /// </summary>
        public bool AllowAdvancedSearchDepth { get; set; } = false;

        /// <summary>
        /// Permit Tavily news topic.
        /// </summary>
        public bool AllowNewsTopic { get; set; } = true;

        /// <summary>
        /// Require safe-search behavior.
        /// </summary>
        public bool RequireSafeSearch { get; set; } = true;

        /// <summary>
        /// Maximum web-search calls per turn.
        /// </summary>
        public int MaxWebSearchesPerTurn { get; set; } = 3;

        /// <summary>
        /// Normalize limits and collection properties to safe server-enforced ranges.
        /// </summary>
        public void Normalize()
        {
            ToolChoiceMode = NormalizeToolChoiceMode(ToolChoiceMode);
            MaxToolIterations = Math.Clamp(MaxToolIterations, 1, 20);
            MaxToolCallsPerTurn = Math.Clamp(MaxToolCallsPerTurn, 1, 50);
            MaxParallelToolCalls = Math.Clamp(MaxParallelToolCalls, 1, 16);
            if (!AllowParallelToolCalls) MaxParallelToolCalls = 1;
            ToolCallTimeoutMs = Math.Clamp(ToolCallTimeoutMs, 1000, 300000);
            MaxToolOutputChars = Math.Clamp(MaxToolOutputChars, 1024, 200000);
            MaxToolOutputCharactersPerTurn = Math.Clamp(MaxToolOutputCharactersPerTurn, MaxToolOutputChars, 500000);
            MaxToolResultItems = Math.Clamp(MaxToolResultItems, 1, 1000);
            MaxSearchResultsPerCall = Math.Clamp(MaxSearchResultsPerCall, 1, 100);
            MaxSearchTopK = Math.Clamp(MaxSearchTopK, 1, 100);
            MaxSearchQueriesPerCall = Math.Clamp(MaxSearchQueriesPerCall, 1, 20);
            MaxDocumentsConsideredPerSearch = Math.Clamp(MaxDocumentsConsideredPerSearch, 1, 10000);
            MaxResultsConsideredPerSearch = Math.Clamp(MaxResultsConsideredPerSearch, 1, 10000);
            MaxChunksPerRead = Math.Clamp(MaxChunksPerRead, 1, 100);
            MaxReadRangesPerCall = Math.Clamp(MaxReadRangesPerCall, 1, 50);
            MaxNeighborWindow = Math.Clamp(MaxNeighborWindow, 0, 10);
            AllowedSearchModes = NormalizeSearchModes(AllowedSearchModes);
            DefaultSearchMode = NormalizeSearchModeOrNull(DefaultSearchMode);
            if (DefaultSearchMode != null && !AllowedSearchModes.Contains(DefaultSearchMode, StringComparer.OrdinalIgnoreCase))
                AllowedSearchModes.Add(DefaultSearchMode);
            if (EnableCollectionEnumerationTool) EnableCollectionEnumerateDocumentsTool = true;
            if (EnableVerbexSearchTool) EnableVerbexFullTextSearchTool = true;
            if (EnableIndexEnumerationTool) EnableIndexEnumerateRecordsTool = true;
            MaxVerbexResults = Math.Clamp(MaxVerbexResults, 1, 100);
            MaxObjectReadBytes = Math.Clamp(MaxObjectReadBytes, 1, 10485760);
            MaxObjectBytesPerTurn = Math.Clamp(MaxObjectBytesPerTurn, MaxObjectReadBytes, 10485760);
            MaxBucketEnumerationResults = Math.Clamp(MaxBucketEnumerationResults, 1, 1000);
            if (DocumentBackedObjectsOnly) AllowBucketWideObjectRead = false;
            MaxWebResults = Math.Clamp(MaxWebResults, 1, 20);
            SearchDepth = NormalizeSearchDepth(SearchDepth);
            if (!AllowAdvancedSearchDepth) SearchDepth = "basic";
            MaxWebSearchesPerTurn = Math.Clamp(MaxWebSearchesPerTurn, 1, 50);
            TavilyEndpoint = NormalizeString(TavilyEndpoint);
            TavilyApiKey = NormalizeString(TavilyApiKey);
            DefaultIndexId = NormalizeString(DefaultIndexId);

            AllowedToolNames = NormalizeToolNameList(AllowedToolNames);
            AllowedVerbexIndexIds = NormalizeStringList(AllowedVerbexIndexIds);
            AllowedBucketNames = NormalizeStringList(AllowedBucketNames);
            AllowedBucketPrefixes = NormalizeStringList(AllowedBucketPrefixes);
            AllowedObjectSuffixes = NormalizeStringList(AllowedObjectSuffixes);
            AllowedContentTypes = NormalizeStringList(AllowedContentTypes);
            AllowedProviders = NormalizeStringList(AllowedProviders);
            AllowedWebDomains = NormalizeStringList(AllowedWebDomains);
            BlockedWebDomains = NormalizeStringList(BlockedWebDomains);
        }

        private static string NormalizeToolChoiceMode(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "Auto";

            string normalized = value.Trim();
            if (String.Equals(normalized, "Auto", StringComparison.OrdinalIgnoreCase)) return "Auto";
            if (String.Equals(normalized, "Required", StringComparison.OrdinalIgnoreCase)) return "Required";
            if (String.Equals(normalized, "None", StringComparison.OrdinalIgnoreCase)) return "None";
            if (String.Equals(normalized, "AllowedOnly", StringComparison.OrdinalIgnoreCase)) return "AllowedOnly";
            return "Auto";
        }

        private static string NormalizeSearchDepth(string value)
        {
            if (String.Equals(value?.Trim(), "advanced", StringComparison.OrdinalIgnoreCase)) return "advanced";
            return "basic";
        }

        private static List<string> NormalizeSearchModes(List<string> values)
        {
            List<string> modes = new List<string>();
            foreach (string value in values ?? new List<string>())
            {
                string normalized = NormalizeSearchModeOrNull(value);
                if (normalized != null && !modes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                    modes.Add(normalized);
            }

            if (modes.Count == 0)
            {
                modes.Add("Vector");
                modes.Add("FullText");
                modes.Add("Hybrid");
            }

            return modes;
        }

        private static string NormalizeSearchModeOrNull(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return null;
            if (String.Equals(value.Trim(), "Vector", StringComparison.OrdinalIgnoreCase)) return "Vector";
            if (String.Equals(value.Trim(), "FullText", StringComparison.OrdinalIgnoreCase)) return "FullText";
            if (String.Equals(value.Trim(), "Hybrid", StringComparison.OrdinalIgnoreCase)) return "Hybrid";
            return null;
        }

        private static List<string> NormalizeStringList(List<string> values)
        {
            if (values == null) return new List<string>();

            return values
                .Where(value => !String.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(100)
                .ToList();
        }

        private static List<string> NormalizeToolNameList(List<string> values)
        {
            return NormalizeStringList(values)
                .Select(value => value.ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string NormalizeString(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
