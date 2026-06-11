namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;

    /// <summary>
    /// Builds model-facing tool definitions from assistant policy and server capabilities.
    /// </summary>
    public class AssistantToolRegistry
    {
        private static readonly string[] _ImplementedToolNames = new[]
        {
            "collection_search",
            "collection_read_chunks",
            "collection_enumerate_documents",
            "verbex_full_text_search",
            "index_enumerate_records",
            "s3_object_read",
            "document_atom_extract",
            "bucket_enumerate_objects",
            "web_search"
        };

        private readonly AssistantHubSettings _Settings;
        private readonly AssistantToolPolicyResolver _Resolver;

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Server settings.</param>
        public AssistantToolRegistry(AssistantHubSettings settings)
        {
            _Settings = settings ?? new AssistantHubSettings();
            _Resolver = new AssistantToolPolicyResolver(_Settings);
        }

        /// <summary>
        /// Build tool definitions that may be sent to a tool-capable model.
        /// </summary>
        /// <param name="assistant">Assistant.</param>
        /// <param name="settings">Assistant settings.</param>
        /// <returns>Model-facing tool definitions.</returns>
        public List<AssistantToolDefinition> BuildDefinitions(Assistant assistant, AssistantSettings settings)
        {
            AssistantToolPolicy policy = settings?.ToolPolicy ?? new AssistantToolPolicy();
            policy.Normalize();

            List<AssistantToolDefinition> definitions = new List<AssistantToolDefinition>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<AssistantToolDescriptor> descriptors = _Resolver.Resolve(assistant, settings);
            foreach (AssistantToolDescriptor descriptor in descriptors)
            {
                string toolName = NormalizeToolName(descriptor.ToolName);
                if (String.IsNullOrWhiteSpace(toolName) || !seen.Add(toolName))
                    continue;

                AssistantToolDefinition definition = BuildDefinition(toolName, policy);
                if (definition != null)
                    definitions.Add(definition);
            }

            return definitions;
        }

        /// <summary>
        /// Determine whether the current server can execute a tool.
        /// </summary>
        /// <param name="toolName">Tool name.</param>
        /// <returns>True if execution is implemented.</returns>
        public static bool IsImplementedTool(string toolName)
        {
            return NormalizeToolName(toolName) != null;
        }

        /// <summary>
        /// Normalize a model- or policy-supplied tool name to the implemented canonical name.
        /// </summary>
        /// <param name="toolName">Tool name.</param>
        /// <returns>Canonical tool name or null when unknown.</returns>
        public static string NormalizeToolName(string toolName)
        {
            if (String.IsNullOrWhiteSpace(toolName)) return null;

            string trimmed = toolName.Trim();
            foreach (string implemented in _ImplementedToolNames)
            {
                if (String.Equals(trimmed, implemented, StringComparison.OrdinalIgnoreCase))
                    return implemented;
            }

            return null;
        }

        private static AssistantToolDefinition BuildDefinition(string toolName, AssistantToolPolicy policy)
        {
            if (!IsImplementedTool(toolName))
                return null;

            int collectionMaxResults = Math.Min(policy.MaxSearchResultsPerCall, policy.MaxSearchTopK);
            int verbexMaxResults = Math.Min(policy.MaxSearchResultsPerCall, policy.MaxVerbexResults);
            int bucketMaxResults = Math.Min(policy.MaxSearchResultsPerCall, policy.MaxBucketEnumerationResults);
            int webMaxResults = Math.Min(policy.MaxWebResults, Math.Min(20, policy.MaxSearchResultsPerCall));

            if (String.Equals(toolName, "collection_search", StringComparison.OrdinalIgnoreCase))
                return Function(
                    "collection_search",
                    "Search the assistant's assigned collection for relevant document chunks. Requires a non-empty query or non-empty queries array; the server applies tenant, collection, and policy limits.",
                    Object(
                        new Dictionary<string, AssistantToolJsonSchema>
                        {
                            ["query"] = StringField("One search query."),
                            ["queries"] = ArrayField(StringField("One search query."), "Multiple search queries for a bounded multi-query search."),
                            ["document_ids"] = ArrayField(StringField("AssistantDocument.Id value."), "Optional completed assistant document IDs to narrow the search."),
                            ["search_mode"] = StringField("Search mode. Use Hybrid unless exact lexical matching or vector-only behavior is specifically needed.", BuildSearchModeEnum(policy)),
                            ["strategy"] = StringField("Search strategy. Use exhaustive when broad recall is more important than a single fast pass.", new List<string> { "single", "multi_query", "broad", "narrow", "exhaustive" }),
                            ["max_results"] = IntegerField("Maximum results to return.", 1, collectionMaxResults),
                            ["top_k"] = IntegerField("Alias for max_results.", 1, collectionMaxResults),
                            ["score_threshold"] = NumberField("Minimum result score threshold. The server also enforces the assistant's configured retrieval threshold.", 0, 1),
                            ["fulltext_search_type"] = StringField("Optional full-text ranking function override for FullText/Hybrid modes, such as TsRank or TsRankCd."),
                            ["fulltext_language"] = StringField("Optional full-text language/configuration override for FullText/Hybrid modes."),
                            ["fulltext_normalization"] = IntegerField("Optional full-text normalization override for FullText/Hybrid modes.", 0, 64),
                            ["fulltext_minimum_score"] = NumberField("Optional full-text minimum score override for FullText/Hybrid modes.", 0, 1),
                            ["include_neighbors"] = IntegerField("Neighbor chunks to include around each match.", 0, policy.MaxNeighborWindow),
                            ["labels"] = ArrayField(StringField("Required label."), "Optional labels that further narrow assistant-visible documents."),
                            ["required_labels"] = ArrayField(StringField("Required label."), "Optional required labels that further narrow assistant-visible documents."),
                            ["excluded_labels"] = ArrayField(StringField("Excluded label."), "Optional excluded labels that further narrow assistant-visible documents."),
                            ["tags"] = ObjectMapField("Optional tag key/value pairs that further narrow assistant-visible documents."),
                            ["source_url_contains"] = StringField("Optional source URL substring filter. Accepted only when assistant policy allows source URLs.")
                        },
                        new List<string>()));

            if (String.Equals(toolName, "collection_read_chunks", StringComparison.OrdinalIgnoreCase))
                return Function(
                    "collection_read_chunks",
                    "Read exact chunks from a completed assistant document by chunk position. Requires document_id plus either a non-empty positions array or a non-empty ranges array. Use this after collection_search or document enumeration when exact surrounding text is needed.",
                    Object(
                        new Dictionary<string, AssistantToolJsonSchema>
                        {
                            ["document_id"] = StringField("Completed AssistantDocument.Id value in the assistant collection."),
                            ["positions"] = ArrayField(IntegerField("Zero-based chunk position.", 0, 1000000), "Exact chunk positions to read. Do not send an empty array unless ranges is non-empty."),
                            ["ranges"] = ArrayField(
                                Object(
                                    new Dictionary<string, AssistantToolJsonSchema>
                                    {
                                        ["start_position"] = IntegerField("Zero-based first chunk position.", 0, 1000000),
                                        ["count"] = IntegerField("Number of chunks to read from start_position.", 1, Math.Min(policy.MaxChunksPerRead, policy.MaxToolResultItems)),
                                        ["start"] = IntegerField("Alias for start_position.", 0, 1000000),
                                        ["length"] = IntegerField("Alias for count.", 1, Math.Min(policy.MaxChunksPerRead, policy.MaxToolResultItems))
                                    },
                                    new List<string> { "start_position", "count" }),
                                "Chunk ranges to read. Prefer start_position/count. Do not request an entire document with a huge count; read small ranges and continue only if needed."),
                            ["neighbor_window"] = IntegerField("Neighbor chunks to include around requested positions.", 0, policy.MaxNeighborWindow),
                            ["max_chunks"] = IntegerField("Maximum chunks to return for this call.", 1, Math.Min(policy.MaxChunksPerRead, policy.MaxToolResultItems))
                        },
                        new List<string> { "document_id" }));

            if (String.Equals(toolName, "collection_enumerate_documents", StringComparison.OrdinalIgnoreCase))
                return Function(
                    "collection_enumerate_documents",
                    "List one page of completed documents available in the assistant's assigned collection using safe metadata. This is paginated; use ContinuationToken for more pages and do not treat one page as the full corpus unless EndOfResults is true.",
                    Object(
                        new Dictionary<string, AssistantToolJsonSchema>
                        {
                            ["query"] = StringField("Optional document name, filename, content type, or allowed source URL search text."),
                            ["content_type"] = StringField("Optional content type filter, for example application/pdf or text/plain."),
                            ["status"] = StringField("Optional document status filter. Accepted only when policy allows non-completed metadata visibility."),
                            ["labels"] = ArrayField(StringField("Required label."), "Optional labels that further narrow assistant-visible documents."),
                            ["required_labels"] = ArrayField(StringField("Required label."), "Optional required labels that further narrow assistant-visible documents."),
                            ["excluded_labels"] = ArrayField(StringField("Excluded label."), "Optional excluded labels that further narrow assistant-visible documents."),
                            ["tags"] = ObjectMapField("Optional tag key/value pairs that further narrow assistant-visible documents."),
                            ["source_url_contains"] = StringField("Optional source URL substring filter. Accepted only when assistant policy allows source URLs."),
                            ["max_results"] = IntegerField("Maximum documents to return for this page.", 1, Math.Min(policy.MaxSearchResultsPerCall, policy.MaxToolResultItems)),
                            ["continuation_token"] = StringField("Continuation token from a previous enumeration response.")
                        },
                        new List<string>()));

            if (String.Equals(toolName, "verbex_full_text_search", StringComparison.OrdinalIgnoreCase))
                return Function(
                    "verbex_full_text_search",
                    "Search the assistant tenant's allowed Verbex full-text index for exact terms, phrases, identifiers, or lexical matches. The server filters results back to completed assistant documents.",
                    Object(
                        new Dictionary<string, AssistantToolJsonSchema>
                        {
                            ["query"] = StringField("Full-text query. Use * only when browsing is intentionally needed."),
                            ["index_id"] = StringField("Optional allowed Verbex index ID. Omit to use the assistant tenant default index."),
                            ["record_ids"] = ArrayField(StringField("Verbex record ID that already maps to a completed assistant document."), "Optional Verbex record IDs to narrow results. Each ID must map to a visible assistant document."),
                            ["max_results"] = IntegerField("Maximum results to return.", 1, verbexMaxResults),
                            ["use_and_logic"] = BooleanField("Whether all query terms must match."),
                            ["required_terms"] = ArrayField(StringField("Required term."), "Terms that must be present."),
                            ["excluded_terms"] = ArrayField(StringField("Excluded term."), "Terms that must not be present.")
                        },
                        new List<string> { "query" }));

            if (String.Equals(toolName, "index_enumerate_records", StringComparison.OrdinalIgnoreCase))
                return Function(
                    "index_enumerate_records",
                    "List safe metadata for records in an allowed Verbex index. The server maps records back to completed assistant documents before returning them.",
                    Object(
                        new Dictionary<string, AssistantToolJsonSchema>
                        {
                            ["index_id"] = StringField("Optional allowed Verbex index ID. Omit to use the assistant tenant default index."),
                            ["record_ids"] = ArrayField(StringField("Verbex record ID that already maps to a completed assistant document."), "Optional Verbex record IDs to narrow enumeration results. Each ID must map to a visible assistant document."),
                            ["query"] = StringField("Optional safe metadata filter over record ID, document name, filename, content type, and allowed source URL."),
                            ["record_id_prefix"] = StringField("Optional record ID prefix filter."),
                            ["max_results"] = IntegerField("Maximum records to return.", 1, verbexMaxResults),
                            ["continuation_token"] = StringField("Continuation token from a previous enumeration response.")
                        },
                        new List<string>()));

            if (String.Equals(toolName, "s3_object_read", StringComparison.OrdinalIgnoreCase))
                return Function(
                    "s3_object_read",
                    "Read bounded text, base64 bytes, or metadata from a completed assistant document's S3 object. Bucket-wide object keys are accepted only when policy and storage support explicitly allow them.",
                    Object(
                        new Dictionary<string, AssistantToolJsonSchema>
                        {
                            ["document_id"] = StringField("Completed AssistantDocument.Id value in the assistant collection. Preferred document-backed path."),
                            ["object_key"] = StringField("S3 object key for bucket-wide reads when assistant policy explicitly allows them."),
                            ["bucket"] = StringField("Optional bucket name for bucket-wide reads; ignored for document-backed reads."),
                            ["range_start"] = IntegerField("Zero-based byte offset to begin reading.", 0, Int32.MaxValue),
                            ["range_length"] = IntegerField("Maximum bytes to return from range_start.", 0, policy.MaxObjectReadBytes),
                            ["text_start"] = IntegerField("Zero-based character offset after UTF-8 decoding.", 0, Int32.MaxValue),
                            ["text_length"] = IntegerField("Maximum decoded text characters to return.", 0, policy.MaxToolOutputChars),
                            ["content_mode"] = StringField("Output mode. Use metadata_only for binary objects unless base64 output is explicitly needed and allowed.", new List<string> { "text", "base64", "metadata_only" })
                        },
                        new List<string>()));

            if (String.Equals(toolName, "document_atom_extract", StringComparison.OrdinalIgnoreCase))
                return Function(
                    "document_atom_extract",
                    "Extract text from a completed assistant document or a user-uploaded local attachment using the server-configured DocumentAtom service. Use document_id for assistant collection documents, or local_attachment_id for files uploaded in this chat turn.",
                    Object(
                        new Dictionary<string, AssistantToolJsonSchema>
                        {
                            ["document_id"] = StringField("Completed AssistantDocument.Id value in the assistant collection."),
                            ["local_attachment_id"] = StringField("Per-turn local attachment ID, such as local_attachment_1."),
                            ["document_type"] = StringField("Optional DocumentAtom document type override, such as pdf, docx, xlsx, text, html, json, markdown, or xml."),
                            ["text_start"] = IntegerField("Zero-based character offset in extracted text.", 0, Int32.MaxValue),
                            ["text_length"] = IntegerField("Maximum extracted text characters to return.", 1, Math.Min(policy.MaxAtomExtractionCharacters, policy.MaxToolOutputChars))
                        },
                        new List<string>()));

            if (String.Equals(toolName, "bucket_enumerate_objects", StringComparison.OrdinalIgnoreCase))
                return Function(
                    "bucket_enumerate_objects",
                    "List S3 object metadata from an explicitly allowed bucket and prefix. Object keys are redacted by default and mapped back to assistant documents when possible.",
                    Object(
                        new Dictionary<string, AssistantToolJsonSchema>
                        {
                            ["bucket"] = StringField("Optional bucket name. Omit to use the configured default bucket."),
                            ["prefix"] = StringField("Required object key prefix unless assistant policy provides exactly one allowed prefix."),
                            ["suffix"] = StringField("Optional object key suffix filter, such as .pdf or .txt."),
                            ["content_type"] = StringField("Optional content type filter applied to mapped assistant documents when available."),
                            ["max_results"] = IntegerField("Maximum objects to return.", 1, bucketMaxResults),
                            ["continuation_token"] = StringField("Continuation token from a previous bucket_enumerate_objects response.")
                        },
                        new List<string>()));

            if (String.Equals(toolName, "web_search", StringComparison.OrdinalIgnoreCase))
            {
                Dictionary<string, AssistantToolJsonSchema> properties = new Dictionary<string, AssistantToolJsonSchema>
                {
                    ["query"] = StringField("Public web search query."),
                    ["max_results"] = IntegerField("Maximum web results to return.", 1, webMaxResults),
                    ["search_depth"] = StringField("Tavily search depth.", policy.AllowAdvancedSearchDepth ? new List<string> { "basic", "advanced" } : new List<string> { "basic" }),
                    ["topic"] = StringField("Tavily search topic.", policy.AllowNewsTopic ? new List<string> { "general", "news" } : new List<string> { "general" }),
                    ["time_range"] = StringField("Optional Tavily time range, such as day, week, month, or year."),
                    ["start_date"] = StringField("Optional start date formatted yyyy-MM-dd."),
                    ["end_date"] = StringField("Optional end date formatted yyyy-MM-dd."),
                    ["include_answer"] = BooleanField("Whether to request a provider-generated answer."),
                    ["safe_search"] = BooleanField("Whether to request safe-search filtering. Server policy can force this to true."),
                    ["country"] = StringField("Optional Tavily country hint."),
                    ["include_domains"] = ArrayField(StringField("Domain name."), "Domains to include, further restricted by assistant policy."),
                    ["exclude_domains"] = ArrayField(StringField("Domain name."), "Domains to exclude; assistant blocked domains are always excluded.")
                };

                if (policy.AllowRawWebContent)
                    properties["include_raw_content"] = BooleanField("Whether to request raw web page content.");

                if (policy.AllowWebImages)
                {
                    properties["include_images"] = BooleanField("Whether to request image URLs.");
                    properties["include_image_descriptions"] = BooleanField("Whether to request image descriptions.");
                }

                return Function(
                    "web_search",
                    "Search the public web through the server-configured Tavily provider. Use this only for public, current, or external information.",
                    Object(properties, new List<string> { "query" }));
            }

            return null;
        }

        private static AssistantToolDefinition Function(string name, string description, AssistantToolJsonSchema parameters)
        {
            return new AssistantToolDefinition
            {
                Function = new AssistantToolFunctionDefinition
                {
                    Name = name,
                    Description = description,
                    Parameters = parameters
                }
            };
        }

        private static AssistantToolJsonSchema Object(Dictionary<string, AssistantToolJsonSchema> properties, List<string> required)
        {
            return new AssistantToolJsonSchema
            {
                Type = "object",
                Properties = properties,
                Required = required,
                AdditionalProperties = false
            };
        }

        private static AssistantToolJsonSchema StringField(string description, List<string> allowedValues = null)
        {
            return new AssistantToolJsonSchema
            {
                Type = "string",
                Description = description,
                Enum = allowedValues
            };
        }

        private static AssistantToolJsonSchema IntegerField(string description, int minimum, int maximum)
        {
            return new AssistantToolJsonSchema
            {
                Type = "integer",
                Description = description,
                Minimum = minimum,
                Maximum = maximum
            };
        }

        private static AssistantToolJsonSchema NumberField(string description, double minimum, double maximum)
        {
            return new AssistantToolJsonSchema
            {
                Type = "number",
                Description = description,
                Minimum = minimum,
                Maximum = maximum
            };
        }

        private static AssistantToolJsonSchema BooleanField(string description)
        {
            return new AssistantToolJsonSchema
            {
                Type = "boolean",
                Description = description
            };
        }

        private static AssistantToolJsonSchema ObjectMapField(string description)
        {
            return new AssistantToolJsonSchema
            {
                Type = "object",
                Description = description,
                AdditionalProperties = true
            };
        }

        private static AssistantToolJsonSchema ArrayField(AssistantToolJsonSchema itemSchema, string description)
        {
            return new AssistantToolJsonSchema
            {
                Type = "array",
                Description = description,
                Items = itemSchema
            };
        }

        private static List<string> BuildSearchModeEnum(AssistantToolPolicy policy)
        {
            List<string> modes = new List<string>(policy.AllowedSearchModes ?? new List<string>());
            if (!modes.Contains("Auto", StringComparer.OrdinalIgnoreCase))
                modes.Add("Auto");
            return modes;
        }
    }
}
