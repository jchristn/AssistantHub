namespace AssistantHub.Server.OpenApi
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for assistant analytics routes.
    /// </summary>
    public static class AssistantAnalyticsApiDocs
    {
        #region Private-Members

        private const string _Tag = "Assistant Analytics";

        private const string _CommonDescription =
            " Owner or administrator only; assistants in another tenant return 404 unless the caller is a global administrator. "
            + "The window defaults to range=lastDay. Supplying startUtc and endUtc (both required together) selects a custom window. "
            + "Invalid range, startUtc, endUtc, bucketSeconds, or limit values return 400. Buckets are widened automatically so that at most 240 are returned.";

        private const string _InferenceEndpointId = "cep_01JH3ZA1B3C5D7E9F1G3H5J7K9";

        private static readonly DateTime _RangeEnd = new DateTime(2026, 1, 16, 9, 45, 0, DateTimeKind.Utc);

        private static readonly DateTime _RangeStart = new DateTime(2026, 1, 15, 9, 45, 0, DateTimeKind.Utc);

        private static AssistantAnalyticsRange ExampleRange()
        {
            return new AssistantAnalyticsRange
            {
                RangeId = "lastDay",
                StartUtc = _RangeStart,
                EndUtc = _RangeEnd,
                BucketSeconds = 900,
                BucketCount = 96
            };
        }

        private static ApiDoc AnalyticsDoc(string summary, string description)
        {
            return ApiDoc.Create(summary, _Tag)
                .Describe(description + _CommonDescription)
                .Query("range", "string", "Predefined window: lastHour (60s buckets), lastDay (900s buckets, default), lastWeek (7200s buckets), or lastMonth (86400s buckets). Ignored when startUtc and endUtc are supplied.", false, "lastDay")
                .Query("startUtc", "string", "Custom window start (ISO 8601 UTC). Requires endUtc.", false, "2026-01-15T09:45:00Z")
                .Query("endUtc", "string", "Custom window end (ISO 8601 UTC). Requires startUtc and must be later than startUtc.", false, "2026-01-16T09:45:00Z")
                .Query("bucketSeconds", "integer", "Bucket width in seconds (must be greater than zero). Defaults to the range's bucket width, or about 96 buckets for a custom window. Reflected in the returned Range.", false, 900);
        }

        private static ApiDoc EventFilters(ApiDoc doc)
        {
            return doc
                .Query("stage", "string", "Only include telemetry events for this pipeline stage, for example final_inference, query_rewrite, rerank, or tools.", false, "final_inference")
                .Query("endpointId", "string", "Only include telemetry events for this endpoint identifier.", false, _InferenceEndpointId)
                .Query("endpointType", "string", "Only include telemetry events for this endpoint type.", false, "Completion")
                .Query("model", "string", "Only include telemetry events for this model name.", false, "qwen3:8b");
        }

        private static ApiDoc Limit(ApiDoc doc)
        {
            return doc.Query("limit", "integer", "Maximum rows to return (1 to 250, default 25).", false, 25);
        }

        #endregion

        #region Public-Members

        /// <summary>GET /v1.0/assistants/{assistantId}/analytics/overview.</summary>
        public static OpenApiRouteMetadata Overview => EventFilters(AnalyticsDoc(
                "Get assistant analytics overview",
                "Returns request volume, success and failure rates, latency percentiles, telemetry coverage, dominant stage, most-used endpoint, and feedback totals for the window. The event filters narrow the telemetry-derived fields only."))
            .Returns(200, "Analytics overview.", new AssistantAnalyticsOverviewResult
            {
                AssistantId = ApiExamples.AssistantId,
                Range = ExampleRange(),
                GeneratedUtc = _RangeEnd,
                RequestCount = 128,
                SuccessCount = 124,
                FailureCount = 4,
                SuccessRate = 0.9688,
                FailureRate = 0.0313,
                AverageDurationMs = 2140.37,
                P50DurationMs = 1820.5,
                P90DurationMs = 3610.2,
                P95DurationMs = 4205.8,
                P99DurationMs = 6120.44,
                MaxDurationMs = 7015.9,
                TelemetryEventCount = 912,
                RequestsWithTelemetry = 126,
                TelemetryCoverageRate = 0.9844,
                DominantStage = "final_inference",
                DominantStageAverageMs = 1604.12,
                TopEndpointId = _InferenceEndpointId,
                TopEndpointName = "Primary Completion",
                TopEndpointProvider = "Ollama",
                TopEndpointModel = "qwen3:8b",
                FeedbackCount = 20,
                ThumbsUpCount = 17,
                ThumbsDownCount = 3,
                NegativeFeedbackRate = 0.15
            })
            .Errors(400, 401, 403, 404, 500);

        /// <summary>GET /v1.0/assistants/{assistantId}/analytics/timeseries.</summary>
        public static OpenApiRouteMetadata TimeSeries => EventFilters(AnalyticsDoc(
                "Get assistant analytics time series",
                "Returns chart-ready series with one point per bucket. Available metrics: request_count, success_count, failure_count, success_rate, avg_duration_ms, p95_duration_ms, p99_duration_ms, max_duration_ms, endpoint_limiter_wait_avg_ms, endpoint_limiter_wait_p95_ms, endpoint_wait_calls, "
                    + "provider_load_avg_ms, provider_generation_avg_ms, provider_tokens_per_second_avg, input_tokens, output_tokens, total_tokens, retrieval_query_count_avg, chunks_output_avg, query_rewrite_calls, rerank_calls, and final_inference_calls. The example shows the first two buckets only.")
                .Query("metrics", "string", "Comma-separated metric names to return. All metrics are returned when omitted.", false, "request_count,avg_duration_ms"))
            .Returns(200, "Time series.", new AssistantAnalyticsTimeSeriesResult
            {
                AssistantId = ApiExamples.AssistantId,
                Range = ExampleRange(),
                GeneratedUtc = _RangeEnd,
                Series = new List<AssistantAnalyticsSeries>
                {
                    new AssistantAnalyticsSeries
                    {
                        Metric = "request_count",
                        Label = "Requests",
                        Unit = "count",
                        Points = new List<AssistantAnalyticsPoint>
                        {
                            new AssistantAnalyticsPoint { BucketStartUtc = _RangeStart, BucketEndUtc = _RangeStart.AddSeconds(900), Value = 3, SampleCount = 1, NullCount = 0 },
                            new AssistantAnalyticsPoint { BucketStartUtc = _RangeStart.AddSeconds(900), BucketEndUtc = _RangeStart.AddSeconds(1800), Value = 5, SampleCount = 1, NullCount = 0 }
                        }
                    },
                    new AssistantAnalyticsSeries
                    {
                        Metric = "avg_duration_ms",
                        Label = "Average latency",
                        Unit = "ms",
                        Points = new List<AssistantAnalyticsPoint>
                        {
                            new AssistantAnalyticsPoint { BucketStartUtc = _RangeStart, BucketEndUtc = _RangeStart.AddSeconds(900), Value = 1980.25, SampleCount = 1, NullCount = 0 },
                            new AssistantAnalyticsPoint { BucketStartUtc = _RangeStart.AddSeconds(900), BucketEndUtc = _RangeStart.AddSeconds(1800), Value = 2311.6, SampleCount = 1, NullCount = 0 }
                        }
                    }
                }
            })
            .Errors(400, 401, 403, 404, 500);

        /// <summary>GET /v1.0/assistants/{assistantId}/analytics/stages.</summary>
        public static OpenApiRouteMetadata Stages => EventFilters(AnalyticsDoc(
                "Get assistant stage analytics",
                "Returns per-bucket call counts, failures, skipped counts, and latency statistics for each pipeline stage and event kind that has telemetry in the window. Buckets without events are omitted."))
            .Returns(200, "Stage analytics.", new AssistantAnalyticsStageResult
            {
                AssistantId = ApiExamples.AssistantId,
                Range = ExampleRange(),
                GeneratedUtc = _RangeEnd,
                Buckets = new List<AssistantAnalyticsStageBucket>
                {
                    new AssistantAnalyticsStageBucket
                    {
                        BucketStartUtc = _RangeStart,
                        BucketEndUtc = _RangeStart.AddSeconds(900),
                        Stage = "retrieval",
                        Kind = "retrieval",
                        Calls = 3,
                        Failures = 0,
                        SkippedCount = 0,
                        AverageDurationMs = 42.17,
                        P95DurationMs = 55.3,
                        MaxDurationMs = 57.1
                    },
                    new AssistantAnalyticsStageBucket
                    {
                        BucketStartUtc = _RangeStart,
                        BucketEndUtc = _RangeStart.AddSeconds(900),
                        Stage = "final_inference",
                        Kind = "inference",
                        Calls = 3,
                        Failures = 0,
                        SkippedCount = 0,
                        AverageDurationMs = 1604.12,
                        P95DurationMs = 1890.4,
                        MaxDurationMs = 1921.0
                    }
                }
            })
            .Errors(400, 401, 403, 404, 500);

        /// <summary>GET /v1.0/assistants/{assistantId}/analytics/endpoints.</summary>
        public static OpenApiRouteMetadata Endpoints => Limit(EventFilters(AnalyticsDoc(
                "Get assistant endpoint analytics",
                "Returns usage and latency grouped by endpoint, provider, API format, model, and stage, ordered by call count.")))
            .Returns(200, "Endpoint analytics.", new AssistantAnalyticsEndpointResult
            {
                AssistantId = ApiExamples.AssistantId,
                Range = ExampleRange(),
                GeneratedUtc = _RangeEnd,
                Endpoints = new List<AssistantAnalyticsEndpointSummary>
                {
                    new AssistantAnalyticsEndpointSummary
                    {
                        EndpointId = _InferenceEndpointId,
                        EndpointName = "Primary Completion",
                        EndpointType = "Completion",
                        Provider = "Ollama",
                        ApiFormat = "Ollama",
                        Model = "qwen3:8b",
                        Stage = "final_inference",
                        Calls = 124,
                        Failures = 2,
                        AverageDurationMs = 1604.12,
                        P95DurationMs = 3350.8,
                        AverageLimiterWaitMs = 12.4,
                        P95LimiterWaitMs = 88.0,
                        AverageRequestToHeadersMs = 310.25,
                        AverageProviderLoadMs = 45.1,
                        AverageProviderGenerationMs = 1220.7,
                        AverageTokensPerSecond = 38.6,
                        InputTokens = 186240,
                        OutputTokens = 41210
                    }
                }
            })
            .Errors(400, 401, 403, 404, 500);

        /// <summary>GET /v1.0/assistants/{assistantId}/analytics/slowest.</summary>
        public static OpenApiRouteMetadata Slowest => Limit(EventFilters(AnalyticsDoc(
                "Get slowest assistant requests",
                "Returns the slowest requests in the window, ordered by duration, with the dominant stage, the endpoint used by that stage, and tool-call summary counts. When any event filter is supplied, only requests with matching telemetry events are considered.")))
            .Returns(200, "Slowest requests.", new AssistantAnalyticsSlowestResult
            {
                AssistantId = ApiExamples.AssistantId,
                Range = ExampleRange(),
                GeneratedUtc = _RangeEnd,
                Requests = new List<AssistantAnalyticsSlowRequest>
                {
                    new AssistantAnalyticsSlowRequest
                    {
                        RequestHistoryId = "req_01JH3ZB4E6F8G0H2J4K6M8N0P2",
                        ChatHistoryId = "chist_01JH3ZB5F7G9H1J3K5M7N9P1Q3",
                        TraceId = "trace_01JH3ZB6G8H0J2K4M6N8P0Q2R4",
                        CreatedUtc = ApiExamples.Updated,
                        StatusCode = 200,
                        Success = true,
                        DurationMs = 7015.9,
                        RequestPath = "/v1.0/assistants/" + ApiExamples.AssistantId + "/chat",
                        DominantStage = "final_inference",
                        DominantStageDurationMs = 5120.33,
                        EndpointId = _InferenceEndpointId,
                        EndpointName = "Primary Completion",
                        Provider = "Ollama",
                        Model = "qwen3:8b",
                        ToolCallCount = 2,
                        ToolFailureCount = 0,
                        ToolDeniedCount = 0,
                        ToolTruncatedCount = 0,
                        ToolDurationMs = 412.6,
                        SlowestToolName = "collection_search",
                        SlowestToolDurationMs = 301.2,
                        FailingToolNames = new List<string>()
                    }
                }
            })
            .Errors(400, 401, 403, 404, 500);

        /// <summary>GET /v1.0/assistants/{assistantId}/analytics/feedback.</summary>
        public static OpenApiRouteMetadata Feedback => AnalyticsDoc(
                "Get assistant feedback analytics",
                "Returns thumbs-up and thumbs-down totals, the negative feedback rate, and one bucket per interval across the window. The example shows the first two buckets only.")
            .Returns(200, "Feedback analytics.", new AssistantAnalyticsFeedbackResult
            {
                AssistantId = ApiExamples.AssistantId,
                Range = ExampleRange(),
                GeneratedUtc = _RangeEnd,
                TotalCount = 20,
                ThumbsUpCount = 17,
                ThumbsDownCount = 3,
                NegativeRate = 0.15,
                Buckets = new List<AssistantAnalyticsFeedbackBucket>
                {
                    new AssistantAnalyticsFeedbackBucket
                    {
                        BucketStartUtc = _RangeStart,
                        BucketEndUtc = _RangeStart.AddSeconds(900),
                        ThumbsUpCount = 1,
                        ThumbsDownCount = 0,
                        UnknownCount = 0,
                        TotalCount = 1,
                        NegativeRate = 0
                    },
                    new AssistantAnalyticsFeedbackBucket
                    {
                        BucketStartUtc = _RangeStart.AddSeconds(900),
                        BucketEndUtc = _RangeStart.AddSeconds(1800),
                        ThumbsUpCount = 0,
                        ThumbsDownCount = 0,
                        UnknownCount = 0,
                        TotalCount = 0,
                        NegativeRate = null
                    }
                }
            })
            .Errors(400, 401, 403, 404, 500);

        #endregion
    }
}
