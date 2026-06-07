import type { CrawlOperationState, CrawlPlanState, EvalStatus, NfsVersion, RepositoryType, ScheduleInterval, WebAuthType } from './types';

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

/** Shared crawl repository settings. */
export interface CrawlRepositorySettingsBase {
  RepositoryType?: RepositoryType;
}

/** Web crawl repository settings. */
export interface WebCrawlRepositorySettings extends CrawlRepositorySettingsBase {
  RepositoryType?: 'Web';
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

/** CIFS crawl repository settings. */
export interface CifsCrawlRepositorySettings extends CrawlRepositorySettingsBase {
  RepositoryType?: 'CIFS';
  CifsHostname?: string;
  CifsUsername?: string;
  CifsPassword?: string;
  CifsShareName?: string;
  IncludeSubdirectories?: boolean;
}

/** NFS crawl repository settings. */
export interface NfsCrawlRepositorySettings extends CrawlRepositorySettingsBase {
  RepositoryType?: 'NFS';
  NfsHostname?: string;
  NfsUserId?: number | null;
  NfsGroupId?: number | null;
  NfsShareName?: string;
  NfsVersion?: NfsVersion;
  IncludeSubdirectories?: boolean;
}

/** Crawl repository settings. */
export type CrawlRepositorySettings =
  | WebCrawlRepositorySettings
  | CifsCrawlRepositorySettings
  | NfsCrawlRepositorySettings;

/** Crawl repository connectivity test result. */
export interface CrawlConnectivityResult {
  Success: boolean;
  Message?: string | null;
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
