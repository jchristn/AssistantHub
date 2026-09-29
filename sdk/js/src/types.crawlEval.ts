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
  /** Most redirects followed for one page (1 to 50, default 10). */
  MaxRedirects?: number;
  /** Additional origins (absolute http or https URLs) that receive the crawl's credentials. */
  CredentialOrigins?: string[];
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
  /** TCP port of the SMB server (default 445). */
  CifsPort?: number | null;
  /** Domain or workgroup of the user (optional). */
  CifsDomain?: string | null;
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
  /** TCP port of the NFS service (default 2049). */
  NfsPort?: number | null;
  /** TCP port of the MOUNT service (default: discovered through the portmapper). */
  NfsMountPort?: number | null;
  /** TCP port of the portmapper (default 111). */
  NfsPortmapperPort?: number | null;
  IncludeSubdirectories?: boolean;
}

/** Amazon S3 or S3-compatible (MinIO, Less3, Ceph, Wasabi, Cloudflare R2) crawl repository settings. */
export interface S3CrawlRepositorySettings extends CrawlRepositorySettingsBase {
  RepositoryType?: 'S3';
  /** Service URL of an S3-compatible store, e.g. http://minio.example.com:9000/. Omit for Amazon S3. */
  S3Endpoint?: string | null;
  /** Region (default us-east-1). Required by Amazon S3. */
  S3Region?: string;
  /** Bucket name only, e.g. company-docs (not a URL or s3:// path). */
  S3BucketName?: string;
  /** Access key ID. Leave both keys empty for a public bucket. */
  S3AccessKey?: string | null;
  /** Secret access key. Sensitive: stored with the crawl plan. */
  S3SecretKey?: string | null;
}

/** Azure Blob Storage crawl repository settings. */
export interface AzureBlobCrawlRepositorySettings extends CrawlRepositorySettingsBase {
  RepositoryType?: 'AzureBlob';
  /** Storage account name, e.g. contosodocs. */
  AzureAccountName?: string;
  /** Storage account access key. Sensitive: stored with the crawl plan. */
  AzureAccessKey?: string;
  /** Container name only, e.g. documents. */
  AzureContainer?: string;
  /** Blob service endpoint (default https://{account}.blob.core.windows.net/); set for Azurite or private endpoints. */
  AzureEndpoint?: string | null;
}

/** Google Cloud Storage crawl repository settings. */
export interface GoogleCloudCrawlRepositorySettings extends CrawlRepositorySettingsBase {
  RepositoryType?: 'GoogleCloud';
  /** Google Cloud project ID. */
  GcpProjectId?: string;
  /** Bucket name only, e.g. contoso-documents (not gs://...). */
  GcpBucketName?: string;
  /** Full service account key JSON (Storage Object Viewer). Sensitive: stored with the crawl plan. */
  GcpJsonCredentials?: string;
  /** Custom endpoint, e.g. for an emulator. Omit for the standard endpoint. */
  GcpEndpoint?: string | null;
}

/** Local disk crawl repository settings: a folder on the AssistantHub server, inside Crawl.AllowedLocalPaths. */
export interface LocalDiskCrawlRepositorySettings extends CrawlRepositorySettingsBase {
  RepositoryType?: 'LocalDisk';
  /** Absolute path on the server (inside the container when running in Docker), e.g. /app/crawl-sources/handbook. */
  DiskPath?: string;
  /** Include files in subfolders (default true). */
  IncludeSubdirectories?: boolean;
}

/** Git repository crawl settings (github.com, default branch). Use Filter.ObjectPrefix to crawl one folder. */
export interface GitCrawlRepositorySettings extends CrawlRepositorySettingsBase {
  RepositoryType?: 'Git';
  /** https://github.com/owner/repo, https://github.com/owner/repo.git or git@github.com:owner/repo.git. */
  GitRepositoryUrl?: string;
  /** Personal access token; required for private repositories. Sensitive: stored with the crawl plan. */
  GitAccessToken?: string | null;
}

/** Crawl repository settings. */
export type CrawlRepositorySettings =
  | WebCrawlRepositorySettings
  | CifsCrawlRepositorySettings
  | NfsCrawlRepositorySettings
  | S3CrawlRepositorySettings
  | AzureBlobCrawlRepositorySettings
  | GoogleCloudCrawlRepositorySettings
  | LocalDiskCrawlRepositorySettings
  | GitCrawlRepositorySettings;

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
  ExecutionMode?: string;
  CategoryFilterJson?: string;
  StartedUtc?: string | null;
  CompletedUtc?: string | null;
  CreatedUtc?: string;
}

/** Request to start an evaluation run. */
export interface EvalRunRequest {
  AssistantId: string;
  JudgePrompt?: string;
  ExecutionMode?: string;
  Categories?: string[];
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
  ChatHistoryId?: string;
  TraceId?: string;
  RetrievalJson?: string;
  CitationsJson?: string;
  ToolCallsJson?: string;
  QueryClass?: string;
  AnswerabilityDecision?: string;
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
