#pragma warning disable CS1591

namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Builds, serializes, and projects provider-agnostic assistant performance telemetry.
    /// </summary>
    public static class AssistantPerformanceTelemetryBuilder
    {
        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Build provider-agnostic performance telemetry for a chat history row.
        /// </summary>
        /// <param name="history">Chat history row.</param>
        /// <param name="finalInferenceStage">Measured final inference stage.</param>
        /// <param name="retrievalQueryCount">Number of retrieval queries issued.</param>
        /// <param name="retrievalChunksReturned">Number of chunks returned by retrieval.</param>
        /// <param name="retrievalGateStage">Measured retrieval gate stage.</param>
        /// <param name="queryRewriteStage">Measured query rewrite stage.</param>
        /// <param name="rerankStage">Measured rerank stage.</param>
        /// <returns>Provider-agnostic performance telemetry.</returns>
        public static AssistantPerformanceTelemetry Build(
            ChatHistory history,
            AssistantPerformanceStage finalInferenceStage,
            int retrievalQueryCount,
            int retrievalChunksReturned,
            AssistantPerformanceStage retrievalGateStage = null,
            AssistantPerformanceStage queryRewriteStage = null,
            AssistantPerformanceStage rerankStage = null)
        {
            if (history == null) throw new ArgumentNullException(nameof(history));

            AssistantPerformanceTelemetry telemetry = new AssistantPerformanceTelemetry
            {
                SchemaVersion = history.PerformanceSchemaVersion <= 0 ? 1 : history.PerformanceSchemaVersion,
                TraceId = history.TraceId,
                ChatHistoryId = history.Id,
                RequestHistoryId = history.RequestHistoryId,
                AssistantId = history.AssistantId,
                CreatedUtc = history.CreatedUtc == default ? DateTime.UtcNow : history.CreatedUtc,
                WallTimeMs = Math.Round(history.TimeToLastTokenMs, 2)
            };

            AddMeasuredOrLegacyStage(
                telemetry,
                "retrieval_gate",
                "inference",
                10,
                history.RetrievalGateDurationMs,
                null,
                new Dictionary<string, object>
                {
                    ["decision"] = history.RetrievalGateDecision
                },
                retrievalGateStage);

            AddMeasuredOrLegacyStage(
                telemetry,
                "query_rewrite",
                "inference",
                20,
                history.QueryRewriteDurationMs,
                null,
                new Dictionary<string, object>
                {
                    ["retrieval_query_count"] = retrievalQueryCount
                },
                queryRewriteStage);

            AddLegacyStage(
                telemetry,
                "retrieval",
                "retrieval",
                30,
                history.RetrievalDurationMs,
                history.RetrievalStartUtc,
                new Dictionary<string, object>
                {
                    ["retrieval_query_count"] = retrievalQueryCount,
                    ["chunks_output"] = retrievalChunksReturned,
                    ["metadata_filter"] = history.MetadataFilter
                });

            AddMeasuredOrLegacyStage(
                telemetry,
                "rerank",
                "inference",
                40,
                history.RerankDurationMs,
                null,
                new Dictionary<string, object>
                {
                    ["chunks_input"] = history.RerankInputCount,
                    ["chunks_output"] = history.RerankOutputCount
                },
                rerankStage);

            AddLegacyStage(telemetry, "endpoint_resolution", "network", 50, history.EndpointResolutionDurationMs, null, null);
            AddLegacyStage(telemetry, "context_compaction", "inference", 60, history.CompactionDurationMs, null, null);

            AssistantPerformanceStage finalStage = finalInferenceStage != null
                ? CloneStage(finalInferenceStage)
                : new AssistantPerformanceStage();

            finalStage.Name = "final_inference";
            finalStage.Kind ??= "inference";
            finalStage.Sequence = 70;
            finalStage.DurationMs = RoundPositive(finalStage.DurationMs > 0 ? finalStage.DurationMs : history.TimeToLastTokenMs);
            finalStage.Success = finalStage.Success || String.IsNullOrEmpty(finalStage.ErrorMessage);
            finalStage.ClientTimings ??= new AssistantPerformanceClientTimings();
            finalStage.ClientTimings.RequestToHeadersMs ??= Positive(history.InferenceConnectionDurationMs);
            finalStage.ClientTimings.TotalMs ??= Positive(history.TimeToLastTokenMs);

            if (history.TimeToFirstTokenMs > 0 && history.InferenceConnectionDurationMs > 0)
                finalStage.ClientTimings.HeadersToFirstTokenMs ??= Positive(history.TimeToFirstTokenMs - history.InferenceConnectionDurationMs);

            if (history.TimeToLastTokenMs > 0 && history.TimeToFirstTokenMs > 0)
                finalStage.ClientTimings.FirstTokenToLastTokenMs ??= Positive(history.TimeToLastTokenMs - history.TimeToFirstTokenMs);

            finalStage.Tokens ??= new AssistantTokenUsageTelemetry();
            finalStage.Tokens.Input ??= history.PromptTokens > 0 ? history.PromptTokens : null;
            finalStage.Tokens.Output ??= history.CompletionTokens > 0 ? history.CompletionTokens : null;
            finalStage.Tokens.Total ??= (history.PromptTokens + history.CompletionTokens) > 0 ? history.PromptTokens + history.CompletionTokens : null;
            telemetry.Stages.Add(finalStage);

            return telemetry;
        }

        /// <summary>
        /// Serialize provider-agnostic performance telemetry.
        /// </summary>
        /// <param name="telemetry">Telemetry object.</param>
        /// <returns>Serialized telemetry JSON, or null when telemetry is null.</returns>
        public static string Serialize(AssistantPerformanceTelemetry telemetry)
        {
            return telemetry == null ? null : JsonSerializer.Serialize(telemetry, _JsonOptions);
        }

        /// <summary>
        /// Deserialize provider-agnostic performance telemetry.
        /// </summary>
        /// <param name="json">Serialized telemetry JSON.</param>
        /// <returns>Deserialized telemetry, or null when input is empty.</returns>
        public static AssistantPerformanceTelemetry Deserialize(string json)
        {
            if (String.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<AssistantPerformanceTelemetry>(json, _JsonOptions);
        }

        /// <summary>
        /// Project provider-agnostic performance telemetry into normalized database event rows.
        /// </summary>
        /// <param name="telemetry">Telemetry object.</param>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <returns>Normalized performance event rows.</returns>
        public static List<ChatHistoryPerformanceEvent> ToEvents(AssistantPerformanceTelemetry telemetry, string tenantId)
        {
            List<ChatHistoryPerformanceEvent> events = new List<ChatHistoryPerformanceEvent>();
            if (telemetry == null || telemetry.Stages == null) return events;

            foreach (AssistantPerformanceStage stage in telemetry.Stages)
            {
                if (stage == null || String.IsNullOrWhiteSpace(stage.Name)) continue;

                ChatHistoryPerformanceEvent evt = new ChatHistoryPerformanceEvent
                {
                    Id = IdGenerator.NewChatHistoryPerformanceEventId(),
                    TenantId = tenantId,
                    AssistantId = telemetry.AssistantId,
                    ChatHistoryId = telemetry.ChatHistoryId,
                    RequestHistoryId = telemetry.RequestHistoryId,
                    TraceId = telemetry.TraceId,
                    SequenceNumber = stage.Sequence,
                    Stage = stage.Name,
                    Phase = GetMetadataString(stage.Metadata, "phase"),
                    Kind = stage.Kind,
                    EndpointId = stage.EndpointId,
                    EndpointName = stage.EndpointName,
                    EndpointType = stage.EndpointType,
                    Provider = stage.Provider,
                    ApiFormat = stage.ApiFormat,
                    Model = stage.Model,
                    StartedUtc = stage.StartedUtc,
                    FinishedUtc = stage.FinishedUtc,
                    DurationMs = stage.DurationMs,
                    Success = stage.Success,
                    HttpStatusCode = stage.HttpStatusCode,
                    ErrorType = stage.ErrorType,
                    ErrorMessage = stage.ErrorMessage,
                    InputTokens = stage.Tokens?.Input,
                    OutputTokens = stage.Tokens?.Output,
                    TotalTokens = stage.Tokens?.Total,
                    ChunksInput = GetMetadataInt(stage.Metadata, "chunks_input"),
                    ChunksOutput = GetMetadataInt(stage.Metadata, "chunks_output"),
                    RetrievalQueryCount = GetMetadataInt(stage.Metadata, "retrieval_query_count"),
                    EndpointLimiterWaitMs = stage.ClientTimings?.EndpointLimiterWaitMs,
                    RequestToHeadersMs = stage.ClientTimings?.RequestToHeadersMs,
                    HeadersToFirstTokenMs = stage.ClientTimings?.HeadersToFirstTokenMs,
                    FirstTokenToLastTokenMs = stage.ClientTimings?.FirstTokenToLastTokenMs,
                    ClientTotalMs = stage.ClientTimings?.TotalMs,
                    ProviderQueueMs = stage.ProviderMetrics?.QueueMs,
                    ProviderLoadMs = stage.ProviderMetrics?.LoadMs,
                    ProviderPromptEvalMs = stage.ProviderMetrics?.PromptEvalMs,
                    ProviderGenerationMs = stage.ProviderMetrics?.GenerationMs,
                    ProviderTotalMs = stage.ProviderMetrics?.TotalMs,
                    ProviderTokensPerSecond = stage.ProviderMetrics?.TokensPerSecond,
                    ProviderRequestId = stage.ProviderMetrics?.RequestId,
                    MetadataJson = SerializeObject(stage.Metadata),
                    ProviderMetricsJson = SerializeObject(stage.ProviderMetrics),
                    ProviderRawJson = SerializeObject(stage.ProviderRaw),
                    CreatedUtc = telemetry.CreatedUtc
                };

                events.Add(evt);
            }

            return events;
        }

        private static void AddLegacyStage(
            AssistantPerformanceTelemetry telemetry,
            string name,
            string kind,
            int sequence,
            double durationMs,
            DateTime? startedUtc,
            Dictionary<string, object> metadata)
        {
            bool hasMetadata = metadata != null && metadata.Count > 0;
            bool hasMeaningfulDuration = durationMs > 0;
            if (!hasMeaningfulDuration && !hasMetadata) return;

            DateTime? finishedUtc = null;
            if (startedUtc.HasValue && hasMeaningfulDuration)
                finishedUtc = startedUtc.Value.AddMilliseconds(durationMs);

            telemetry.Stages.Add(new AssistantPerformanceStage
            {
                Name = name,
                Kind = kind,
                Sequence = sequence,
                StartedUtc = startedUtc,
                FinishedUtc = finishedUtc,
                DurationMs = RoundPositive(durationMs),
                Success = true,
                Metadata = NormalizeMetadata(metadata)
            });
        }

        private static void AddMeasuredOrLegacyStage(
            AssistantPerformanceTelemetry telemetry,
            string name,
            string kind,
            int sequence,
            double durationMs,
            DateTime? startedUtc,
            Dictionary<string, object> metadata,
            AssistantPerformanceStage measuredStage)
        {
            if (measuredStage == null)
            {
                AddLegacyStage(telemetry, name, kind, sequence, durationMs, startedUtc, metadata);
                return;
            }

            AssistantPerformanceStage stage = CloneStage(measuredStage);
            stage.Name = name;
            stage.Kind = !String.IsNullOrWhiteSpace(kind) ? kind : stage.Kind;
            stage.Sequence = sequence;
            stage.DurationMs = RoundPositive(stage.DurationMs > 0 ? stage.DurationMs : durationMs);

            if (!stage.StartedUtc.HasValue)
                stage.StartedUtc = startedUtc;

            if (!stage.FinishedUtc.HasValue && stage.StartedUtc.HasValue && stage.DurationMs > 0)
                stage.FinishedUtc = stage.StartedUtc.Value.AddMilliseconds(stage.DurationMs);

            stage.Success = stage.Success || String.IsNullOrEmpty(stage.ErrorMessage);
            stage.Metadata = MergeMetadata(stage.Metadata, metadata);

            bool hasMetadata = stage.Metadata != null && stage.Metadata.Count > 0;
            bool hasMeaningfulDuration = stage.DurationMs > 0;
            bool hasEndpointDetails =
                !String.IsNullOrWhiteSpace(stage.EndpointId)
                || !String.IsNullOrWhiteSpace(stage.EndpointName)
                || !String.IsNullOrWhiteSpace(stage.Provider)
                || !String.IsNullOrWhiteSpace(stage.Model);

            if (!hasMeaningfulDuration && !hasMetadata && !hasEndpointDetails) return;

            telemetry.Stages.Add(stage);
        }

        private static Dictionary<string, object> MergeMetadata(Dictionary<string, object> existing, Dictionary<string, object> additional)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();

            Dictionary<string, object> normalizedExisting = NormalizeMetadata(existing);
            if (normalizedExisting != null)
            {
                foreach (KeyValuePair<string, object> kvp in normalizedExisting)
                    ret[kvp.Key] = kvp.Value;
            }

            Dictionary<string, object> normalizedAdditional = NormalizeMetadata(additional);
            if (normalizedAdditional != null)
            {
                foreach (KeyValuePair<string, object> kvp in normalizedAdditional)
                    ret[kvp.Key] = kvp.Value;
            }

            return ret.Count > 0 ? ret : null;
        }

        private static Dictionary<string, object> NormalizeMetadata(Dictionary<string, object> metadata)
        {
            if (metadata == null) return null;

            Dictionary<string, object> ret = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kvp in metadata)
            {
                if (kvp.Value == null) continue;
                if (kvp.Value is string str && String.IsNullOrWhiteSpace(str)) continue;
                ret[kvp.Key] = kvp.Value;
            }

            return ret.Count > 0 ? ret : null;
        }

        private static AssistantPerformanceStage CloneStage(AssistantPerformanceStage stage)
        {
            string json = JsonSerializer.Serialize(stage, _JsonOptions);
            return JsonSerializer.Deserialize<AssistantPerformanceStage>(json, _JsonOptions);
        }

        private static string SerializeObject(object value)
        {
            return value == null ? null : JsonSerializer.Serialize(value, _JsonOptions);
        }

        private static int? GetMetadataInt(Dictionary<string, object> metadata, string key)
        {
            if (metadata == null || !metadata.TryGetValue(key, out object value) || value == null) return null;
            if (value is int intValue) return intValue;
            if (value is long longValue) return (int)longValue;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int jsonValue)) return jsonValue;
            if (Int32.TryParse(value.ToString(), out int parsed)) return parsed;
            return null;
        }

        private static string GetMetadataString(Dictionary<string, object> metadata, string key)
        {
            if (metadata == null || !metadata.TryGetValue(key, out object value) || value == null) return null;
            return value.ToString();
        }

        private static double RoundPositive(double value)
        {
            return value > 0 ? Math.Round(value, 2) : 0;
        }

        private static double? Positive(double value)
        {
            return value > 0 ? Math.Round(value, 2) : null;
        }
    }
}

#pragma warning restore CS1591
