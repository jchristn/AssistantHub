import type {
  AssistantHubClientOptions,
  EnumerationQuery,
  EnumerationResult,
  ApiErrorResponse,
  AuthenticateRequest,
  AuthenticateResult,
  TenantMetadata,
  UserMaster,
  Credential,
  Assistant,
  AssistantSettings,
  AssistantPublicInfo,
  SlackVerificationRequest,
  SlackVerificationResponse,
  AssistantDocument,
  DocumentUploadRequest,
  IngestionRule,
  ChatCompletionRequest,
  ChatCompletionResponse,
  ChatCompletionChunk,
  ChatRequest,
  FeedbackRequest,
  AssistantFeedback,
  ChatHistory,
  PartioEndpointRequest,
  PartioEndpointConfig,
  EndpointHealthStatus,
  EndpointExplorerEmbeddingRequest,
  EndpointExplorerEmbeddingResponse,
  EndpointExplorerCompletionRequest,
  EndpointExplorerCompletionResponse,
  InferenceModel,
  PullModelRequest,
  PullProgress,
  Collection,
  CollectionRecord,
  BucketCreateRequest,
  BucketObject,
  CrawlPlan,
  CrawlOperation,
  EvalFact,
  EvalRun,
  EvalRunRequest,
  EvalResult,
  AssistantHubSettings,
} from "./types.js";

/** Error thrown when the API returns a non-success status code. */
export class AssistantHubApiError extends Error {
  /** HTTP status code. */
  public readonly statusCode: number;
  /** Parsed error body, if available. */
  public readonly body: ApiErrorResponse | null;

  constructor(statusCode: number, body: ApiErrorResponse | null, message?: string) {
    super(message || body?.Message || `API request failed with status ${statusCode}`);
    this.name = "AssistantHubApiError";
    this.statusCode = statusCode;
    this.body = body;
  }
}

/**
 * Client for the AssistantHub REST API.
 *
 * Provides typed methods for every API endpoint including assistants, documents,
 * chat completions, collections, endpoints, evaluation, crawl plans, and more.
 *
 * @example
 * ```ts
 * const client = new AssistantHubClient({
 *   baseUrl: "http://localhost:8000",
 *   apiKey: "my-api-key",
 * });
 * const assistants = await client.listAssistants();
 * ```
 */
export class AssistantHubClient {
  private readonly _baseUrl: string;
  private readonly _apiKey: string | undefined;
  private readonly _tenantId: string | undefined;
  private readonly _fetch: typeof globalThis.fetch;

  constructor(options: AssistantHubClientOptions) {
    this._baseUrl = options.baseUrl.replace(/\/+$/, "");
    this._apiKey = options.apiKey;
    this._tenantId = options.tenantId;
    this._fetch = options.fetch || globalThis.fetch;
  }

  // --------------------------------------------------------------------------
  // Internal helpers
  // --------------------------------------------------------------------------

  private _headers(extra?: Record<string, string>): Record<string, string> {
    const headers: Record<string, string> = { "Content-Type": "application/json" };
    if (this._apiKey) {
      headers["Authorization"] = `Bearer ${this._apiKey}`;
    }
    if (extra) {
      Object.assign(headers, extra);
    }
    return headers;
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private _qs(params?: any): string {
    if (!params) return "";
    const parts: string[] = [];
    for (const [key, value] of Object.entries(params as Record<string, unknown>)) {
      if (value !== undefined && value !== null) {
        parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`);
      }
    }
    return parts.length ? `?${parts.join("&")}` : "";
  }

  private async _request<T>(method: string, path: string, body?: unknown, extraHeaders?: Record<string, string>): Promise<T> {
    const url = `${this._baseUrl}${path}`;
    const response = await this._fetch(url, {
      method,
      headers: this._headers(extraHeaders),
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    if (!response.ok) {
      let errorBody: ApiErrorResponse | null = null;
      try {
        errorBody = await response.json() as ApiErrorResponse;
      } catch {
        // ignore parse failure
      }
      throw new AssistantHubApiError(response.status, errorBody);
    }
    if (response.status === 204) return undefined as T;
    const text = await response.text();
    if (!text) return undefined as T;
    return JSON.parse(text) as T;
  }

  private async _requestRaw(method: string, path: string, body?: unknown, extraHeaders?: Record<string, string>): Promise<Response> {
    const url = `${this._baseUrl}${path}`;
    const response = await this._fetch(url, {
      method,
      headers: this._headers(extraHeaders),
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    if (!response.ok) {
      let errorBody: ApiErrorResponse | null = null;
      try {
        errorBody = await response.json() as ApiErrorResponse;
      } catch {
        // ignore parse failure
      }
      throw new AssistantHubApiError(response.status, errorBody);
    }
    return response;
  }

  private async _head(path: string): Promise<boolean> {
    const url = `${this._baseUrl}${path}`;
    const response = await this._fetch(url, {
      method: "HEAD",
      headers: this._headers(),
    });
    return response.ok;
  }

  // --------------------------------------------------------------------------
  // Health
  // --------------------------------------------------------------------------

  /** Check server health. Returns product info. */
  async health(): Promise<{ Product: string; Version: string; Timestamp: string }> {
    return this._request("GET", "/");
  }

  /** Check server health via HEAD request. Returns true if the server is reachable. */
  async healthHead(): Promise<boolean> {
    return this._head("/");
  }

  // --------------------------------------------------------------------------
  // Authentication
  // --------------------------------------------------------------------------

  /** Authenticate and obtain a bearer token. */
  async authenticate(request: AuthenticateRequest): Promise<AuthenticateResult> {
    return this._request("POST", "/v1.0/authenticate", request);
  }

  /** Get the authenticated user's identity information. */
  async whoami(): Promise<unknown> {
    return this._request("GET", "/v1.0/whoami");
  }

  // --------------------------------------------------------------------------
  // Tenants
  // --------------------------------------------------------------------------

  /** Create a new tenant (Global Admin). */
  async createTenant(tenant: TenantMetadata): Promise<{ Tenant: TenantMetadata; Provisioning: unknown }> {
    return this._request("PUT", "/v1.0/tenants", tenant);
  }

  /** List tenants. */
  async listTenants(query?: EnumerationQuery): Promise<EnumerationResult<TenantMetadata>> {
    return this._request("GET", `/v1.0/tenants${this._qs(query)}`);
  }

  /** Get a tenant by ID. */
  async getTenant(tenantId: string): Promise<TenantMetadata> {
    return this._request("GET", `/v1.0/tenants/${encodeURIComponent(tenantId)}`);
  }

  /** Update a tenant. */
  async updateTenant(tenantId: string, tenant: TenantMetadata): Promise<TenantMetadata> {
    return this._request("PUT", `/v1.0/tenants/${encodeURIComponent(tenantId)}`, tenant);
  }

  /** Delete a tenant. */
  async deleteTenant(tenantId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/tenants/${encodeURIComponent(tenantId)}`);
  }

  /** Check if a tenant exists. */
  async tenantExists(tenantId: string): Promise<boolean> {
    return this._head(`/v1.0/tenants/${encodeURIComponent(tenantId)}`);
  }

  // --------------------------------------------------------------------------
  // Users (tenant-scoped)
  // --------------------------------------------------------------------------

  /** Create a user within a tenant. */
  async createUser(tenantId: string, user: UserMaster): Promise<UserMaster> {
    return this._request("PUT", `/v1.0/tenants/${encodeURIComponent(tenantId)}/users`, user);
  }

  /** List users in a tenant. */
  async listUsers(tenantId: string, query?: EnumerationQuery): Promise<EnumerationResult<UserMaster>> {
    return this._request("GET", `/v1.0/tenants/${encodeURIComponent(tenantId)}/users${this._qs(query)}`);
  }

  /** Get a user by ID. */
  async getUser(tenantId: string, userId: string): Promise<UserMaster> {
    return this._request("GET", `/v1.0/tenants/${encodeURIComponent(tenantId)}/users/${encodeURIComponent(userId)}`);
  }

  /** Update a user. */
  async updateUser(tenantId: string, userId: string, user: UserMaster): Promise<UserMaster> {
    return this._request("PUT", `/v1.0/tenants/${encodeURIComponent(tenantId)}/users/${encodeURIComponent(userId)}`, user);
  }

  /** Delete a user. */
  async deleteUser(tenantId: string, userId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/tenants/${encodeURIComponent(tenantId)}/users/${encodeURIComponent(userId)}`);
  }

  /** Check if a user exists. */
  async userExists(tenantId: string, userId: string): Promise<boolean> {
    return this._head(`/v1.0/tenants/${encodeURIComponent(tenantId)}/users/${encodeURIComponent(userId)}`);
  }

  // --------------------------------------------------------------------------
  // Credentials (tenant-scoped)
  // --------------------------------------------------------------------------

  /** Create a credential within a tenant. */
  async createCredential(tenantId: string, credential: Credential): Promise<Credential> {
    return this._request("PUT", `/v1.0/tenants/${encodeURIComponent(tenantId)}/credentials`, credential);
  }

  /** List credentials in a tenant. */
  async listCredentials(tenantId: string, query?: EnumerationQuery): Promise<EnumerationResult<Credential>> {
    return this._request("GET", `/v1.0/tenants/${encodeURIComponent(tenantId)}/credentials${this._qs(query)}`);
  }

  /** Get a credential by ID. */
  async getCredential(tenantId: string, credentialId: string): Promise<Credential> {
    return this._request("GET", `/v1.0/tenants/${encodeURIComponent(tenantId)}/credentials/${encodeURIComponent(credentialId)}`);
  }

  /** Update a credential. */
  async updateCredential(tenantId: string, credentialId: string, credential: Credential): Promise<Credential> {
    return this._request("PUT", `/v1.0/tenants/${encodeURIComponent(tenantId)}/credentials/${encodeURIComponent(credentialId)}`, credential);
  }

  /** Delete a credential. */
  async deleteCredential(tenantId: string, credentialId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/tenants/${encodeURIComponent(tenantId)}/credentials/${encodeURIComponent(credentialId)}`);
  }

  /** Check if a credential exists. */
  async credentialExists(tenantId: string, credentialId: string): Promise<boolean> {
    return this._head(`/v1.0/tenants/${encodeURIComponent(tenantId)}/credentials/${encodeURIComponent(credentialId)}`);
  }

  // --------------------------------------------------------------------------
  // Assistants
  // --------------------------------------------------------------------------

  /** Create a new assistant. */
  async createAssistant(assistant: Assistant): Promise<Assistant> {
    return this._request("PUT", "/v1.0/assistants", assistant);
  }

  /** List assistants. */
  async listAssistants(query?: EnumerationQuery): Promise<EnumerationResult<Assistant>> {
    return this._request("GET", `/v1.0/assistants${this._qs(query)}`);
  }

  /** Get an assistant by ID. */
  async getAssistant(assistantId: string): Promise<Assistant> {
    return this._request("GET", `/v1.0/assistants/${encodeURIComponent(assistantId)}`);
  }

  /** Update an assistant. */
  async updateAssistant(assistantId: string, assistant: Assistant): Promise<Assistant> {
    return this._request("PUT", `/v1.0/assistants/${encodeURIComponent(assistantId)}`, assistant);
  }

  /** Delete an assistant. */
  async deleteAssistant(assistantId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/assistants/${encodeURIComponent(assistantId)}`);
  }

  /** Check if an assistant exists. */
  async assistantExists(assistantId: string): Promise<boolean> {
    return this._head(`/v1.0/assistants/${encodeURIComponent(assistantId)}`);
  }

  /** Get public info for an assistant (no auth required). */
  async getAssistantPublic(assistantId: string): Promise<AssistantPublicInfo> {
    return this._request("GET", `/v1.0/assistants/${encodeURIComponent(assistantId)}/public`);
  }

  // --------------------------------------------------------------------------
  // Assistant Settings
  // --------------------------------------------------------------------------

  /** Get settings for an assistant. */
  async getAssistantSettings(assistantId: string): Promise<AssistantSettings> {
    return this._request("GET", `/v1.0/assistants/${encodeURIComponent(assistantId)}/settings`);
  }

  /** Update settings for an assistant. */
  async updateAssistantSettings(assistantId: string, settings: AssistantSettings): Promise<AssistantSettings> {
    return this._request("PUT", `/v1.0/assistants/${encodeURIComponent(assistantId)}/settings`, settings);
  }

  /** Verify Slack settings for an assistant. */
  async verifySlack(assistantId: string, request: SlackVerificationRequest): Promise<SlackVerificationResponse> {
    return this._request("POST", `/v1.0/assistants/${encodeURIComponent(assistantId)}/settings/slack/verify`, request);
  }

  // --------------------------------------------------------------------------
  // Documents
  // --------------------------------------------------------------------------

  /** Upload a document. */
  async createDocument(document: DocumentUploadRequest): Promise<AssistantDocument> {
    return this._request("PUT", "/v1.0/documents", document);
  }

  /** List documents. */
  async listDocuments(query?: EnumerationQuery): Promise<EnumerationResult<AssistantDocument>> {
    return this._request("GET", `/v1.0/documents${this._qs(query)}`);
  }

  /** Get a document by ID. */
  async getDocument(documentId: string): Promise<AssistantDocument> {
    return this._request("GET", `/v1.0/documents/${encodeURIComponent(documentId)}`);
  }

  /** Delete a document. */
  async deleteDocument(documentId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/documents/${encodeURIComponent(documentId)}`);
  }

  /** Check if a document exists. */
  async documentExists(documentId: string): Promise<boolean> {
    return this._head(`/v1.0/documents/${encodeURIComponent(documentId)}`);
  }

  /** Bulk delete documents by IDs. */
  async bulkDeleteDocuments(documentIds: string[]): Promise<void> {
    return this._request("POST", "/v1.0/documents/delete", documentIds);
  }

  /** Get processing log for a document. */
  async getDocumentProcessingLog(documentId: string): Promise<unknown> {
    return this._request("GET", `/v1.0/documents/${encodeURIComponent(documentId)}/processing-log`);
  }

  /** Download a document file. Returns the raw Response for binary handling. */
  async downloadDocument(documentId: string): Promise<Response> {
    return this._requestRaw("GET", `/v1.0/documents/${encodeURIComponent(documentId)}/download`);
  }

  /** Download a document publicly (no auth required). */
  async downloadDocumentPublic(assistantId: string, documentId: string): Promise<Response> {
    return this._requestRaw("GET", `/v1.0/assistants/${encodeURIComponent(assistantId)}/documents/${encodeURIComponent(documentId)}/download`);
  }

  // --------------------------------------------------------------------------
  // Ingestion Rules
  // --------------------------------------------------------------------------

  /** Create an ingestion rule. */
  async createIngestionRule(rule: IngestionRule): Promise<IngestionRule> {
    return this._request("PUT", "/v1.0/ingestion-rules", rule);
  }

  /** List ingestion rules. */
  async listIngestionRules(query?: EnumerationQuery): Promise<EnumerationResult<IngestionRule>> {
    return this._request("GET", `/v1.0/ingestion-rules${this._qs(query)}`);
  }

  /** Get an ingestion rule by ID. */
  async getIngestionRule(ruleId: string): Promise<IngestionRule> {
    return this._request("GET", `/v1.0/ingestion-rules/${encodeURIComponent(ruleId)}`);
  }

  /** Update an ingestion rule. */
  async updateIngestionRule(ruleId: string, rule: IngestionRule): Promise<IngestionRule> {
    return this._request("PUT", `/v1.0/ingestion-rules/${encodeURIComponent(ruleId)}`, rule);
  }

  /** Delete an ingestion rule. */
  async deleteIngestionRule(ruleId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/ingestion-rules/${encodeURIComponent(ruleId)}`);
  }

  /** Check if an ingestion rule exists. */
  async ingestionRuleExists(ruleId: string): Promise<boolean> {
    return this._head(`/v1.0/ingestion-rules/${encodeURIComponent(ruleId)}`);
  }

  // --------------------------------------------------------------------------
  // Chat / Conversation
  // --------------------------------------------------------------------------

  /**
   * Send a chat completion request (non-streaming).
   * @param assistantId - The assistant ID.
   * @param request - The chat completion request. `Stream` should be false or omitted.
   * @param threadId - Optional thread ID for conversation context.
   */
  async chatCompletion(assistantId: string, request: ChatCompletionRequest, threadId?: string): Promise<ChatCompletionResponse> {
    const extraHeaders: Record<string, string> = {};
    if (threadId) {
      extraHeaders["X-ThreadId"] = threadId;
    }
    return this._request("POST", `/v1.0/assistants/${encodeURIComponent(assistantId)}/chat`, { ...request, Stream: false }, extraHeaders);
  }

  /**
   * Send a streaming chat completion request. Returns an async iterable of chunks.
   * @param assistantId - The assistant ID.
   * @param request - The chat completion request. `Stream` is set to true automatically.
   * @param threadId - Optional thread ID for conversation context.
   */
  async *chatCompletionStream(assistantId: string, request: ChatCompletionRequest, threadId?: string): AsyncGenerator<ChatCompletionChunk> {
    const extraHeaders: Record<string, string> = {};
    if (threadId) {
      extraHeaders["X-ThreadId"] = threadId;
    }
    const response = await this._requestRaw(
      "POST",
      `/v1.0/assistants/${encodeURIComponent(assistantId)}/chat`,
      { ...request, Stream: true },
      extraHeaders
    );
    yield* this._parseSSE<ChatCompletionChunk>(response);
  }

  /** Submit feedback for a chat interaction. */
  async submitFeedback(assistantId: string, request: FeedbackRequest): Promise<AssistantFeedback> {
    return this._request("POST", `/v1.0/assistants/${encodeURIComponent(assistantId)}/feedback`, request);
  }

  /** Compact/summarize a conversation. */
  async compact(assistantId: string, request: ChatRequest): Promise<unknown> {
    return this._request("POST", `/v1.0/assistants/${encodeURIComponent(assistantId)}/compact`, request);
  }

  /** Generate a completion with RAG. */
  async generate(assistantId: string, request: ChatRequest): Promise<unknown> {
    return this._request("POST", `/v1.0/assistants/${encodeURIComponent(assistantId)}/generate`, request);
  }

  // --------------------------------------------------------------------------
  // Threads
  // --------------------------------------------------------------------------

  /** Create a new conversation thread. */
  async createThread(assistantId: string, metadata?: Record<string, unknown>): Promise<{ ThreadId: string }> {
    return this._request("POST", `/v1.0/assistants/${encodeURIComponent(assistantId)}/threads`, metadata ? { Metadata: metadata } : {});
  }

  /** Get conversation history for a thread. */
  async getThreadHistory(assistantId: string, threadId: string): Promise<ChatHistory[]> {
    return this._request("GET", `/v1.0/assistants/${encodeURIComponent(assistantId)}/threads/${encodeURIComponent(threadId)}/history`);
  }

  /** List all threads. */
  async listThreads(query?: EnumerationQuery): Promise<EnumerationResult<ChatHistory>> {
    return this._request("GET", `/v1.0/threads${this._qs(query)}`);
  }

  // --------------------------------------------------------------------------
  // Distinct Labels / Tags
  // --------------------------------------------------------------------------

  /** Get distinct labels for an assistant's documents. */
  async getDistinctLabels(assistantId: string): Promise<string[]> {
    return this._request("GET", `/v1.0/assistants/${encodeURIComponent(assistantId)}/labels/distinct`);
  }

  /** Get distinct tags for an assistant's documents. */
  async getDistinctTags(assistantId: string): Promise<string[]> {
    return this._request("GET", `/v1.0/assistants/${encodeURIComponent(assistantId)}/tags/distinct`);
  }

  // --------------------------------------------------------------------------
  // Feedback Management
  // --------------------------------------------------------------------------

  /** List feedback entries. */
  async listFeedback(query?: EnumerationQuery): Promise<EnumerationResult<AssistantFeedback>> {
    return this._request("GET", `/v1.0/feedback${this._qs(query)}`);
  }

  /** Get a feedback entry by ID. */
  async getFeedback(feedbackId: string): Promise<AssistantFeedback> {
    return this._request("GET", `/v1.0/feedback/${encodeURIComponent(feedbackId)}`);
  }

  /** Delete a feedback entry. */
  async deleteFeedback(feedbackId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/feedback/${encodeURIComponent(feedbackId)}`);
  }

  // --------------------------------------------------------------------------
  // History Management
  // --------------------------------------------------------------------------

  /** List chat history entries. */
  async listHistory(query?: EnumerationQuery): Promise<EnumerationResult<ChatHistory>> {
    return this._request("GET", `/v1.0/history${this._qs(query)}`);
  }

  /** Get a chat history entry by ID. */
  async getHistory(historyId: string): Promise<ChatHistory> {
    return this._request("GET", `/v1.0/history/${encodeURIComponent(historyId)}`);
  }

  /** Delete a chat history entry. */
  async deleteHistory(historyId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/history/${encodeURIComponent(historyId)}`);
  }

  // --------------------------------------------------------------------------
  // Embedding Endpoints
  // --------------------------------------------------------------------------

  /** Create an embedding endpoint. */
  async createEmbeddingEndpoint(request: PartioEndpointRequest): Promise<PartioEndpointConfig> {
    return this._request("PUT", "/v1.0/endpoints/embedding", request);
  }

  /** List embedding endpoints. */
  async listEmbeddingEndpoints(query?: Record<string, unknown>): Promise<EnumerationResult<PartioEndpointConfig>> {
    return this._request("POST", "/v1.0/endpoints/embedding/enumerate", query || {});
  }

  /** Get an embedding endpoint by ID. */
  async getEmbeddingEndpoint(endpointId: string): Promise<PartioEndpointConfig> {
    return this._request("GET", `/v1.0/endpoints/embedding/${encodeURIComponent(endpointId)}`);
  }

  /** Update an embedding endpoint. */
  async updateEmbeddingEndpoint(endpointId: string, config: PartioEndpointConfig): Promise<PartioEndpointConfig> {
    return this._request("PUT", `/v1.0/endpoints/embedding/${encodeURIComponent(endpointId)}`, config);
  }

  /** Delete an embedding endpoint. */
  async deleteEmbeddingEndpoint(endpointId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/endpoints/embedding/${encodeURIComponent(endpointId)}`);
  }

  /** Check if an embedding endpoint exists. */
  async embeddingEndpointExists(endpointId: string): Promise<boolean> {
    return this._head(`/v1.0/endpoints/embedding/${encodeURIComponent(endpointId)}`);
  }

  /** Get health status for all embedding endpoints. */
  async getEmbeddingEndpointHealth(): Promise<EndpointHealthStatus[]> {
    return this._request("GET", "/v1.0/endpoints/embedding/health");
  }

  /** Get health status for a specific embedding endpoint. */
  async getEmbeddingEndpointHealthById(endpointId: string): Promise<EndpointHealthStatus> {
    return this._request("GET", `/v1.0/endpoints/embedding/${encodeURIComponent(endpointId)}/health`);
  }

  /** Test an embedding endpoint. */
  async testEmbeddingEndpoint(endpointId: string, request: EndpointExplorerEmbeddingRequest): Promise<EndpointExplorerEmbeddingResponse> {
    return this._request("POST", `/v1.0/endpoints/embedding/${encodeURIComponent(endpointId)}/test`, request);
  }

  // --------------------------------------------------------------------------
  // Completion Endpoints
  // --------------------------------------------------------------------------

  /** Create a completion endpoint. */
  async createCompletionEndpoint(request: PartioEndpointRequest): Promise<PartioEndpointConfig> {
    return this._request("PUT", "/v1.0/endpoints/completion", request);
  }

  /** List completion endpoints. */
  async listCompletionEndpoints(query?: Record<string, unknown>): Promise<EnumerationResult<PartioEndpointConfig>> {
    return this._request("POST", "/v1.0/endpoints/completion/enumerate", query || {});
  }

  /** Get a completion endpoint by ID. */
  async getCompletionEndpoint(endpointId: string): Promise<PartioEndpointConfig> {
    return this._request("GET", `/v1.0/endpoints/completion/${encodeURIComponent(endpointId)}`);
  }

  /** Update a completion endpoint. */
  async updateCompletionEndpoint(endpointId: string, config: PartioEndpointConfig): Promise<PartioEndpointConfig> {
    return this._request("PUT", `/v1.0/endpoints/completion/${encodeURIComponent(endpointId)}`, config);
  }

  /** Delete a completion endpoint. */
  async deleteCompletionEndpoint(endpointId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/endpoints/completion/${encodeURIComponent(endpointId)}`);
  }

  /** Check if a completion endpoint exists. */
  async completionEndpointExists(endpointId: string): Promise<boolean> {
    return this._head(`/v1.0/endpoints/completion/${encodeURIComponent(endpointId)}`);
  }

  /** Get health status for all completion endpoints. */
  async getCompletionEndpointHealth(): Promise<EndpointHealthStatus[]> {
    return this._request("GET", "/v1.0/endpoints/completion/health");
  }

  /** Get health status for a specific completion endpoint. */
  async getCompletionEndpointHealthById(endpointId: string): Promise<EndpointHealthStatus> {
    return this._request("GET", `/v1.0/endpoints/completion/${encodeURIComponent(endpointId)}/health`);
  }

  /** Test a completion endpoint. */
  async testCompletionEndpoint(endpointId: string, request: EndpointExplorerCompletionRequest): Promise<EndpointExplorerCompletionResponse> {
    return this._request("POST", `/v1.0/endpoints/completion/${encodeURIComponent(endpointId)}/test`, request);
  }

  // --------------------------------------------------------------------------
  // Inference / Models
  // --------------------------------------------------------------------------

  /** List available models. Optionally filter by assistant ID. */
  async listModels(assistantId?: string): Promise<InferenceModel[]> {
    const qs = assistantId ? `?assistantId=${encodeURIComponent(assistantId)}` : "";
    return this._request("GET", `/v1.0/models${qs}`);
  }

  /** Start pulling a model. Returns 202 Accepted. */
  async pullModel(request: PullModelRequest): Promise<void> {
    return this._request("POST", "/v1.0/models/pull", request);
  }

  /** Get the status of an ongoing model pull operation. */
  async getPullStatus(): Promise<PullProgress> {
    return this._request("GET", "/v1.0/models/pull/status");
  }

  /** Delete a model. */
  async deleteModel(modelName: string): Promise<void> {
    return this._request("DELETE", `/v1.0/models/${encodeURIComponent(modelName)}`);
  }

  // --------------------------------------------------------------------------
  // Collections (RecallDB proxy)
  // --------------------------------------------------------------------------

  /** Create a collection. */
  async createCollection(collection: Collection): Promise<Collection> {
    return this._request("PUT", "/v1.0/collections", collection);
  }

  /** List collections. */
  async listCollections(query?: EnumerationQuery): Promise<EnumerationResult<Collection>> {
    return this._request("GET", `/v1.0/collections${this._qs(query)}`);
  }

  /** Get a collection by ID. */
  async getCollection(collectionId: string): Promise<Collection> {
    return this._request("GET", `/v1.0/collections/${encodeURIComponent(collectionId)}`);
  }

  /** Update a collection. */
  async updateCollection(collectionId: string, collection: Collection): Promise<Collection> {
    return this._request("PUT", `/v1.0/collections/${encodeURIComponent(collectionId)}`, collection);
  }

  /** Delete a collection. */
  async deleteCollection(collectionId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/collections/${encodeURIComponent(collectionId)}`);
  }

  /** Check if a collection exists. */
  async collectionExists(collectionId: string): Promise<boolean> {
    return this._head(`/v1.0/collections/${encodeURIComponent(collectionId)}`);
  }

  /** Get distinct labels in a collection. */
  async getCollectionDistinctLabels(collectionId: string): Promise<string[]> {
    return this._request("GET", `/v1.0/collections/${encodeURIComponent(collectionId)}/labels/distinct`);
  }

  /** Get distinct tags in a collection. */
  async getCollectionDistinctTags(collectionId: string): Promise<string[]> {
    return this._request("GET", `/v1.0/collections/${encodeURIComponent(collectionId)}/tags/distinct`);
  }

  // --------------------------------------------------------------------------
  // Collection Records
  // --------------------------------------------------------------------------

  /** Create a record in a collection. */
  async createCollectionRecord(collectionId: string, record: CollectionRecord): Promise<CollectionRecord> {
    return this._request("PUT", `/v1.0/collections/${encodeURIComponent(collectionId)}/records`, record);
  }

  /** List records in a collection. */
  async listCollectionRecords(collectionId: string, query?: EnumerationQuery): Promise<EnumerationResult<CollectionRecord>> {
    return this._request("GET", `/v1.0/collections/${encodeURIComponent(collectionId)}/records${this._qs(query)}`);
  }

  /** Get a record by ID. */
  async getCollectionRecord(collectionId: string, recordId: string): Promise<CollectionRecord> {
    return this._request("GET", `/v1.0/collections/${encodeURIComponent(collectionId)}/records/${encodeURIComponent(recordId)}`);
  }

  /** Delete a record. */
  async deleteCollectionRecord(collectionId: string, recordId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/collections/${encodeURIComponent(collectionId)}/records/${encodeURIComponent(recordId)}`);
  }

  /** Batch delete records in a collection. */
  async batchDeleteCollectionRecords(collectionId: string, recordIds: string[]): Promise<void> {
    return this._request("POST", `/v1.0/collections/${encodeURIComponent(collectionId)}/records/batch/delete`, recordIds);
  }

  // --------------------------------------------------------------------------
  // Buckets (S3)
  // --------------------------------------------------------------------------

  /** Create a bucket. */
  async createBucket(request: BucketCreateRequest): Promise<{ Name: string }> {
    return this._request("PUT", "/v1.0/buckets", request);
  }

  /** List buckets. */
  async listBuckets(): Promise<string[]> {
    return this._request("GET", "/v1.0/buckets");
  }

  /** Get bucket info. */
  async getBucket(name: string): Promise<unknown> {
    return this._request("GET", `/v1.0/buckets/${encodeURIComponent(name)}`);
  }

  /** Delete a bucket. */
  async deleteBucket(name: string): Promise<void> {
    return this._request("DELETE", `/v1.0/buckets/${encodeURIComponent(name)}`);
  }

  /** Check if a bucket exists. */
  async bucketExists(name: string): Promise<boolean> {
    return this._head(`/v1.0/buckets/${encodeURIComponent(name)}`);
  }

  /** Put an object in a bucket. */
  async putBucketObject(bucketName: string, object: BucketObject): Promise<unknown> {
    return this._request("PUT", `/v1.0/buckets/${encodeURIComponent(bucketName)}/objects`, object);
  }

  /** List objects in a bucket. */
  async listBucketObjects(bucketName: string, params?: { prefix?: string; maxResults?: number; continuationToken?: string }): Promise<unknown> {
    return this._request("GET", `/v1.0/buckets/${encodeURIComponent(bucketName)}/objects${this._qs(params)}`);
  }

  /** Delete an object from a bucket. */
  async deleteBucketObject(bucketName: string, key: string): Promise<void> {
    return this._request("DELETE", `/v1.0/buckets/${encodeURIComponent(bucketName)}/objects?Key=${encodeURIComponent(key)}`);
  }

  /** Get object metadata. */
  async getBucketObjectMetadata(bucketName: string, key: string): Promise<unknown> {
    return this._request("GET", `/v1.0/buckets/${encodeURIComponent(bucketName)}/objects/metadata?Key=${encodeURIComponent(key)}`);
  }

  /** Download an object from a bucket. Returns the raw Response for binary handling. */
  async downloadBucketObject(bucketName: string, key: string): Promise<Response> {
    return this._requestRaw("GET", `/v1.0/buckets/${encodeURIComponent(bucketName)}/objects/download?Key=${encodeURIComponent(key)}`);
  }

  // --------------------------------------------------------------------------
  // Crawl Plans
  // --------------------------------------------------------------------------

  /** Create a crawl plan. */
  async createCrawlPlan(plan: CrawlPlan): Promise<CrawlPlan> {
    return this._request("PUT", "/v1.0/crawlplans", plan);
  }

  /** List crawl plans. */
  async listCrawlPlans(query?: EnumerationQuery): Promise<EnumerationResult<CrawlPlan>> {
    return this._request("GET", `/v1.0/crawlplans${this._qs(query)}`);
  }

  /** Get a crawl plan by ID. */
  async getCrawlPlan(planId: string): Promise<CrawlPlan> {
    return this._request("GET", `/v1.0/crawlplans/${encodeURIComponent(planId)}`);
  }

  /** Update a crawl plan. */
  async updateCrawlPlan(planId: string, plan: CrawlPlan): Promise<CrawlPlan> {
    return this._request("PUT", `/v1.0/crawlplans/${encodeURIComponent(planId)}`, plan);
  }

  /** Delete a crawl plan. */
  async deleteCrawlPlan(planId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/crawlplans/${encodeURIComponent(planId)}`);
  }

  /** Check if a crawl plan exists. */
  async crawlPlanExists(planId: string): Promise<boolean> {
    return this._head(`/v1.0/crawlplans/${encodeURIComponent(planId)}`);
  }

  /** Start a crawl. */
  async startCrawl(planId: string): Promise<{ Started: boolean }> {
    return this._request("POST", `/v1.0/crawlplans/${encodeURIComponent(planId)}/start`);
  }

  /** Stop a crawl. */
  async stopCrawl(planId: string): Promise<{ Stopped: boolean }> {
    return this._request("POST", `/v1.0/crawlplans/${encodeURIComponent(planId)}/stop`);
  }

  /** Test connectivity for a crawl plan's URL. */
  async testCrawlConnectivity(planId: string): Promise<{ Connected: boolean; Status?: string; Message?: string }> {
    return this._request("POST", `/v1.0/crawlplans/${encodeURIComponent(planId)}/connectivity`);
  }

  /** Enumerate crawl plan contents. */
  async enumerateCrawl(planId: string, params?: { maxResults?: number; continuationToken?: string }): Promise<unknown> {
    return this._request("GET", `/v1.0/crawlplans/${encodeURIComponent(planId)}/enumerate${this._qs(params)}`);
  }

  // --------------------------------------------------------------------------
  // Crawl Operations
  // --------------------------------------------------------------------------

  /** List operations for a crawl plan. */
  async listCrawlOperations(planId: string, query?: EnumerationQuery): Promise<EnumerationResult<CrawlOperation>> {
    return this._request("GET", `/v1.0/crawlplans/${encodeURIComponent(planId)}/operations${this._qs(query)}`);
  }

  /** Get statistics for a crawl plan. */
  async getCrawlPlanStatistics(planId: string): Promise<unknown> {
    return this._request("GET", `/v1.0/crawlplans/${encodeURIComponent(planId)}/operations/statistics`);
  }

  /** Get a crawl operation by ID. */
  async getCrawlOperation(planId: string, operationId: string): Promise<CrawlOperation> {
    return this._request("GET", `/v1.0/crawlplans/${encodeURIComponent(planId)}/operations/${encodeURIComponent(operationId)}`);
  }

  /** Get statistics for a crawl operation. */
  async getCrawlOperationStatistics(planId: string, operationId: string): Promise<unknown> {
    return this._request("GET", `/v1.0/crawlplans/${encodeURIComponent(planId)}/operations/${encodeURIComponent(operationId)}/statistics`);
  }

  /** Delete a crawl operation. */
  async deleteCrawlOperation(planId: string, operationId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/crawlplans/${encodeURIComponent(planId)}/operations/${encodeURIComponent(operationId)}`);
  }

  /** Get enumeration results for a crawl operation. */
  async getCrawlOperationEnumeration(planId: string, operationId: string): Promise<unknown> {
    return this._request("GET", `/v1.0/crawlplans/${encodeURIComponent(planId)}/operations/${encodeURIComponent(operationId)}/enumeration`);
  }

  // --------------------------------------------------------------------------
  // Evaluation
  // --------------------------------------------------------------------------

  /** Create an evaluation fact. */
  async createEvalFact(fact: EvalFact): Promise<EvalFact> {
    return this._request("PUT", "/v1.0/eval/facts", fact);
  }

  /** List evaluation facts. */
  async listEvalFacts(query?: EnumerationQuery): Promise<EnumerationResult<EvalFact>> {
    return this._request("GET", `/v1.0/eval/facts${this._qs(query)}`);
  }

  /** Get an evaluation fact by ID. */
  async getEvalFact(factId: string): Promise<EvalFact> {
    return this._request("GET", `/v1.0/eval/facts/${encodeURIComponent(factId)}`);
  }

  /** Update an evaluation fact. */
  async updateEvalFact(factId: string, fact: EvalFact): Promise<EvalFact> {
    return this._request("PUT", `/v1.0/eval/facts/${encodeURIComponent(factId)}`, fact);
  }

  /** Delete an evaluation fact. */
  async deleteEvalFact(factId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/eval/facts/${encodeURIComponent(factId)}`);
  }

  /** Start an evaluation run. */
  async createEvalRun(request: EvalRunRequest): Promise<EvalRun> {
    return this._request("POST", "/v1.0/eval/runs", request);
  }

  /** List evaluation runs. */
  async listEvalRuns(query?: EnumerationQuery): Promise<EnumerationResult<EvalRun>> {
    return this._request("GET", `/v1.0/eval/runs${this._qs(query)}`);
  }

  /** Get an evaluation run by ID. */
  async getEvalRun(runId: string): Promise<EvalRun> {
    return this._request("GET", `/v1.0/eval/runs/${encodeURIComponent(runId)}`);
  }

  /** Delete an evaluation run. */
  async deleteEvalRun(runId: string): Promise<void> {
    return this._request("DELETE", `/v1.0/eval/runs/${encodeURIComponent(runId)}`);
  }

  /** Get results for an evaluation run. */
  async getEvalRunResults(runId: string): Promise<EvalResult[]> {
    return this._request("GET", `/v1.0/eval/runs/${encodeURIComponent(runId)}/results`);
  }

  /**
   * Stream evaluation run results as SSE events. Returns an async iterable.
   */
  async *streamEvalRunResults(runId: string): AsyncGenerator<EvalResult> {
    const response = await this._requestRaw("GET", `/v1.0/eval/runs/${encodeURIComponent(runId)}/stream`);
    yield* this._parseSSE<EvalResult>(response);
  }

  /** Get a single evaluation result by ID. */
  async getEvalResult(resultId: string): Promise<EvalResult> {
    return this._request("GET", `/v1.0/eval/results/${encodeURIComponent(resultId)}`);
  }

  /** Get the default judge prompt. */
  async getDefaultJudgePrompt(): Promise<{ Prompt: string }> {
    return this._request("GET", "/v1.0/eval/judge-prompt/default");
  }

  // --------------------------------------------------------------------------
  // Configuration
  // --------------------------------------------------------------------------

  /** Get server configuration (Global Admin). */
  async getConfiguration(): Promise<AssistantHubSettings> {
    return this._request("GET", "/v1.0/configuration");
  }

  /** Update server configuration (Global Admin). */
  async updateConfiguration(settings: AssistantHubSettings): Promise<AssistantHubSettings> {
    return this._request("PUT", "/v1.0/configuration", settings);
  }

  // --------------------------------------------------------------------------
  // SSE Parsing
  // --------------------------------------------------------------------------

  private async *_parseSSE<T>(response: Response): AsyncGenerator<T> {
    const reader = response.body?.getReader();
    if (!reader) return;

    const decoder = new TextDecoder();
    let buffer = "";

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split("\n");
        buffer = lines.pop() || "";

        for (const line of lines) {
          const trimmed = line.trim();
          if (!trimmed || trimmed.startsWith(":")) continue;
          if (trimmed === "data: [DONE]") return;
          if (trimmed.startsWith("data: ")) {
            const json = trimmed.slice(6);
            try {
              yield JSON.parse(json) as T;
            } catch {
              // skip malformed JSON
            }
          }
        }
      }

      // Process any remaining buffer
      if (buffer.trim()) {
        const trimmed = buffer.trim();
        if (trimmed.startsWith("data: ") && trimmed !== "data: [DONE]") {
          try {
            yield JSON.parse(trimmed.slice(6)) as T;
          } catch {
            // skip malformed JSON
          }
        }
      }
    } finally {
      reader.releaseLock();
    }
  }
}
