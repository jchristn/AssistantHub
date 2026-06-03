"""Pydantic v2 models for the AssistantHub API."""

from __future__ import annotations

from datetime import datetime
from typing import Any, Generic, Optional, TypeVar

from pydantic import BaseModel, ConfigDict, Field

from .enums import (
    ApiError,
    CrawlOperationState,
    CrawlPlanState,
    DocumentStatus,
    EnumerationOrder,
    EvalStatus,
    FeedbackRating,
    RepositoryType,
    ScheduleInterval,
    SummarizationOrder,
    WebAuthType,
)

T = TypeVar("T")


# ---------------------------------------------------------------------------
# Error
# ---------------------------------------------------------------------------


class ApiErrorResponse(BaseModel):
    """Standard API error response."""

    error: ApiError
    context: Optional[Any] = None
    description: Optional[str] = None


# ---------------------------------------------------------------------------
# Auth
# ---------------------------------------------------------------------------


class AuthenticateRequest(BaseModel):
    """Authentication request body."""

    email: Optional[str] = None
    password: Optional[str] = None
    bearer_token: Optional[str] = Field(None, alias="bearerToken")
    tenant_id: Optional[str] = Field(None, alias="tenantId")


class AuthenticateResult(BaseModel):
    """Authentication response."""

    success: bool = False
    user: Optional[UserMaster] = None
    credential: Optional[Credential] = None
    error_message: Optional[str] = Field(None, alias="errorMessage")
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    tenant_name: Optional[str] = Field(None, alias="tenantName")
    is_global_admin: bool = Field(False, alias="isGlobalAdmin")
    is_tenant_admin: bool = Field(False, alias="isTenantAdmin")


# ---------------------------------------------------------------------------
# Tenant / User / Credential
# ---------------------------------------------------------------------------


class TenantMetadata(BaseModel):
    """Tenant metadata."""

    id: Optional[str] = None
    name: Optional[str] = None
    active: bool = True
    is_protected: bool = Field(False, alias="isProtected")
    labels: Optional[list[str]] = None
    tags: Optional[dict[str, str]] = None
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


class UserMaster(BaseModel):
    """User account."""

    id: Optional[str] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    email: Optional[str] = None
    password_sha256: Optional[str] = Field(None, alias="passwordSha256")
    first_name: Optional[str] = Field(None, alias="firstName")
    last_name: Optional[str] = Field(None, alias="lastName")
    is_admin: bool = Field(False, alias="isAdmin")
    is_tenant_admin: bool = Field(False, alias="isTenantAdmin")
    active: bool = True
    is_protected: bool = Field(False, alias="isProtected")
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


class Credential(BaseModel):
    """API credential / bearer token."""

    id: Optional[str] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    user_id: Optional[str] = Field(None, alias="userId")
    name: Optional[str] = None
    bearer_token: Optional[str] = Field(None, alias="bearerToken")
    active: bool = True
    is_protected: bool = Field(False, alias="isProtected")
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


# ---------------------------------------------------------------------------
# Assistant
# ---------------------------------------------------------------------------


class Assistant(BaseModel):
    """An AI assistant."""

    id: Optional[str] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    user_id: Optional[str] = Field(None, alias="userId")
    name: Optional[str] = None
    description: Optional[str] = None
    active: bool = True
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


class AssistantPublicInfo(BaseModel):
    """Public metadata for an assistant."""

    id: Optional[str] = Field(None, alias="Id")
    name: Optional[str] = Field(None, alias="Name")
    description: Optional[str] = Field(None, alias="Description")
    title: Optional[str] = Field(None, alias="Title")
    logo_url: Optional[str] = Field(None, alias="LogoUrl")
    favicon_url: Optional[str] = Field(None, alias="FaviconUrl")


class AssistantSettings(BaseModel):
    """Configuration for an assistant."""

    id: Optional[str] = None
    assistant_id: Optional[str] = Field(None, alias="assistantId")
    temperature: float = 0.7
    top_p: float = 1.0
    system_prompt: Optional[str] = Field(None, alias="systemPrompt")
    max_tokens: int = Field(4096, alias="maxTokens")
    context_window: int = Field(4096, alias="contextWindow")
    enable_rag: bool = Field(False, alias="enableRag")
    enable_retrieval_gate: bool = Field(False, alias="enableRetrievalGate")
    enable_query_rewrite: bool = Field(False, alias="enableQueryRewrite")
    query_rewrite_prompt: Optional[str] = Field(None, alias="queryRewritePrompt")
    enable_reranking: bool = Field(False, alias="enableReranking")
    reranker_top_k: int = Field(5, alias="rerankerTopK")
    reranker_score_threshold: float = Field(0.0, alias="rerankerScoreThreshold")
    rerank_prompt: Optional[str] = Field(None, alias="rerankPrompt")
    enable_citations: bool = Field(False, alias="enableCitations")
    citation_link_mode: Optional[str] = Field(None, alias="citationLinkMode")
    collection_id: Optional[str] = Field(None, alias="collectionId")
    retrieval_top_k: int = Field(10, alias="retrievalTopK")
    retrieval_score_threshold: float = Field(0.0, alias="retrievalScoreThreshold")
    search_mode: Optional[str] = Field(None, alias="searchMode")
    text_weight: float = Field(0.5, alias="textWeight")
    full_text_search_type: Optional[str] = Field(None, alias="fullTextSearchType")
    full_text_language: Optional[str] = Field(None, alias="fullTextLanguage")
    full_text_normalization: int = Field(0, alias="fullTextNormalization")
    full_text_minimum_score: Optional[float] = Field(None, alias="fullTextMinimumScore")
    retrieval_include_neighbors: int = Field(0, alias="retrievalIncludeNeighbors")
    inference_endpoint_id: Optional[str] = Field(None, alias="inferenceEndpointId")
    retrieval_gate_inference_endpoint_id: Optional[str] = Field(
        None, alias="retrievalGateInferenceEndpointId"
    )
    query_rewrite_inference_endpoint_id: Optional[str] = Field(
        None, alias="queryRewriteInferenceEndpointId"
    )
    rerank_inference_endpoint_id: Optional[str] = Field(
        None, alias="rerankInferenceEndpointId"
    )
    embedding_endpoint_id: Optional[str] = Field(None, alias="embeddingEndpointId")
    title: Optional[str] = None
    logo_url: Optional[str] = Field(None, alias="logoUrl")
    favicon_url: Optional[str] = Field(None, alias="faviconUrl")
    retrieval_label_filter: Optional[str] = Field(None, alias="retrievalLabelFilter")
    retrieval_tag_filter: Optional[str] = Field(None, alias="retrievalTagFilter")
    eval_judge_prompt: Optional[str] = Field(None, alias="evalJudgePrompt")
    streaming: bool = False
    enable_slack: bool = Field(False, alias="enableSlack")
    slack_app_token: Optional[str] = Field(None, alias="slackAppToken")
    slack_bot_token: Optional[str] = Field(None, alias="slackBotToken")
    slack_channel_id: Optional[str] = Field(None, alias="slackChannelId")
    slack_message_prefix: Optional[str] = Field(None, alias="slackMessagePrefix")
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


# ---------------------------------------------------------------------------
# Documents
# ---------------------------------------------------------------------------


class AssistantDocument(BaseModel):
    """A document managed by AssistantHub."""

    id: Optional[str] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    name: Optional[str] = None
    original_filename: Optional[str] = Field(None, alias="originalFilename")
    content_type: Optional[str] = Field(None, alias="contentType")
    size_bytes: int = Field(0, alias="sizeBytes")
    s3_key: Optional[str] = Field(None, alias="s3Key")
    status: Optional[DocumentStatus] = None
    status_message: Optional[str] = Field(None, alias="statusMessage")
    ingestion_rule_id: Optional[str] = Field(None, alias="ingestionRuleId")
    bucket_name: Optional[str] = Field(None, alias="bucketName")
    collection_id: Optional[str] = Field(None, alias="collectionId")
    labels: Optional[str] = None
    tags: Optional[str] = None
    chunk_record_ids: Optional[str] = Field(None, alias="chunkRecordIds")
    crawl_plan_id: Optional[str] = Field(None, alias="crawlPlanId")
    crawl_operation_id: Optional[str] = Field(None, alias="crawlOperationId")
    source_url: Optional[str] = Field(None, alias="sourceUrl")
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


# ---------------------------------------------------------------------------
# Chat
# ---------------------------------------------------------------------------


class ChatCompletionMessage(BaseModel):
    """A single message in a chat conversation."""

    role: Optional[str] = None
    content: Optional[str] = None


class ChatMetadataFilter(BaseModel):
    """Metadata filter for chat retrieval."""

    required_labels: Optional[list[str]] = Field(None, alias="requiredLabels")
    excluded_labels: Optional[list[str]] = Field(None, alias="excludedLabels")
    required_tags: Optional[list[ChatTagCondition]] = Field(None, alias="requiredTags")
    excluded_tags: Optional[list[ChatTagCondition]] = Field(None, alias="excludedTags")


class ChatTagCondition(BaseModel):
    """Tag filter condition for chat retrieval."""

    key: Optional[str] = None
    condition: Optional[str] = None
    value: Optional[str] = None


class ChatCompletionRequest(BaseModel):
    """OpenAI-compatible chat completion request."""

    model: Optional[str] = None
    messages: Optional[list[ChatCompletionMessage]] = None
    stream: bool = False
    temperature: Optional[float] = None
    top_p: Optional[float] = Field(None, alias="top_p")
    max_tokens: Optional[int] = Field(None, alias="max_tokens")
    metadata_filter: Optional[ChatMetadataFilter] = Field(
        None, alias="metadata_filter"
    )


class ChatCompletionUsage(BaseModel):
    """Token usage information for a chat completion."""

    prompt_tokens: int = Field(0, alias="prompt_tokens")
    completion_tokens: int = Field(0, alias="completion_tokens")
    total_tokens: int = Field(0, alias="total_tokens")
    context_window: int = Field(0, alias="context_window")


class RetrievalChunk(BaseModel):
    """A chunk returned from retrieval search."""

    document_id: Optional[str] = Field(None, alias="documentId")
    score: float = 0.0
    rerank_score: Optional[float] = Field(None, alias="rerankScore")
    fusion_score: Optional[float] = Field(None, alias="fusionScore")
    text_score: Optional[float] = Field(None, alias="textScore")
    content: Optional[str] = None
    position: Optional[int] = None
    neighbors: Optional[list[RetrievalChunk]] = None


class CitationSource(BaseModel):
    """A citation source in a chat completion response."""

    index: int = 0
    document_id: Optional[str] = Field(None, alias="documentId")
    document_name: Optional[str] = Field(None, alias="documentName")
    content_type: Optional[str] = Field(None, alias="contentType")
    score: float = 0.0
    fusion_score: Optional[float] = Field(None, alias="fusionScore")
    rerank_score: Optional[float] = Field(None, alias="rerankScore")
    excerpt: Optional[str] = None
    download_url: Optional[str] = Field(None, alias="downloadUrl")


class ChatCompletionRetrieval(BaseModel):
    """Retrieval metadata in a chat completion response."""

    collection_id: Optional[str] = Field(None, alias="collectionId")
    duration_ms: float = Field(0.0, alias="durationMs")
    chunks_returned: int = Field(0, alias="chunksReturned")
    rerank_duration_ms: float = Field(0.0, alias="rerankDurationMs")
    rerank_input_count: int = Field(0, alias="rerankInputCount")
    rerank_output_count: int = Field(0, alias="rerankOutputCount")
    chunks: Optional[list[RetrievalChunk]] = None


class ChatCompletionCitations(BaseModel):
    """Citation metadata in a chat completion response."""

    sources: Optional[list[CitationSource]] = None
    referenced_indices: Optional[list[int]] = Field(None, alias="referencedIndices")
    auto_populated: bool = Field(False, alias="autoPopulated")


class ChatCompletionChoice(BaseModel):
    """A choice in a chat completion response."""

    index: int = 0
    message: Optional[ChatCompletionMessage] = None
    delta: Optional[ChatCompletionMessage] = None
    finish_reason: Optional[str] = Field(None, alias="finish_reason")


class ChatCompletionResponse(BaseModel):
    """OpenAI-compatible chat completion response."""

    id: Optional[str] = None
    object: Optional[str] = None
    created: int = 0
    model: Optional[str] = None
    choices: Optional[list[ChatCompletionChoice]] = None
    usage: Optional[ChatCompletionUsage] = None
    status: Optional[str] = None
    retrieval: Optional[ChatCompletionRetrieval] = None
    citations: Optional[ChatCompletionCitations] = None


class ChatHistory(BaseModel):
    """A chat history record."""

    id: Optional[str] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    thread_id: Optional[str] = Field(None, alias="threadId")
    assistant_id: Optional[str] = Field(None, alias="assistantId")
    collection_id: Optional[str] = Field(None, alias="collectionId")
    user_message_utc: Optional[datetime] = Field(None, alias="userMessageUtc")
    user_message: Optional[str] = Field(None, alias="userMessage")
    retrieval_start_utc: Optional[datetime] = Field(None, alias="retrievalStartUtc")
    retrieval_duration_ms: float = Field(0.0, alias="retrievalDurationMs")
    retrieval_gate_decision: Optional[str] = Field(None, alias="retrievalGateDecision")
    retrieval_gate_duration_ms: float = Field(0.0, alias="retrievalGateDurationMs")
    query_rewrite_result: Optional[str] = Field(None, alias="queryRewriteResult")
    query_rewrite_duration_ms: float = Field(0.0, alias="queryRewriteDurationMs")
    rerank_duration_ms: float = Field(0.0, alias="rerankDurationMs")
    rerank_input_count: int = Field(0, alias="rerankInputCount")
    rerank_output_count: int = Field(0, alias="rerankOutputCount")
    retrieval_context: Optional[str] = Field(None, alias="retrievalContext")
    prompt_sent_utc: Optional[datetime] = Field(None, alias="promptSentUtc")
    prompt_tokens: int = Field(0, alias="promptTokens")
    endpoint_resolution_duration_ms: float = Field(
        0.0, alias="endpointResolutionDurationMs"
    )
    compaction_duration_ms: float = Field(0.0, alias="compactionDurationMs")
    inference_connection_duration_ms: float = Field(
        0.0, alias="inferenceConnectionDurationMs"
    )
    time_to_first_token_ms: float = Field(0.0, alias="timeToFirstTokenMs")
    time_to_last_token_ms: float = Field(0.0, alias="timeToLastTokenMs")
    completion_tokens: int = Field(0, alias="completionTokens")
    tokens_per_second_overall: float = Field(0.0, alias="tokensPerSecondOverall")
    tokens_per_second_generation: float = Field(
        0.0, alias="tokensPerSecondGeneration"
    )
    metadata_filter: Optional[str] = Field(None, alias="metadataFilter")
    origin: Optional[str] = None
    assistant_response: Optional[str] = Field(None, alias="assistantResponse")
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


class ThreadSummary(BaseModel):
    """Summary information for a thread."""

    thread_id: Optional[str] = Field(None, alias="ThreadId")
    assistant_id: Optional[str] = Field(None, alias="AssistantId")
    first_message_utc: Optional[datetime] = Field(None, alias="FirstMessageUtc")
    last_message_utc: Optional[datetime] = Field(None, alias="LastMessageUtc")
    turn_count: int = Field(0, alias="TurnCount")


# ---------------------------------------------------------------------------
# Request History
# ---------------------------------------------------------------------------


class RequestHistorySearchFilter(BaseModel):
    """Filter parameters for request-history search and summary endpoints."""

    max_results: int = Field(100, alias="maxResults")
    continuation_token: Optional[str] = Field(None, alias="continuationToken")
    ordering: EnumerationOrder = Field(
        EnumerationOrder.CREATED_DESCENDING, alias="ordering"
    )
    request_type: Optional[str] = Field(None, alias="requestType")
    http_method: Optional[str] = Field(None, alias="method")
    path_contains: Optional[str] = Field(None, alias="path")
    status_code: Optional[int] = Field(None, alias="statusCode")
    success: Optional[bool] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    user_id: Optional[str] = Field(None, alias="userId")
    credential_id: Optional[str] = Field(None, alias="credentialId")
    assistant_id: Optional[str] = Field(None, alias="assistantId")
    thread_id: Optional[str] = Field(None, alias="threadId")
    source_type: Optional[str] = Field(None, alias="sourceType")
    search_text: Optional[str] = Field(None, alias="search")
    start_utc: Optional[datetime] = Field(None, alias="startUtc")
    end_utc: Optional[datetime] = Field(None, alias="endUtc")
    bucket_seconds: int = Field(900, alias="bucketSeconds")


class RequestHistoryEntry(BaseModel):
    """Captured HTTP request and response record."""

    id: Optional[str] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    user_id: Optional[str] = Field(None, alias="userId")
    credential_id: Optional[str] = Field(None, alias="credentialId")
    assistant_id: Optional[str] = Field(None, alias="assistantId")
    thread_id: Optional[str] = Field(None, alias="threadId")
    principal_name: Optional[str] = Field(None, alias="principalName")
    request_type: Optional[str] = Field(None, alias="requestType")
    source_type: Optional[str] = Field(None, alias="sourceType")
    http_method: Optional[str] = Field(None, alias="httpMethod")
    route_template: Optional[str] = Field(None, alias="routeTemplate")
    request_path: Optional[str] = Field(None, alias="requestPath")
    request_url: Optional[str] = Field(None, alias="requestUrl")
    source_ip: Optional[str] = Field(None, alias="sourceIp")
    status_code: int = Field(0, alias="statusCode")
    success: bool = False
    duration_ms: float = Field(0.0, alias="durationMs")
    request_content_type: Optional[str] = Field(None, alias="requestContentType")
    response_content_type: Optional[str] = Field(None, alias="responseContentType")
    request_size_bytes: int = Field(0, alias="requestSizeBytes")
    response_size_bytes: int = Field(0, alias="responseSizeBytes")
    request_body_truncated: bool = Field(False, alias="requestBodyTruncated")
    response_body_truncated: bool = Field(False, alias="responseBodyTruncated")
    request_body_is_binary: bool = Field(False, alias="requestBodyIsBinary")
    response_body_is_binary: bool = Field(False, alias="responseBodyIsBinary")
    route_parameters: Optional[dict[str, str]] = Field(None, alias="routeParameters")
    query_parameters: Optional[dict[str, str]] = Field(None, alias="queryParameters")
    request_headers: Optional[dict[str, str]] = Field(None, alias="requestHeaders")
    response_headers: Optional[dict[str, str]] = Field(None, alias="responseHeaders")
    request_body: Optional[str] = Field(None, alias="requestBody")
    response_body: Optional[str] = Field(None, alias="responseBody")
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


class RequestHistorySummaryBucket(BaseModel):
    """Aggregated request-history bucket."""

    bucket_start_utc: Optional[datetime] = Field(None, alias="bucketStartUtc")
    bucket_end_utc: Optional[datetime] = Field(None, alias="bucketEndUtc")
    request_count: int = Field(0, alias="requestCount")
    success_count: int = Field(0, alias="successCount")
    failure_count: int = Field(0, alias="failureCount")
    average_duration_ms: float = Field(0.0, alias="averageDurationMs")


class RequestHistorySummaryResult(BaseModel):
    """Summary of request-history matches."""

    total_count: int = Field(0, alias="totalCount")
    total_success: int = Field(0, alias="totalSuccess")
    total_failure: int = Field(0, alias="totalFailure")
    average_duration_ms: float = Field(0.0, alias="averageDurationMs")
    buckets: Optional[list[RequestHistorySummaryBucket]] = None


class RequestHistoryDeleteResult(BaseModel):
    """Bulk request-history deletion result."""

    deleted_count: int = Field(0, alias="deletedCount")


# ---------------------------------------------------------------------------
# Feedback
# ---------------------------------------------------------------------------


class AssistantFeedback(BaseModel):
    """Stored feedback for an assistant response."""

    id: Optional[str] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    assistant_id: Optional[str] = Field(None, alias="assistantId")
    user_message: Optional[str] = Field(None, alias="userMessage")
    assistant_response: Optional[str] = Field(None, alias="assistantResponse")
    rating: Optional[FeedbackRating] = None
    feedback_text: Optional[str] = Field(None, alias="feedbackText")
    message_history: Optional[str] = Field(None, alias="messageHistory")
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


class FeedbackRequest(BaseModel):
    """Request body for submitting feedback."""

    assistant_id: Optional[str] = Field(None, alias="AssistantId")
    user_message: Optional[str] = Field(None, alias="UserMessage")
    assistant_response: Optional[str] = Field(None, alias="AssistantResponse")
    rating: Optional[FeedbackRating] = Field(None, alias="Rating")
    feedback_text: Optional[str] = Field(None, alias="FeedbackText")
    message_history: Optional[str] = Field(None, alias="MessageHistory")


# ---------------------------------------------------------------------------
# Endpoints (Partio)
# ---------------------------------------------------------------------------


class PartioEndpointConfig(BaseModel):
    """An embedding or completion endpoint configuration."""

    id: Optional[str] = None
    name: Optional[str] = None
    model: Optional[str] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    endpoint: Optional[str] = None
    api_format: Optional[str] = Field(None, alias="apiFormat")
    api_key: Optional[str] = Field(None, alias="apiKey")
    active: bool = True
    max_concurrent_requests: int = Field(2, alias="maxConcurrentRequests")
    health_check_enabled: bool = Field(False, alias="healthCheckEnabled")
    health_check_url: Optional[str] = Field(None, alias="healthCheckUrl")
    health_check_method: Optional[str] = Field(None, alias="healthCheckMethod")
    health_check_interval_ms: int = Field(60000, alias="healthCheckIntervalMs")
    health_check_timeout_ms: int = Field(5000, alias="healthCheckTimeoutMs")
    health_check_expected_status_code: int = Field(
        200, alias="healthCheckExpectedStatusCode"
    )
    healthy_threshold: int = Field(1, alias="healthyThreshold")
    unhealthy_threshold: int = Field(3, alias="unhealthyThreshold")
    health_check_use_auth: bool = Field(False, alias="healthCheckUseAuth")


class PartioEndpointRequest(BaseModel):
    """Request body for creating/updating an endpoint."""

    tenant_id: Optional[str] = Field(None, alias="tenantId")
    name: Optional[str] = None
    model: Optional[str] = None
    endpoint: Optional[str] = None
    api_format: Optional[str] = Field(None, alias="apiFormat")
    api_key: Optional[str] = Field(None, alias="apiKey")
    active: bool = True
    max_concurrent_requests: int = Field(2, alias="maxConcurrentRequests")
    enable_request_history: bool = Field(False, alias="enableRequestHistory")
    labels: Optional[list[str]] = None
    tags: Optional[dict[str, str]] = None
    health_check_enabled: bool = Field(False, alias="healthCheckEnabled")
    health_check_url: Optional[str] = Field(None, alias="healthCheckUrl")
    health_check_method: Optional[str] = Field(None, alias="healthCheckMethod")
    health_check_interval_ms: int = Field(60000, alias="healthCheckIntervalMs")
    health_check_timeout_ms: int = Field(5000, alias="healthCheckTimeoutMs")
    health_check_expected_status_code: int = Field(
        200, alias="healthCheckExpectedStatusCode"
    )
    healthy_threshold: int = Field(1, alias="healthyThreshold")
    unhealthy_threshold: int = Field(3, alias="unhealthyThreshold")
    health_check_use_auth: bool = Field(False, alias="healthCheckUseAuth")


# ---------------------------------------------------------------------------
# Endpoint Explorer
# ---------------------------------------------------------------------------


class EndpointExplorerCompletionRequest(BaseModel):
    """Request for testing a completion endpoint."""

    endpoint_id: Optional[str] = Field(None, alias="endpointId")
    prompt: Optional[str] = None
    system_prompt: Optional[str] = Field(None, alias="systemPrompt")
    max_tokens: int = Field(256, alias="maxTokens")
    timeout_ms: int = Field(30000, alias="timeoutMs")


class CompletionCallDetail(BaseModel):
    """Details of a completion API call."""

    url: Optional[str] = None
    method: Optional[str] = None
    request_headers: Optional[dict[str, str]] = Field(None, alias="requestHeaders")
    request_body: Optional[str] = Field(None, alias="requestBody")
    status_code: Optional[int] = Field(None, alias="statusCode")
    response_headers: Optional[dict[str, str]] = Field(None, alias="responseHeaders")
    response_body: Optional[str] = Field(None, alias="responseBody")
    response_time_ms: Optional[int] = Field(None, alias="responseTimeMs")
    success: bool = False
    error: Optional[str] = None
    timestamp_utc: Optional[datetime] = Field(None, alias="timestampUtc")


class EndpointExplorerCompletionResponse(BaseModel):
    """Response from testing a completion endpoint."""

    success: bool = False
    status_code: int = Field(0, alias="statusCode")
    error: Optional[str] = None
    endpoint_id: Optional[str] = Field(None, alias="endpointId")
    model: Optional[str] = None
    prompt: Optional[str] = None
    system_prompt: Optional[str] = Field(None, alias="systemPrompt")
    output: Optional[str] = None
    response_time_ms: int = Field(0, alias="responseTimeMs")
    request_history_id: Optional[str] = Field(None, alias="requestHistoryId")
    completion_calls: Optional[list[CompletionCallDetail]] = Field(
        None, alias="completionCalls"
    )


class EndpointExplorerEmbeddingRequest(BaseModel):
    """Request for testing an embedding endpoint."""

    endpoint_id: Optional[str] = Field(None, alias="endpointId")
    input: Optional[str] = None
    l2_normalization: bool = Field(False, alias="l2Normalization")


class EmbeddingCallDetail(BaseModel):
    """Details of an embedding API call."""

    url: Optional[str] = None
    method: Optional[str] = None
    request_headers: Optional[dict[str, str]] = Field(None, alias="requestHeaders")
    request_body: Optional[str] = Field(None, alias="requestBody")
    status_code: Optional[int] = Field(None, alias="statusCode")
    response_headers: Optional[dict[str, str]] = Field(None, alias="responseHeaders")
    response_body: Optional[str] = Field(None, alias="responseBody")
    response_time_ms: Optional[int] = Field(None, alias="responseTimeMs")
    success: bool = False
    error: Optional[str] = None
    timestamp_utc: Optional[datetime] = Field(None, alias="timestampUtc")


class EndpointExplorerEmbeddingResponse(BaseModel):
    """Response from testing an embedding endpoint."""

    success: bool = False
    status_code: int = Field(0, alias="statusCode")
    error: Optional[str] = None
    endpoint_id: Optional[str] = Field(None, alias="endpointId")
    model: Optional[str] = None
    input: Optional[str] = None
    embedding: Optional[list[float]] = None
    dimensions: int = 0
    response_time_ms: int = Field(0, alias="responseTimeMs")
    request_history_id: Optional[str] = Field(None, alias="requestHistoryId")
    embedding_calls: Optional[list[EmbeddingCallDetail]] = Field(
        None, alias="embeddingCalls"
    )


# ---------------------------------------------------------------------------
# Endpoint Health
# ---------------------------------------------------------------------------


class HealthCheckRecord(BaseModel):
    """A single health check result."""

    timestamp_utc: Optional[datetime] = Field(None, alias="timestampUtc")
    success: bool = False


class EndpointHealthStatus(BaseModel):
    """Health status of an endpoint."""

    endpoint_id: Optional[str] = Field(None, alias="endpointId")
    endpoint_name: Optional[str] = Field(None, alias="endpointName")
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    is_healthy: bool = Field(False, alias="isHealthy")
    first_check_utc: Optional[datetime] = Field(None, alias="firstCheckUtc")
    last_check_utc: Optional[datetime] = Field(None, alias="lastCheckUtc")
    last_healthy_utc: Optional[datetime] = Field(None, alias="lastHealthyUtc")
    last_unhealthy_utc: Optional[datetime] = Field(None, alias="lastUnhealthyUtc")
    last_state_change_utc: Optional[datetime] = Field(
        None, alias="lastStateChangeUtc"
    )
    total_uptime_ms: int = Field(0, alias="totalUptimeMs")
    total_downtime_ms: int = Field(0, alias="totalDowntimeMs")
    uptime_percentage: float = Field(0.0, alias="uptimePercentage")
    consecutive_successes: int = Field(0, alias="consecutiveSuccesses")
    consecutive_failures: int = Field(0, alias="consecutiveFailures")
    last_error: Optional[str] = Field(None, alias="lastError")
    history: Optional[list[HealthCheckRecord]] = None


# ---------------------------------------------------------------------------
# Inference
# ---------------------------------------------------------------------------


class InferenceModel(BaseModel):
    """An inference model available on an endpoint."""

    name: Optional[str] = None
    size_bytes: int = Field(0, alias="sizeBytes")
    modified_utc: Optional[datetime] = Field(None, alias="modifiedUtc")
    owned_by: Optional[str] = Field(None, alias="ownedBy")
    pull_supported: bool = Field(False, alias="pullSupported")


class PullProgress(BaseModel):
    """Progress of a model pull operation."""

    model_name: Optional[str] = Field(None, alias="modelName")
    status: Optional[str] = None
    digest: Optional[str] = None
    total_bytes: int = Field(0, alias="totalBytes")
    completed_bytes: int = Field(0, alias="completedBytes")
    percent_complete: int = Field(0, alias="percentComplete")
    is_complete: bool = Field(False, alias="isComplete")
    has_error: bool = Field(False, alias="hasError")
    error_message: Optional[str] = Field(None, alias="errorMessage")
    started_utc: Optional[datetime] = Field(None, alias="startedUtc")


# ---------------------------------------------------------------------------
# Ingestion
# ---------------------------------------------------------------------------


class IngestionChunkingConfig(BaseModel):
    """Chunking configuration for ingestion rules."""

    strategy: Optional[str] = None
    fixed_token_count: int = Field(512, alias="fixedTokenCount")
    overlap_count: int = Field(0, alias="overlapCount")
    overlap_percentage: Optional[float] = Field(None, alias="overlapPercentage")
    overlap_strategy: Optional[str] = Field(None, alias="overlapStrategy")
    row_group_size: int = Field(100, alias="rowGroupSize")
    context_prefix: Optional[str] = Field(None, alias="contextPrefix")
    regex_pattern: Optional[str] = Field(None, alias="regexPattern")


class IngestionEmbeddingConfig(BaseModel):
    """Embedding configuration for ingestion rules."""

    embedding_endpoint_id: Optional[str] = Field(None, alias="embeddingEndpointId")
    l2_normalization: bool = Field(False, alias="l2Normalization")


class IngestionSummarizationConfig(BaseModel):
    """Summarization configuration for ingestion rules."""

    completion_endpoint_id: Optional[str] = Field(None, alias="completionEndpointId")
    order: SummarizationOrder = Field(
        SummarizationOrder.BOTTOM_UP, alias="order"
    )
    summarization_prompt: Optional[str] = Field(None, alias="summarizationPrompt")
    max_summary_tokens: int = Field(256, alias="maxSummaryTokens")
    min_cell_length: int = Field(128, alias="minCellLength")
    max_parallel_tasks: int = Field(1, alias="maxParallelTasks")
    max_retries_per_summary: int = Field(3, alias="maxRetriesPerSummary")
    max_retries: int = Field(9, alias="maxRetries")
    timeout_ms: int = Field(30000, alias="timeoutMs")


class IngestionRule(BaseModel):
    """An ingestion rule defining how documents are processed."""

    id: Optional[str] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    name: Optional[str] = None
    description: Optional[str] = None
    bucket: Optional[str] = None
    collection_name: Optional[str] = Field(None, alias="collectionName")
    collection_id: Optional[str] = Field(None, alias="collectionId")
    labels: Optional[list[str]] = None
    tags: Optional[dict[str, str]] = None
    atomization: Optional[dict[str, Any]] = None
    summarization: Optional[IngestionSummarizationConfig] = None
    chunking: Optional[IngestionChunkingConfig] = None
    embedding: Optional[IngestionEmbeddingConfig] = None
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


# ---------------------------------------------------------------------------
# Crawl
# ---------------------------------------------------------------------------


class CrawlScheduleSettings(BaseModel):
    """Schedule configuration for a crawl plan."""

    interval_type: ScheduleInterval = Field(
        ScheduleInterval.ONE_TIME, alias="intervalType"
    )
    interval_value: int = Field(1, alias="intervalValue")


class CrawlFilterSettings(BaseModel):
    """Filter configuration for a crawl plan."""

    object_prefix: Optional[str] = Field(None, alias="objectPrefix")
    object_suffix: Optional[str] = Field(None, alias="objectSuffix")
    allowed_content_types: Optional[list[str]] = Field(
        None, alias="allowedContentTypes"
    )
    minimum_size: int = Field(0, alias="minimumSize")
    maximum_size: Optional[int] = Field(None, alias="maximumSize")


class CrawlIngestionSettings(BaseModel):
    """Ingestion settings for a crawl plan."""

    ingestion_rule_id: Optional[str] = Field(None, alias="ingestionRuleId")
    store_in_s3: bool = Field(False, alias="storeInS3")
    s3_bucket_name: Optional[str] = Field(None, alias="s3BucketName")


class CrawlRepositorySettings(BaseModel):
    """Base repository settings for crawling."""

    repository_type: RepositoryType = Field(
        RepositoryType.WEB, alias="repositoryType"
    )


class WebCrawlRepositorySettings(CrawlRepositorySettings):
    """Web-specific repository settings for crawling."""

    authentication_type: WebAuthType = Field(
        WebAuthType.NONE, alias="authenticationType"
    )
    username: Optional[str] = None
    password: Optional[str] = None
    api_key_header: Optional[str] = Field(None, alias="apiKeyHeader")
    api_key_value: Optional[str] = Field(None, alias="apiKeyValue")
    bearer_token: Optional[str] = Field(None, alias="bearerToken")
    user_agent: Optional[str] = Field(None, alias="userAgent")
    start_url: Optional[str] = Field(None, alias="startUrl")
    use_headless_browser: bool = Field(False, alias="useHeadlessBrowser")
    follow_links: bool = Field(True, alias="followLinks")
    follow_redirects: bool = Field(True, alias="followRedirects")
    extract_sitemap_links: bool = Field(False, alias="extractSitemapLinks")
    restrict_to_child_urls: bool = Field(True, alias="restrictToChildUrls")
    restrict_to_subdomain: bool = Field(False, alias="restrictToSubdomain")
    restrict_to_root_domain: bool = Field(False, alias="restrictToRootDomain")
    ignore_robots_txt: bool = Field(False, alias="ignoreRobotsTxt")
    max_depth: int = Field(3, alias="maxDepth")
    max_parallel_tasks: int = Field(1, alias="maxParallelTasks")
    crawl_delay_ms: int = Field(1000, alias="crawlDelayMs")


class CrawlPlan(BaseModel):
    """A crawl plan defining how to crawl a repository."""

    id: Optional[str] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    name: Optional[str] = None
    repository_type: RepositoryType = Field(
        RepositoryType.WEB, alias="repositoryType"
    )
    ingestion_settings: Optional[CrawlIngestionSettings] = Field(
        None, alias="ingestionSettings"
    )
    repository_settings: Optional[CrawlRepositorySettings] = Field(
        None, alias="repositorySettings"
    )
    schedule: Optional[CrawlScheduleSettings] = None
    filter: Optional[CrawlFilterSettings] = None
    process_additions: bool = Field(True, alias="processAdditions")
    process_updates: bool = Field(True, alias="processUpdates")
    process_deletions: bool = Field(True, alias="processDeletions")
    max_drain_tasks: int = Field(1, alias="maxDrainTasks")
    retention_days: int = Field(30, alias="retentionDays")
    state: CrawlPlanState = Field(CrawlPlanState.STOPPED, alias="state")
    last_crawl_start_utc: Optional[datetime] = Field(None, alias="lastCrawlStartUtc")
    last_crawl_finish_utc: Optional[datetime] = Field(
        None, alias="lastCrawlFinishUtc"
    )
    last_crawl_success: Optional[bool] = Field(None, alias="lastCrawlSuccess")
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


class CrawlOperation(BaseModel):
    """A crawl operation instance."""

    id: Optional[str] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    crawl_plan_id: Optional[str] = Field(None, alias="crawlPlanId")
    state: CrawlOperationState = Field(
        CrawlOperationState.NOT_STARTED, alias="state"
    )
    status_message: Optional[str] = Field(None, alias="statusMessage")
    objects_enumerated: int = Field(0, alias="objectsEnumerated")
    bytes_enumerated: int = Field(0, alias="bytesEnumerated")
    objects_added: int = Field(0, alias="objectsAdded")
    bytes_added: int = Field(0, alias="bytesAdded")
    objects_updated: int = Field(0, alias="objectsUpdated")
    bytes_updated: int = Field(0, alias="bytesUpdated")
    objects_deleted: int = Field(0, alias="objectsDeleted")
    bytes_deleted: int = Field(0, alias="bytesDeleted")
    objects_success: int = Field(0, alias="objectsSuccess")
    bytes_success: int = Field(0, alias="bytesSuccess")
    objects_failed: int = Field(0, alias="objectsFailed")
    bytes_failed: int = Field(0, alias="bytesFailed")
    enumeration_file: Optional[str] = Field(None, alias="enumerationFile")
    start_utc: Optional[datetime] = Field(None, alias="startUtc")
    start_enumeration_utc: Optional[datetime] = Field(
        None, alias="startEnumerationUtc"
    )
    finish_enumeration_utc: Optional[datetime] = Field(
        None, alias="finishEnumerationUtc"
    )
    start_retrieval_utc: Optional[datetime] = Field(None, alias="startRetrievalUtc")
    finish_retrieval_utc: Optional[datetime] = Field(
        None, alias="finishRetrievalUtc"
    )
    finish_utc: Optional[datetime] = Field(None, alias="finishUtc")
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


# ---------------------------------------------------------------------------
# Eval
# ---------------------------------------------------------------------------


class EvalFact(BaseModel):
    """An evaluation fact for RAG testing."""

    id: Optional[str] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    assistant_id: Optional[str] = Field(None, alias="assistantId")
    category: Optional[str] = None
    question: Optional[str] = None
    expected_facts: Optional[str] = Field(None, alias="expectedFacts")
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


class EvalRunRequest(BaseModel):
    """Request to start an evaluation run."""

    assistant_id: Optional[str] = Field(None, alias="AssistantId")
    judge_prompt: Optional[str] = Field(None, alias="JudgePrompt")


class EvalRun(BaseModel):
    """An evaluation run."""

    id: Optional[str] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    assistant_id: Optional[str] = Field(None, alias="assistantId")
    status: EvalStatus = Field(EvalStatus.PENDING, alias="status")
    total_facts: int = Field(0, alias="totalFacts")
    facts_evaluated: int = Field(0, alias="factsEvaluated")
    facts_passed: int = Field(0, alias="factsPassed")
    facts_failed: int = Field(0, alias="factsFailed")
    pass_rate: float = Field(0.0, alias="passRate")
    judge_prompt: Optional[str] = Field(None, alias="judgePrompt")
    started_utc: Optional[datetime] = Field(None, alias="startedUtc")
    completed_utc: Optional[datetime] = Field(None, alias="completedUtc")
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")


class FactVerdict(BaseModel):
    """Verdict for a single fact in an evaluation."""

    fact: Optional[str] = None
    passed: bool = Field(False, alias="pass")
    reasoning: Optional[str] = None


class EvalResult(BaseModel):
    """Result of evaluating a single fact in a run."""

    id: Optional[str] = None
    run_id: Optional[str] = Field(None, alias="runId")
    fact_id: Optional[str] = Field(None, alias="factId")
    question: Optional[str] = None
    expected_facts: Optional[str] = Field(None, alias="expectedFacts")
    llm_response: Optional[str] = Field(None, alias="llmResponse")
    fact_verdicts: Optional[str] = Field(None, alias="factVerdicts")
    overall_pass: bool = Field(False, alias="overallPass")
    duration_ms: int = Field(0, alias="durationMs")
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")


# ---------------------------------------------------------------------------
# Enumeration (generic pagination)
# ---------------------------------------------------------------------------


class EnumerationQuery(BaseModel):
    """Query parameters for paginated enumeration."""

    max_results: int = Field(100, alias="maxResults")
    continuation_token: Optional[str] = Field(None, alias="continuationToken")
    ordering: EnumerationOrder = Field(
        EnumerationOrder.CREATED_DESCENDING, alias="ordering"
    )
    assistant_id_filter: Optional[str] = Field(None, alias="assistantIdFilter")
    bucket_name_filter: Optional[str] = Field(None, alias="bucketNameFilter")
    collection_id_filter: Optional[str] = Field(None, alias="collectionIdFilter")
    thread_id_filter: Optional[str] = Field(None, alias="threadIdFilter")


class EnumerationResult(BaseModel, Generic[T]):
    """Paginated enumeration result."""

    success: bool = True
    max_results: int = Field(100, alias="maxResults")
    total_records: int = Field(0, alias="totalRecords")
    records_remaining: int = Field(0, alias="recordsRemaining")
    continuation_token: Optional[str] = Field(None, alias="continuationToken")
    end_of_results: bool = Field(False, alias="endOfResults")
    objects: Optional[list[T]] = None
    total_ms: float = Field(0.0, alias="totalMs")


# ---------------------------------------------------------------------------
# Slack Verification
# ---------------------------------------------------------------------------


class SlackVerificationRequest(BaseModel):
    """Request to verify Slack connectivity."""

    enable_slack: bool = Field(False, alias="enableSlack")
    slack_app_token: Optional[str] = Field(None, alias="slackAppToken")
    slack_bot_token: Optional[str] = Field(None, alias="slackBotToken")
    slack_channel_id: Optional[str] = Field(None, alias="slackChannelId")
    slack_message_prefix: Optional[str] = Field(None, alias="slackMessagePrefix")


class SlackVerificationCheck(BaseModel):
    """Result of a single Slack verification check."""

    success: bool = False
    message: Optional[str] = None
    details: Optional[Any] = None


class SlackVerificationResponse(BaseModel):
    """Response from Slack connectivity verification."""

    success: bool = False
    bot_token: Optional[SlackVerificationCheck] = Field(None, alias="botToken")
    channel: Optional[SlackVerificationCheck] = None
    socket_mode: Optional[SlackVerificationCheck] = Field(None, alias="socketMode")


class CollectionRecord(BaseModel):
    """A flexible record stored in a collection."""

    model_config = ConfigDict(extra="allow")

    id: Optional[str] = Field(None, alias="Id")


class BucketCreateRequest(BaseModel):
    """Request to create a bucket."""

    name: str = Field(alias="Name")


# ---------------------------------------------------------------------------
# Misc
# ---------------------------------------------------------------------------


class IdentifierResponse(BaseModel):
    """Response containing an identifier."""

    id: Optional[str] = None
    guid: Optional[str] = Field(None, alias="GUID")


class RetrievalSearchOptions(BaseModel):
    """Options for retrieval search."""

    search_mode: Optional[str] = Field(None, alias="searchMode")
    text_weight: float = Field(0.5, alias="textWeight")
    full_text_search_type: Optional[str] = Field(None, alias="fullTextSearchType")
    full_text_language: Optional[str] = Field(None, alias="fullTextLanguage")
    full_text_normalization: int = Field(0, alias="fullTextNormalization")
    full_text_minimum_score: Optional[float] = Field(
        None, alias="fullTextMinimumScore"
    )
    include_neighbors: int = Field(0, alias="includeNeighbors")
    metadata_filter: Optional[ChatMetadataFilter] = Field(
        None, alias="metadataFilter"
    )


# Enable forward reference resolution for self-referencing models
RetrievalChunk.model_rebuild()

# Enable forward reference resolution for AuthenticateResult
AuthenticateResult.model_rebuild()
