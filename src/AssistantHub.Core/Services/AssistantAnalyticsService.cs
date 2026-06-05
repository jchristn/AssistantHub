namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Aggregates assistant request and performance telemetry into chart-ready analytics.
    /// </summary>
    public class AssistantAnalyticsService
    {
        private const int MaxBucketCount = 240;
        private readonly DatabaseDriverBase _Database;

        /// <summary>
        /// Instantiate the assistant analytics service.
        /// </summary>
        /// <param name="database">Database driver.</param>
        public AssistantAnalyticsService(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Resolve a filter to a concrete analytics range.
        /// </summary>
        /// <param name="filter">Analytics filter.</param>
        /// <returns>Resolved range.</returns>
        public AssistantAnalyticsRange ResolveRange(AssistantAnalyticsFilter filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            DateTime now = DateTime.UtcNow;
            string rangeId = NormalizeRangeId(filter.Range);
            DateTime startUtc;
            DateTime endUtc;
            int bucketSeconds;

            bool hasExplicitStart = filter.StartUtc.HasValue;
            bool hasExplicitEnd = filter.EndUtc.HasValue;

            if (hasExplicitStart || hasExplicitEnd)
            {
                if (!hasExplicitStart || !hasExplicitEnd)
                    throw new ArgumentException("Both startUtc and endUtc are required for explicit analytics ranges.");

                startUtc = DateTime.SpecifyKind(filter.StartUtc.GetValueOrDefault(), DateTimeKind.Utc).ToUniversalTime();
                endUtc = DateTime.SpecifyKind(filter.EndUtc.GetValueOrDefault(), DateTimeKind.Utc).ToUniversalTime();
                rangeId = "custom";
                bucketSeconds = filter.BucketSeconds ?? GuessBucketSeconds(startUtc, endUtc);
            }
            else
            {
                if (String.IsNullOrWhiteSpace(rangeId))
                    rangeId = "lastDay";

                switch (rangeId)
                {
                    case "lastHour":
                        startUtc = now.AddHours(-1);
                        endUtc = now;
                        bucketSeconds = 60;
                        break;
                    case "lastDay":
                        startUtc = now.AddDays(-1);
                        endUtc = now;
                        bucketSeconds = 900;
                        break;
                    case "lastWeek":
                        startUtc = now.AddDays(-7);
                        endUtc = now;
                        bucketSeconds = 7200;
                        break;
                    case "lastMonth":
                        startUtc = now.AddDays(-30);
                        endUtc = now;
                        bucketSeconds = 86400;
                        break;
                    default:
                        throw new ArgumentException("Unsupported analytics range '" + filter.Range + "'.");
                }

                if (filter.BucketSeconds.HasValue)
                    bucketSeconds = filter.BucketSeconds.Value;
            }

            if (bucketSeconds < 1)
                throw new ArgumentException("bucketSeconds must be greater than zero.");

            if (endUtc <= startUtc)
                throw new ArgumentException("endUtc must be later than startUtc.");

            double totalSeconds = (endUtc - startUtc).TotalSeconds;
            int bucketCount = (int)Math.Ceiling(totalSeconds / bucketSeconds);
            if (bucketCount > MaxBucketCount)
            {
                bucketSeconds = (int)Math.Ceiling(totalSeconds / MaxBucketCount);
                bucketCount = (int)Math.Ceiling(totalSeconds / bucketSeconds);
            }

            return new AssistantAnalyticsRange
            {
                RangeId = rangeId,
                StartUtc = startUtc,
                EndUtc = endUtc,
                BucketSeconds = bucketSeconds,
                BucketCount = Math.Max(1, bucketCount)
            };
        }

        /// <summary>
        /// Get high-level analytics for an assistant.
        /// </summary>
        /// <param name="filter">Analytics filter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Overview result.</returns>
        public async Task<AssistantAnalyticsOverviewResult> GetOverviewAsync(AssistantAnalyticsFilter filter, CancellationToken token = default)
        {
            AssistantAnalyticsRange range = ResolveRange(filter);
            List<RequestAnalyticsRow> requests = await LoadRequestsAsync(filter, range, token).ConfigureAwait(false);
            List<ChatHistoryPerformanceEvent> events = await LoadEventsAsync(filter, range, token).ConfigureAwait(false);
            List<FeedbackAnalyticsRow> feedback = await LoadFeedbackAsync(filter, range, token).ConfigureAwait(false);

            List<double> durations = requests.Select(r => r.DurationMs).Where(v => v >= 0).ToList();
            AssistantAnalyticsOverviewResult ret = new AssistantAnalyticsOverviewResult
            {
                AssistantId = filter.AssistantId,
                Range = range,
                GeneratedUtc = DateTime.UtcNow,
                RequestCount = requests.Count,
                SuccessCount = requests.Count(r => r.Success),
                FailureCount = requests.Count(r => !r.Success),
                AverageDurationMs = Average(durations),
                P50DurationMs = Percentile(durations, 0.50),
                P90DurationMs = Percentile(durations, 0.90),
                P95DurationMs = Percentile(durations, 0.95),
                P99DurationMs = Percentile(durations, 0.99),
                MaxDurationMs = durations.Count > 0 ? Math.Round(durations.Max(), 2) : null,
                TelemetryEventCount = events.Count,
                FeedbackCount = feedback.Count,
                ThumbsUpCount = feedback.Count(f => IsThumbsUp(f.Rating)),
                ThumbsDownCount = feedback.Count(f => IsThumbsDown(f.Rating))
            };

            if (ret.RequestCount > 0)
            {
                ret.SuccessRate = RoundRatio(ret.SuccessCount, ret.RequestCount);
                ret.FailureRate = RoundRatio(ret.FailureCount, ret.RequestCount);
            }

            HashSet<string> telemetryRequestIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ChatHistoryPerformanceEvent evt in events)
            {
                if (!String.IsNullOrEmpty(evt.RequestHistoryId)) telemetryRequestIds.Add(evt.RequestHistoryId);
                else if (!String.IsNullOrEmpty(evt.ChatHistoryId)) telemetryRequestIds.Add("chat:" + evt.ChatHistoryId);
            }

            foreach (RequestAnalyticsRow request in requests)
            {
                if (!String.IsNullOrEmpty(request.Id) && telemetryRequestIds.Contains(request.Id))
                    ret.RequestsWithTelemetry++;
                else if (!String.IsNullOrEmpty(request.ChatHistoryId) && telemetryRequestIds.Contains("chat:" + request.ChatHistoryId))
                    ret.RequestsWithTelemetry++;
            }

            if (ret.RequestCount > 0)
                ret.TelemetryCoverageRate = RoundRatio(ret.RequestsWithTelemetry, ret.RequestCount);

            StageDurationAggregate dominant = events
                .Where(e => !String.IsNullOrEmpty(e.Stage) && e.DurationMs > 0)
                .GroupBy(e => e.Stage)
                .Select(g => new StageDurationAggregate
                {
                    Stage = g.Key,
                    TotalDuration = g.Sum(e => e.DurationMs),
                    AverageDuration = g.Average(e => e.DurationMs)
                })
                .OrderByDescending(g => g.TotalDuration)
                .FirstOrDefault();

            if (dominant != null)
            {
                ret.DominantStage = dominant.Stage;
                ret.DominantStageAverageMs = Math.Round(dominant.AverageDuration, 2);
            }

            EndpointUsageAggregate topEndpoint = events
                .Where(e => !String.IsNullOrEmpty(e.EndpointId) || !String.IsNullOrEmpty(e.Model) || !String.IsNullOrEmpty(e.Provider))
                .GroupBy(BuildEndpointKey)
                .Select(g => new EndpointUsageAggregate { Event = g.First(), Calls = g.Count() })
                .OrderByDescending(g => g.Calls)
                .FirstOrDefault();

            if (topEndpoint != null)
            {
                ret.TopEndpointId = topEndpoint.Event.EndpointId;
                ret.TopEndpointName = topEndpoint.Event.EndpointName;
                ret.TopEndpointProvider = topEndpoint.Event.Provider;
                ret.TopEndpointModel = topEndpoint.Event.Model;
            }

            if (ret.FeedbackCount > 0)
                ret.NegativeFeedbackRate = RoundRatio(ret.ThumbsDownCount, ret.FeedbackCount);

            return ret;
        }

        /// <summary>
        /// Get chart-ready time series for an assistant.
        /// </summary>
        /// <param name="filter">Analytics filter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Time-series result.</returns>
        public async Task<AssistantAnalyticsTimeSeriesResult> GetTimeSeriesAsync(AssistantAnalyticsFilter filter, CancellationToken token = default)
        {
            AssistantAnalyticsRange range = ResolveRange(filter);
            List<RequestAnalyticsRow> requests = await LoadRequestsAsync(filter, range, token).ConfigureAwait(false);
            List<ChatHistoryPerformanceEvent> events = await LoadEventsAsync(filter, range, token).ConfigureAwait(false);

            List<RequestAnalyticsRow>[] requestBuckets = Bucketize(requests, range, r => r.CreatedUtc);
            List<ChatHistoryPerformanceEvent>[] eventBuckets = Bucketize(events, range, e => e.CreatedUtc);
            HashSet<string> requestedMetrics = BuildMetricSet(filter);

            AssistantAnalyticsTimeSeriesResult ret = new AssistantAnalyticsTimeSeriesResult
            {
                AssistantId = filter.AssistantId,
                Range = range,
                GeneratedUtc = DateTime.UtcNow
            };

            AddSeries(ret, requestedMetrics, "request_count", "Requests", "count", range, requestBuckets, b => CountMetric(b.Count));
            AddSeries(ret, requestedMetrics, "success_count", "Succeeded", "count", range, requestBuckets, b => CountMetric(b.Count(r => r.Success)));
            AddSeries(ret, requestedMetrics, "failure_count", "Failed", "count", range, requestBuckets, b => CountMetric(b.Count(r => !r.Success)));
            AddSeries(ret, requestedMetrics, "success_rate", "Success rate", "ratio", range, requestBuckets, b => RatioMetric(b.Count(r => r.Success), b.Count));
            AddSeries(ret, requestedMetrics, "avg_duration_ms", "Average latency", "ms", range, requestBuckets, b => NumericMetric(Average(b.Select(r => r.DurationMs))));
            AddSeries(ret, requestedMetrics, "p95_duration_ms", "P95 latency", "ms", range, requestBuckets, b => NumericMetric(Percentile(b.Select(r => r.DurationMs), 0.95)));
            AddSeries(ret, requestedMetrics, "p99_duration_ms", "P99 latency", "ms", range, requestBuckets, b => NumericMetric(Percentile(b.Select(r => r.DurationMs), 0.99)));
            AddSeries(ret, requestedMetrics, "max_duration_ms", "Max latency", "ms", range, requestBuckets, b => NumericMetric(Max(b.Select(r => r.DurationMs))));
            AddSeries(ret, requestedMetrics, "endpoint_limiter_wait_avg_ms", "Avg limiter wait", "ms", range, eventBuckets, b => NullableAverageMetric(b.Select(e => e.EndpointLimiterWaitMs)));
            AddSeries(ret, requestedMetrics, "endpoint_limiter_wait_p95_ms", "P95 limiter wait", "ms", range, eventBuckets, b => NullablePercentileMetric(b.Select(e => e.EndpointLimiterWaitMs), 0.95));
            AddSeries(ret, requestedMetrics, "endpoint_wait_calls", "Calls waiting for limiter", "count", range, eventBuckets, b => CountMetric(b.Count(e => e.EndpointLimiterWaitMs.HasValue && e.EndpointLimiterWaitMs.Value > 0)));
            AddSeries(ret, requestedMetrics, "provider_load_avg_ms", "Avg provider load", "ms", range, eventBuckets, b => NullableAverageMetric(b.Select(e => e.ProviderLoadMs)));
            AddSeries(ret, requestedMetrics, "provider_generation_avg_ms", "Avg provider generation", "ms", range, eventBuckets, b => NullableAverageMetric(b.Select(e => e.ProviderGenerationMs)));
            AddSeries(ret, requestedMetrics, "provider_tokens_per_second_avg", "Avg provider throughput", "tokens/s", range, eventBuckets, b => NullableAverageMetric(b.Select(e => e.ProviderTokensPerSecond)));
            AddSeries(ret, requestedMetrics, "input_tokens", "Input tokens", "tokens", range, eventBuckets, b => CountMetric(b.Where(e => e.InputTokens.HasValue).Sum(e => e.InputTokens.GetValueOrDefault())));
            AddSeries(ret, requestedMetrics, "output_tokens", "Output tokens", "tokens", range, eventBuckets, b => CountMetric(b.Where(e => e.OutputTokens.HasValue).Sum(e => e.OutputTokens.GetValueOrDefault())));
            AddSeries(ret, requestedMetrics, "total_tokens", "Total tokens", "tokens", range, eventBuckets, b => CountMetric(b.Where(e => e.TotalTokens.HasValue).Sum(e => e.TotalTokens.GetValueOrDefault())));
            AddSeries(ret, requestedMetrics, "retrieval_query_count_avg", "Avg retrieval queries", "queries", range, eventBuckets, b => NullableAverageMetric(b.Select(e => e.RetrievalQueryCount.HasValue ? (double?)e.RetrievalQueryCount.Value : null)));
            AddSeries(ret, requestedMetrics, "chunks_output_avg", "Avg chunks returned", "chunks", range, eventBuckets, b => NullableAverageMetric(b.Select(e => e.ChunksOutput.HasValue ? (double?)e.ChunksOutput.Value : null)));
            AddSeries(ret, requestedMetrics, "query_rewrite_calls", "Query rewrite calls", "count", range, eventBuckets, b => CountMetric(b.Count(e => String.Equals(e.Stage, "query_rewrite", StringComparison.OrdinalIgnoreCase))));
            AddSeries(ret, requestedMetrics, "rerank_calls", "Rerank calls", "count", range, eventBuckets, b => CountMetric(b.Count(e => String.Equals(e.Stage, "rerank", StringComparison.OrdinalIgnoreCase))));
            AddSeries(ret, requestedMetrics, "final_inference_calls", "Final inference calls", "count", range, eventBuckets, b => CountMetric(b.Count(e => String.Equals(e.Stage, "final_inference", StringComparison.OrdinalIgnoreCase))));

            return ret;
        }

        /// <summary>
        /// Get stage-level analytics for an assistant.
        /// </summary>
        /// <param name="filter">Analytics filter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Stage analytics result.</returns>
        public async Task<AssistantAnalyticsStageResult> GetStagesAsync(AssistantAnalyticsFilter filter, CancellationToken token = default)
        {
            AssistantAnalyticsRange range = ResolveRange(filter);
            List<ChatHistoryPerformanceEvent> events = await LoadEventsAsync(filter, range, token).ConfigureAwait(false);
            List<ChatHistoryPerformanceEvent>[] eventBuckets = Bucketize(events, range, e => e.CreatedUtc);

            AssistantAnalyticsStageResult ret = new AssistantAnalyticsStageResult
            {
                AssistantId = filter.AssistantId,
                Range = range,
                GeneratedUtc = DateTime.UtcNow
            };

            for (int i = 0; i < range.BucketCount; i++)
            {
                DateTime bucketStart = range.StartUtc.AddSeconds(i * range.BucketSeconds);
                DateTime bucketEnd = bucketStart.AddSeconds(range.BucketSeconds);

                foreach (IGrouping<string, ChatHistoryPerformanceEvent> group in eventBuckets[i]
                    .Where(e => !String.IsNullOrEmpty(e.Stage))
                    .GroupBy(e => e.Stage + "\u001f" + (e.Kind ?? "")))
                {
                    ChatHistoryPerformanceEvent first = group.First();
                    List<double> durations = group.Where(e => e.DurationMs > 0).Select(e => e.DurationMs).ToList();
                    ret.Buckets.Add(new AssistantAnalyticsStageBucket
                    {
                        BucketStartUtc = bucketStart,
                        BucketEndUtc = bucketEnd,
                        Stage = first.Stage,
                        Kind = first.Kind,
                        Calls = group.Count(),
                        Failures = group.Count(e => !e.Success),
                        SkippedCount = group.Count(e => e.DurationMs <= 0),
                        AverageDurationMs = Average(durations),
                        P95DurationMs = Percentile(durations, 0.95),
                        MaxDurationMs = Max(durations)
                    });
                }
            }

            return ret;
        }

        /// <summary>
        /// Get endpoint/model/provider analytics for an assistant.
        /// </summary>
        /// <param name="filter">Analytics filter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Endpoint analytics result.</returns>
        public async Task<AssistantAnalyticsEndpointResult> GetEndpointsAsync(AssistantAnalyticsFilter filter, CancellationToken token = default)
        {
            AssistantAnalyticsRange range = ResolveRange(filter);
            List<ChatHistoryPerformanceEvent> events = await LoadEventsAsync(filter, range, token).ConfigureAwait(false);
            int limit = ClampLimit(filter.Limit);

            AssistantAnalyticsEndpointResult ret = new AssistantAnalyticsEndpointResult
            {
                AssistantId = filter.AssistantId,
                Range = range,
                GeneratedUtc = DateTime.UtcNow
            };

            foreach (IGrouping<string, ChatHistoryPerformanceEvent> group in events
                .Where(e => !String.IsNullOrEmpty(e.EndpointId) || !String.IsNullOrEmpty(e.EndpointName) || !String.IsNullOrEmpty(e.Provider) || !String.IsNullOrEmpty(e.Model))
                .GroupBy(BuildEndpointKey)
                .OrderByDescending(g => g.Count())
                .Take(limit))
            {
                ChatHistoryPerformanceEvent first = group.First();
                List<double> durations = group.Where(e => e.DurationMs > 0).Select(e => e.DurationMs).ToList();
                List<double?> limiterWaits = group.Select(e => e.EndpointLimiterWaitMs).ToList();

                ret.Endpoints.Add(new AssistantAnalyticsEndpointSummary
                {
                    EndpointId = first.EndpointId,
                    EndpointName = first.EndpointName,
                    EndpointType = first.EndpointType,
                    Provider = first.Provider,
                    ApiFormat = first.ApiFormat,
                    Model = first.Model,
                    Stage = first.Stage,
                    Calls = group.Count(),
                    Failures = group.Count(e => !e.Success),
                    AverageDurationMs = Average(durations),
                    P95DurationMs = Percentile(durations, 0.95),
                    AverageLimiterWaitMs = AverageNullable(limiterWaits),
                    P95LimiterWaitMs = PercentileNullable(limiterWaits, 0.95),
                    AverageRequestToHeadersMs = AverageNullable(group.Select(e => e.RequestToHeadersMs)),
                    AverageProviderLoadMs = AverageNullable(group.Select(e => e.ProviderLoadMs)),
                    AverageProviderGenerationMs = AverageNullable(group.Select(e => e.ProviderGenerationMs)),
                    AverageTokensPerSecond = AverageNullable(group.Select(e => e.ProviderTokensPerSecond)),
                    InputTokens = group.Where(e => e.InputTokens.HasValue).Sum(e => e.InputTokens.GetValueOrDefault()),
                    OutputTokens = group.Where(e => e.OutputTokens.HasValue).Sum(e => e.OutputTokens.GetValueOrDefault())
                });
            }

            return ret;
        }

        /// <summary>
        /// Get the slowest assistant requests.
        /// </summary>
        /// <param name="filter">Analytics filter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Slowest request result.</returns>
        public async Task<AssistantAnalyticsSlowestResult> GetSlowestAsync(AssistantAnalyticsFilter filter, CancellationToken token = default)
        {
            AssistantAnalyticsRange range = ResolveRange(filter);
            List<RequestAnalyticsRow> requests = await LoadRequestsAsync(filter, range, token).ConfigureAwait(false);
            List<ChatHistoryPerformanceEvent> events = await LoadEventsAsync(filter, range, token).ConfigureAwait(false);
            int limit = ClampLimit(filter.Limit);

            bool hasEventFilter = !String.IsNullOrEmpty(filter.Stage)
                || !String.IsNullOrEmpty(filter.EndpointId)
                || !String.IsNullOrEmpty(filter.EndpointType)
                || !String.IsNullOrEmpty(filter.Model);

            if (hasEventFilter)
            {
                HashSet<string> requestMatches = new HashSet<string>(events.Where(e => !String.IsNullOrEmpty(e.RequestHistoryId)).Select(e => e.RequestHistoryId), StringComparer.Ordinal);
                HashSet<string> chatMatches = new HashSet<string>(events.Where(e => !String.IsNullOrEmpty(e.ChatHistoryId)).Select(e => e.ChatHistoryId), StringComparer.Ordinal);
                requests = requests.Where(r =>
                    (!String.IsNullOrEmpty(r.Id) && requestMatches.Contains(r.Id))
                    || (!String.IsNullOrEmpty(r.ChatHistoryId) && chatMatches.Contains(r.ChatHistoryId))).ToList();
            }

            Dictionary<string, List<ChatHistoryPerformanceEvent>> eventsByRequest = events
                .Where(e => !String.IsNullOrEmpty(e.RequestHistoryId))
                .GroupBy(e => e.RequestHistoryId)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            Dictionary<string, List<ChatHistoryPerformanceEvent>> eventsByChat = events
                .Where(e => !String.IsNullOrEmpty(e.ChatHistoryId))
                .GroupBy(e => e.ChatHistoryId)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            AssistantAnalyticsSlowestResult ret = new AssistantAnalyticsSlowestResult
            {
                AssistantId = filter.AssistantId,
                Range = range,
                GeneratedUtc = DateTime.UtcNow
            };

            foreach (RequestAnalyticsRow request in requests.OrderByDescending(r => r.DurationMs).Take(limit))
            {
                List<ChatHistoryPerformanceEvent> requestEvents = new List<ChatHistoryPerformanceEvent>();
                if (!String.IsNullOrEmpty(request.Id) && eventsByRequest.TryGetValue(request.Id, out List<ChatHistoryPerformanceEvent> byRequest))
                    requestEvents.AddRange(byRequest);
                if (!String.IsNullOrEmpty(request.ChatHistoryId) && eventsByChat.TryGetValue(request.ChatHistoryId, out List<ChatHistoryPerformanceEvent> byChat))
                    requestEvents.AddRange(byChat);

                ChatHistoryPerformanceEvent dominant = requestEvents
                    .Where(e => e.DurationMs > 0)
                    .OrderByDescending(e => e.DurationMs)
                    .FirstOrDefault();

                ret.Requests.Add(new AssistantAnalyticsSlowRequest
                {
                    RequestHistoryId = request.Id,
                    ChatHistoryId = request.ChatHistoryId,
                    TraceId = request.TraceId,
                    CreatedUtc = request.CreatedUtc,
                    StatusCode = request.StatusCode,
                    Success = request.Success,
                    DurationMs = request.DurationMs,
                    RequestPath = request.RequestPath,
                    DominantStage = dominant?.Stage,
                    DominantStageDurationMs = dominant == null ? null : Math.Round(dominant.DurationMs, 2),
                    EndpointId = dominant?.EndpointId,
                    EndpointName = dominant?.EndpointName,
                    Provider = dominant?.Provider,
                    Model = dominant?.Model
                });
            }

            return ret;
        }

        /// <summary>
        /// Get assistant feedback analytics.
        /// </summary>
        /// <param name="filter">Analytics filter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Feedback analytics result.</returns>
        public async Task<AssistantAnalyticsFeedbackResult> GetFeedbackAsync(AssistantAnalyticsFilter filter, CancellationToken token = default)
        {
            AssistantAnalyticsRange range = ResolveRange(filter);
            List<FeedbackAnalyticsRow> feedback = await LoadFeedbackAsync(filter, range, token).ConfigureAwait(false);
            List<FeedbackAnalyticsRow>[] buckets = Bucketize(feedback, range, f => f.CreatedUtc);

            AssistantAnalyticsFeedbackResult ret = new AssistantAnalyticsFeedbackResult
            {
                AssistantId = filter.AssistantId,
                Range = range,
                GeneratedUtc = DateTime.UtcNow,
                TotalCount = feedback.Count,
                ThumbsUpCount = feedback.Count(f => IsThumbsUp(f.Rating)),
                ThumbsDownCount = feedback.Count(f => IsThumbsDown(f.Rating))
            };

            if (ret.TotalCount > 0)
                ret.NegativeRate = RoundRatio(ret.ThumbsDownCount, ret.TotalCount);

            for (int i = 0; i < range.BucketCount; i++)
            {
                DateTime bucketStart = range.StartUtc.AddSeconds(i * range.BucketSeconds);
                DateTime bucketEnd = bucketStart.AddSeconds(range.BucketSeconds);
                int thumbsUp = buckets[i].Count(f => IsThumbsUp(f.Rating));
                int thumbsDown = buckets[i].Count(f => IsThumbsDown(f.Rating));
                int total = buckets[i].Count;

                ret.Buckets.Add(new AssistantAnalyticsFeedbackBucket
                {
                    BucketStartUtc = bucketStart,
                    BucketEndUtc = bucketEnd,
                    ThumbsUpCount = thumbsUp,
                    ThumbsDownCount = thumbsDown,
                    UnknownCount = total - thumbsUp - thumbsDown,
                    TotalCount = total,
                    NegativeRate = total > 0 ? RoundRatio(thumbsDown, total) : null
                });
            }

            return ret;
        }

        private async Task<List<RequestAnalyticsRow>> LoadRequestsAsync(AssistantAnalyticsFilter filter, AssistantAnalyticsRange range, CancellationToken token)
        {
            string query =
                "SELECT " +
                "COALESCE(r.id, h.request_history_id) AS id, " +
                "COALESCE(r.trace_id, h.trace_id) AS trace_id, " +
                "h.id AS chat_history_id, " +
                "h.tenant_id AS tenant_id, " +
                "h.assistant_id AS assistant_id, " +
                "h.thread_id AS thread_id, " +
                "COALESCE(r.request_type, 'AssistantChat') AS request_type, " +
                "COALESCE(r.source_type, h.origin) AS source_type, " +
                "COALESCE(r.http_method, 'POST') AS http_method, " +
                "COALESCE(r.request_path, '') AS request_path, " +
                "COALESCE(r.status_code, 200) AS status_code, " +
                "CASE WHEN r.id IS NULL THEN 1 ELSE r.success END AS success, " +
                "COALESCE(r.duration_ms, " +
                "h.retrieval_duration_ms + h.retrieval_gate_duration_ms + h.query_rewrite_duration_ms + h.rerank_duration_ms + " +
                "h.endpoint_resolution_duration_ms + h.compaction_duration_ms + h.inference_connection_duration_ms + h.time_to_last_token_ms) AS duration_ms, " +
                "COALESCE(r.created_utc, h.created_utc) AS created_utc " +
                "FROM chat_history h " +
                "LEFT JOIN request_history r ON (r.id = h.request_history_id OR r.chat_history_id = h.id) " +
                "WHERE " + BuildChatHistoryWhere(filter, range, "h") + " " +
                "ORDER BY COALESCE(r.created_utc, h.created_utc) ASC;";

            DataTable data = await _Database.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            List<RequestAnalyticsRow> ret = new List<RequestAnalyticsRow>();
            if (data == null) return ret;

            foreach (DataRow row in data.Rows)
            {
                ret.Add(new RequestAnalyticsRow
                {
                    Id = DataTableHelper.GetStringValue(row, "id"),
                    TraceId = DataTableHelper.GetStringValue(row, "trace_id"),
                    ChatHistoryId = DataTableHelper.GetStringValue(row, "chat_history_id"),
                    TenantId = DataTableHelper.GetStringValue(row, "tenant_id"),
                    AssistantId = DataTableHelper.GetStringValue(row, "assistant_id"),
                    ThreadId = DataTableHelper.GetStringValue(row, "thread_id"),
                    RequestType = DataTableHelper.GetStringValue(row, "request_type"),
                    SourceType = DataTableHelper.GetStringValue(row, "source_type"),
                    HttpMethod = DataTableHelper.GetStringValue(row, "http_method"),
                    RequestPath = DataTableHelper.GetStringValue(row, "request_path"),
                    StatusCode = DataTableHelper.GetIntValue(row, "status_code"),
                    Success = DataTableHelper.GetBooleanValue(row, "success"),
                    DurationMs = DataTableHelper.GetDoubleValue(row, "duration_ms"),
                    CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc")
                });
            }

            return ret;
        }

        private async Task<List<ChatHistoryPerformanceEvent>> LoadEventsAsync(AssistantAnalyticsFilter filter, AssistantAnalyticsRange range, CancellationToken token)
        {
            string where = BuildChatHistoryWhere(filter, range, "h", "e");
            if (!String.IsNullOrEmpty(filter.Stage))
                where += " AND e.stage = " + _Database.FormatNullableString(filter.Stage);
            if (!String.IsNullOrEmpty(filter.EndpointId))
                where += " AND e.endpoint_id = " + _Database.FormatNullableString(filter.EndpointId);
            if (!String.IsNullOrEmpty(filter.EndpointType))
                where += " AND e.endpoint_type = " + _Database.FormatNullableString(filter.EndpointType);
            if (!String.IsNullOrEmpty(filter.Model))
                where += " AND e.model = " + _Database.FormatNullableString(filter.Model);

            string query =
                "SELECT e.* FROM chat_history_performance_events e " +
                "INNER JOIN chat_history h ON h.id = e.chat_history_id " +
                "WHERE " + where + " ORDER BY e.created_utc ASC, e.sequence_number ASC;";
            DataTable data = await _Database.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            List<ChatHistoryPerformanceEvent> ret = new List<ChatHistoryPerformanceEvent>();
            if (data == null) return ret;
            foreach (DataRow row in data.Rows) ret.Add(ChatHistoryPerformanceEvent.FromDataRow(row));
            return ret;
        }

        private async Task<List<FeedbackAnalyticsRow>> LoadFeedbackAsync(AssistantAnalyticsFilter filter, AssistantAnalyticsRange range, CancellationToken token)
        {
            string query =
                "SELECT f.id, f.tenant_id, f.assistant_id, f.rating, f.created_utc FROM assistant_feedback f WHERE "
                + BuildWhere(filter, range, "f")
                + " AND EXISTS (SELECT 1 FROM chat_history h WHERE "
                + "h.tenant_id = f.tenant_id AND h.assistant_id = f.assistant_id"
                + " AND h.created_utc >= " + _Database.FormatNullableString(_Database.FormatDateTime(range.StartUtc))
                + " AND h.created_utc < " + _Database.FormatNullableString(_Database.FormatDateTime(range.EndUtc))
                + ") ORDER BY f.created_utc ASC;";

            DataTable data = await _Database.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            List<FeedbackAnalyticsRow> ret = new List<FeedbackAnalyticsRow>();
            if (data == null) return ret;

            foreach (DataRow row in data.Rows)
            {
                ret.Add(new FeedbackAnalyticsRow
                {
                    Id = DataTableHelper.GetStringValue(row, "id"),
                    Rating = DataTableHelper.GetStringValue(row, "rating"),
                    CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc")
                });
            }

            return ret;
        }

        private string BuildWhere(AssistantAnalyticsFilter filter, AssistantAnalyticsRange range)
        {
            return BuildWhere(filter, range, String.Empty);
        }

        private string BuildWhere(AssistantAnalyticsFilter filter, AssistantAnalyticsRange range, string alias)
        {
            if (String.IsNullOrEmpty(filter.TenantId)) throw new ArgumentException("TenantId is required.");
            if (String.IsNullOrEmpty(filter.AssistantId)) throw new ArgumentException("AssistantId is required.");

            string prefix = String.IsNullOrEmpty(alias) ? String.Empty : alias + ".";

            return
                prefix + "tenant_id = " + _Database.FormatNullableString(filter.TenantId) +
                " AND " + prefix + "assistant_id = " + _Database.FormatNullableString(filter.AssistantId) +
                " AND " + prefix + "created_utc >= " + _Database.FormatNullableString(_Database.FormatDateTime(range.StartUtc)) +
                " AND " + prefix + "created_utc < " + _Database.FormatNullableString(_Database.FormatDateTime(range.EndUtc));
        }

        private string BuildChatHistoryWhere(AssistantAnalyticsFilter filter, AssistantAnalyticsRange range, string chatHistoryAlias)
        {
            return BuildChatHistoryWhere(filter, range, chatHistoryAlias, chatHistoryAlias);
        }

        private string BuildChatHistoryWhere(AssistantAnalyticsFilter filter, AssistantAnalyticsRange range, string chatHistoryAlias, string timeAlias)
        {
            if (String.IsNullOrEmpty(filter.TenantId)) throw new ArgumentException("TenantId is required.");
            if (String.IsNullOrEmpty(filter.AssistantId)) throw new ArgumentException("AssistantId is required.");

            string chatPrefix = String.IsNullOrEmpty(chatHistoryAlias) ? String.Empty : chatHistoryAlias + ".";
            string timePrefix = String.IsNullOrEmpty(timeAlias) ? String.Empty : timeAlias + ".";

            return
                chatPrefix + "tenant_id = " + _Database.FormatNullableString(filter.TenantId) +
                " AND " + chatPrefix + "assistant_id = " + _Database.FormatNullableString(filter.AssistantId) +
                " AND " + timePrefix + "created_utc >= " + _Database.FormatNullableString(_Database.FormatDateTime(range.StartUtc)) +
                " AND " + timePrefix + "created_utc < " + _Database.FormatNullableString(_Database.FormatDateTime(range.EndUtc));
        }

        private static string NormalizeRangeId(string rangeId)
        {
            if (String.IsNullOrWhiteSpace(rangeId)) return "lastDay";
            switch (rangeId.Trim().ToLowerInvariant())
            {
                case "lasthour": return "lastHour";
                case "lastday": return "lastDay";
                case "lastweek": return "lastWeek";
                case "lastmonth": return "lastMonth";
                case "custom": return "custom";
                default: return rangeId.Trim();
            }
        }

        private static int GuessBucketSeconds(DateTime startUtc, DateTime endUtc)
        {
            double totalSeconds = Math.Max(1, (endUtc - startUtc).TotalSeconds);
            return Math.Max(1, (int)Math.Ceiling(totalSeconds / 96));
        }

        private static int ClampLimit(int limit)
        {
            if (limit < 1) return 25;
            if (limit > 250) return 250;
            return limit;
        }

        private static HashSet<string> BuildMetricSet(AssistantAnalyticsFilter filter)
        {
            HashSet<string> ret = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (filter?.Metrics == null) return ret;
            foreach (string metric in filter.Metrics)
            {
                if (!String.IsNullOrWhiteSpace(metric))
                    ret.Add(metric.Trim());
            }

            return ret;
        }

        private static List<T>[] Bucketize<T>(List<T> rows, AssistantAnalyticsRange range, Func<T, DateTime> timestampSelector)
        {
            List<T>[] buckets = new List<T>[range.BucketCount];
            for (int i = 0; i < buckets.Length; i++) buckets[i] = new List<T>();

            if (rows == null) return buckets;
            foreach (T row in rows)
            {
                DateTime timestamp = timestampSelector(row);
                int index = GetBucketIndex(range, timestamp);
                if (index >= 0 && index < buckets.Length)
                    buckets[index].Add(row);
            }

            return buckets;
        }

        private static int GetBucketIndex(AssistantAnalyticsRange range, DateTime timestamp)
        {
            if (timestamp < range.StartUtc || timestamp >= range.EndUtc) return -1;
            return (int)Math.Floor((timestamp - range.StartUtc).TotalSeconds / range.BucketSeconds);
        }

        private static void AddSeries<T>(
            AssistantAnalyticsTimeSeriesResult result,
            HashSet<string> requestedMetrics,
            string metric,
            string label,
            string unit,
            AssistantAnalyticsRange range,
            List<T>[] buckets,
            Func<List<T>, MetricAggregate> selector)
        {
            if (requestedMetrics != null && requestedMetrics.Count > 0 && !requestedMetrics.Contains(metric))
                return;

            AssistantAnalyticsSeries series = new AssistantAnalyticsSeries
            {
                Metric = metric,
                Label = label,
                Unit = unit
            };

            for (int i = 0; i < range.BucketCount; i++)
            {
                DateTime bucketStart = range.StartUtc.AddSeconds(i * range.BucketSeconds);
                DateTime bucketEnd = bucketStart.AddSeconds(range.BucketSeconds);
                MetricAggregate aggregate = selector(buckets[i]) ?? new MetricAggregate();

                series.Points.Add(new AssistantAnalyticsPoint
                {
                    BucketStartUtc = bucketStart,
                    BucketEndUtc = bucketEnd,
                    Value = aggregate.Value,
                    SampleCount = aggregate.SampleCount,
                    NullCount = aggregate.NullCount
                });
            }

            result.Series.Add(series);
        }

        private static MetricAggregate CountMetric(double value)
        {
            return new MetricAggregate { Value = Math.Round(value, 2), SampleCount = 1 };
        }

        private static MetricAggregate RatioMetric(int numerator, int denominator)
        {
            return new MetricAggregate
            {
                Value = denominator > 0 ? RoundRatio(numerator, denominator) : null,
                SampleCount = denominator,
                NullCount = denominator > 0 ? 0 : 1
            };
        }

        private static MetricAggregate NumericMetric(double? value)
        {
            return new MetricAggregate
            {
                Value = value,
                SampleCount = value.HasValue ? 1 : 0,
                NullCount = value.HasValue ? 0 : 1
            };
        }

        private static MetricAggregate NullableAverageMetric(IEnumerable<double?> values)
        {
            List<double?> input = values?.ToList() ?? new List<double?>();
            List<double> nonNull = input.Where(v => v.HasValue).Select(v => v.GetValueOrDefault()).ToList();
            return new MetricAggregate
            {
                Value = Average(nonNull),
                SampleCount = nonNull.Count,
                NullCount = input.Count - nonNull.Count
            };
        }

        private static MetricAggregate NullablePercentileMetric(IEnumerable<double?> values, double percentile)
        {
            List<double?> input = values?.ToList() ?? new List<double?>();
            List<double> nonNull = input.Where(v => v.HasValue).Select(v => v.GetValueOrDefault()).ToList();
            return new MetricAggregate
            {
                Value = Percentile(nonNull, percentile),
                SampleCount = nonNull.Count,
                NullCount = input.Count - nonNull.Count
            };
        }

        private static double? Average(IEnumerable<double> values)
        {
            List<double> list = values?.Where(v => !Double.IsNaN(v) && !Double.IsInfinity(v)).ToList() ?? new List<double>();
            if (list.Count < 1) return null;
            return Math.Round(list.Average(), 2);
        }

        private static double? AverageNullable(IEnumerable<double?> values)
        {
            return Average(values?.Where(v => v.HasValue).Select(v => v.GetValueOrDefault()));
        }

        private static double? Max(IEnumerable<double> values)
        {
            List<double> list = values?.Where(v => !Double.IsNaN(v) && !Double.IsInfinity(v)).ToList() ?? new List<double>();
            if (list.Count < 1) return null;
            return Math.Round(list.Max(), 2);
        }

        private static double? Percentile(IEnumerable<double> values, double percentile)
        {
            List<double> sorted = values?.Where(v => !Double.IsNaN(v) && !Double.IsInfinity(v)).OrderBy(v => v).ToList() ?? new List<double>();
            if (sorted.Count < 1) return null;
            if (sorted.Count == 1) return Math.Round(sorted[0], 2);

            double rank = (sorted.Count - 1) * percentile;
            int lower = (int)Math.Floor(rank);
            int upper = (int)Math.Ceiling(rank);
            double fraction = rank - lower;
            double value = sorted[lower] + ((sorted[upper] - sorted[lower]) * fraction);
            return Math.Round(value, 2);
        }

        private static double? PercentileNullable(IEnumerable<double?> values, double percentile)
        {
            return Percentile(values?.Where(v => v.HasValue).Select(v => v.GetValueOrDefault()), percentile);
        }

        private static double RoundRatio(int numerator, int denominator)
        {
            if (denominator <= 0) return 0;
            return Math.Round((double)numerator / denominator, 4);
        }

        private static string BuildEndpointKey(ChatHistoryPerformanceEvent evt)
        {
            return String.Join("\u001f", new[]
            {
                evt.EndpointId ?? "",
                evt.EndpointName ?? "",
                evt.EndpointType ?? "",
                evt.Provider ?? "",
                evt.ApiFormat ?? "",
                evt.Model ?? "",
                evt.Stage ?? ""
            });
        }

        private static bool IsThumbsUp(string rating)
        {
            return String.Equals(rating, "ThumbsUp", StringComparison.OrdinalIgnoreCase)
                || String.Equals(rating, "Positive", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsThumbsDown(string rating)
        {
            return String.Equals(rating, "ThumbsDown", StringComparison.OrdinalIgnoreCase)
                || String.Equals(rating, "Negative", StringComparison.OrdinalIgnoreCase);
        }
    }
}
