/** Versioned provider-agnostic performance telemetry for a chat turn. */
export interface AssistantPerformanceTelemetry {
  SchemaVersion?: number;
  TraceId?: string | null;
  ChatHistoryId?: string | null;
  RequestHistoryId?: string | null;
  WallTimeMs?: number;
  CreatedUtc?: string;
  Stages?: AssistantPerformanceStage[];
}

/** A measured stage in the assistant pipeline. */
export interface AssistantPerformanceStage {
  Name?: string;
  Kind?: string;
  Sequence?: number;
  EndpointId?: string | null;
  EndpointName?: string | null;
  EndpointType?: string | null;
  Provider?: string | null;
  ApiFormat?: string | null;
  Model?: string | null;
  StartedUtc?: string | null;
  FinishedUtc?: string | null;
  DurationMs?: number;
  Success?: boolean;
  HttpStatusCode?: number | null;
  ErrorType?: string | null;
  ErrorMessage?: string | null;
  ClientTimings?: AssistantPerformanceClientTimings | null;
  Tokens?: AssistantTokenUsageTelemetry | null;
  ProviderMetrics?: AssistantProviderMetrics | null;
  Metadata?: Record<string, unknown> | null;
  ProviderRaw?: Record<string, unknown> | null;
}

/** Client-observed timings for an upstream provider call. */
export interface AssistantPerformanceClientTimings {
  EndpointLimiterWaitMs?: number | null;
  RequestToHeadersMs?: number | null;
  HeadersToFirstTokenMs?: number | null;
  FirstTokenToLastTokenMs?: number | null;
  TotalMs?: number | null;
}

/** Normalized token counters. */
export interface AssistantTokenUsageTelemetry {
  Input?: number | null;
  Output?: number | null;
  Total?: number | null;
  PromptEvalCount?: number | null;
  EvalCount?: number | null;
}

/** Provider-native metrics normalized into common fields. */
export interface AssistantProviderMetrics {
  QueueMs?: number | null;
  LoadMs?: number | null;
  PromptEvalMs?: number | null;
  GenerationMs?: number | null;
  TotalMs?: number | null;
  TokensPerSecond?: number | null;
  RequestId?: string | null;
}
