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
    /// Typed facade for assistant S3/object tools.
    /// </summary>
    public class ObjectToolService
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
        public ObjectToolService(IAssistantToolExecutor executor)
        {
            _Executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        /// <summary>
        /// Read a document-backed or explicitly bucket-wide object through the policy-scoped s3_object_read tool.
        /// </summary>
        public Task<AssistantToolExecutionResult> ReadObjectAsync(
            AssistantToolExecutionContext context,
            ObjectToolReadRequest request,
            CancellationToken token = default)
        {
            return ExecuteAsync(context, "s3_object_read", request, token);
        }

        /// <summary>
        /// Enumerate objects through the policy-scoped bucket_enumerate_objects tool.
        /// </summary>
        public Task<AssistantToolExecutionResult> EnumerateObjectsAsync(
            AssistantToolExecutionContext context,
            ObjectToolEnumerateRequest request,
            CancellationToken token = default)
        {
            return ExecuteAsync(context, "bucket_enumerate_objects", request, token);
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
    /// Strongly typed s3_object_read request.
    /// </summary>
    public class ObjectToolReadRequest
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

    /// <summary>
    /// Strongly typed bucket_enumerate_objects request.
    /// </summary>
    public class ObjectToolEnumerateRequest
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

    /// <summary>
    /// Safe response model for mapped object reads.
    /// </summary>
    public class ObjectToolReadResult
    {
        public bool DocumentBacked { get; set; }
        public string DocumentId { get; set; }
        public string DocumentName { get; set; }
        public string ContentType { get; set; }
        public long SizeBytes { get; set; }
        public string Bucket { get; set; }
        public string ObjectKey { get; set; }
        public int RangeStart { get; set; }
        public int RangeLength { get; set; }
        public int RangeEndExclusive { get; set; }
        public bool Truncated { get; set; }
        public string ContentMode { get; set; }
        public string Content { get; set; }
        public string Base64 { get; set; }
        public string CitationHandle { get; set; }
    }

    /// <summary>
    /// Safe response model for bucket object enumeration.
    /// </summary>
    public class ObjectToolEnumerationItem
    {
        public string Bucket { get; set; }
        public string Key { get; set; }
        public long SizeBytes { get; set; }
        public string ContentType { get; set; }
        public DateTime? LastModifiedUtc { get; set; }
        public string ETag { get; set; }
        public string DocumentId { get; set; }
        public string DocumentName { get; set; }
        public bool ReadAllowed { get; set; }
    }
}

#pragma warning restore CS1591
