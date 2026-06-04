// ============================================================================
// Enums
// ============================================================================

/** API error types returned by the server. */
export const ApiError = {
  AuthenticationFailed: "AuthenticationFailed",
  AuthorizationFailed: "AuthorizationFailed",
  BadRequest: "BadRequest",
  NotFound: "NotFound",
  Conflict: "Conflict",
  InternalError: "InternalError",
} as const;
export type ApiError = (typeof ApiError)[keyof typeof ApiError];

/** Document processing status. */
export const DocumentStatus = {
  Pending: "Pending",
  Uploading: "Uploading",
  Uploaded: "Uploaded",
  TypeDetecting: "TypeDetecting",
  TypeDetectionSuccess: "TypeDetectionSuccess",
  TypeDetectionFailed: "TypeDetectionFailed",
  Processing: "Processing",
  ProcessingChunks: "ProcessingChunks",
  Summarizing: "Summarizing",
  StoringEmbeddings: "StoringEmbeddings",
  Completed: "Completed",
  Failed: "Failed",
} as const;
export type DocumentStatus = (typeof DocumentStatus)[keyof typeof DocumentStatus];

/** Inference provider type. */
export const InferenceProvider = {
  OpenAI: "OpenAI",
  Ollama: "Ollama",
  Gemini: "Gemini",
} as const;
export type InferenceProvider = (typeof InferenceProvider)[keyof typeof InferenceProvider];

/** Crawl plan state. */
export const CrawlPlanState = {
  Stopped: "Stopped",
  Running: "Running",
} as const;
export type CrawlPlanState = (typeof CrawlPlanState)[keyof typeof CrawlPlanState];

/** Crawl operation state. */
export const CrawlOperationState = {
  NotStarted: "NotStarted",
  Starting: "Starting",
  Enumerating: "Enumerating",
  Retrieving: "Retrieving",
  Success: "Success",
  Failed: "Failed",
  Stopped: "Stopped",
  Canceled: "Canceled",
} as const;
export type CrawlOperationState = (typeof CrawlOperationState)[keyof typeof CrawlOperationState];

/** Evaluation run status. */
export const EvalStatus = {
  Pending: "Pending",
  Running: "Running",
  Completed: "Completed",
  Failed: "Failed",
} as const;
export type EvalStatus = (typeof EvalStatus)[keyof typeof EvalStatus];

/** Feedback rating. */
export const FeedbackRating = {
  ThumbsUp: "ThumbsUp",
  ThumbsDown: "ThumbsDown",
} as const;
export type FeedbackRating = (typeof FeedbackRating)[keyof typeof FeedbackRating];

/** Crawl schedule interval. */
export const ScheduleInterval = {
  OneTime: "OneTime",
  Minutes: "Minutes",
  Hours: "Hours",
  Days: "Days",
  Weeks: "Weeks",
} as const;
export type ScheduleInterval = (typeof ScheduleInterval)[keyof typeof ScheduleInterval];

/** Web authentication type. */
export const WebAuthType = {
  None: "None",
  Basic: "Basic",
  ApiKey: "ApiKey",
  BearerToken: "BearerToken",
} as const;
export type WebAuthType = (typeof WebAuthType)[keyof typeof WebAuthType];

/** Repository type for crawls. */
export const RepositoryType = {
  Web: "Web",
} as const;
export type RepositoryType = (typeof RepositoryType)[keyof typeof RepositoryType];

/** Summarization order. */
export const SummarizationOrder = {
  BottomUp: "BottomUp",
  TopDown: "TopDown",
} as const;
export type SummarizationOrder = (typeof SummarizationOrder)[keyof typeof SummarizationOrder];

/** Enumeration ordering. */
export const EnumerationOrder = {
  CreatedAscending: "CreatedAscending",
  CreatedDescending: "CreatedDescending",
} as const;
export type EnumerationOrder = (typeof EnumerationOrder)[keyof typeof EnumerationOrder];

/** API format string for endpoint configuration. */
export const ApiFormat = {
  OpenAI: "OpenAI",
  Gemini: "Gemini",
  Ollama: "Ollama",
} as const;
export type ApiFormat = (typeof ApiFormat)[keyof typeof ApiFormat];

// ============================================================================
// Common Types
// ============================================================================

/** Standard error response from the API. */
export interface ApiErrorResponse {
  Error: ApiError;
  Message: string;
  StatusCode: number;
  Context?: unknown;
  Description?: string;
}

/** Pagination parameters for list endpoints. */
export interface EnumerationQuery {
  maxResults?: number;
  continuationToken?: string;
  ordering?: EnumerationOrder;
  assistantId?: string;
  bucketName?: string;
  collectionId?: string;
  threadId?: string;
}

/** Paginated list response. */
export interface EnumerationResult<T> {
  Success: boolean;
  MaxResults: number;
  TotalRecords: number;
  RecordsRemaining: number;
  ContinuationToken: string | null;
  EndOfResults: boolean;
  Objects: T[];
  TotalMs: number;
}

// ============================================================================
// Authentication
// ============================================================================

/** Request body for authentication. */
export interface AuthenticateRequest {
  Email?: string;
  Password?: string;
  BearerToken?: string;
  TenantId?: string;
}

/** Authentication result. */
export interface AuthenticateResult {
  Success: boolean;
  User?: UserMaster;
  Credential?: Credential;
  ErrorMessage?: string;
  TenantId?: string;
  TenantName?: string;
  IsGlobalAdmin?: boolean;
  IsTenantAdmin?: boolean;
}

// ============================================================================
// Tenant
// ============================================================================

/** Tenant metadata. */
export interface TenantMetadata {
  Id?: string;
  Name?: string;
  Active?: boolean;
  IsProtected?: boolean;
  Labels?: string[];
  Tags?: Record<string, string>;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
}

// ============================================================================
// User
// ============================================================================

/** User record. */
export interface UserMaster {
  Id?: string;
  TenantId?: string;
  Email?: string;
  PasswordSha256?: string;
  FirstName?: string;
  LastName?: string;
  IsAdmin?: boolean;
  IsTenantAdmin?: boolean;
  Active?: boolean;
  IsProtected?: boolean;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
}

// ============================================================================
// Credential
// ============================================================================

/** API credential/token. */
export interface Credential {
  Id?: string;
  TenantId?: string;
  UserId?: string;
  Name?: string;
  BearerToken?: string;
  Active?: boolean;
  IsProtected?: boolean;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
}

// ============================================================================
// Assistant
// ============================================================================

/** Assistant resource. */
export interface Assistant {
  Id?: string;
  TenantId?: string;
  UserId?: string;
  Name?: string;
  Description?: string;
  Active?: boolean;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
}

/** Assistant settings/configuration. */
export interface AssistantSettings {
  Id?: string;
  AssistantId?: string;
  Temperature?: number;
  TopP?: number;
  SystemPrompt?: string;
  MaxTokens?: number;
  ContextWindow?: number;
  EnableRag?: boolean;
  EnableRetrievalGate?: boolean;
  EnableQueryRewrite?: boolean;
  QueryRewritePrompt?: string;
  EnableReranking?: boolean;
  RerankerTopK?: number;
  RerankerScoreThreshold?: number;
  RerankPrompt?: string;
  EnableCitations?: boolean;
  CitationLinkMode?: string;
  CollectionId?: string;
  RetrievalTopK?: number;
  RetrievalScoreThreshold?: number;
  SearchMode?: string;
  TextWeight?: number;
  FullTextSearchType?: string;
  FullTextLanguage?: string;
  FullTextNormalization?: number;
  FullTextMinimumScore?: number | null;
  RetrievalIncludeNeighbors?: number;
  InferenceEndpointId?: string;
  RetrievalGateInferenceEndpointId?: string;
  QueryRewriteInferenceEndpointId?: string;
  RerankInferenceEndpointId?: string;
  EmbeddingEndpointId?: string;
  Title?: string;
  LogoUrl?: string;
  FaviconUrl?: string;
  RetrievalLabelFilter?: string;
  RetrievalTagFilter?: string;
  EvalJudgePrompt?: string;
  Streaming?: boolean;
  EnableSlack?: boolean;
  SlackAppToken?: string;
  SlackBotToken?: string;
  SlackChannelId?: string;
  SlackMessagePrefix?: string;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
}

/** Public assistant info returned for unauthenticated requests. */
export interface AssistantPublicInfo {
  Id: string;
  Name: string;
  Description?: string;
  Title?: string;
  LogoUrl?: string;
  FaviconUrl?: string;
}

/** Slack verification request. */
export interface SlackVerificationRequest {
  EnableSlack?: boolean;
  SlackAppToken?: string;
  SlackBotToken?: string;
  SlackChannelId?: string;
  SlackMessagePrefix?: string;
}

/** Result of an individual Slack verification check. */
export interface SlackVerificationCheck {
  Success: boolean;
  Message?: string;
  Details?: unknown;
}

/** Slack verification response. */
export interface SlackVerificationResponse {
  Success: boolean;
  BotToken?: SlackVerificationCheck;
  Channel?: SlackVerificationCheck;
  SocketMode?: SlackVerificationCheck;
}

// ============================================================================
// Documents
// ============================================================================

/** Document upload request. */
export interface DocumentUploadRequest {
  IngestionRuleId?: string;
  Base64Content?: string;
  Name?: string;
  ContentType?: string;
}

/** Document resource. */
export interface AssistantDocument {
  Id?: string;
  TenantId?: string;
  Name?: string;
  OriginalFilename?: string;
  ContentType?: string;
  SizeBytes?: number;
  S3Key?: string;
  Status?: DocumentStatus;
  StatusMessage?: string;
  IngestionRuleId?: string;
  BucketName?: string;
  CollectionId?: string;
  Labels?: string;
  Tags?: string;
  ChunkRecordIds?: string;
  CrawlPlanId?: string;
  CrawlOperationId?: string;
  SourceUrl?: string;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
}

// ============================================================================
// Ingestion Rules
// ============================================================================

/** Ingestion rule configuration. */
export interface IngestionRule {
  Id?: string;
  TenantId?: string;
  Name?: string;
  Description?: string;
  Bucket?: string;
  CollectionName?: string;
  CollectionId?: string;
  Labels?: string[];
  Tags?: Record<string, string>;
  Atomization?: Record<string, unknown>;
  Summarization?: IngestionSummarizationConfig;
  Chunking?: IngestionChunkingConfig;
  Embedding?: IngestionEmbeddingConfig;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
}

/** Chunking configuration for ingestion. */
export interface IngestionChunkingConfig {
  Strategy?: string;
  FixedTokenCount?: number;
  OverlapCount?: number;
  OverlapPercentage?: number | null;
  OverlapStrategy?: string;
  RowGroupSize?: number;
  ContextPrefix?: string;
  RegexPattern?: string;
}

/** Embedding configuration for ingestion. */
export interface IngestionEmbeddingConfig {
  EmbeddingEndpointId?: string;
  L2Normalization?: boolean;
}

/** Summarization configuration for ingestion. */
export interface IngestionSummarizationConfig {
  CompletionEndpointId?: string;
  Order?: SummarizationOrder;
  SummarizationPrompt?: string;
  MaxSummaryTokens?: number;
  MinCellLength?: number;
  MaxParallelTasks?: number;
  MaxRetriesPerSummary?: number;
  MaxRetries?: number;
  TimeoutMs?: number;
}

// ============================================================================
// Chat / Conversation
// ============================================================================

/** Chat completion request (OpenAI-compatible). */
export interface ChatCompletionRequest {
  Model?: string;
  Messages: ChatCompletionMessage[];
  Stream?: boolean;
  Temperature?: number;
  TopP?: number;
  MaxTokens?: number;
  MetadataFilter?: ChatMetadataFilter;
}

/** A single chat message. */
export interface ChatCompletionMessage {
  role: string;
  content: string;
}

/** Chat completion response. */
export interface ChatCompletionResponse {
  id: string;
  object: string;
  created: number;
  model: string;
  choices: ChatCompletionChoice[];
  usage?: ChatCompletionUsage;
  status?: string;
  retrieval?: ChatCompletionRetrieval;
  citations?: ChatCompletionCitations;
}

/** A choice in a chat completion response. */
export interface ChatCompletionChoice {
  index: number;
  message?: ChatCompletionMessage;
  delta?: ChatCompletionMessage;
  finish_reason?: string;
}

/** Token usage statistics. */
export interface ChatCompletionUsage {
  prompt_tokens: number;
  completion_tokens: number;
  total_tokens: number;
  context_window?: number;
}

/** Retrieval metadata from RAG. */
export interface ChatCompletionRetrieval {
  collection_id?: string;
  duration_ms?: number;
  chunks_returned?: number;
  rerank_duration_ms?: number;
  rerank_input_count?: number;
  rerank_output_count?: number;
  chunks?: RetrievalChunk[];
}

/** Citation information. */
export interface ChatCompletionCitations {
  sources?: CitationSource[];
  referenced_indices?: number[];
  auto_populated?: boolean;
}

/** A citation source. */
export interface CitationSource {
  index: number;
  document_id: string;
  document_name?: string;
  content_type?: string;
  score?: number;
  fusion_score?: number;
  rerank_score?: number;
  excerpt?: string;
  download_url?: string;
}

/** A retrieved chunk from vector search. */
export interface RetrievalChunk {
  document_id?: string;
  score?: number;
  rerank_score?: number;
  fusion_score?: number;
  text_score?: number;
  content?: string;
  position?: number;
  neighbors?: RetrievalChunk[];
}

/** Streaming chunk from SSE. */
export interface ChatCompletionChunk {
  id: string;
  object: string;
  created: number;
  model: string;
  choices: ChatCompletionChoice[];
  usage?: ChatCompletionUsage;
  retrieval?: ChatCompletionRetrieval;
  citations?: ChatCompletionCitations;
}

/** Metadata filter for chat retrieval. */
export interface ChatMetadataFilter {
  RequiredLabels?: string[];
  ExcludedLabels?: string[];
  RequiredTags?: ChatTagCondition[];
  ExcludedTags?: ChatTagCondition[];
}

/** Tag condition for metadata filtering. */
export interface ChatTagCondition {
  Key: string;
  Condition: string;
  Value?: string;
}

/** Simple chat request for generate/compact endpoints. */
export interface ChatRequest {
  AssistantId?: string;
  Message?: string;
}

/** Response from the compaction endpoint. */
export interface CompactResponse {
  messages: ChatCompletionMessage[];
  usage: unknown;
}

/** Feedback submission request. */
export interface FeedbackRequest {
  AssistantId?: string;
  UserMessage?: string;
  AssistantResponse?: string;
  Rating?: FeedbackRating;
  FeedbackText?: string;
  MessageHistory?: string;
}

/** Feedback resource. */
export interface AssistantFeedback {
  Id?: string;
  TenantId?: string;
  AssistantId?: string;
  UserMessage?: string;
  AssistantResponse?: string;
  Rating?: FeedbackRating;
  FeedbackText?: string;
  MessageHistory?: string;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
}

/** Chat history entry. */
export interface ChatHistory {
  Id?: string;
  TraceId?: string | null;
  RequestHistoryId?: string | null;
  PerformanceSchemaVersion?: number;
  PerformanceJson?: string | null;
  TenantId?: string;
  ThreadId?: string;
  AssistantId?: string;
  CollectionId?: string;
  UserMessageUtc?: string;
  UserMessage?: string;
  RetrievalStartUtc?: string | null;
  RetrievalDurationMs?: number;
  RetrievalGateDecision?: string;
  RetrievalGateDurationMs?: number;
  QueryRewriteResult?: string;
  QueryRewriteDurationMs?: number;
  RerankDurationMs?: number;
  RerankInputCount?: number;
  RerankOutputCount?: number;
  RetrievalContext?: string;
  PromptSentUtc?: string | null;
  PromptTokens?: number;
  EndpointResolutionDurationMs?: number;
  CompactionDurationMs?: number;
  InferenceConnectionDurationMs?: number;
  TimeToFirstTokenMs?: number;
  TimeToLastTokenMs?: number;
  CompletionTokens?: number;
  TokensPerSecondOverall?: number;
  TokensPerSecondGeneration?: number;
  MetadataFilter?: string;
  Origin?: string;
  AssistantResponse?: string;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
}

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

/** Summary of a conversation thread. */
export interface ThreadSummary {
  ThreadId: string;
  AssistantId?: string;
  FirstMessageUtc?: string;
  LastMessageUtc?: string;
  TurnCount: number;
}

// ============================================================================
// Request History
// ============================================================================

/** Query parameters for request-history search and summary endpoints. */
export interface RequestHistorySearchFilter {
  maxResults?: number;
  continuationToken?: string;
  ordering?: EnumerationOrder;
  requestType?: string;
  httpMethod?: string;
  pathContains?: string;
  statusCode?: number;
  success?: boolean;
  tenantId?: string;
  userId?: string;
  credentialId?: string;
  assistantId?: string;
  threadId?: string;
  sourceType?: string;
  searchText?: string;
  startUtc?: string;
  endUtc?: string;
  bucketSeconds?: number;
}

/** Captured HTTP request/response entry. */
export interface RequestHistoryEntry {
  Id?: string;
  TraceId?: string | null;
  ChatHistoryId?: string | null;
  TenantId?: string;
  UserId?: string;
  CredentialId?: string;
  AssistantId?: string;
  ThreadId?: string;
  PrincipalName?: string;
  RequestType?: string;
  SourceType?: string;
  HttpMethod?: string;
  RouteTemplate?: string;
  RequestPath?: string;
  RequestUrl?: string;
  SourceIp?: string;
  StatusCode?: number;
  Success?: boolean;
  DurationMs?: number;
  RequestContentType?: string;
  ResponseContentType?: string;
  RequestSizeBytes?: number;
  ResponseSizeBytes?: number;
  RequestBodyTruncated?: boolean;
  ResponseBodyTruncated?: boolean;
  RequestBodyIsBinary?: boolean;
  ResponseBodyIsBinary?: boolean;
  RouteParameters?: Record<string, string>;
  QueryParameters?: Record<string, string>;
  RequestHeaders?: Record<string, string>;
  ResponseHeaders?: Record<string, string>;
  RequestBody?: string;
  ResponseBody?: string;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
}

/** Aggregated request-history summary bucket. */
export interface RequestHistorySummaryBucket {
  BucketStartUtc?: string;
  BucketEndUtc?: string;
  RequestCount?: number;
  SuccessCount?: number;
  FailureCount?: number;
  AverageDurationMs?: number;
}

/** Request-history summary response. */
export interface RequestHistorySummaryResult {
  TotalCount?: number;
  TotalSuccess?: number;
  TotalFailure?: number;
  AverageDurationMs?: number;
  Buckets?: RequestHistorySummaryBucket[];
}

/** Bulk request-history deletion response. */
export interface RequestHistoryDeleteResult {
  DeletedCount?: number;
}

// ============================================================================
// Endpoint Configuration (Partio)
// ============================================================================

/** Request to create an endpoint configuration. */
export interface PartioEndpointRequest {
  TenantId?: string;
  Name?: string;
  Model?: string;
  Endpoint?: string;
  ApiFormat?: ApiFormat;
  ApiKey?: string;
  Active?: boolean;
  MaxConcurrentRequests?: number;
  EnableRequestHistory?: boolean;
  Labels?: string[];
  Tags?: Record<string, string>;
  HealthCheckEnabled?: boolean;
  HealthCheckUrl?: string;
  HealthCheckMethod?: string;
  HealthCheckIntervalMs?: number;
  HealthCheckTimeoutMs?: number;
  HealthCheckExpectedStatusCode?: number;
  HealthyThreshold?: number;
  UnhealthyThreshold?: number;
  HealthCheckUseAuth?: boolean;
}

/** Endpoint configuration returned from the server. */
export interface PartioEndpointConfig {
  Id?: string;
  Name?: string;
  Model?: string;
  TenantId?: string;
  Endpoint?: string;
  ApiFormat?: string;
  ApiKey?: string;
  Active?: boolean;
  MaxConcurrentRequests?: number;
  HealthCheckEnabled?: boolean;
  HealthCheckUrl?: string;
  HealthCheckMethod?: string;
  HealthCheckIntervalMs?: number;
  HealthCheckTimeoutMs?: number;
  HealthCheckExpectedStatusCode?: number;
  HealthyThreshold?: number;
  UnhealthyThreshold?: number;
  HealthCheckUseAuth?: boolean;
}

/** Endpoint health status. */
export interface EndpointHealthStatus {
  EndpointId?: string;
  EndpointName?: string;
  Healthy?: boolean;
  LastCheckUtc?: string;
  LastSuccessUtc?: string;
  LastFailureUtc?: string;
  ConsecutiveSuccesses?: number;
  ConsecutiveFailures?: number;
}

/** Request to test an embedding endpoint. */
export interface EndpointExplorerEmbeddingRequest {
  EndpointId?: string;
  Input?: string;
  L2Normalization?: boolean;
}

/** Response from testing an embedding endpoint. */
export interface EndpointExplorerEmbeddingResponse {
  Success: boolean;
  StatusCode?: number;
  Error?: string;
  EndpointId?: string;
  Model?: string;
  Input?: string;
  Embedding?: number[];
  Dimensions?: number;
  ResponseTimeMs?: number;
  RequestHistoryId?: string;
  EmbeddingCalls?: unknown[];
}

/** Request to test a completion endpoint. */
export interface EndpointExplorerCompletionRequest {
  EndpointId?: string;
  Prompt?: string;
  SystemPrompt?: string;
  MaxTokens?: number;
  TimeoutMs?: number;
}

/** Response from testing a completion endpoint. */
export interface EndpointExplorerCompletionResponse {
  Success: boolean;
  StatusCode?: number;
  Error?: string;
  EndpointId?: string;
  Model?: string;
  Prompt?: string;
  SystemPrompt?: string;
  Output?: string;
  ResponseTimeMs?: number;
  RequestHistoryId?: string;
  CompletionCalls?: unknown[];
}

// ============================================================================
// Inference / Models
// ============================================================================

/** An available inference model. */
export interface InferenceModel {
  Name?: string;
  SizeBytes?: number;
  ModifiedUtc?: string;
  OwnedBy?: string;
  PullSupported?: boolean;
}

/** Request to pull a model. */
export interface PullModelRequest {
  Name: string;
}

/** Progress of a model pull operation. */
export interface PullProgress {
  IsComplete?: boolean;
  HasError?: boolean;
  Status?: string;
  Progress?: number;
}

// ============================================================================
// Collections (RecallDB proxy)
// ============================================================================

/** Collection metadata (proxied from RecallDB). */
export interface Collection {
  Id?: string;
  Name?: string;
  [key: string]: unknown;
}

/** A record within a collection. */
export interface CollectionRecord {
  Id?: string;
  [key: string]: unknown;
}

// ============================================================================
// Buckets (S3)
// ============================================================================

/** Bucket creation request. */
export interface BucketCreateRequest {
  Name: string;
}

/** Bucket summary. */
export interface BucketSummary {
  Name: string;
  CreationDate?: string;
}

/** Bucket list response. */
export interface BucketListResponse {
  Objects: BucketSummary[];
  TotalRecords: number;
}

/** Object metadata in a bucket. */
export interface BucketObject {
  Key?: string;
  [key: string]: unknown;
}

/** Bucket object listing response. */
export interface BucketObjectListResponse {
  Prefix?: string;
  Delimiter?: string;
  CommonPrefixes?: Array<{ Prefix: string }>;
  Objects: BucketObject[];
  TotalRecords: number;
}

/** Bucket object metadata response. */
export interface BucketObjectMetadata {
  Key: string;
  ContentLength?: number;
  ContentType?: string;
  LastModified?: string;
  ETag?: string;
  Metadata?: Record<string, string>;
}

// ============================================================================
// Crawl
// ============================================================================

/** Crawl plan configuration. */
export interface CrawlPlan {
  Id?: string;
  TenantId?: string;
  Name?: string;
  RepositoryType?: RepositoryType;
  IngestionSettings?: CrawlIngestionSettings;
  RepositorySettings?: CrawlRepositorySettings;
  Schedule?: CrawlScheduleSettings;
  Filter?: CrawlFilterSettings;
  ProcessAdditions?: boolean;
  ProcessUpdates?: boolean;
  ProcessDeletions?: boolean;
  MaxDrainTasks?: number;
  RetentionDays?: number;
  State?: CrawlPlanState;
  LastCrawlStartUtc?: string | null;
  LastCrawlFinishUtc?: string | null;
  LastCrawlSuccess?: boolean | null;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
}

/** Crawl ingestion settings. */
export interface CrawlIngestionSettings {
  IngestionRuleId?: string;
  StoreInS3?: boolean;
  S3BucketName?: string;
}

/** Crawl repository settings. */
export interface CrawlRepositorySettings {
  RepositoryType?: RepositoryType;
  AuthenticationType?: WebAuthType;
  Username?: string;
  Password?: string;
  ApiKeyHeader?: string;
  ApiKeyValue?: string;
  BearerToken?: string;
  UserAgent?: string;
  StartUrl?: string;
  UseHeadlessBrowser?: boolean;
  FollowLinks?: boolean;
  FollowRedirects?: boolean;
  ExtractSitemapLinks?: boolean;
  RestrictToChildUrls?: boolean;
  RestrictToSubdomain?: boolean;
  RestrictToRootDomain?: boolean;
  IgnoreRobotsTxt?: boolean;
  MaxDepth?: number;
  MaxParallelTasks?: number;
  CrawlDelayMs?: number;
}

/** Crawl schedule settings. */
export interface CrawlScheduleSettings {
  IntervalType?: ScheduleInterval;
  IntervalValue?: number;
}

/** Crawl filter settings. */
export interface CrawlFilterSettings {
  ObjectPrefix?: string;
  ObjectSuffix?: string;
  AllowedContentTypes?: string[];
  MinimumSize?: number;
  MaximumSize?: number | null;
}

/** Crawl operation record. */
export interface CrawlOperation {
  Id?: string;
  TenantId?: string;
  CrawlPlanId?: string;
  State?: CrawlOperationState;
  StatusMessage?: string;
  ObjectsEnumerated?: number;
  BytesEnumerated?: number;
  ObjectsAdded?: number;
  BytesAdded?: number;
  ObjectsUpdated?: number;
  BytesUpdated?: number;
  ObjectsDeleted?: number;
  BytesDeleted?: number;
  ObjectsSuccess?: number;
  BytesSuccess?: number;
  ObjectsFailed?: number;
  BytesFailed?: number;
  EnumerationFile?: string;
  StartUtc?: string | null;
  StartEnumerationUtc?: string | null;
  FinishEnumerationUtc?: string | null;
  StartRetrievalUtc?: string | null;
  FinishRetrievalUtc?: string | null;
  FinishUtc?: string | null;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
}

// ============================================================================
// Evaluation
// ============================================================================

/** Evaluation fact. */
export interface EvalFact {
  Id?: string;
  TenantId?: string;
  AssistantId?: string;
  Category?: string;
  Question?: string;
  ExpectedFacts?: string;
  CreatedUtc?: string;
  LastUpdateUtc?: string;
}

/** Evaluation run. */
export interface EvalRun {
  Id?: string;
  TenantId?: string;
  AssistantId?: string;
  Status?: EvalStatus;
  TotalFacts?: number;
  FactsEvaluated?: number;
  FactsPassed?: number;
  FactsFailed?: number;
  PassRate?: number;
  JudgePrompt?: string;
  StartedUtc?: string | null;
  CompletedUtc?: string | null;
  CreatedUtc?: string;
}

/** Request to start an evaluation run. */
export interface EvalRunRequest {
  AssistantId: string;
  JudgePrompt?: string;
}

/** Evaluation result. */
export interface EvalResult {
  Id?: string;
  RunId?: string;
  FactId?: string;
  Question?: string;
  ExpectedFacts?: string;
  LlmResponse?: string;
  FactVerdicts?: string;
  OverallPass?: boolean;
  DurationMs?: number;
  CreatedUtc?: string;
}

/** Individual fact verdict within an eval result. */
export interface FactVerdict {
  Fact: string;
  Pass: boolean;
  Reasoning?: string;
}

// ============================================================================
// Configuration
// ============================================================================

/** AssistantHub server configuration (partial -- includes common fields). */
export interface AssistantHubSettings {
  [key: string]: unknown;
}

// ============================================================================
// SDK Options
// ============================================================================

/** Options for constructing the AssistantHubClient. */
export interface AssistantHubClientOptions {
  /** Base URL of the AssistantHub server (e.g. "http://localhost:8000"). */
  baseUrl: string;
  /** Optional bearer token or API key for authentication. */
  apiKey?: string;
  /** Optional tenant ID to include in authentication. */
  tenantId?: string;
  /** Optional custom fetch implementation. */
  fetch?: typeof globalThis.fetch;
}
