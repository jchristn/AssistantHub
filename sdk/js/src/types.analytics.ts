/** Query parameters for assistant analytics endpoints. */
export interface AssistantAnalyticsQuery {
  range?: "lastHour" | "lastDay" | "lastWeek" | "lastMonth" | string;
  startUtc?: string;
  endUtc?: string;
  bucketSeconds?: number;
  metrics?: string | string[];
  stage?: string;
  endpointId?: string;
  endpointType?: string;
  model?: string;
  limit?: number;
}

/** Resolved assistant analytics range. */
export interface AssistantAnalyticsRange {
  RangeId?: string;
  StartUtc?: string;
  EndUtc?: string;
  BucketSeconds?: number;
  BucketCount?: number;
}

/** Assistant analytics point. */
export interface AssistantAnalyticsPoint {
  BucketStartUtc?: string;
  BucketEndUtc?: string;
  Value?: number | null;
  SampleCount?: number;
  NullCount?: number;
}

/** Assistant analytics series. */
export interface AssistantAnalyticsSeries {
  Metric?: string;
  Label?: string;
  Unit?: string;
  Points?: AssistantAnalyticsPoint[];
}

/** Assistant analytics overview. */
export interface AssistantAnalyticsOverviewResult {
  AssistantId?: string;
  Range?: AssistantAnalyticsRange;
  GeneratedUtc?: string;
  RequestCount?: number;
  SuccessCount?: number;
  FailureCount?: number;
  SuccessRate?: number | null;
  FailureRate?: number | null;
  AverageDurationMs?: number | null;
  P50DurationMs?: number | null;
  P90DurationMs?: number | null;
  P95DurationMs?: number | null;
  P99DurationMs?: number | null;
  MaxDurationMs?: number | null;
  TelemetryEventCount?: number;
  RequestsWithTelemetry?: number;
  TelemetryCoverageRate?: number | null;
  DominantStage?: string | null;
  DominantStageAverageMs?: number | null;
  TopEndpointId?: string | null;
  TopEndpointName?: string | null;
  TopEndpointProvider?: string | null;
  TopEndpointModel?: string | null;
  FeedbackCount?: number;
  ThumbsUpCount?: number;
  ThumbsDownCount?: number;
  NegativeFeedbackRate?: number | null;
}

/** Assistant analytics time-series result. */
export interface AssistantAnalyticsTimeSeriesResult {
  AssistantId?: string;
  Range?: AssistantAnalyticsRange;
  GeneratedUtc?: string;
  Series?: AssistantAnalyticsSeries[];
}

/** Assistant analytics stage bucket. */
export interface AssistantAnalyticsStageBucket {
  BucketStartUtc?: string;
  BucketEndUtc?: string;
  Stage?: string;
  Kind?: string;
  Calls?: number;
  Failures?: number;
  SkippedCount?: number;
  AverageDurationMs?: number | null;
  P95DurationMs?: number | null;
  MaxDurationMs?: number | null;
}

/** Assistant analytics stage result. */
export interface AssistantAnalyticsStageResult {
  AssistantId?: string;
  Range?: AssistantAnalyticsRange;
  GeneratedUtc?: string;
  Buckets?: AssistantAnalyticsStageBucket[];
}

/** Assistant analytics endpoint summary. */
export interface AssistantAnalyticsEndpointSummary {
  EndpointId?: string | null;
  EndpointName?: string | null;
  EndpointType?: string | null;
  Provider?: string | null;
  ApiFormat?: string | null;
  Model?: string | null;
  Stage?: string | null;
  Calls?: number;
  Failures?: number;
  AverageDurationMs?: number | null;
  P95DurationMs?: number | null;
  AverageLimiterWaitMs?: number | null;
  P95LimiterWaitMs?: number | null;
  AverageRequestToHeadersMs?: number | null;
  AverageProviderLoadMs?: number | null;
  AverageProviderGenerationMs?: number | null;
  AverageTokensPerSecond?: number | null;
  InputTokens?: number;
  OutputTokens?: number;
}

/** Assistant analytics endpoint result. */
export interface AssistantAnalyticsEndpointResult {
  AssistantId?: string;
  Range?: AssistantAnalyticsRange;
  GeneratedUtc?: string;
  Endpoints?: AssistantAnalyticsEndpointSummary[];
}

/** Assistant analytics slow request. */
export interface AssistantAnalyticsSlowRequest {
  RequestHistoryId?: string | null;
  ChatHistoryId?: string | null;
  TraceId?: string | null;
  CreatedUtc?: string;
  StatusCode?: number;
  Success?: boolean;
  DurationMs?: number;
  RequestPath?: string | null;
  DominantStage?: string | null;
  DominantStageDurationMs?: number | null;
  EndpointId?: string | null;
  EndpointName?: string | null;
  Provider?: string | null;
  Model?: string | null;
  ToolCallCount?: number;
  ToolFailureCount?: number;
  ToolDeniedCount?: number;
  ToolTruncatedCount?: number;
  ToolDurationMs?: number | null;
  SlowestToolName?: string | null;
  SlowestToolDurationMs?: number | null;
  FailingToolNames?: string[];
}

/** Assistant analytics slowest result. */
export interface AssistantAnalyticsSlowestResult {
  AssistantId?: string;
  Range?: AssistantAnalyticsRange;
  GeneratedUtc?: string;
  Requests?: AssistantAnalyticsSlowRequest[];
}

/** Assistant analytics feedback bucket. */
export interface AssistantAnalyticsFeedbackBucket {
  BucketStartUtc?: string;
  BucketEndUtc?: string;
  ThumbsUpCount?: number;
  ThumbsDownCount?: number;
  UnknownCount?: number;
  TotalCount?: number;
  NegativeRate?: number | null;
}

/** Assistant analytics feedback result. */
export interface AssistantAnalyticsFeedbackResult {
  AssistantId?: string;
  Range?: AssistantAnalyticsRange;
  GeneratedUtc?: string;
  TotalCount?: number;
  ThumbsUpCount?: number;
  ThumbsDownCount?: number;
  NegativeRate?: number | null;
  Buckets?: AssistantAnalyticsFeedbackBucket[];
}
