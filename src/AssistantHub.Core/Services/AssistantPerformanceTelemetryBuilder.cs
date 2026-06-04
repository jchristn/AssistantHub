#pragma warning disable CS1591

namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;

    public static class AssistantPerformanceTelemetryBuilder
    {
        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };

        public static AssistantPerformanceTelemetry Build(
            ChatHistory history,
            AssistantPerformanceStage finalInferenceStage,
            int retrievalQueryCount,
            int retrievalChunksReturned)
        {
            if (history == null) throw new ArgumentNullException(nameof(history));

            AssistantPerformanceTelemetry telemetry = new AssistantPerformanceTelemetry
            {
                SchemaVersion = history.PerformanceSchemaVersion <= 0 ? 1 : history.PerformanceSchemaVersion,
                TraceId = history.TraceId,
                ChatHistoryId = history.Id,
                RequestHistoryId = history.RequestHistoryId,
                CreatedUtc = history.CreatedUtc == default ? DateTime.UtcNow : history.CreatedUtc,
                WallTimeMs = Math.Round(history.TimeToLastTokenMs, 2)
            };

            AddLegacyStage(
                telemetry,
                "retrieval_gate",
                "inference",
                10,
                history.RetrievalGateDurationMs,
                null,
                new Dictionary<string, object>
                {
                    ["decision"] = history.RetrievalGateDecision
                });

            AddLegacyStage(
                telemetry,
                "query_rewrite",
                "inference",
                20,
                history.QueryRewriteDurationMs,
                null,
                new Dictionary<string, object>
                {
                    ["retrieval_query_count"] = retrievalQueryCount
                });

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

            AddLegacyStage(
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
                });

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

        public static string Serialize(AssistantPerformanceTelemetry telemetry)
        {
            return telemetry == null ? null : JsonSerializer.Serialize(telemetry, _JsonOptions);
        }

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
