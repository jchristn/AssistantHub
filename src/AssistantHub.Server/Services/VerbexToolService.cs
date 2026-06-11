#pragma warning disable CS1591

namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Typed facade for assistant Verbex tools.
    /// </summary>
    public class VerbexToolService
    {
        private readonly IAssistantToolExecutor _Executor;
        private readonly DatabaseDriverBase _Database;

        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="executor">Assistant tool executor.</param>
        /// <param name="database">Optional database driver used for scope and mapping helpers.</param>
        public VerbexToolService(IAssistantToolExecutor executor, DatabaseDriverBase database = null)
        {
            _Executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _Database = database;
        }

        /// <summary>
        /// Resolve the assistant-visible Verbex tenant/index scope.
        /// </summary>
        public async Task<VerbexToolScope> ResolveAllowedVerbexScopeAsync(
            AssistantToolExecutionContext context,
            CancellationToken token = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            string assistantTenantId = context.Assistant?.TenantId;
            string verbexTenantId = assistantTenantId;
            string defaultIndexId = context.Policy?.DefaultIndexId;
            List<string> allowedIndexIds = new List<string>();

            void AddIndex(string value)
            {
                if (!String.IsNullOrWhiteSpace(value)
                    && !allowedIndexIds.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase))
                    allowedIndexIds.Add(value.Trim());
            }

            AddIndex(defaultIndexId);
            foreach (string indexId in context.Policy?.AllowedVerbexIndexIds ?? new List<string>())
                AddIndex(indexId);

            if (_Database != null && !String.IsNullOrWhiteSpace(assistantTenantId))
            {
                try
                {
                    TenantMetadata tenant = await _Database.Tenant.ReadByIdAsync(assistantTenantId, token).ConfigureAwait(false);
                    if (tenant?.Tags != null)
                    {
                        if (tenant.Tags.TryGetValue(Constants.VerbexTenantIdTag, out string mappedTenantId)
                            && !String.IsNullOrWhiteSpace(mappedTenantId))
                            verbexTenantId = mappedTenantId.Trim();

                        if (String.IsNullOrWhiteSpace(defaultIndexId)
                            && tenant.Tags.TryGetValue(Constants.VerbexDefaultIndexIdTag, out string mappedDefaultIndexId)
                            && !String.IsNullOrWhiteSpace(mappedDefaultIndexId))
                        {
                            defaultIndexId = mappedDefaultIndexId.Trim();
                            AddIndex(defaultIndexId);
                        }
                    }
                }
                catch
                {
                }

                try
                {
                    EnumerationResult<AssistantDocument> documents = await _Database.AssistantDocument.EnumerateAsync(
                        assistantTenantId,
                        new EnumerationQuery
                        {
                            CollectionIdFilter = context.Settings?.CollectionId,
                            MaxResults = 1000
                        },
                        token).ConfigureAwait(false);

                    foreach (AssistantDocument document in documents.Objects ?? new List<AssistantDocument>())
                    {
                        if (!IsVisibleDocument(context, document)) continue;
                        AddIndex(document.VerbexIndexId);
                    }
                }
                catch
                {
                }
            }

            return new VerbexToolScope
            {
                AssistantTenantId = assistantTenantId,
                VerbexTenantId = verbexTenantId,
                AssistantId = context.Assistant?.Id,
                CollectionId = context.Settings?.CollectionId,
                DefaultIndexId = defaultIndexId,
                AllowedIndexIds = allowedIndexIds
            };
        }

        /// <summary>
        /// Search an allowed Verbex index through the policy-scoped verbex_full_text_search tool.
        /// </summary>
        public Task<AssistantToolExecutionResult> SearchAsync(
            AssistantToolExecutionContext context,
            VerbexToolSearchRequest request,
            CancellationToken token = default)
        {
            return ExecuteAsync(context, "verbex_full_text_search", request, token);
        }

        /// <summary>
        /// Enumerate records from an allowed Verbex index through the policy-scoped index_enumerate_records tool.
        /// </summary>
        public Task<AssistantToolExecutionResult> EnumerateRecordsAsync(
            AssistantToolExecutionContext context,
            VerbexToolEnumerateRecordsRequest request,
            CancellationToken token = default)
        {
            return ExecuteAsync(context, "index_enumerate_records", request, token);
        }

        /// <summary>
        /// Map a Verbex record back to an assistant-visible document.
        /// </summary>
        public async Task<VerbexToolDocumentMap> MapRecordToAssistantDocumentAsync(
            AssistantToolExecutionContext context,
            string indexId,
            string recordId,
            string assistantDocumentId = null,
            CancellationToken token = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (_Database == null) return null;

            AssistantDocument document = null;
            if (!String.IsNullOrWhiteSpace(assistantDocumentId))
            {
                try
                {
                    document = await _Database.AssistantDocument.ReadAsync(assistantDocumentId, token).ConfigureAwait(false);
                    if (!IsVisibleDocument(context, document)) document = null;
                }
                catch
                {
                    document = null;
                }
            }

            if (document == null)
            {
                EnumerationResult<AssistantDocument> documents = await _Database.AssistantDocument.EnumerateAsync(
                    context.Assistant?.TenantId,
                    new EnumerationQuery
                    {
                        CollectionIdFilter = context.Settings?.CollectionId,
                        MaxResults = 1000
                    },
                    token).ConfigureAwait(false);

                foreach (AssistantDocument candidate in documents.Objects ?? new List<AssistantDocument>())
                {
                    if (!IsVisibleDocument(context, candidate)) continue;
                    if (!MatchesIndex(candidate, indexId)) continue;
                    if (!MatchesRecord(candidate, recordId)) continue;

                    document = candidate;
                    break;
                }
            }

            if (document == null) return null;

            List<string> chunkRecordIds = ParseChunkRecordIds(document.ChunkRecordIds);
            int chunkPosition = String.IsNullOrWhiteSpace(recordId)
                ? -1
                : chunkRecordIds.FindIndex(value => String.Equals(value, recordId, StringComparison.Ordinal));

            return new VerbexToolDocumentMap
            {
                IndexId = indexId,
                RecordId = recordId,
                DocumentId = document.Id,
                DocumentName = document.Name ?? document.OriginalFilename,
                ContentType = document.ContentType,
                AvailableChunkCount = chunkRecordIds.Count,
                ChunkPosition = chunkPosition >= 0 ? chunkPosition : (int?)null
            };
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

        private static bool IsVisibleDocument(AssistantToolExecutionContext context, AssistantDocument document)
        {
            if (context == null || context.Assistant == null || context.Settings == null || document == null) return false;
            if (!String.Equals(document.TenantId, context.Assistant.TenantId, StringComparison.Ordinal)) return false;
            if (!String.Equals(document.CollectionId, context.Settings.CollectionId, StringComparison.Ordinal)) return false;
            if (document.Status != DocumentStatusEnum.Completed) return false;
            return AssistantDocumentPolicyFilter.MatchesAssistantMetadataFilters(document, context.Settings);
        }

        private static bool MatchesIndex(AssistantDocument document, string indexId)
        {
            if (document == null || String.IsNullOrWhiteSpace(indexId)) return true;
            if (String.IsNullOrWhiteSpace(document.VerbexIndexId)) return true;
            return String.Equals(document.VerbexIndexId, indexId, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesRecord(AssistantDocument document, string recordId)
        {
            if (document == null || String.IsNullOrWhiteSpace(recordId)) return false;
            return String.Equals(document.Id, recordId, StringComparison.Ordinal)
                || String.Equals(document.VerbexRecordId, recordId, StringComparison.Ordinal)
                || ParseChunkRecordIds(document.ChunkRecordIds).Contains(recordId, StringComparer.Ordinal);
        }

        private static List<string> ParseChunkRecordIds(string chunkRecordIdsJson)
        {
            if (String.IsNullOrWhiteSpace(chunkRecordIdsJson)) return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(chunkRecordIdsJson, _JsonOptions)?
                    .Where(value => !String.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim())
                    .ToList()
                    ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }
    }

    /// <summary>
    /// Assistant-visible Verbex scope.
    /// </summary>
    public class VerbexToolScope
    {
        public string AssistantTenantId { get; set; }
        public string VerbexTenantId { get; set; }
        public string AssistantId { get; set; }
        public string CollectionId { get; set; }
        public string DefaultIndexId { get; set; }
        public List<string> AllowedIndexIds { get; set; } = new List<string>();
    }

    /// <summary>
    /// Strongly typed verbex_full_text_search request.
    /// </summary>
    public class VerbexToolSearchRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; }

        [JsonPropertyName("index_id")]
        public string IndexId { get; set; }

        [JsonPropertyName("record_ids")]
        public List<string> RecordIds { get; set; }

        [JsonPropertyName("max_results")]
        public int? MaxResults { get; set; }

        [JsonPropertyName("use_and_logic")]
        public bool? UseAndLogic { get; set; }

        [JsonPropertyName("required_terms")]
        public List<string> RequiredTerms { get; set; }

        [JsonPropertyName("excluded_terms")]
        public List<string> ExcludedTerms { get; set; }
    }

    /// <summary>
    /// Strongly typed index_enumerate_records request.
    /// </summary>
    public class VerbexToolEnumerateRecordsRequest
    {
        [JsonPropertyName("index_id")]
        public string IndexId { get; set; }

        [JsonPropertyName("record_ids")]
        public List<string> RecordIds { get; set; }

        [JsonPropertyName("max_results")]
        public int? MaxResults { get; set; }

        [JsonPropertyName("continuation_token")]
        public string ContinuationToken { get; set; }

        [JsonPropertyName("query")]
        public string Query { get; set; }

        [JsonPropertyName("record_id_prefix")]
        public string RecordIdPrefix { get; set; }
    }

    /// <summary>
    /// Safe response model for Verbex search hits.
    /// </summary>
    public class VerbexToolSearchHit
    {
        public string IndexId { get; set; }
        public string RecordId { get; set; }
        public string DocumentId { get; set; }
        public string DocumentName { get; set; }
        public string ContentType { get; set; }
        public double? Score { get; set; }
        public string Excerpt { get; set; }
        public List<string> MatchedTerms { get; set; } = new List<string>();
        public int AvailableChunkCount { get; set; }
        public int? ChunkPosition { get; set; }
        public string CitationHandle { get; set; }
    }

    /// <summary>
    /// Safe response model for Verbex record enumeration.
    /// </summary>
    public class VerbexToolRecordItem
    {
        public string IndexId { get; set; }
        public string RecordId { get; set; }
        public string DocumentId { get; set; }
        public string DocumentName { get; set; }
        public string ContentType { get; set; }
        public string SourceUrl { get; set; }
        public string Excerpt { get; set; }
        public int AvailableChunkCount { get; set; }
        public int? ChunkPosition { get; set; }
        public string CitationHandle { get; set; }
    }

    /// <summary>
    /// Safe mapping from a Verbex record to an assistant document.
    /// </summary>
    public class VerbexToolDocumentMap
    {
        public string IndexId { get; set; }
        public string RecordId { get; set; }
        public string DocumentId { get; set; }
        public string DocumentName { get; set; }
        public string ContentType { get; set; }
        public int AvailableChunkCount { get; set; }
        public int? ChunkPosition { get; set; }
    }
}

#pragma warning restore CS1591
