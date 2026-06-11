namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Validates model-supplied tool arguments before dispatch.
    /// </summary>
    public static class AssistantToolArgumentValidator
    {
        private static readonly Dictionary<string, HashSet<string>> _AllowedProperties = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["collection_search"] = Set("query", "queries", "max_results", "top_k", "include_neighbors", "strategy", "search_mode", "score_threshold", "document_ids", "labels", "required_labels", "excluded_labels", "tags", "required_tags", "excluded_tags", "source_url_contains", "fulltext_search_type", "fulltext_language", "fulltext_normalization", "fulltext_minimum_score"),
            ["collection_read_chunks"] = Set("document_id", "positions", "ranges", "max_chunks", "neighbor_window"),
            ["collection_enumerate_documents"] = Set("max_results", "continuation_token", "query", "content_type", "status", "labels", "required_labels", "excluded_labels", "tags", "required_tags", "excluded_tags", "source_url_contains"),
            ["verbex_full_text_search"] = Set("query", "index_id", "record_ids", "max_results", "use_and_logic", "required_terms", "excluded_terms"),
            ["index_enumerate_records"] = Set("index_id", "record_ids", "max_results", "continuation_token", "query", "record_id_prefix"),
            ["s3_object_read"] = Set("document_id", "bucket", "bucket_name", "object_key", "range_start", "range_length", "text_start", "text_length", "content_mode"),
            ["document_atom_extract"] = Set("document_id", "local_attachment_id", "document_type", "text_start", "text_length"),
            ["bucket_enumerate_objects"] = Set("bucket", "bucket_name", "prefix", "suffix", "content_type", "max_results", "continuation_token"),
            ["web_search"] = Set("query", "max_results", "search_depth", "topic", "time_range", "start_date", "end_date", "include_answer", "safe_search", "country", "include_raw_content", "include_images", "include_image_descriptions", "include_domains", "exclude_domains")
        };

        private static readonly Dictionary<string, Type> _ArgumentTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["collection_search"] = typeof(CollectionSearchArguments),
            ["collection_read_chunks"] = typeof(CollectionReadChunksArguments),
            ["collection_enumerate_documents"] = typeof(CollectionEnumerateDocumentsArguments),
            ["verbex_full_text_search"] = typeof(VerbexFullTextSearchArguments),
            ["index_enumerate_records"] = typeof(IndexEnumerateRecordsArguments),
            ["s3_object_read"] = typeof(S3ObjectReadArguments),
            ["document_atom_extract"] = typeof(DocumentAtomExtractArguments),
            ["bucket_enumerate_objects"] = typeof(BucketEnumerateObjectsArguments),
            ["web_search"] = typeof(WebSearchArguments)
        };

        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new FlexibleNullableIntConverter(),
                new FlexibleNullableDoubleConverter(),
                new FlexibleNullableBoolConverter(),
                new FlexibleStringListConverter(),
                new FlexibleIntListConverter(),
                new FlexibleTagFilterConverter()
            }
        };

        /// <summary>
        /// Ensure the argument payload is a JSON object with only known properties and expected value types.
        /// </summary>
        /// <param name="toolName">Tool name.</param>
        /// <param name="arguments">Parsed argument root.</param>
        public static void Validate(string toolName, JsonElement arguments)
        {
            if (arguments.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("Tool arguments must be a JSON object.");

            string normalizedToolName = AssistantToolRegistry.NormalizeToolName(toolName) ?? toolName?.Trim();
            if (String.IsNullOrWhiteSpace(normalizedToolName))
                return;

            if (!_AllowedProperties.TryGetValue(normalizedToolName, out HashSet<string> allowed))
                return;

            List<string> unknown = arguments
                .EnumerateObject()
                .Select(property => property.Name)
                .Where(name => !allowed.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (unknown.Count > 0)
                throw new ArgumentException("Unknown argument propert" + (unknown.Count == 1 ? "y" : "ies") + " for " + normalizedToolName + ": " + String.Join(", ", unknown) + ".");

            if (!_ArgumentTypes.TryGetValue(normalizedToolName, out Type argumentType))
                return;

            try
            {
                JsonSerializer.Deserialize(arguments.GetRawText(), argumentType, _JsonOptions);
            }
            catch (JsonException e)
            {
                throw new ArgumentException("Invalid JSON argument payload for " + normalizedToolName + ": " + e.Message, e);
            }
        }

        private static HashSet<string> Set(params string[] names)
        {
            return new HashSet<string>(names ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        }

        private sealed class CollectionSearchArguments
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
            public TagFilterArguments Tags { get; set; }

            [JsonPropertyName("required_tags")]
            public TagFilterArguments RequiredTags { get; set; }

            [JsonPropertyName("excluded_tags")]
            public TagFilterArguments ExcludedTags { get; set; }

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

        private sealed class CollectionReadChunksArguments
        {
            [JsonPropertyName("document_id")]
            public string DocumentId { get; set; }

            [JsonPropertyName("positions")]
            public List<int> Positions { get; set; }

            [JsonPropertyName("ranges")]
            public List<ChunkRangeArguments> Ranges { get; set; }

            [JsonPropertyName("max_chunks")]
            public int? MaxChunks { get; set; }

            [JsonPropertyName("neighbor_window")]
            public int? NeighborWindow { get; set; }
        }

        private sealed class ChunkRangeArguments
        {
            [JsonPropertyName("start_position")]
            public int? StartPosition { get; set; }

            [JsonPropertyName("startPosition")]
            public int? StartPositionCamel { get; set; }

            [JsonPropertyName("start")]
            public int? Start { get; set; }

            [JsonPropertyName("count")]
            public int? Count { get; set; }

            [JsonPropertyName("length")]
            public int? Length { get; set; }
        }

        private sealed class CollectionEnumerateDocumentsArguments
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
            public TagFilterArguments Tags { get; set; }

            [JsonPropertyName("required_tags")]
            public TagFilterArguments RequiredTags { get; set; }

            [JsonPropertyName("excluded_tags")]
            public TagFilterArguments ExcludedTags { get; set; }

            [JsonPropertyName("source_url_contains")]
            public string SourceUrlContains { get; set; }
        }

        private sealed class VerbexFullTextSearchArguments
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

        private sealed class IndexEnumerateRecordsArguments
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

        private sealed class S3ObjectReadArguments
        {
            [JsonPropertyName("document_id")]
            public string DocumentId { get; set; }

            [JsonPropertyName("bucket")]
            public string Bucket { get; set; }

            [JsonPropertyName("bucket_name")]
            public string BucketName { get; set; }

            [JsonPropertyName("object_key")]
            public string ObjectKey { get; set; }

            [JsonPropertyName("range_start")]
            public int? RangeStart { get; set; }

            [JsonPropertyName("range_length")]
            public int? RangeLength { get; set; }

            [JsonPropertyName("text_start")]
            public int? TextStart { get; set; }

            [JsonPropertyName("text_length")]
            public int? TextLength { get; set; }

            [JsonPropertyName("content_mode")]
            public string ContentMode { get; set; }
        }

        private sealed class DocumentAtomExtractArguments
        {
            [JsonPropertyName("document_id")]
            public string DocumentId { get; set; }

            [JsonPropertyName("local_attachment_id")]
            public string LocalAttachmentId { get; set; }

            [JsonPropertyName("document_type")]
            public string DocumentType { get; set; }

            [JsonPropertyName("text_start")]
            public int? TextStart { get; set; }

            [JsonPropertyName("text_length")]
            public int? TextLength { get; set; }
        }

        private sealed class BucketEnumerateObjectsArguments
        {
            [JsonPropertyName("bucket")]
            public string Bucket { get; set; }

            [JsonPropertyName("bucket_name")]
            public string BucketName { get; set; }

            [JsonPropertyName("prefix")]
            public string Prefix { get; set; }

            [JsonPropertyName("suffix")]
            public string Suffix { get; set; }

            [JsonPropertyName("content_type")]
            public string ContentType { get; set; }

            [JsonPropertyName("max_results")]
            public int? MaxResults { get; set; }

            [JsonPropertyName("continuation_token")]
            public string ContinuationToken { get; set; }
        }

        private sealed class WebSearchArguments
        {
            [JsonPropertyName("query")]
            public string Query { get; set; }

            [JsonPropertyName("max_results")]
            public int? MaxResults { get; set; }

            [JsonPropertyName("search_depth")]
            public string SearchDepth { get; set; }

            [JsonPropertyName("topic")]
            public string Topic { get; set; }

            [JsonPropertyName("time_range")]
            public string TimeRange { get; set; }

            [JsonPropertyName("start_date")]
            public string StartDate { get; set; }

            [JsonPropertyName("end_date")]
            public string EndDate { get; set; }

            [JsonPropertyName("include_answer")]
            public bool? IncludeAnswer { get; set; }

            [JsonPropertyName("safe_search")]
            public bool? SafeSearch { get; set; }

            [JsonPropertyName("country")]
            public string Country { get; set; }

            [JsonPropertyName("include_raw_content")]
            public bool? IncludeRawContent { get; set; }

            [JsonPropertyName("include_images")]
            public bool? IncludeImages { get; set; }

            [JsonPropertyName("include_image_descriptions")]
            public bool? IncludeImageDescriptions { get; set; }

            [JsonPropertyName("include_domains")]
            public List<string> IncludeDomains { get; set; }

            [JsonPropertyName("exclude_domains")]
            public List<string> ExcludeDomains { get; set; }
        }

        private sealed class TagFilterArguments
        {
        }

        private sealed class FlexibleNullableIntConverter : JsonConverter<int?>
        {
            public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;
                if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int numeric)) return numeric;
                if (reader.TokenType == JsonTokenType.String && Int32.TryParse(reader.GetString(), out numeric)) return numeric;
                throw new JsonException("Expected an integer or numeric string.");
            }

            public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
            {
                if (value.HasValue) writer.WriteNumberValue(value.Value);
                else writer.WriteNullValue();
            }
        }

        private sealed class FlexibleNullableDoubleConverter : JsonConverter<double?>
        {
            public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;
                if (reader.TokenType == JsonTokenType.Number && reader.TryGetDouble(out double numeric)) return numeric;
                if (reader.TokenType == JsonTokenType.String && Double.TryParse(reader.GetString(), out numeric)) return numeric;
                throw new JsonException("Expected a number or numeric string.");
            }

            public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
            {
                if (value.HasValue) writer.WriteNumberValue(value.Value);
                else writer.WriteNullValue();
            }
        }

        private sealed class FlexibleNullableBoolConverter : JsonConverter<bool?>
        {
            public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;
                if (reader.TokenType == JsonTokenType.True) return true;
                if (reader.TokenType == JsonTokenType.False) return false;
                if (reader.TokenType == JsonTokenType.String && Boolean.TryParse(reader.GetString(), out bool boolean)) return boolean;
                throw new JsonException("Expected a boolean or boolean string.");
            }

            public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
            {
                if (value.HasValue) writer.WriteBooleanValue(value.Value);
                else writer.WriteNullValue();
            }
        }

        private sealed class FlexibleStringListConverter : JsonConverter<List<string>>
        {
            public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;

                List<string> values = new List<string>();
                if (reader.TokenType == JsonTokenType.String)
                {
                    AddString(values, reader.GetString());
                    return values;
                }

                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new JsonException("Expected a string or string array.");

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return values;
                    if (reader.TokenType != JsonTokenType.String)
                        throw new JsonException("Expected string array items.");

                    AddString(values, reader.GetString());
                }

                throw new JsonException("Unterminated string array.");
            }

            public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }

            private static void AddString(List<string> values, string value)
            {
                if (!String.IsNullOrWhiteSpace(value))
                    values.Add(value.Trim());
            }
        }

        private sealed class FlexibleIntListConverter : JsonConverter<List<int>>
        {
            public override List<int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;

                List<int> values = new List<int>();
                if (TryReadInt(ref reader, out int single))
                {
                    values.Add(single);
                    return values;
                }

                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new JsonException("Expected an integer or integer array.");

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return values;
                    if (!TryReadInt(ref reader, out int value))
                        throw new JsonException("Expected integer array items.");

                    values.Add(value);
                }

                throw new JsonException("Unterminated integer array.");
            }

            public override void Write(Utf8JsonWriter writer, List<int> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }

            private static bool TryReadInt(ref Utf8JsonReader reader, out int value)
            {
                if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out value))
                    return true;
                if (reader.TokenType == JsonTokenType.String && Int32.TryParse(reader.GetString(), out value))
                    return true;

                value = 0;
                return false;
            }
        }

        private sealed class FlexibleTagFilterConverter : JsonConverter<TagFilterArguments>
        {
            public override TagFilterArguments Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;

                using JsonDocument document = JsonDocument.ParseValue(ref reader);
                JsonElement root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                    return new TagFilterArguments();

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in root.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object)
                            throw new JsonException("Expected tag filter array items to be objects.");
                    }

                    return new TagFilterArguments();
                }

                throw new JsonException("Expected a tag filter object or tag condition array.");
            }

            public override void Write(Utf8JsonWriter writer, TagFilterArguments value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
            }
        }
    }
}
