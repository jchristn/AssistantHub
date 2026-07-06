"""Pydantic v2 models for the AssistantHub API."""

from __future__ import annotations

from datetime import datetime
from typing import Any, Generic, Optional, TypeVar

from pydantic import AliasChoices, BaseModel, ConfigDict, Field, field_validator

from .enums import (
    ApiError,
    CrawlOperationState,
    CrawlPlanState,
    DocumentStatus,
    EnumerationOrder,
    EvalStatus,
    FeedbackRating,
    NfsVersion,
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
    load_models_on_chat_open: bool = Field(False, alias="LoadModelsOnChatOpen")
    expose_thinking: bool = Field(False, alias="ExposeThinking")
    enable_document_attachments: bool = Field(False, alias="EnableDocumentAttachments")
    document_attachment_max_count: int = Field(10, alias="DocumentAttachmentMaxCount")
    expose_document_source_urls: bool = Field(False, alias="ExposeDocumentSourceUrls")


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
    enable_document_attachments: bool = Field(False, alias="enableDocumentAttachments")
    document_attachment_max_count: int = Field(10, alias="documentAttachmentMaxCount")
    expose_document_source_urls: bool = Field(False, alias="exposeDocumentSourceUrls")
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
    inference_endpoint_id: Optional[str] = Field(
        None,
        alias="inferenceEndpointId",
        validation_alias=AliasChoices("InferenceEndpointId", "inferenceEndpointId", "inference_endpoint_id"),
    )
    tool_routing_inference_endpoint_id: Optional[str] = Field(
        None,
        alias="toolRoutingInferenceEndpointId",
        validation_alias=AliasChoices("ToolRoutingInferenceEndpointId", "toolRoutingInferenceEndpointId", "tool_routing_inference_endpoint_id"),
    )
    retrieval_gate_inference_endpoint_id: Optional[str] = Field(
        None,
        alias="retrievalGateInferenceEndpointId",
        validation_alias=AliasChoices("RetrievalGateInferenceEndpointId", "retrievalGateInferenceEndpointId", "retrieval_gate_inference_endpoint_id"),
    )
    query_rewrite_inference_endpoint_id: Optional[str] = Field(
        None,
        alias="queryRewriteInferenceEndpointId",
        validation_alias=AliasChoices("QueryRewriteInferenceEndpointId", "queryRewriteInferenceEndpointId", "query_rewrite_inference_endpoint_id"),
    )
    rerank_inference_endpoint_id: Optional[str] = Field(
        None,
        alias="rerankInferenceEndpointId",
        validation_alias=AliasChoices("RerankInferenceEndpointId", "rerankInferenceEndpointId", "rerank_inference_endpoint_id"),
    )
    enable_answerability_check: bool = Field(
        False,
        alias="enableAnswerabilityCheck",
        validation_alias=AliasChoices("EnableAnswerabilityCheck", "enableAnswerabilityCheck", "enable_answerability_check"),
    )
    answerability_inference_endpoint_id: Optional[str] = Field(
        None,
        alias="answerabilityInferenceEndpointId",
        validation_alias=AliasChoices("AnswerabilityInferenceEndpointId", "answerabilityInferenceEndpointId", "answerability_inference_endpoint_id"),
    )
    answerability_mode: Optional[str] = Field(
        None,
        alias="answerabilityMode",
        validation_alias=AliasChoices("AnswerabilityMode", "answerabilityMode", "answerability_mode"),
    )
    answerability_prompt: Optional[str] = Field(
        None,
        alias="answerabilityPrompt",
        validation_alias=AliasChoices("AnswerabilityPrompt", "answerabilityPrompt", "answerability_prompt"),
    )
    embedding_endpoint_id: Optional[str] = Field(
        None,
        alias="embeddingEndpointId",
        validation_alias=AliasChoices("EmbeddingEndpointId", "embeddingEndpointId", "embedding_endpoint_id"),
    )
    load_models_on_chat_open: bool = Field(False, alias="loadModelsOnChatOpen")
    expose_thinking: bool = Field(
        False,
        alias="exposeThinking",
        validation_alias=AliasChoices("ExposeThinking", "exposeThinking", "expose_thinking"),
    )
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
    tool_policy_json: Optional[str] = Field(
        None,
        alias="ToolPolicyJson",
        validation_alias=AliasChoices("ToolPolicyJson", "toolPolicyJson", "tool_policy_json"),
    )
    tool_policy: Optional[AssistantToolPolicy] = Field(
        None,
        alias="ToolPolicy",
        validation_alias=AliasChoices("ToolPolicy", "toolPolicy", "tool_policy"),
    )
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


class AssistantToolPolicy(BaseModel):
    """Administrator-controlled policy for server-side tools exposed to a model."""

    model_config = ConfigDict(populate_by_name=True)

    enable_tool_calls: bool = Field(False, alias="EnableToolCalls", validation_alias=AliasChoices("EnableToolCalls", "enableToolCalls", "enable_tool_calls"))
    max_tool_iterations: int = Field(6, alias="MaxToolIterations", validation_alias=AliasChoices("MaxToolIterations", "maxToolIterations", "max_tool_iterations"))
    max_tool_calls_per_turn: int = Field(12, alias="MaxToolCallsPerTurn", validation_alias=AliasChoices("MaxToolCallsPerTurn", "maxToolCallsPerTurn", "max_tool_calls_per_turn"))
    tool_choice_mode: str = Field("Auto", alias="ToolChoiceMode", validation_alias=AliasChoices("ToolChoiceMode", "toolChoiceMode", "tool_choice_mode"))
    max_parallel_tool_calls: int = Field(1, alias="MaxParallelToolCalls", validation_alias=AliasChoices("MaxParallelToolCalls", "maxParallelToolCalls", "max_parallel_tool_calls"))
    allow_parallel_tool_calls: bool = Field(False, alias="AllowParallelToolCalls", validation_alias=AliasChoices("AllowParallelToolCalls", "allowParallelToolCalls", "allow_parallel_tool_calls"))
    tool_call_timeout_ms: int = Field(30000, alias="ToolCallTimeoutMs", validation_alias=AliasChoices("ToolCallTimeoutMs", "toolCallTimeoutMs", "tool_call_timeout_ms"))
    max_tool_output_chars: int = Field(12000, alias="MaxToolOutputChars", validation_alias=AliasChoices("MaxToolOutputChars", "maxToolOutputChars", "max_tool_output_chars"))
    max_tool_output_characters_per_turn: int = Field(50000, alias="MaxToolOutputCharactersPerTurn", validation_alias=AliasChoices("MaxToolOutputCharactersPerTurn", "maxToolOutputCharactersPerTurn", "max_tool_output_characters_per_turn"))
    max_tool_result_items: int = Field(20, alias="MaxToolResultItems", validation_alias=AliasChoices("MaxToolResultItems", "maxToolResultItems", "max_tool_result_items"))
    expose_tool_trace_to_user: bool = Field(False, alias="ExposeToolTraceToUser", validation_alias=AliasChoices("ExposeToolTraceToUser", "exposeToolTraceToUser", "expose_tool_trace_to_user"))
    persist_tool_arguments: bool = Field(True, alias="PersistToolArguments", validation_alias=AliasChoices("PersistToolArguments", "persistToolArguments", "persist_tool_arguments"))
    persist_tool_outputs: bool = Field(False, alias="PersistToolOutputs", validation_alias=AliasChoices("PersistToolOutputs", "persistToolOutputs", "persist_tool_outputs"))
    require_citations_for_tool_evidence: bool = Field(True, alias="RequireCitationsForToolEvidence", validation_alias=AliasChoices("RequireCitationsForToolEvidence", "requireCitationsForToolEvidence", "require_citations_for_tool_evidence"))
    allowed_tool_names: list[str] = Field(default_factory=list, alias="AllowedToolNames", validation_alias=AliasChoices("AllowedToolNames", "allowedToolNames", "allowed_tool_names"))
    enable_tool_feedback_events: bool = Field(True, alias="EnableToolFeedbackEvents", validation_alias=AliasChoices("EnableToolFeedbackEvents", "enableToolFeedbackEvents", "enable_tool_feedback_events"))
    enable_slack_tool_progress_messages: bool = Field(True, alias="EnableSlackToolProgressMessages", validation_alias=AliasChoices("EnableSlackToolProgressMessages", "enableSlackToolProgressMessages", "enable_slack_tool_progress_messages"))
    enable_collection_search_tool: bool = Field(False, alias="EnableCollectionSearchTool", validation_alias=AliasChoices("EnableCollectionSearchTool", "enableCollectionSearchTool", "enable_collection_search_tool"))
    enable_collection_read_chunks_tool: bool = Field(False, alias="EnableCollectionReadChunksTool", validation_alias=AliasChoices("EnableCollectionReadChunksTool", "enableCollectionReadChunksTool", "enable_collection_read_chunks_tool"))
    enable_verbex_full_text_search_tool: bool = Field(False, alias="EnableVerbexFullTextSearchTool", validation_alias=AliasChoices("EnableVerbexFullTextSearchTool", "enableVerbexFullTextSearchTool", "enable_verbex_full_text_search_tool"))
    enable_s3_object_read_tool: bool = Field(False, alias="EnableS3ObjectReadTool", validation_alias=AliasChoices("EnableS3ObjectReadTool", "enableS3ObjectReadTool", "enable_s3_object_read_tool"))
    enable_document_atom_extraction_tool: bool = Field(False, alias="EnableDocumentAtomExtractionTool", validation_alias=AliasChoices("EnableDocumentAtomExtractionTool", "enableDocumentAtomExtractionTool", "enable_document_atom_extraction_tool"))
    enable_collection_enumerate_documents_tool: bool = Field(False, alias="EnableCollectionEnumerateDocumentsTool", validation_alias=AliasChoices("EnableCollectionEnumerateDocumentsTool", "enableCollectionEnumerateDocumentsTool", "enable_collection_enumerate_documents_tool"))
    enable_collection_enumeration_tool: bool = Field(False, alias="EnableCollectionEnumerationTool", validation_alias=AliasChoices("EnableCollectionEnumerationTool", "enableCollectionEnumerationTool", "enable_collection_enumeration_tool"))
    enable_index_enumerate_records_tool: bool = Field(False, alias="EnableIndexEnumerateRecordsTool", validation_alias=AliasChoices("EnableIndexEnumerateRecordsTool", "enableIndexEnumerateRecordsTool", "enable_index_enumerate_records_tool"))
    enable_bucket_enumerate_objects_tool: bool = Field(False, alias="EnableBucketEnumerateObjectsTool", validation_alias=AliasChoices("EnableBucketEnumerateObjectsTool", "enableBucketEnumerateObjectsTool", "enable_bucket_enumerate_objects_tool"))
    enable_web_search_tool: bool = Field(False, alias="EnableWebSearchTool", validation_alias=AliasChoices("EnableWebSearchTool", "enableWebSearchTool", "enable_web_search_tool"))
    tavily_endpoint: Optional[str] = Field(None, alias="TavilyEndpoint", validation_alias=AliasChoices("TavilyEndpoint", "tavilyEndpoint", "tavily_endpoint"))
    tavily_api_key: Optional[str] = Field(None, alias="TavilyApiKey", validation_alias=AliasChoices("TavilyApiKey", "tavilyApiKey", "tavily_api_key"))
    max_search_results_per_call: int = Field(10, alias="MaxSearchResultsPerCall", validation_alias=AliasChoices("MaxSearchResultsPerCall", "maxSearchResultsPerCall", "max_search_results_per_call"))
    max_search_top_k: int = Field(50, alias="MaxSearchTopK", validation_alias=AliasChoices("MaxSearchTopK", "maxSearchTopK", "max_search_top_k"))
    max_search_queries_per_call: int = Field(3, alias="MaxSearchQueriesPerCall", validation_alias=AliasChoices("MaxSearchQueriesPerCall", "maxSearchQueriesPerCall", "max_search_queries_per_call"))
    max_documents_considered_per_search: int = Field(1000, alias="MaxDocumentsConsideredPerSearch", validation_alias=AliasChoices("MaxDocumentsConsideredPerSearch", "maxDocumentsConsideredPerSearch", "max_documents_considered_per_search"))
    max_results_considered_per_search: int = Field(1000, alias="MaxResultsConsideredPerSearch", validation_alias=AliasChoices("MaxResultsConsideredPerSearch", "maxResultsConsideredPerSearch", "max_results_considered_per_search"))
    enable_server_generated_query_variants: bool = Field(False, alias="EnableServerGeneratedQueryVariants", validation_alias=AliasChoices("EnableServerGeneratedQueryVariants", "enableServerGeneratedQueryVariants", "enable_server_generated_query_variants"))
    max_chunks_per_read: int = Field(20, alias="MaxChunksPerRead", validation_alias=AliasChoices("MaxChunksPerRead", "maxChunksPerRead", "max_chunks_per_read"))
    max_read_ranges_per_call: int = Field(5, alias="MaxReadRangesPerCall", validation_alias=AliasChoices("MaxReadRangesPerCall", "maxReadRangesPerCall", "max_read_ranges_per_call"))
    max_neighbor_window: int = Field(2, alias="MaxNeighborWindow", validation_alias=AliasChoices("MaxNeighborWindow", "maxNeighborWindow", "max_neighbor_window"))
    allowed_search_modes: list[str] = Field(default_factory=lambda: ["Vector", "FullText", "Hybrid"], alias="AllowedSearchModes", validation_alias=AliasChoices("AllowedSearchModes", "allowedSearchModes", "allowed_search_modes"))
    default_search_mode: Optional[str] = Field(None, alias="DefaultSearchMode", validation_alias=AliasChoices("DefaultSearchMode", "defaultSearchMode", "default_search_mode"))
    allow_model_document_id_filter: bool = Field(True, alias="AllowModelDocumentIdFilter", validation_alias=AliasChoices("AllowModelDocumentIdFilter", "allowModelDocumentIdFilter", "allow_model_document_id_filter"))
    return_labels: bool = Field(False, alias="ReturnLabels", validation_alias=AliasChoices("ReturnLabels", "returnLabels", "return_labels"))
    return_tags: bool = Field(False, alias="ReturnTags", validation_alias=AliasChoices("ReturnTags", "returnTags", "return_tags"))
    return_full_search_content: bool = Field(False, alias="ReturnFullSearchContent", validation_alias=AliasChoices("ReturnFullSearchContent", "returnFullSearchContent", "return_full_search_content"))
    allow_non_completed_document_metadata: bool = Field(False, alias="AllowNonCompletedDocumentMetadata", validation_alias=AliasChoices("AllowNonCompletedDocumentMetadata", "allowNonCompletedDocumentMetadata", "allow_non_completed_document_metadata"))
    enable_verbex_search_tool: bool = Field(False, alias="EnableVerbexSearchTool", validation_alias=AliasChoices("EnableVerbexSearchTool", "enableVerbexSearchTool", "enable_verbex_search_tool"))
    enable_index_enumeration_tool: bool = Field(False, alias="EnableIndexEnumerationTool", validation_alias=AliasChoices("EnableIndexEnumerationTool", "enableIndexEnumerationTool", "enable_index_enumeration_tool"))
    default_index_id: Optional[str] = Field(None, alias="DefaultIndexId", validation_alias=AliasChoices("DefaultIndexId", "defaultIndexId", "default_index_id"))
    max_verbex_results: int = Field(20, alias="MaxVerbexResults", validation_alias=AliasChoices("MaxVerbexResults", "maxVerbexResults", "max_verbex_results"))
    allow_raw_index_records: bool = Field(False, alias="AllowRawIndexRecords", validation_alias=AliasChoices("AllowRawIndexRecords", "allowRawIndexRecords", "allow_raw_index_records"))
    require_document_mapping: bool = Field(True, alias="RequireDocumentMapping", validation_alias=AliasChoices("RequireDocumentMapping", "requireDocumentMapping", "require_document_mapping"))
    return_verbex_record_metadata: bool = Field(False, alias="ReturnVerbexRecordMetadata", validation_alias=AliasChoices("ReturnVerbexRecordMetadata", "returnVerbexRecordMetadata", "return_verbex_record_metadata"))
    max_object_read_bytes: int = Field(131072, alias="MaxObjectReadBytes", validation_alias=AliasChoices("MaxObjectReadBytes", "maxObjectReadBytes", "max_object_read_bytes"))
    max_atom_extraction_bytes: int = Field(10485760, alias="MaxAtomExtractionBytes", validation_alias=AliasChoices("MaxAtomExtractionBytes", "maxAtomExtractionBytes", "max_atom_extraction_bytes"))
    max_atom_extraction_characters: int = Field(50000, alias="MaxAtomExtractionCharacters", validation_alias=AliasChoices("MaxAtomExtractionCharacters", "maxAtomExtractionCharacters", "max_atom_extraction_characters"))
    max_object_bytes_per_turn: int = Field(524288, alias="MaxObjectBytesPerTurn", validation_alias=AliasChoices("MaxObjectBytesPerTurn", "maxObjectBytesPerTurn", "max_object_bytes_per_turn"))
    max_bucket_enumeration_results: int = Field(50, alias="MaxBucketEnumerationResults", validation_alias=AliasChoices("MaxBucketEnumerationResults", "maxBucketEnumerationResults", "max_bucket_enumeration_results"))
    allow_bucket_wide_object_read: bool = Field(False, alias="AllowBucketWideObjectRead", validation_alias=AliasChoices("AllowBucketWideObjectRead", "allowBucketWideObjectRead", "allow_bucket_wide_object_read"))
    document_backed_objects_only: bool = Field(True, alias="DocumentBackedObjectsOnly", validation_alias=AliasChoices("DocumentBackedObjectsOnly", "documentBackedObjectsOnly", "document_backed_objects_only"))
    redact_object_keys: bool = Field(True, alias="RedactObjectKeys", validation_alias=AliasChoices("RedactObjectKeys", "redactObjectKeys", "redact_object_keys"))
    allow_binary_object_output: bool = Field(False, alias="AllowBinaryObjectOutput", validation_alias=AliasChoices("AllowBinaryObjectOutput", "allowBinaryObjectOutput", "allow_binary_object_output"))
    allow_raw_web_content: bool = Field(False, alias="AllowRawWebContent", validation_alias=AliasChoices("AllowRawWebContent", "allowRawWebContent", "allow_raw_web_content"))
    allow_web_images: bool = Field(False, alias="AllowWebImages", validation_alias=AliasChoices("AllowWebImages", "allowWebImages", "allow_web_images"))
    allow_ungoverned_web_access: bool = Field(False, alias="AllowUngovernedWebAccess", validation_alias=AliasChoices("AllowUngovernedWebAccess", "allowUngovernedWebAccess", "allow_ungoverned_web_access"))
    allow_document_source_urls: bool = Field(False, alias="AllowDocumentSourceUrls", validation_alias=AliasChoices("AllowDocumentSourceUrls", "allowDocumentSourceUrls", "allow_document_source_urls"))
    allow_document_metadata_details: bool = Field(False, alias="AllowDocumentMetadataDetails", validation_alias=AliasChoices("AllowDocumentMetadataDetails", "allowDocumentMetadataDetails", "allow_document_metadata_details"))
    allowed_verbex_index_ids: list[str] = Field(default_factory=list, alias="AllowedVerbexIndexIds", validation_alias=AliasChoices("AllowedVerbexIndexIds", "allowedVerbexIndexIds", "allowed_verbex_index_ids"))
    allowed_bucket_names: list[str] = Field(default_factory=list, alias="AllowedBucketNames", validation_alias=AliasChoices("AllowedBucketNames", "allowedBucketNames", "allowed_bucket_names"))
    allowed_bucket_prefixes: list[str] = Field(default_factory=list, alias="AllowedBucketPrefixes", validation_alias=AliasChoices("AllowedBucketPrefixes", "allowedBucketPrefixes", "allowed_bucket_prefixes"))
    allowed_object_suffixes: list[str] = Field(default_factory=list, alias="AllowedObjectSuffixes", validation_alias=AliasChoices("AllowedObjectSuffixes", "allowedObjectSuffixes", "allowed_object_suffixes"))
    allowed_content_types: list[str] = Field(default_factory=list, alias="AllowedContentTypes", validation_alias=AliasChoices("AllowedContentTypes", "allowedContentTypes", "allowed_content_types"))
    allowed_web_domains: list[str] = Field(default_factory=list, alias="AllowedWebDomains", validation_alias=AliasChoices("AllowedWebDomains", "allowedWebDomains", "allowed_web_domains"))
    blocked_web_domains: list[str] = Field(default_factory=list, alias="BlockedWebDomains", validation_alias=AliasChoices("BlockedWebDomains", "blockedWebDomains", "blocked_web_domains"))
    allowed_providers: list[str] = Field(default_factory=list, alias="AllowedProviders", validation_alias=AliasChoices("AllowedProviders", "allowedProviders", "allowed_providers"))
    max_web_results: int = Field(5, alias="MaxWebResults", validation_alias=AliasChoices("MaxWebResults", "maxWebResults", "max_web_results"))
    search_depth: str = Field("basic", alias="SearchDepth", validation_alias=AliasChoices("SearchDepth", "searchDepth", "search_depth"))
    allow_advanced_search_depth: bool = Field(False, alias="AllowAdvancedSearchDepth", validation_alias=AliasChoices("AllowAdvancedSearchDepth", "allowAdvancedSearchDepth", "allow_advanced_search_depth"))
    allow_news_topic: bool = Field(True, alias="AllowNewsTopic", validation_alias=AliasChoices("AllowNewsTopic", "allowNewsTopic", "allow_news_topic"))
    require_safe_search: bool = Field(True, alias="RequireSafeSearch", validation_alias=AliasChoices("RequireSafeSearch", "requireSafeSearch", "require_safe_search"))
    max_web_searches_per_turn: int = Field(3, alias="MaxWebSearchesPerTurn", validation_alias=AliasChoices("MaxWebSearchesPerTurn", "maxWebSearchesPerTurn", "max_web_searches_per_turn"))


class AssistantToolDescriptor(BaseModel):
    """Effective server-side tool availability for an assistant."""

    tool_name: Optional[str] = Field(None, alias="ToolName")
    display_name: Optional[str] = Field(None, alias="DisplayName")
    category: Optional[str] = Field(None, alias="Category")
    enabled_by_policy: bool = Field(False, alias="EnabledByPolicy")
    available: bool = Field(False, alias="Available")
    unavailable_reason: Optional[str] = Field(None, alias="UnavailableReason")


class AssistantToolCallRecord(BaseModel):
    """Redacted persistent trace for one model-directed assistant tool call."""

    id: Optional[str] = Field(None, alias="Id", validation_alias=AliasChoices("Id", "id"))
    tenant_id: Optional[str] = Field(None, alias="TenantId", validation_alias=AliasChoices("TenantId", "tenantId", "tenant_id"))
    assistant_id: Optional[str] = Field(None, alias="AssistantId", validation_alias=AliasChoices("AssistantId", "assistantId", "assistant_id"))
    chat_history_id: Optional[str] = Field(None, alias="ChatHistoryId", validation_alias=AliasChoices("ChatHistoryId", "chatHistoryId", "chat_history_id"))
    request_history_id: Optional[str] = Field(None, alias="RequestHistoryId", validation_alias=AliasChoices("RequestHistoryId", "requestHistoryId", "request_history_id"))
    trace_id: Optional[str] = Field(None, alias="TraceId", validation_alias=AliasChoices("TraceId", "traceId", "trace_id"))
    thread_id: Optional[str] = Field(None, alias="ThreadId", validation_alias=AliasChoices("ThreadId", "threadId", "thread_id"))
    origin: Optional[str] = Field(None, alias="Origin", validation_alias=AliasChoices("Origin", "origin"))
    turn_index: int = Field(0, alias="TurnIndex", validation_alias=AliasChoices("TurnIndex", "turnIndex", "turn_index"))
    iteration: int = Field(0, alias="Iteration", validation_alias=AliasChoices("Iteration", "iteration"))
    sequence_number: int = Field(0, alias="SequenceNumber", validation_alias=AliasChoices("SequenceNumber", "sequenceNumber", "sequence_number"))
    provider_tool_call_id: Optional[str] = Field(None, alias="ProviderToolCallId", validation_alias=AliasChoices("ProviderToolCallId", "providerToolCallId", "provider_tool_call_id"))
    tool_name: Optional[str] = Field(None, alias="ToolName", validation_alias=AliasChoices("ToolName", "toolName", "tool_name"))
    arguments_json: Optional[str] = Field(None, alias="ArgumentsJson", validation_alias=AliasChoices("ArgumentsJson", "argumentsJson", "arguments_json"))
    output_json: Optional[str] = Field(None, alias="OutputJson", validation_alias=AliasChoices("OutputJson", "outputJson", "output_json"))
    result_summary_json: Optional[str] = Field(None, alias="ResultSummaryJson", validation_alias=AliasChoices("ResultSummaryJson", "resultSummaryJson", "result_summary_json"))
    success: bool = Field(False, alias="Success", validation_alias=AliasChoices("Success", "success"))
    denied: bool = Field(False, alias="Denied", validation_alias=AliasChoices("Denied", "denied"))
    truncated: bool = Field(False, alias="Truncated", validation_alias=AliasChoices("Truncated", "truncated"))
    output_characters: int = Field(0, alias="OutputCharacters", validation_alias=AliasChoices("OutputCharacters", "outputCharacters", "output_characters"))
    input_bytes: int = Field(0, alias="InputBytes", validation_alias=AliasChoices("InputBytes", "inputBytes", "input_bytes"))
    output_bytes: int = Field(0, alias="OutputBytes", validation_alias=AliasChoices("OutputBytes", "outputBytes", "output_bytes"))
    duration_ms: float = Field(0.0, alias="DurationMs", validation_alias=AliasChoices("DurationMs", "durationMs", "duration_ms"))
    error_type: Optional[str] = Field(None, alias="ErrorType", validation_alias=AliasChoices("ErrorType", "errorType", "error_type"))
    error_message: Optional[str] = Field(None, alias="ErrorMessage", validation_alias=AliasChoices("ErrorMessage", "errorMessage", "error_message"))
    provider: Optional[str] = Field(None, alias="Provider", validation_alias=AliasChoices("Provider", "provider"))
    model: Optional[str] = Field(None, alias="Model", validation_alias=AliasChoices("Model", "model"))
    active: bool = Field(True, alias="Active", validation_alias=AliasChoices("Active", "active"))
    started_utc: Optional[datetime] = Field(None, alias="StartedUtc", validation_alias=AliasChoices("StartedUtc", "startedUtc", "started_utc"))
    finished_utc: Optional[datetime] = Field(None, alias="FinishedUtc", validation_alias=AliasChoices("FinishedUtc", "finishedUtc", "finished_utc"))
    created_utc: Optional[datetime] = Field(None, alias="CreatedUtc", validation_alias=AliasChoices("CreatedUtc", "createdUtc", "created_utc"))
    last_update_utc: Optional[datetime] = Field(None, alias="LastUpdateUtc", validation_alias=AliasChoices("LastUpdateUtc", "lastUpdateUtc", "last_update_utc"))


class AssistantToolPolicyValidationRequest(BaseModel):
    """Request to validate an assistant tool policy without persisting it."""

    tool_policy_json: Optional[str] = Field(
        None,
        alias="ToolPolicyJson",
        validation_alias=AliasChoices("ToolPolicyJson", "toolPolicyJson", "tool_policy_json"),
    )
    tool_policy: Optional[AssistantToolPolicy] = Field(
        None,
        alias="ToolPolicy",
        validation_alias=AliasChoices("ToolPolicy", "toolPolicy", "tool_policy"),
    )


class AssistantToolPolicyValidationResult(BaseModel):
    """Result of validating an assistant tool policy."""

    success: bool = Field(False, alias="Success")
    message: Optional[str] = Field(None, alias="Message")
    tool_policy_json: Optional[str] = Field(
        None,
        alias="ToolPolicyJson",
        validation_alias=AliasChoices("ToolPolicyJson", "toolPolicyJson", "tool_policy_json"),
    )
    tool_policy: Optional[AssistantToolPolicy] = Field(
        None,
        alias="ToolPolicy",
        validation_alias=AliasChoices("ToolPolicy", "toolPolicy", "tool_policy"),
    )
    tools: list[AssistantToolDescriptor] = Field(default_factory=list, alias="Tools")
    errors: list[str] = Field(default_factory=list, alias="Errors")
    error_codes: list[str] = Field(
        default_factory=list,
        alias="ErrorCodes",
        validation_alias=AliasChoices("ErrorCodes", "errorCodes", "error_codes"),
    )


class AssistantToolPolicyTestResult(BaseModel):
    """Result of an administrator dry-run diagnostic for assistant tool policy."""

    success: bool = Field(False, alias="Success")
    message: Optional[str] = Field(None, alias="Message")
    assistant_id: Optional[str] = Field(
        None,
        alias="AssistantId",
        validation_alias=AliasChoices("AssistantId", "assistantId", "assistant_id"),
    )
    inference_endpoint_id: Optional[str] = Field(
        None,
        alias="InferenceEndpointId",
        validation_alias=AliasChoices("InferenceEndpointId", "inferenceEndpointId", "inference_endpoint_id"),
    )
    tool_routing_inference_endpoint_id: Optional[str] = Field(
        None,
        alias="ToolRoutingInferenceEndpointId",
        validation_alias=AliasChoices("ToolRoutingInferenceEndpointId", "toolRoutingInferenceEndpointId", "tool_routing_inference_endpoint_id"),
    )
    effective_tool_routing_inference_endpoint_id: Optional[str] = Field(
        None,
        alias="EffectiveToolRoutingInferenceEndpointId",
        validation_alias=AliasChoices("EffectiveToolRoutingInferenceEndpointId", "effectiveToolRoutingInferenceEndpointId", "effective_tool_routing_inference_endpoint_id"),
    )
    endpoint_resolved: bool = Field(False, alias="EndpointResolved")
    endpoint_model: Optional[str] = Field(None, alias="EndpointModel")
    endpoint_api_format: Optional[str] = Field(None, alias="EndpointApiFormat")
    endpoint_active: bool = Field(False, alias="EndpointActive")
    endpoint_supports_tool_calling: bool = Field(False, alias="EndpointSupportsToolCalling")
    endpoint_tool_calling_api_format: Optional[str] = Field(None, alias="EndpointToolCallingApiFormat")
    endpoint_supports_parallel_tool_calls: bool = Field(False, alias="EndpointSupportsParallelToolCalls")
    endpoint_supports_streaming_tool_calls: bool = Field(False, alias="EndpointSupportsStreamingToolCalls")
    validation: Optional[AssistantToolPolicyValidationResult] = Field(None, alias="Validation")
    tools: list[AssistantToolDescriptor] = Field(default_factory=list, alias="Tools")
    warnings: list[str] = Field(default_factory=list, alias="Warnings")
    errors: list[str] = Field(default_factory=list, alias="Errors")
    error_codes: list[str] = Field(
        default_factory=list,
        alias="ErrorCodes",
        validation_alias=AliasChoices("ErrorCodes", "errorCodes", "error_codes"),
    )


class AssistantChatOpenModelLoadResult(BaseModel):
    """Result of loading one configured assistant endpoint model during chat open."""

    endpoint_type: Optional[str] = Field(None, alias="endpointType")
    success: bool = False
    status_code: int = Field(0, alias="statusCode")


class AssistantChatOpenResult(BaseModel):
    """Result returned when a chat window is opened for an assistant."""

    success: bool = False
    enabled: bool = False
    loaded: bool = False
    completion_endpoint_count: int = Field(0, alias="completionEndpointCount")
    embedding_endpoint_count: int = Field(0, alias="embeddingEndpointCount")
    results: Optional[list[AssistantChatOpenModelLoadResult]] = None


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
    verbex_tenant_id: Optional[str] = Field(None, alias="verbexTenantId")
    verbex_index_id: Optional[str] = Field(None, alias="verbexIndexId")
    verbex_record_id: Optional[str] = Field(None, alias="verbexRecordId")
    labels: Optional[str] = None
    tags: Optional[str] = None
    chunk_record_ids: Optional[str] = Field(None, alias="chunkRecordIds")
    crawl_plan_id: Optional[str] = Field(None, alias="crawlPlanId")
    crawl_operation_id: Optional[str] = Field(None, alias="crawlOperationId")
    source_url: Optional[str] = Field(None, alias="sourceUrl")
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="lastUpdateUtc")


class AssistantDocumentSelectionItem(BaseModel):
    """Safe public metadata for documents selectable in assistant chat."""

    id: Optional[str] = Field(None, alias="Id")
    name: Optional[str] = Field(None, alias="Name")
    original_filename: Optional[str] = Field(None, alias="OriginalFilename")
    content_type: Optional[str] = Field(None, alias="ContentType")
    size_bytes: int = Field(0, alias="SizeBytes")
    source_url: Optional[str] = Field(None, alias="SourceUrl")
    created_utc: Optional[datetime] = Field(None, alias="CreatedUtc")
    last_update_utc: Optional[datetime] = Field(None, alias="LastUpdateUtc")


class DocumentReindexRequest(BaseModel):
    """Request body for document Verbex reindex operations."""

    document_ids: Optional[list[str]] = Field(None, alias="DocumentIds")
    include_already_indexed: bool = Field(False, alias="IncludeAlreadyIndexed")


class DocumentReindexResult(BaseModel):
    """Result for a single document Verbex reindex operation."""

    document_id: Optional[str] = Field(None, alias="DocumentId")
    success: bool = Field(False, alias="Success")
    status: Optional[str] = Field(None, alias="Status")
    message: Optional[str] = Field(None, alias="Message")
    verbex_tenant_id: Optional[str] = Field(None, alias="VerbexTenantId")
    verbex_index_id: Optional[str] = Field(None, alias="VerbexIndexId")
    verbex_record_id: Optional[str] = Field(None, alias="VerbexRecordId")
    total_ms: float = Field(0.0, alias="TotalMs")


class DocumentReindexBatchResult(BaseModel):
    """Result for a batch document Verbex reindex operation."""

    requested: int = Field(0, alias="Requested")
    eligible: int = Field(0, alias="Eligible")
    reindexed: int = Field(0, alias="Reindexed")
    skipped: int = Field(0, alias="Skipped")
    failed: int = Field(0, alias="Failed")
    continuation_token: Optional[str] = Field(None, alias="ContinuationToken")
    end_of_results: bool = Field(True, alias="EndOfResults")
    results: list[DocumentReindexResult] = Field(default_factory=list, alias="Results")
    total_ms: float = Field(0.0, alias="TotalMs")


# ---------------------------------------------------------------------------
# Chat
# ---------------------------------------------------------------------------


class ChatCompletionMessage(BaseModel):
    """A single message in a chat conversation."""

    role: Optional[str] = None
    content: Optional[str] = None
    thinking: Optional[str] = None


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


class ChatLocalAttachment(BaseModel):
    """User-supplied file attachment for a single chat turn."""

    name: Optional[str] = None
    content_type: Optional[str] = Field(None, alias="content_type")
    base64_content: Optional[str] = Field(None, alias="base64_content")
    text: Optional[str] = None


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
    attached_document_ids: Optional[list[str]] = Field(
        None, alias="attached_document_ids"
    )
    local_attachments: Optional[list[ChatLocalAttachment]] = Field(
        None, alias="local_attachments"
    )


class ChatCompletionPromptTokensDetails(BaseModel):
    """Provider-specific prompt token details."""

    cached_tokens: int = Field(0, alias="cached_tokens")
    audio_tokens: int = Field(0, alias="audio_tokens")
    tool_definition_tokens: int = Field(0, alias="tool_definition_tokens")
    tool_tokens: int = Field(0, alias="tool_tokens")


class ChatCompletionCompletionTokensDetails(BaseModel):
    """Provider-specific completion token details."""

    reasoning_tokens: int = Field(0, alias="reasoning_tokens")
    audio_tokens: int = Field(0, alias="audio_tokens")
    accepted_prediction_tokens: int = Field(0, alias="accepted_prediction_tokens")
    rejected_prediction_tokens: int = Field(0, alias="rejected_prediction_tokens")


class ChatCompletionUsage(BaseModel):
    """Token usage information for a chat completion."""

    prompt_tokens: int = Field(0, alias="prompt_tokens")
    completion_tokens: int = Field(0, alias="completion_tokens")
    total_tokens: int = Field(0, alias="total_tokens")
    context_window: int = Field(0, alias="context_window")
    reasoning_tokens: int = Field(0, alias="reasoning_tokens")
    tool_definition_tokens: int = Field(0, alias="tool_definition_tokens")
    tool_tokens: int = Field(0, alias="tool_tokens")
    prompt_tokens_details: Optional[ChatCompletionPromptTokensDetails] = Field(
        None, alias="prompt_tokens_details"
    )
    completion_tokens_details: Optional[ChatCompletionCompletionTokensDetails] = Field(
        None, alias="completion_tokens_details"
    )


class RetrievalChunk(BaseModel):
    """A chunk returned from retrieval search."""

    document_id: Optional[str] = Field(None, alias="document_id")
    score: float = 0.0
    rerank_score: Optional[float] = Field(None, alias="rerank_score")
    fusion_score: Optional[float] = Field(None, alias="fusion_score")
    text_score: Optional[float] = Field(None, alias="text_score")
    content: Optional[str] = None
    position: Optional[int] = None
    neighbors: Optional[list[RetrievalChunk]] = None


class CitationSource(BaseModel):
    """A citation source in a chat completion response."""

    index: int = 0
    source_type: Optional[str] = Field(None, alias="source_type")
    document_id: Optional[str] = Field(None, alias="document_id")
    url: Optional[str] = None
    document_name: Optional[str] = Field(None, alias="document_name")
    content_type: Optional[str] = Field(None, alias="content_type")
    score: float = 0.0
    fusion_score: Optional[float] = Field(None, alias="fusion_score")
    rerank_score: Optional[float] = Field(None, alias="rerank_score")
    excerpt: Optional[str] = None
    download_url: Optional[str] = Field(None, alias="download_url")


class RetrievalCandidateDropSummary(BaseModel):
    """Aggregated count of retrieval candidates dropped at a pipeline stage."""

    stage: Optional[str] = None
    reason: Optional[str] = None
    count: int = 0


class ChatCompletionRetrieval(BaseModel):
    """Retrieval metadata in a chat completion response."""

    collection_id: Optional[str] = Field(None, alias="collection_id")
    duration_ms: float = Field(0.0, alias="duration_ms")
    chunks_returned: int = Field(0, alias="chunks_returned")
    rerank_duration_ms: float = Field(0.0, alias="rerank_duration_ms")
    rerank_input_count: int = Field(0, alias="rerank_input_count")
    rerank_output_count: int = Field(0, alias="rerank_output_count")
    attached_document_ids: Optional[list[str]] = Field(
        None, alias="attached_document_ids"
    )
    attached_documents: Optional[list[AssistantDocumentSelectionItem]] = Field(
        None, alias="attached_documents"
    )
    document_filter_applied: bool = Field(
        False, alias="document_filter_applied"
    )
    query_class: Optional[str] = Field(None, alias="query_class")
    answerability_decision: Optional[str] = Field(None, alias="answerability_decision")
    answerability_reason: Optional[str] = Field(None, alias="answerability_reason")
    dropped_candidate_count: int = Field(0, alias="dropped_candidate_count")
    dropped_candidates: Optional[list[RetrievalCandidateDropSummary]] = Field(
        None, alias="dropped_candidates"
    )
    final_citation_count: Optional[int] = Field(None, alias="final_citation_count")
    chunks: Optional[list[RetrievalChunk]] = None


class ChatCompletionCitations(BaseModel):
    """Citation metadata in a chat completion response."""

    sources: Optional[list[CitationSource]] = None
    referenced_indices: Optional[list[int]] = Field(None, alias="referenced_indices")
    auto_populated: bool = Field(False, alias="auto_populated")


class ChatCompletionChoice(BaseModel):
    """A choice in a chat completion response."""

    index: int = 0
    message: Optional[ChatCompletionMessage] = None
    delta: Optional[ChatCompletionMessage] = None
    finish_reason: Optional[str] = Field(None, alias="finish_reason")


class ChatCompletionToolTrace(BaseModel):
    """Safe metadata for a model-directed assistant tool call."""

    tool_call_id: Optional[str] = Field(None, alias="tool_call_id")
    tool_name: Optional[str] = Field(None, alias="tool_name")
    display_label: Optional[str] = Field(None, alias="display_label")
    iteration: int = 0
    sequence_number: int = Field(0, alias="sequence_number")
    success: bool = False
    denied: bool = False
    truncated: bool = False
    output_characters: int = Field(0, alias="output_characters")
    result_count: Optional[int] = Field(None, alias="result_count")
    credits_used: Optional[int] = Field(None, alias="credits_used")
    provider_latency_ms: Optional[float] = Field(None, alias="provider_latency_ms")
    duration_ms: float = Field(0.0, alias="duration_ms")
    summary: Optional[str] = None
    started_utc: Optional[datetime] = Field(None, alias="started_utc")
    finished_utc: Optional[datetime] = Field(None, alias="finished_utc")


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
    tool_calls: Optional[list[ChatCompletionToolTrace]] = Field(
        None, alias="tool_calls"
    )


class AssistantPerformanceClientTimings(BaseModel):
    """Client-observed timings for an upstream provider call."""

    endpoint_limiter_wait_ms: Optional[float] = Field(
        None, alias="EndpointLimiterWaitMs"
    )
    request_to_headers_ms: Optional[float] = Field(None, alias="RequestToHeadersMs")
    headers_to_first_token_ms: Optional[float] = Field(
        None, alias="HeadersToFirstTokenMs"
    )
    first_token_to_last_token_ms: Optional[float] = Field(
        None, alias="FirstTokenToLastTokenMs"
    )
    total_ms: Optional[float] = Field(None, alias="TotalMs")


class AssistantTokenUsageTelemetry(BaseModel):
    """Normalized token counters."""

    input: Optional[int] = Field(None, alias="Input")
    output: Optional[int] = Field(None, alias="Output")
    total: Optional[int] = Field(None, alias="Total")
    reasoning: Optional[int] = Field(None, alias="Reasoning")
    tool_definitions: Optional[int] = Field(None, alias="ToolDefinitions")
    prompt_eval_count: Optional[int] = Field(None, alias="PromptEvalCount")
    eval_count: Optional[int] = Field(None, alias="EvalCount")


class AssistantProviderMetrics(BaseModel):
    """Provider-native metrics normalized into common fields."""

    queue_ms: Optional[float] = Field(None, alias="QueueMs")
    load_ms: Optional[float] = Field(None, alias="LoadMs")
    prompt_eval_ms: Optional[float] = Field(None, alias="PromptEvalMs")
    generation_ms: Optional[float] = Field(None, alias="GenerationMs")
    total_ms: Optional[float] = Field(None, alias="TotalMs")
    tokens_per_second: Optional[float] = Field(None, alias="TokensPerSecond")
    request_id: Optional[str] = Field(None, alias="RequestId")


class AssistantPerformanceStage(BaseModel):
    """A measured stage in the assistant pipeline."""

    name: Optional[str] = Field(None, alias="Name")
    kind: Optional[str] = Field(None, alias="Kind")
    sequence: int = Field(0, alias="Sequence")
    endpoint_id: Optional[str] = Field(None, alias="EndpointId")
    endpoint_name: Optional[str] = Field(None, alias="EndpointName")
    endpoint_type: Optional[str] = Field(None, alias="EndpointType")
    provider: Optional[str] = Field(None, alias="Provider")
    api_format: Optional[str] = Field(None, alias="ApiFormat")
    model: Optional[str] = Field(None, alias="Model")
    started_utc: Optional[datetime] = Field(None, alias="StartedUtc")
    finished_utc: Optional[datetime] = Field(None, alias="FinishedUtc")
    duration_ms: float = Field(0.0, alias="DurationMs")
    success: bool = Field(True, alias="Success")
    http_status_code: Optional[int] = Field(None, alias="HttpStatusCode")
    error_type: Optional[str] = Field(None, alias="ErrorType")
    error_message: Optional[str] = Field(None, alias="ErrorMessage")
    client_timings: Optional[AssistantPerformanceClientTimings] = Field(
        None, alias="ClientTimings"
    )
    tokens: Optional[AssistantTokenUsageTelemetry] = Field(None, alias="Tokens")
    provider_metrics: Optional[AssistantProviderMetrics] = Field(
        None, alias="ProviderMetrics"
    )
    metadata: Optional[dict[str, Any]] = Field(None, alias="Metadata")
    provider_raw: Optional[dict[str, Any]] = Field(None, alias="ProviderRaw")


class AssistantPerformanceTelemetry(BaseModel):
    """Versioned provider-agnostic performance telemetry for a chat turn."""

    schema_version: int = Field(1, alias="SchemaVersion")
    trace_id: Optional[str] = Field(None, alias="TraceId")
    chat_history_id: Optional[str] = Field(None, alias="ChatHistoryId")
    request_history_id: Optional[str] = Field(None, alias="RequestHistoryId")
    wall_time_ms: float = Field(0.0, alias="WallTimeMs")
    created_utc: Optional[datetime] = Field(None, alias="CreatedUtc")
    stages: Optional[list[AssistantPerformanceStage]] = Field(None, alias="Stages")


class ChatHistory(BaseModel):
    """A chat history record."""

    model_config = ConfigDict(populate_by_name=True)

    id: Optional[str] = Field(None, alias="Id", validation_alias=AliasChoices("Id", "id"))
    trace_id: Optional[str] = Field(None, alias="TraceId")
    request_history_id: Optional[str] = Field(None, alias="RequestHistoryId")
    performance_schema_version: int = Field(1, alias="PerformanceSchemaVersion")
    performance_json: Optional[str] = Field(None, alias="PerformanceJson")
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
    query_class: Optional[str] = Field(
        None,
        alias="QueryClass",
        validation_alias=AliasChoices("QueryClass", "queryClass", "query_class"),
    )
    answerability_decision: Optional[str] = Field(
        None,
        alias="AnswerabilityDecision",
        validation_alias=AliasChoices("AnswerabilityDecision", "answerabilityDecision", "answerability_decision"),
    )
    answerability_reason: Optional[str] = Field(
        None,
        alias="AnswerabilityReason",
        validation_alias=AliasChoices("AnswerabilityReason", "answerabilityReason", "answerability_reason"),
    )
    dropped_candidate_count: Optional[int] = Field(
        None,
        alias="DroppedCandidateCount",
        validation_alias=AliasChoices("DroppedCandidateCount", "droppedCandidateCount", "dropped_candidate_count"),
    )
    dropped_candidate_summary_json: Optional[str] = Field(
        None,
        alias="DroppedCandidateSummaryJson",
        validation_alias=AliasChoices("DroppedCandidateSummaryJson", "droppedCandidateSummaryJson", "dropped_candidate_summary_json"),
    )
    final_citation_count: Optional[int] = Field(
        None,
        alias="FinalCitationCount",
        validation_alias=AliasChoices("FinalCitationCount", "finalCitationCount", "final_citation_count"),
    )
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
    attached_document_ids_json: Optional[str] = Field(
        None,
        alias="AttachedDocumentIdsJson",
        validation_alias=AliasChoices(
            "AttachedDocumentIdsJson",
            "attachedDocumentIdsJson",
            "attached_document_ids_json",
        ),
    )
    attached_documents_json: Optional[str] = Field(
        None,
        alias="AttachedDocumentsJson",
        validation_alias=AliasChoices(
            "AttachedDocumentsJson",
            "attachedDocumentsJson",
            "attached_documents_json",
        ),
    )
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
    trace_id: Optional[str] = Field(None, alias="TraceId")
    chat_history_id: Optional[str] = Field(None, alias="ChatHistoryId")
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

    model_config = ConfigDict(populate_by_name=True)

    deleted_count: int = Field(0, alias="DeletedCount", validation_alias=AliasChoices("DeletedCount", "deletedCount", "deleted_count"))


# ---------------------------------------------------------------------------
# Assistant Analytics
# ---------------------------------------------------------------------------


class AssistantAnalyticsQuery(BaseModel):
    """Query parameters for assistant analytics endpoints."""

    model_config = ConfigDict(populate_by_name=True)

    range: Optional[str] = "lastDay"
    start_utc: Optional[datetime] = Field(None, alias="startUtc")
    end_utc: Optional[datetime] = Field(None, alias="endUtc")
    bucket_seconds: Optional[int] = Field(None, alias="bucketSeconds")
    metrics: Optional[str | list[str]] = None
    stage: Optional[str] = None
    endpoint_id: Optional[str] = Field(None, alias="endpointId")
    endpoint_type: Optional[str] = Field(None, alias="endpointType")
    model: Optional[str] = None
    limit: Optional[int] = None


class AssistantAnalyticsRange(BaseModel):
    """Resolved assistant analytics range."""

    model_config = ConfigDict(populate_by_name=True)

    range_id: Optional[str] = Field(None, alias="rangeId")
    start_utc: Optional[datetime] = Field(None, alias="startUtc")
    end_utc: Optional[datetime] = Field(None, alias="endUtc")
    bucket_seconds: int = Field(0, alias="bucketSeconds")
    bucket_count: int = Field(0, alias="bucketCount")


class AssistantAnalyticsPoint(BaseModel):
    """Assistant analytics time-series point."""

    model_config = ConfigDict(populate_by_name=True)

    bucket_start_utc: Optional[datetime] = Field(None, alias="bucketStartUtc")
    bucket_end_utc: Optional[datetime] = Field(None, alias="bucketEndUtc")
    value: Optional[float] = None
    sample_count: int = Field(0, alias="sampleCount")
    null_count: int = Field(0, alias="nullCount")


class AssistantAnalyticsSeries(BaseModel):
    """Assistant analytics time-series definition."""

    model_config = ConfigDict(populate_by_name=True)

    metric: Optional[str] = None
    label: Optional[str] = None
    unit: Optional[str] = None
    points: Optional[list[AssistantAnalyticsPoint]] = None


class AssistantAnalyticsOverviewResult(BaseModel):
    """Assistant analytics overview."""

    model_config = ConfigDict(populate_by_name=True)

    assistant_id: Optional[str] = Field(None, alias="assistantId")
    range: Optional[AssistantAnalyticsRange] = None
    generated_utc: Optional[datetime] = Field(None, alias="generatedUtc")
    request_count: int = Field(0, alias="requestCount")
    success_count: int = Field(0, alias="successCount")
    failure_count: int = Field(0, alias="failureCount")
    success_rate: Optional[float] = Field(None, alias="successRate")
    failure_rate: Optional[float] = Field(None, alias="failureRate")
    average_duration_ms: Optional[float] = Field(None, alias="averageDurationMs")
    p50_duration_ms: Optional[float] = Field(None, alias="p50DurationMs")
    p90_duration_ms: Optional[float] = Field(None, alias="p90DurationMs")
    p95_duration_ms: Optional[float] = Field(None, alias="p95DurationMs")
    p99_duration_ms: Optional[float] = Field(None, alias="p99DurationMs")
    max_duration_ms: Optional[float] = Field(None, alias="maxDurationMs")
    telemetry_event_count: int = Field(0, alias="telemetryEventCount")
    requests_with_telemetry: int = Field(0, alias="requestsWithTelemetry")
    telemetry_coverage_rate: Optional[float] = Field(None, alias="telemetryCoverageRate")
    dominant_stage: Optional[str] = Field(None, alias="dominantStage")
    dominant_stage_average_ms: Optional[float] = Field(None, alias="dominantStageAverageMs")
    top_endpoint_id: Optional[str] = Field(None, alias="topEndpointId")
    top_endpoint_name: Optional[str] = Field(None, alias="topEndpointName")
    top_endpoint_provider: Optional[str] = Field(None, alias="topEndpointProvider")
    top_endpoint_model: Optional[str] = Field(None, alias="topEndpointModel")
    feedback_count: int = Field(0, alias="feedbackCount")
    thumbs_up_count: int = Field(0, alias="thumbsUpCount")
    thumbs_down_count: int = Field(0, alias="thumbsDownCount")
    negative_feedback_rate: Optional[float] = Field(None, alias="negativeFeedbackRate")


class AssistantAnalyticsTimeSeriesResult(BaseModel):
    """Assistant analytics time-series result."""

    model_config = ConfigDict(populate_by_name=True)

    assistant_id: Optional[str] = Field(None, alias="assistantId")
    range: Optional[AssistantAnalyticsRange] = None
    generated_utc: Optional[datetime] = Field(None, alias="generatedUtc")
    series: Optional[list[AssistantAnalyticsSeries]] = None


class AssistantAnalyticsStageBucket(BaseModel):
    """Assistant analytics stage bucket."""

    model_config = ConfigDict(populate_by_name=True)

    bucket_start_utc: Optional[datetime] = Field(None, alias="bucketStartUtc")
    bucket_end_utc: Optional[datetime] = Field(None, alias="bucketEndUtc")
    stage: Optional[str] = None
    kind: Optional[str] = None
    calls: int = 0
    failures: int = 0
    skipped_count: int = Field(0, alias="skippedCount")
    average_duration_ms: Optional[float] = Field(None, alias="averageDurationMs")
    p95_duration_ms: Optional[float] = Field(None, alias="p95DurationMs")
    max_duration_ms: Optional[float] = Field(None, alias="maxDurationMs")


class AssistantAnalyticsStageResult(BaseModel):
    """Assistant analytics stage result."""

    model_config = ConfigDict(populate_by_name=True)

    assistant_id: Optional[str] = Field(None, alias="assistantId")
    range: Optional[AssistantAnalyticsRange] = None
    generated_utc: Optional[datetime] = Field(None, alias="generatedUtc")
    buckets: Optional[list[AssistantAnalyticsStageBucket]] = None


class AssistantAnalyticsEndpointSummary(BaseModel):
    """Assistant analytics endpoint/model/provider summary."""

    model_config = ConfigDict(populate_by_name=True)

    endpoint_id: Optional[str] = Field(None, alias="endpointId")
    endpoint_name: Optional[str] = Field(None, alias="endpointName")
    endpoint_type: Optional[str] = Field(None, alias="endpointType")
    provider: Optional[str] = None
    api_format: Optional[str] = Field(None, alias="apiFormat")
    model: Optional[str] = None
    stage: Optional[str] = None
    calls: int = 0
    failures: int = 0
    average_duration_ms: Optional[float] = Field(None, alias="averageDurationMs")
    p95_duration_ms: Optional[float] = Field(None, alias="p95DurationMs")
    average_limiter_wait_ms: Optional[float] = Field(None, alias="averageLimiterWaitMs")
    p95_limiter_wait_ms: Optional[float] = Field(None, alias="p95LimiterWaitMs")
    average_request_to_headers_ms: Optional[float] = Field(None, alias="averageRequestToHeadersMs")
    average_provider_load_ms: Optional[float] = Field(None, alias="averageProviderLoadMs")
    average_provider_generation_ms: Optional[float] = Field(None, alias="averageProviderGenerationMs")
    average_tokens_per_second: Optional[float] = Field(None, alias="averageTokensPerSecond")
    input_tokens: int = Field(0, alias="inputTokens")
    output_tokens: int = Field(0, alias="outputTokens")


class AssistantAnalyticsEndpointResult(BaseModel):
    """Assistant analytics endpoint result."""

    model_config = ConfigDict(populate_by_name=True)

    assistant_id: Optional[str] = Field(None, alias="assistantId")
    range: Optional[AssistantAnalyticsRange] = None
    generated_utc: Optional[datetime] = Field(None, alias="generatedUtc")
    endpoints: Optional[list[AssistantAnalyticsEndpointSummary]] = None


class AssistantAnalyticsSlowRequest(BaseModel):
    """Assistant analytics slow request row."""

    model_config = ConfigDict(populate_by_name=True)

    request_history_id: Optional[str] = Field(None, alias="requestHistoryId")
    chat_history_id: Optional[str] = Field(None, alias="chatHistoryId")
    trace_id: Optional[str] = Field(None, alias="traceId")
    created_utc: Optional[datetime] = Field(None, alias="createdUtc")
    status_code: int = Field(0, alias="statusCode")
    success: bool = False
    duration_ms: float = Field(0.0, alias="durationMs")
    request_path: Optional[str] = Field(None, alias="requestPath")
    dominant_stage: Optional[str] = Field(None, alias="dominantStage")
    dominant_stage_duration_ms: Optional[float] = Field(None, alias="dominantStageDurationMs")
    endpoint_id: Optional[str] = Field(None, alias="endpointId")
    endpoint_name: Optional[str] = Field(None, alias="endpointName")
    provider: Optional[str] = None
    model: Optional[str] = None
    tool_call_count: int = Field(0, alias="toolCallCount")
    tool_failure_count: int = Field(0, alias="toolFailureCount")
    tool_denied_count: int = Field(0, alias="toolDeniedCount")
    tool_truncated_count: int = Field(0, alias="toolTruncatedCount")
    tool_duration_ms: Optional[float] = Field(None, alias="toolDurationMs")
    slowest_tool_name: Optional[str] = Field(None, alias="slowestToolName")
    slowest_tool_duration_ms: Optional[float] = Field(None, alias="slowestToolDurationMs")
    failing_tool_names: Optional[list[str]] = Field(default_factory=list, alias="failingToolNames")


class AssistantAnalyticsSlowestResult(BaseModel):
    """Assistant analytics slowest requests result."""

    model_config = ConfigDict(populate_by_name=True)

    assistant_id: Optional[str] = Field(None, alias="assistantId")
    range: Optional[AssistantAnalyticsRange] = None
    generated_utc: Optional[datetime] = Field(None, alias="generatedUtc")
    requests: Optional[list[AssistantAnalyticsSlowRequest]] = None


class AssistantAnalyticsFeedbackBucket(BaseModel):
    """Assistant analytics feedback bucket."""

    model_config = ConfigDict(populate_by_name=True)

    bucket_start_utc: Optional[datetime] = Field(None, alias="bucketStartUtc")
    bucket_end_utc: Optional[datetime] = Field(None, alias="bucketEndUtc")
    thumbs_up_count: int = Field(0, alias="thumbsUpCount")
    thumbs_down_count: int = Field(0, alias="thumbsDownCount")
    unknown_count: int = Field(0, alias="unknownCount")
    total_count: int = Field(0, alias="totalCount")
    negative_rate: Optional[float] = Field(None, alias="negativeRate")


class AssistantAnalyticsFeedbackResult(BaseModel):
    """Assistant analytics feedback result."""

    model_config = ConfigDict(populate_by_name=True)

    assistant_id: Optional[str] = Field(None, alias="assistantId")
    range: Optional[AssistantAnalyticsRange] = None
    generated_utc: Optional[datetime] = Field(None, alias="generatedUtc")
    total_count: int = Field(0, alias="totalCount")
    thumbs_up_count: int = Field(0, alias="thumbsUpCount")
    thumbs_down_count: int = Field(0, alias="thumbsDownCount")
    negative_rate: Optional[float] = Field(None, alias="negativeRate")
    buckets: Optional[list[AssistantAnalyticsFeedbackBucket]] = None


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
    supports_tool_calling: bool = Field(False, alias="supportsToolCalling", validation_alias=AliasChoices("SupportsToolCalling", "supportsToolCalling", "supports_tool_calling"))
    tool_calling_api_format: Optional[str] = Field(None, alias="toolCallingApiFormat", validation_alias=AliasChoices("ToolCallingApiFormat", "toolCallingApiFormat", "tool_calling_api_format"))
    supports_parallel_tool_calls: bool = Field(False, alias="supportsParallelToolCalls", validation_alias=AliasChoices("SupportsParallelToolCalls", "supportsParallelToolCalls", "supports_parallel_tool_calls"))
    supports_streaming_tool_calls: bool = Field(False, alias="supportsStreamingToolCalls", validation_alias=AliasChoices("SupportsStreamingToolCalls", "supportsStreamingToolCalls", "supports_streaming_tool_calls"))
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
    supports_tool_calling: bool = Field(False, alias="supportsToolCalling", validation_alias=AliasChoices("SupportsToolCalling", "supportsToolCalling", "supports_tool_calling"))
    tool_calling_api_format: Optional[str] = Field(None, alias="toolCallingApiFormat", validation_alias=AliasChoices("ToolCallingApiFormat", "toolCallingApiFormat", "tool_calling_api_format"))
    supports_parallel_tool_calls: bool = Field(False, alias="supportsParallelToolCalls", validation_alias=AliasChoices("SupportsParallelToolCalls", "supportsParallelToolCalls", "supports_parallel_tool_calls"))
    supports_streaming_tool_calls: bool = Field(False, alias="supportsStreamingToolCalls", validation_alias=AliasChoices("SupportsStreamingToolCalls", "supportsStreamingToolCalls", "supports_streaming_tool_calls"))
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
    verbex_index_id: Optional[str] = Field(None, alias="verbexIndexId")
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

    model_config = ConfigDict(populate_by_name=True)

    interval_type: ScheduleInterval = Field(
        ScheduleInterval.ONE_TIME, alias="intervalType"
    )
    interval_value: int = Field(1, alias="intervalValue")


class CrawlFilterSettings(BaseModel):
    """Filter configuration for a crawl plan."""

    model_config = ConfigDict(populate_by_name=True)

    object_prefix: Optional[str] = Field(None, alias="objectPrefix")
    object_suffix: Optional[str] = Field(None, alias="objectSuffix")
    allowed_content_types: Optional[list[str]] = Field(
        None, alias="allowedContentTypes"
    )
    minimum_size: int = Field(0, alias="minimumSize")
    maximum_size: Optional[int] = Field(None, alias="maximumSize")


class CrawlIngestionSettings(BaseModel):
    """Ingestion settings for a crawl plan."""

    model_config = ConfigDict(populate_by_name=True)

    ingestion_rule_id: Optional[str] = Field(None, alias="ingestionRuleId")
    store_in_s3: bool = Field(False, alias="storeInS3")
    s3_bucket_name: Optional[str] = Field(None, alias="s3BucketName")


class CrawlRepositorySettings(BaseModel):
    """Base repository settings for crawling."""

    model_config = ConfigDict(populate_by_name=True)

    repository_type: RepositoryType = Field(
        RepositoryType.WEB, alias="repositoryType"
    )


class WebCrawlRepositorySettings(CrawlRepositorySettings):
    """Web-specific repository settings for crawling."""

    repository_type: RepositoryType = Field(
        RepositoryType.WEB, alias="repositoryType"
    )
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


class CifsCrawlRepositorySettings(CrawlRepositorySettings):
    """CIFS-specific repository settings for crawling."""

    repository_type: RepositoryType = Field(
        RepositoryType.CIFS, alias="repositoryType"
    )
    cifs_hostname: Optional[str] = Field(None, alias="cifsHostname")
    cifs_username: Optional[str] = Field(None, alias="cifsUsername")
    cifs_password: Optional[str] = Field(None, alias="cifsPassword")
    cifs_share_name: Optional[str] = Field(None, alias="cifsShareName")
    include_subdirectories: bool = Field(True, alias="includeSubdirectories")


class NfsCrawlRepositorySettings(CrawlRepositorySettings):
    """NFS-specific repository settings for crawling."""

    repository_type: RepositoryType = Field(
        RepositoryType.NFS, alias="repositoryType"
    )
    nfs_hostname: Optional[str] = Field(None, alias="nfsHostname")
    nfs_user_id: Optional[int] = Field(None, alias="nfsUserId")
    nfs_group_id: Optional[int] = Field(None, alias="nfsGroupId")
    nfs_share_name: Optional[str] = Field(None, alias="nfsShareName")
    nfs_version: NfsVersion = Field(NfsVersion.V3, alias="nfsVersion")
    include_subdirectories: bool = Field(True, alias="includeSubdirectories")


RepositorySettingsValue = (
    WebCrawlRepositorySettings | CifsCrawlRepositorySettings | NfsCrawlRepositorySettings
)


class CrawlPlan(BaseModel):
    """A crawl plan defining how to crawl a repository."""

    model_config = ConfigDict(populate_by_name=True)

    id: Optional[str] = None
    tenant_id: Optional[str] = Field(None, alias="tenantId")
    name: Optional[str] = None
    repository_type: RepositoryType = Field(
        RepositoryType.WEB, alias="repositoryType"
    )
    ingestion_settings: Optional[CrawlIngestionSettings] = Field(
        None, alias="ingestionSettings"
    )
    repository_settings: Optional[RepositorySettingsValue] = Field(
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

    @field_validator("repository_settings", mode="before")
    @classmethod
    def _parse_repository_settings(cls, value: Any) -> Any:
        if value is None or isinstance(value, CrawlRepositorySettings):
            return value

        if isinstance(value, dict):
            repository_type = (
                value.get("repositoryType")
                or value.get("RepositoryType")
                or value.get("repository_type")
            )

            if repository_type == RepositoryType.CIFS.value:
                return CifsCrawlRepositorySettings.model_validate(value)
            if repository_type == RepositoryType.NFS.value:
                return NfsCrawlRepositorySettings.model_validate(value)
            if "cifsHostname" in value or "CifsHostname" in value:
                return CifsCrawlRepositorySettings.model_validate(value)
            if "nfsHostname" in value or "NfsHostname" in value:
                return NfsCrawlRepositorySettings.model_validate(value)

            return WebCrawlRepositorySettings.model_validate(value)

        return value


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
    execution_mode: Optional[str] = Field(None, alias="ExecutionMode")
    categories: Optional[list[str]] = Field(None, alias="Categories")


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
    execution_mode: Optional[str] = Field(
        None,
        alias="executionMode",
        validation_alias=AliasChoices("ExecutionMode", "executionMode", "execution_mode"),
    )
    category_filter_json: Optional[str] = Field(
        None,
        alias="categoryFilterJson",
        validation_alias=AliasChoices("CategoryFilterJson", "categoryFilterJson", "category_filter_json"),
    )
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
    chat_history_id: Optional[str] = Field(
        None,
        alias="chatHistoryId",
        validation_alias=AliasChoices("ChatHistoryId", "chatHistoryId", "chat_history_id"),
    )
    trace_id: Optional[str] = Field(
        None,
        alias="traceId",
        validation_alias=AliasChoices("TraceId", "traceId", "trace_id"),
    )
    retrieval_json: Optional[str] = Field(None, alias="retrievalJson")
    citations_json: Optional[str] = Field(None, alias="citationsJson")
    tool_calls_json: Optional[str] = Field(None, alias="toolCallsJson")
    query_class: Optional[str] = Field(None, alias="queryClass")
    answerability_decision: Optional[str] = Field(None, alias="answerabilityDecision")
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
    request_history_id_filter: Optional[str] = Field(None, alias="requestHistoryIdFilter")
    chat_history_id_filter: Optional[str] = Field(None, alias="chatHistoryIdFilter")
    trace_id_filter: Optional[str] = Field(None, alias="traceIdFilter")
    tool_name_filter: Optional[str] = Field(None, alias="toolNameFilter")
    success_filter: Optional[bool] = Field(None, alias="successFilter")
    denied_filter: Optional[bool] = Field(None, alias="deniedFilter")
    start_utc: Optional[datetime] = Field(None, alias="startUtc")
    end_utc: Optional[datetime] = Field(None, alias="endUtc")


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


class ExternalSearchConfigurationStatus(BaseModel):
    """Safe external-search configuration status."""

    model_config = ConfigDict(populate_by_name=True)

    enabled: bool = Field(False, alias="Enabled", validation_alias=AliasChoices("Enabled", "enabled"))
    enabled_providers: int = Field(0, alias="EnabledProviders", validation_alias=AliasChoices("EnabledProviders", "enabledProviders", "enabled_providers"))
    configured_providers: int = Field(0, alias="ConfiguredProviders", validation_alias=AliasChoices("ConfiguredProviders", "configuredProviders", "configured_providers"))
    misconfigured_providers: int = Field(0, alias="MisconfiguredProviders", validation_alias=AliasChoices("MisconfiguredProviders", "misconfiguredProviders", "misconfigured_providers"))


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
    document_ids: Optional[list[str]] = Field(None, alias="documentIds")


# Enable forward reference resolution for self-referencing models
RetrievalChunk.model_rebuild()

# Enable forward reference resolution for AuthenticateResult
AuthenticateResult.model_rebuild()

# Enable forward reference resolution for AssistantSettings.ToolPolicy
AssistantSettings.model_rebuild()
