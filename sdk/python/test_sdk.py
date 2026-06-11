#!/usr/bin/env python3
"""AssistantHub Python SDK Test Suite."""

from __future__ import annotations

import os
import sys
import time
import traceback
import uuid
import asyncio
from dataclasses import dataclass, field
from typing import Any, Callable, Optional

# Add the SDK to the path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from assistanthub_sdk import AssistantHubClient, AsyncAssistantHubClient
from assistanthub_sdk.models import (
    Assistant,
    AssistantDocument,
    AssistantDocumentSelectionItem,
    AssistantSettings,
    AssistantToolPolicy,
    AssistantToolPolicyValidationRequest,
    AssistantToolPolicyValidationResult,
    AssistantToolPolicyTestResult,
    CifsCrawlRepositorySettings,
    CrawlPlan,
    CrawlScheduleSettings,
    ChatLocalAttachment,
    ChatCompletionMessage,
    ChatCompletionRequest,
    ChatCompletionRetrieval,
    ChatCompletionResponse,
    ChatCompletionUsage,
    ChatHistory,
    Credential,
    EvalFact,
    ExternalSearchConfigurationStatus,
    AssistantTokenUsageTelemetry,
    NfsCrawlRepositorySettings,
    PartioEndpointConfig,
    PartioEndpointRequest,
    TenantMetadata,
    UserMaster,
    WebCrawlRepositorySettings,
)
from assistanthub_sdk.enums import (
    NfsVersion,
    RepositoryType,
    ScheduleInterval,
    WebAuthType,
)


@dataclass
class TestResult:
    """Result of a single test execution."""
    test_name: str
    passed: bool
    runtime_ms: float
    error_message: Optional[str] = None


class TestRunner:
    """Runs tests and collects results, matching the C# TestRunner output format."""

    def __init__(self) -> None:
        self._results: list[TestResult] = []

    @property
    def results(self) -> list[TestResult]:
        return self._results

    def run_test(self, test_name: str, test_func: Callable[[], None]) -> TestResult:
        start = time.perf_counter()
        try:
            test_func()
            elapsed_ms = (time.perf_counter() - start) * 1000.0
            result = TestResult(test_name=test_name, passed=True, runtime_ms=elapsed_ms)
            sys.stdout.write("  PASS ")
        except Exception as ex:
            elapsed_ms = (time.perf_counter() - start) * 1000.0
            error_msg = str(ex)
            result = TestResult(
                test_name=test_name, passed=False, runtime_ms=elapsed_ms, error_message=error_msg
            )
            sys.stdout.write("  FAIL ")

        sys.stdout.write(" {} ({:.1f}ms)\n".format(result.test_name, result.runtime_ms))

        if not result.passed:
            sys.stdout.write("         {}\n".format(result.error_message))

        self._results.append(result)
        return result

    def print_summary(self, total_runtime_ms: float) -> None:
        passed = sum(1 for r in self._results if r.passed)
        failed = sum(1 for r in self._results if not r.passed)
        failures = [r for r in self._results if not r.passed]

        print()
        print("=" * 80)
        print("TEST SUMMARY")
        print("=" * 80)
        print("  Total:   {}".format(len(self._results)))
        print("  Passed:  {}".format(passed))
        print("  Failed:  {}".format(failed))
        print("  Runtime: {:.1f}ms".format(total_runtime_ms))
        print()

        if failures:
            print("FAILED TESTS:")
            for f in failures:
                sys.stdout.write("  FAIL ")
                sys.stdout.write(" {}\n".format(f.test_name))
                sys.stdout.write("         {}\n".format(f.error_message))
            print()

        if failed > 0:
            print("OVERALL: FAIL")
        else:
            print("OVERALL: PASS")


# ---------------------------------------------------------------------------
# Assertion helpers
# ---------------------------------------------------------------------------

def assert_true(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError("{}: expected true".format(message))


def assert_false(condition: bool, message: str) -> None:
    if condition:
        raise AssertionError("{}: expected false".format(message))


def assert_not_none(value: Any, label: str) -> None:
    if value is None:
        raise AssertionError("{} should not be None".format(label))


def assert_equal(expected: Any, actual: Any, label: str) -> None:
    if expected != actual:
        raise AssertionError("{}: expected '{}' but got '{}'".format(label, expected, actual))


def assert_starts_with(value: str, prefix: str, label: str) -> None:
    if not value.startswith(prefix):
        raise AssertionError("{}: expected to start with '{}' but got '{}'".format(label, prefix, value))


def assert_gte(value: int, minimum: int, label: str) -> None:
    if value < minimum:
        raise AssertionError("{}: expected >= {} but got {}".format(label, minimum, value))


# ---------------------------------------------------------------------------
# Unique suffix generator
# ---------------------------------------------------------------------------

def unique_suffix() -> str:
    return uuid.uuid4().hex[:8]


# ---------------------------------------------------------------------------
# Local SDK contract tests
# ---------------------------------------------------------------------------

def _truthy(value: Optional[str]) -> bool:
    if value is None:
        return False
    return value.strip().lower() in ("1", "true", "yes", "y")


def local_only_requested() -> bool:
    if _truthy(os.environ.get("ASSISTANTHUB_SDK_LOCAL_ONLY")):
        return True

    for arg in sys.argv[1:]:
        normalized = arg.strip().lower()
        if normalized in ("--local-only", "local-only", "localonly=true", "local=true"):
            return True

    return False


def run_sdk_contract_tests(runner: TestRunner) -> None:
    def test_request_attached_document_ids() -> None:
        request = ChatCompletionRequest(
            messages=[ChatCompletionMessage(role="user", content="Summarize this document.")],
            attached_document_ids=["adoc_one", "adoc_two"],
            top_p=0.8,
            max_tokens=512,
        )

        payload = request.model_dump(by_alias=True, exclude_none=True)
        assert_true("attached_document_ids" in payload, "request should use attached_document_ids")
        assert_false("AttachedDocumentIds" in payload, "request should not use PascalCase attachment key")
        assert_equal(["adoc_one", "adoc_two"], payload["attached_document_ids"], "attached document IDs")
        assert_equal(0.8, payload["top_p"], "top_p alias")
        assert_equal(512, payload["max_tokens"], "max_tokens alias")

        round_trip = ChatCompletionRequest.model_validate(payload)
        assert_equal(
            ["adoc_one", "adoc_two"],
            round_trip.attached_document_ids,
            "round-trip attached document IDs",
        )

    def test_request_local_attachments() -> None:
        request = ChatCompletionRequest(
            messages=[ChatCompletionMessage(role="user", content="Summarize this local file.")],
            local_attachments=[
                ChatLocalAttachment(
                    name="notes.txt",
                    content_type="text/plain",
                    base64_content="SGVsbG8=",
                )
            ],
        )

        payload = request.model_dump(by_alias=True, exclude_none=True)
        assert_true("local_attachments" in payload, "request should use local_attachments")
        assert_false("LocalAttachments" in payload, "request should not use PascalCase local attachment key")
        assert_equal(1, len(payload["local_attachments"]), "local attachment count")
        assert_equal("notes.txt", payload["local_attachments"][0]["name"], "local attachment name")
        assert_equal("SGVsbG8=", payload["local_attachments"][0]["base64_content"], "local attachment base64")

        round_trip = ChatCompletionRequest.model_validate(payload)
        assert_equal(1, len(round_trip.local_attachments or []), "round-trip local attachment count")
        assert_equal("notes.txt", round_trip.local_attachments[0].name, "round-trip local attachment name")

    def test_response_retrieval_attached_document_metadata() -> None:
        response = ChatCompletionResponse.model_validate(
            {
                "id": "chatcmpl_local",
                "object": "chat.completion",
                "created": 0,
                "model": "test-model",
                "choices": [
                    {
                        "index": 0,
                        "message": {"role": "assistant", "content": "done", "thinking": "hidden reasoning"},
                        "finish_reason": "stop",
                    }
                ],
                "usage": {
                    "prompt_tokens": 12,
                    "completion_tokens": 4,
                    "total_tokens": 16,
                    "tool_definition_tokens": 5,
                    "prompt_tokens_details": {
                        "cached_tokens": 3,
                        "tool_tokens": 5,
                    },
                    "completion_tokens_details": {
                        "reasoning_tokens": 7,
                    },
                },
                "tool_calls": [
                    {
                        "tool_call_id": "call_search",
                        "tool_name": "collection_search",
                        "display_label": "Searching collection",
                        "iteration": 1,
                        "sequence_number": 1,
                        "success": True,
                        "denied": False,
                        "truncated": False,
                        "output_characters": 128,
                        "result_count": 3,
                        "credits_used": 2,
                        "provider_latency_ms": 45.5,
                        "duration_ms": 12.5,
                        "summary": "Searching collection completed.",
                    }
                ],
                "retrieval": {
                    "collection_id": "col_abc123",
                    "duration_ms": 42.7,
                    "chunks_returned": 3,
                    "rerank_duration_ms": 5.5,
                    "rerank_input_count": 4,
                    "rerank_output_count": 2,
                    "attached_document_ids": ["adoc_one"],
                    "attached_documents": [
                        {
                            "Id": "adoc_one",
                            "Name": "Policy Handbook",
                            "OriginalFilename": "policy.pdf",
                            "ContentType": "application/pdf",
                            "SizeBytes": 12345,
                            "CreatedUtc": "2026-01-01T00:00:00Z",
                            "LastUpdateUtc": "2026-01-02T00:00:00Z",
                        }
                    ],
                    "document_filter_applied": True,
                    "chunks": [
                        {
                            "document_id": "adoc_one",
                            "score": 0.91,
                            "rerank_score": 8.5,
                            "fusion_score": 0.42,
                            "text_score": 0.66,
                            "content": "Local fixture content.",
                            "position": 7,
                        }
                    ],
                },
                "citations": {
                    "sources": [
                        {
                            "index": 1,
                            "document_id": "adoc_one",
                            "document_name": "Policy Handbook",
                            "content_type": "application/pdf",
                            "score": 0.91,
                            "rerank_score": 8.5,
                            "excerpt": "Local fixture content.",
                            "download_url": "/v1.0/assistants/asst_local/documents/adoc_one/download",
                        }
                    ],
                    "referenced_indices": [1],
                    "auto_populated": False,
                },
            }
        )

        assert_not_none(response.retrieval, "ChatCompletionResponse retrieval")
        assert_equal("hidden reasoning", response.choices[0].message.thinking, "response message thinking")
        retrieval = response.retrieval
        assert_equal("col_abc123", retrieval.collection_id, "retrieval collection ID")
        assert_equal(42.7, retrieval.duration_ms, "retrieval duration")
        assert_equal(3, retrieval.chunks_returned, "retrieval chunks returned")
        assert_equal(["adoc_one"], retrieval.attached_document_ids, "retrieval attached document IDs")
        assert_true(retrieval.document_filter_applied, "document filter applied")
        assert_not_none(retrieval.attached_documents, "attached document metadata")
        assert_equal("adoc_one", retrieval.attached_documents[0].id, "attached document metadata ID")
        assert_equal("policy.pdf", retrieval.attached_documents[0].original_filename, "attached document filename")
        assert_not_none(retrieval.chunks, "retrieval chunks")
        assert_equal("adoc_one", retrieval.chunks[0].document_id, "retrieval chunk document ID")
        assert_equal(8.5, retrieval.chunks[0].rerank_score, "retrieval chunk rerank score")

        payload = retrieval.model_dump(by_alias=True, exclude_none=True)
        assert_true("collection_id" in payload, "retrieval payload collection_id")
        assert_true("duration_ms" in payload, "retrieval payload duration_ms")
        assert_true("chunks_returned" in payload, "retrieval payload chunks_returned")
        assert_true("attached_document_ids" in payload, "retrieval payload attached_document_ids")
        assert_true("attached_documents" in payload, "retrieval payload attached_documents")
        assert_true("document_filter_applied" in payload, "retrieval payload document_filter_applied")
        assert_true("document_id" in payload["chunks"][0], "retrieval chunk payload document_id")
        assert_true("rerank_score" in payload["chunks"][0], "retrieval chunk payload rerank_score")

        doc_payload = payload["attached_documents"][0]
        assert_false("S3Key" in doc_payload, "selection metadata should not expose S3Key")
        assert_false("BucketName" in doc_payload, "selection metadata should not expose BucketName")
        assert_false("s3_key" in doc_payload, "selection metadata should not expose s3_key")
        assert_false("bucket_name" in doc_payload, "selection metadata should not expose bucket_name")

        assert_not_none(response.citations, "ChatCompletionResponse citations")
        assert_equal("adoc_one", response.citations.sources[0].document_id, "citation document ID")
        citation_payload = response.citations.model_dump(by_alias=True, exclude_none=True)
        assert_true("referenced_indices" in citation_payload, "citation payload referenced_indices")
        assert_true("document_id" in citation_payload["sources"][0], "citation payload document_id")

        assert_not_none(response.tool_calls, "ChatCompletionResponse tool_calls")
        assert_equal(1, len(response.tool_calls), "tool_calls count")
        assert_equal("collection_search", response.tool_calls[0].tool_name, "tool trace name")
        assert_equal("Searching collection", response.tool_calls[0].display_label, "tool trace label")
        assert_equal(3, response.tool_calls[0].result_count, "tool trace result count")
        assert_equal(2, response.tool_calls[0].credits_used, "tool trace credits used")
        assert_equal(45.5, response.tool_calls[0].provider_latency_ms, "tool trace provider latency")
        tool_payload = response.model_dump(by_alias=True, exclude_none=True)["tool_calls"][0]
        assert_true("tool_name" in tool_payload, "tool trace payload tool_name")
        assert_true("result_count" in tool_payload, "tool trace payload result_count")
        assert_true("credits_used" in tool_payload, "tool trace payload credits_used")
        assert_true("provider_latency_ms" in tool_payload, "tool trace payload provider_latency_ms")
        assert_false("ArgumentsJson" in tool_payload, "tool trace should not expose arguments")
        assert_false("OutputJson" in tool_payload, "tool trace should not expose raw output")

        assert_not_none(response.usage, "ChatCompletionResponse usage")
        assert_equal(16, response.usage.total_tokens, "usage total tokens")
        assert_equal(5, response.usage.tool_definition_tokens, "usage tool-definition tokens")
        assert_not_none(response.usage.prompt_tokens_details, "usage prompt token details")
        assert_equal(3, response.usage.prompt_tokens_details.cached_tokens, "usage cached tokens")
        assert_not_none(response.usage.completion_tokens_details, "usage completion token details")
        assert_equal(7, response.usage.completion_tokens_details.reasoning_tokens, "usage reasoning tokens")

        usage_payload = response.usage.model_dump(by_alias=True, exclude_none=True)
        assert_true("tool_definition_tokens" in usage_payload, "usage payload tool_definition_tokens")
        assert_true("completion_tokens_details" in usage_payload, "usage payload completion_tokens_details")

        tokens = AssistantTokenUsageTelemetry.model_validate(
            {"Input": 12, "Output": 4, "Total": 16, "Reasoning": 7, "ToolDefinitions": 5}
        )
        assert_equal(7, tokens.reasoning, "telemetry reasoning tokens")
        assert_equal(5, tokens.tool_definitions, "telemetry tool-definition tokens")

    def test_tool_policy_settings_round_trip() -> None:
        settings = AssistantSettings.model_validate(
            {
                "InferenceEndpointId": "cep_response",
                "ToolRoutingInferenceEndpointId": "cep_router",
                "RetrievalGateInferenceEndpointId": "cep_gate",
                "QueryRewriteInferenceEndpointId": "cep_rewrite",
                "RerankInferenceEndpointId": "cep_rerank",
                "EmbeddingEndpointId": "eep_embed",
                "ExposeThinking": True,
                "ToolPolicyJson": "{}",
                "ToolPolicy": {
                    "EnableToolCalls": True,
                    "EnableCollectionSearchTool": True,
                    "EnableDocumentAtomExtractionTool": True,
                    "EnableWebSearchTool": True,
                    "ToolChoiceMode": "Required",
                    "MaxToolIterations": 4,
                    "MaxToolResultItems": 9,
                    "AllowedToolNames": ["collection_search"],
                    "MaxSearchTopK": 7,
                    "MaxDocumentsConsideredPerSearch": 25,
                    "MaxResultsConsideredPerSearch": 50,
                    "MaxAtomExtractionBytes": 2097152,
                    "MaxAtomExtractionCharacters": 24000,
                    "AllowedSearchModes": ["FullText"],
                    "ReturnFullSearchContent": True,
                    "MaxWebResults": 3,
                    "TavilyEndpoint": "https://assistant.tavily.test/search",
                    "TavilyApiKey": "assistant-key",
                    "AllowUngovernedWebAccess": True,
                    "AllowedWebDomains": ["example.com"],
                    "BlockedWebDomains": ["blocked.example"],
                },
            }
        )

        assert_equal("cep_response", settings.inference_endpoint_id, "settings InferenceEndpointId")
        assert_equal("cep_router", settings.tool_routing_inference_endpoint_id, "settings ToolRoutingInferenceEndpointId")
        assert_equal("cep_gate", settings.retrieval_gate_inference_endpoint_id, "settings RetrievalGateInferenceEndpointId")
        assert_equal("cep_rewrite", settings.query_rewrite_inference_endpoint_id, "settings QueryRewriteInferenceEndpointId")
        assert_equal("cep_rerank", settings.rerank_inference_endpoint_id, "settings RerankInferenceEndpointId")
        assert_equal(True, settings.expose_thinking, "settings ExposeThinking")
        assert_equal("eep_embed", settings.embedding_endpoint_id, "settings EmbeddingEndpointId")
        assert_equal("{}", settings.tool_policy_json, "settings ToolPolicyJson")
        assert_not_none(settings.tool_policy, "settings ToolPolicy")
        assert_true(settings.tool_policy.enable_tool_calls, "settings EnableToolCalls")
        assert_true(settings.tool_policy.enable_collection_search_tool, "settings EnableCollectionSearchTool")
        assert_true(settings.tool_policy.enable_document_atom_extraction_tool, "settings EnableDocumentAtomExtractionTool")
        assert_true(settings.tool_policy.enable_web_search_tool, "settings EnableWebSearchTool")
        assert_equal("Required", settings.tool_policy.tool_choice_mode, "settings ToolChoiceMode")
        assert_equal(4, settings.tool_policy.max_tool_iterations, "settings MaxToolIterations")
        assert_equal(9, settings.tool_policy.max_tool_result_items, "settings MaxToolResultItems")
        assert_equal(["collection_search"], settings.tool_policy.allowed_tool_names, "settings AllowedToolNames")
        assert_equal(7, settings.tool_policy.max_search_top_k, "settings MaxSearchTopK")
        assert_equal(25, settings.tool_policy.max_documents_considered_per_search, "settings MaxDocumentsConsideredPerSearch")
        assert_equal(50, settings.tool_policy.max_results_considered_per_search, "settings MaxResultsConsideredPerSearch")
        assert_equal(2097152, settings.tool_policy.max_atom_extraction_bytes, "settings MaxAtomExtractionBytes")
        assert_equal(24000, settings.tool_policy.max_atom_extraction_characters, "settings MaxAtomExtractionCharacters")
        assert_equal(["FullText"], settings.tool_policy.allowed_search_modes, "settings AllowedSearchModes")
        assert_true(settings.tool_policy.return_full_search_content, "settings ReturnFullSearchContent")
        assert_equal(3, settings.tool_policy.max_web_results, "settings MaxWebResults")
        assert_equal("https://assistant.tavily.test/search", settings.tool_policy.tavily_endpoint, "settings TavilyEndpoint")
        assert_equal("assistant-key", settings.tool_policy.tavily_api_key, "settings TavilyApiKey")
        assert_true(settings.tool_policy.allow_ungoverned_web_access, "settings AllowUngovernedWebAccess")
        assert_equal(["example.com"], settings.tool_policy.allowed_web_domains, "settings AllowedWebDomains")

        payload = settings.model_dump(by_alias=True, exclude_none=True)
        assert_equal("cep_router", payload["toolRoutingInferenceEndpointId"], "settings payload toolRoutingInferenceEndpointId")
        assert_true("ToolPolicyJson" in payload, "settings payload ToolPolicyJson")
        assert_true("ToolPolicy" in payload, "settings payload ToolPolicy")
        assert_true("EnableToolCalls" in payload["ToolPolicy"], "settings payload EnableToolCalls")
        assert_true("ToolChoiceMode" in payload["ToolPolicy"], "settings payload ToolChoiceMode")
        assert_true("AllowedToolNames" in payload["ToolPolicy"], "settings payload AllowedToolNames")
        assert_true("AllowedSearchModes" in payload["ToolPolicy"], "settings payload AllowedSearchModes")
        assert_true("MaxDocumentsConsideredPerSearch" in payload["ToolPolicy"], "settings payload MaxDocumentsConsideredPerSearch")
        assert_true("MaxResultsConsideredPerSearch" in payload["ToolPolicy"], "settings payload MaxResultsConsideredPerSearch")
        assert_true("ReturnFullSearchContent" in payload["ToolPolicy"], "settings payload ReturnFullSearchContent")
        assert_true("TavilyEndpoint" in payload["ToolPolicy"], "settings payload TavilyEndpoint")
        assert_true("AllowUngovernedWebAccess" in payload["ToolPolicy"], "settings payload AllowUngovernedWebAccess")
        assert_true("AllowedWebDomains" in payload["ToolPolicy"], "settings payload AllowedWebDomains")
        assert_false("toolPolicy" in payload, "settings payload should not use lower-camel ToolPolicy")
        assert_false("enableToolCalls" in payload["ToolPolicy"], "policy payload should not use lower-camel EnableToolCalls")

        legacy_policy = AssistantToolPolicy.model_validate(
            {
                "enableToolCalls": True,
                "enableCollectionSearchTool": True,
                "returnFullSearchContent": True,
                "maxDocumentsConsideredPerSearch": 11,
                "maxResultsConsideredPerSearch": 22,
                "allowedWebDomains": ["legacy.example"],
            }
        )
        assert_true(legacy_policy.enable_tool_calls, "legacy EnableToolCalls alias")
        assert_true(legacy_policy.enable_collection_search_tool, "legacy collection search alias")
        assert_true(legacy_policy.return_full_search_content, "legacy ReturnFullSearchContent alias")
        assert_equal(11, legacy_policy.max_documents_considered_per_search, "legacy MaxDocumentsConsideredPerSearch alias")
        assert_equal(22, legacy_policy.max_results_considered_per_search, "legacy MaxResultsConsideredPerSearch alias")
        assert_equal(["legacy.example"], legacy_policy.allowed_web_domains, "legacy allowed web domains")

        validation_request = AssistantToolPolicyValidationRequest(tool_policy=legacy_policy)
        validation_payload = validation_request.model_dump(by_alias=True, exclude_none=True)
        assert_true("ToolPolicy" in validation_payload, "validation request ToolPolicy")
        assert_true("EnableToolCalls" in validation_payload["ToolPolicy"], "validation request EnableToolCalls")

        validation_result = AssistantToolPolicyValidationResult.model_validate(
            {
                "Success": False,
                "Message": "Policy invalid.",
                "ToolPolicyJson": "{}",
                "ToolPolicy": {
                    "EnableToolCalls": True,
                    "EnableCollectionSearchTool": True,
                    "TavilyEndpoint": "https://assistant.tavily.test/search",
                    "AllowedWebDomains": ["example.com"],
                },
                "Tools": [],
                "Errors": ["EnableToolCalls is true but no enabled tool is currently executable."],
                "ErrorCodes": ["no_available_tools"],
            }
        )
        assert_true(not validation_result.success, "validation result success")
        assert_not_none(validation_result.tool_policy, "validation result ToolPolicy")
        assert_true(validation_result.tool_policy.enable_collection_search_tool, "validation result collection search")
        assert_equal("https://assistant.tavily.test/search", validation_result.tool_policy.tavily_endpoint, "validation result TavilyEndpoint")
        assert_equal(["example.com"], validation_result.tool_policy.allowed_web_domains, "validation result AllowedWebDomains")
        assert_equal(["no_available_tools"], validation_result.error_codes, "validation result ErrorCodes")

        diagnostics_result = AssistantToolPolicyTestResult.model_validate(
            {
                "Success": False,
                "Message": "Tool diagnostics found blocking issues.",
                "AssistantId": "asst_local",
                "InferenceEndpointId": "cep_local",
                "ToolRoutingInferenceEndpointId": "cep_router",
                "EffectiveToolRoutingInferenceEndpointId": "cep_router",
                "EndpointResolved": True,
                "EndpointModel": "qwen3-tool",
                "EndpointApiFormat": "OpenAI",
                "EndpointActive": True,
                "EndpointSupportsToolCalling": False,
                "EndpointToolCallingApiFormat": None,
                "EndpointSupportsParallelToolCalls": False,
                "EndpointSupportsStreamingToolCalls": False,
                "Validation": {"Success": True, "Errors": [], "ErrorCodes": []},
                "Tools": [],
                "Warnings": [],
                "Errors": ["The effective tool-routing completion endpoint does not explicitly support tool calling."],
                "ErrorCodes": ["tool_routing_endpoint_not_tool_capable"],
            }
        )
        assert_true(not diagnostics_result.success, "diagnostics result success")
        assert_equal("cep_router", diagnostics_result.tool_routing_inference_endpoint_id, "diagnostics configured tool routing endpoint")
        assert_equal("cep_router", diagnostics_result.effective_tool_routing_inference_endpoint_id, "diagnostics effective tool routing endpoint")
        assert_true(diagnostics_result.endpoint_resolved, "diagnostics endpoint resolved")
        assert_equal("qwen3-tool", diagnostics_result.endpoint_model, "diagnostics endpoint model")
        assert_equal(["tool_routing_endpoint_not_tool_capable"], diagnostics_result.error_codes, "diagnostics ErrorCodes")

        endpoint = PartioEndpointConfig.model_validate(
            {
                "Id": "ep_tool",
                "Model": "qwen3",
                "Endpoint": "http://localhost:11434",
                "ApiFormat": "OpenAI",
                "SupportsToolCalling": True,
                "ToolCallingApiFormat": "OpenAIChatCompletions",
                "SupportsParallelToolCalls": True,
                "SupportsStreamingToolCalls": True,
            }
        )
        assert_true(endpoint.supports_tool_calling, "endpoint SupportsToolCalling")
        assert_equal("OpenAIChatCompletions", endpoint.tool_calling_api_format, "endpoint ToolCallingApiFormat")

        endpoint_payload = PartioEndpointRequest(
            model="qwen3",
            endpoint="http://localhost:11434",
            api_format="OpenAI",
            supports_tool_calling=True,
            tool_calling_api_format="OpenAIChatCompletions",
            supports_parallel_tool_calls=True,
            supports_streaming_tool_calls=True,
        ).model_dump(by_alias=True, exclude_none=True)
        assert_true(endpoint_payload["supportsToolCalling"], "endpoint payload supportsToolCalling")
        assert_equal("OpenAIChatCompletions", endpoint_payload["toolCallingApiFormat"], "endpoint payload toolCallingApiFormat")

    def test_external_search_status_model_and_route() -> None:
        status = ExternalSearchConfigurationStatus.model_validate(
            {
                "enabled": True,
                "enabledProviders": 1,
                "configuredProviders": 1,
                "misconfiguredProviders": 0,
            }
        )
        assert_true(status.enabled, "external-search status enabled")
        assert_equal(1, status.configured_providers, "external-search configured providers")
        payload = status.model_dump(by_alias=True)
        assert_true("ConfiguredProviders" in payload, "external-search status uses server aliases")
        assert_false("ApiKey" in payload, "external-search status must not expose secrets")

        class Response:
            def json(self) -> dict[str, Any]:
                return {
                    "Enabled": True,
                    "EnabledProviders": 1,
                    "ConfiguredProviders": 1,
                    "MisconfiguredProviders": 0,
                }

        class ProbeClient(AssistantHubClient):
            def __init__(self) -> None:
                super().__init__("http://localhost:6600", api_key="test-key")
                self.captured_method: Optional[str] = None
                self.captured_path: Optional[str] = None

            def _request(self, method: str, path: str, **kwargs: Any) -> Response:
                self.captured_method = method
                self.captured_path = path
                return Response()

        client = ProbeClient()
        try:
            result = client.get_external_search_status()
            assert_equal("GET", client.captured_method, "external-search status method")
            assert_equal("/v1.0/configuration/external-search/status", client.captured_path, "external-search status path")
            assert_true(result.enabled, "external-search status client enabled")
        finally:
            client.close()

    def test_assistant_tool_call_trace_routes() -> None:
        class Response:
            def __init__(self, payload: dict[str, Any]) -> None:
                self._payload = payload

            def json(self) -> dict[str, Any]:
                return self._payload

        class ProbeClient(AssistantHubClient):
            def __init__(self) -> None:
                super().__init__("http://localhost:6600", api_key="test-key")
                self.requests: list[dict[str, Any]] = []

            def _request(self, method: str, path: str, **kwargs: Any) -> Response:
                self.requests.append({"method": method, "path": path, "kwargs": kwargs})
                if method == "GET" and path == "/v1.0/assistants/asst_local/tool-calls":
                    return Response(
                        {
                            "Success": True,
                            "MaxResults": 5,
                            "TotalRecords": 1,
                            "RecordsRemaining": 0,
                            "EndOfResults": True,
                            "Objects": [
                                {
                                    "Id": "atc_local",
                                    "AssistantId": "asst_local",
                                    "TraceId": "trace_local",
                                    "ToolName": "collection_search",
                                    "ArgumentsJson": "[redacted]",
                                    "Success": True,
                                }
                            ],
                        }
                    )
                if method == "GET" and path == "/v1.0/assistants/asst_local/tool-calls/atc_local":
                    return Response(
                        {
                            "Id": "atc_local",
                            "AssistantId": "asst_local",
                            "ToolName": "collection_search",
                            "Success": True,
                        }
                    )
                if method == "DELETE" and path == "/v1.0/assistants/asst_local/tool-calls":
                    return Response({"DeletedCount": 1})
                if method == "DELETE" and path == "/v1.0/assistants/asst_local/tool-calls/atc_local":
                    return Response({})
                return Response({})

        client = ProbeClient()
        try:
            listed = client.list_assistant_tool_calls(
                "asst_local",
                max_results=5,
                trace_id="trace_local",
                tool_name="collection_search",
                success=True,
            )
            assert_equal(1, len(listed.objects), "tool-call list count")
            assert_equal("atc_local", listed.objects[0].id, "tool-call list id")
            assert_equal("collection_search", listed.objects[0].tool_name, "tool-call list tool")
            assert_false("secret" in (listed.objects[0].arguments_json or "").lower(), "tool-call list arguments redacted")

            record = client.get_assistant_tool_call("asst_local", "atc_local")
            assert_equal("atc_local", record.id, "tool-call get id")

            deleted = client.delete_assistant_tool_calls("asst_local", tool_name="collection_search")
            assert_equal(1, deleted.deleted_count, "tool-call bulk delete count")

            client.delete_assistant_tool_call("asst_local", "atc_local")

            assert_equal("GET", client.requests[0]["method"], "tool-call list method")
            assert_equal("/v1.0/assistants/asst_local/tool-calls", client.requests[0]["path"], "tool-call list path")
            assert_equal("trace_local", client.requests[0]["kwargs"]["params"]["traceId"], "tool-call list trace query")
            assert_equal("collection_search", client.requests[0]["kwargs"]["params"]["toolName"], "tool-call list tool query")
            assert_equal(True, client.requests[0]["kwargs"]["params"]["success"], "tool-call list success query")
            assert_equal("GET", client.requests[1]["method"], "tool-call get method")
            assert_equal("/v1.0/assistants/asst_local/tool-calls/atc_local", client.requests[1]["path"], "tool-call get path")
            assert_equal("DELETE", client.requests[2]["method"], "tool-call bulk delete method")
            assert_equal("collection_search", client.requests[2]["kwargs"]["params"]["toolName"], "tool-call bulk delete query")
            assert_equal("DELETE", client.requests[3]["method"], "tool-call delete method")
            assert_equal("/v1.0/assistants/asst_local/tool-calls/atc_local", client.requests[3]["path"], "tool-call delete path")
        finally:
            client.close()

    def test_async_assistant_tool_call_trace_routes() -> None:
        class Response:
            def __init__(self, payload: dict[str, Any]) -> None:
                self._payload = payload

            def json(self) -> dict[str, Any]:
                return self._payload

        class ProbeClient(AsyncAssistantHubClient):
            def __init__(self) -> None:
                super().__init__("http://localhost:6600", api_key="test-key")
                self.requests: list[dict[str, Any]] = []

            async def _request(self, method: str, path: str, **kwargs: Any) -> Response:
                self.requests.append({"method": method, "path": path, "kwargs": kwargs})
                if method == "GET" and path == "/v1.0/assistants/asst_local/tool-calls":
                    return Response(
                        {
                            "Success": True,
                            "MaxResults": 5,
                            "TotalRecords": 1,
                            "RecordsRemaining": 0,
                            "EndOfResults": True,
                            "Objects": [
                                {
                                    "Id": "atc_local",
                                    "AssistantId": "asst_local",
                                    "TraceId": "trace_local",
                                    "ToolName": "collection_search",
                                    "ArgumentsJson": "[redacted]",
                                    "Success": True,
                                }
                            ],
                        }
                    )
                if method == "GET" and path == "/v1.0/assistants/asst_local/tool-calls/atc_local":
                    return Response(
                        {
                            "Id": "atc_local",
                            "AssistantId": "asst_local",
                            "ToolName": "collection_search",
                            "Success": True,
                        }
                    )
                if method == "DELETE" and path == "/v1.0/assistants/asst_local/tool-calls":
                    return Response({"DeletedCount": 1})
                if method == "DELETE" and path == "/v1.0/assistants/asst_local/tool-calls/atc_local":
                    return Response({})
                return Response({})

        async def run_probe() -> None:
            client = ProbeClient()
            try:
                listed = await client.list_assistant_tool_calls(
                    "asst_local",
                    max_results=5,
                    trace_id="trace_local",
                    tool_name="collection_search",
                    success=True,
                )
                assert_equal(1, len(listed.objects), "async tool-call list count")
                assert_equal("atc_local", listed.objects[0].id, "async tool-call list id")

                record = await client.get_assistant_tool_call("asst_local", "atc_local")
                assert_equal("atc_local", record.id, "async tool-call get id")

                deleted = await client.delete_assistant_tool_calls("asst_local", tool_name="collection_search")
                assert_equal(1, deleted.deleted_count, "async tool-call bulk delete count")

                await client.delete_assistant_tool_call("asst_local", "atc_local")

                assert_equal("GET", client.requests[0]["method"], "async tool-call list method")
                assert_equal("/v1.0/assistants/asst_local/tool-calls", client.requests[0]["path"], "async tool-call list path")
                assert_equal("trace_local", client.requests[0]["kwargs"]["params"]["traceId"], "async tool-call list trace query")
                assert_equal("collection_search", client.requests[0]["kwargs"]["params"]["toolName"], "async tool-call list tool query")
                assert_equal(True, client.requests[0]["kwargs"]["params"]["success"], "async tool-call list success query")
                assert_equal("GET", client.requests[1]["method"], "async tool-call get method")
                assert_equal("/v1.0/assistants/asst_local/tool-calls/atc_local", client.requests[1]["path"], "async tool-call get path")
                assert_equal("DELETE", client.requests[2]["method"], "async tool-call bulk delete method")
                assert_equal("collection_search", client.requests[2]["kwargs"]["params"]["toolName"], "async tool-call bulk delete query")
                assert_equal("DELETE", client.requests[3]["method"], "async tool-call delete method")
                assert_equal("/v1.0/assistants/asst_local/tool-calls/atc_local", client.requests[3]["path"], "async tool-call delete path")
            finally:
                await client.close()

        asyncio.run(run_probe())

    def test_chat_history_attached_document_metadata() -> None:
        history = ChatHistory.model_validate(
            {
                "Id": "chist_local",
                "TenantId": "default",
                "ThreadId": "thr_local",
                "AssistantId": "asst_local",
                "AttachedDocumentIdsJson": "[\"adoc_one\"]",
                "AttachedDocumentsJson": "[{\"Id\":\"adoc_one\",\"Name\":\"Policy Handbook\"}]",
                "CreatedUtc": "2026-01-01T00:00:00Z",
                "LastUpdateUtc": "2026-01-01T00:00:00Z",
            }
        )

        assert_equal("chist_local", history.id, "history ID")
        assert_true("adoc_one" in history.attached_document_ids_json, "history attached document IDs JSON")
        assert_true("Policy Handbook" in history.attached_documents_json, "history attached documents JSON")

    runner.run_test("SDK contract: ChatCompletionRequest serializes attached_document_ids", test_request_attached_document_ids)
    runner.run_test("SDK contract: ChatCompletionRequest serializes local_attachments", test_request_local_attachments)
    runner.run_test(
        "SDK contract: ChatCompletionResponse parses attached document retrieval metadata",
        test_response_retrieval_attached_document_metadata,
    )
    runner.run_test("SDK contract: AssistantToolPolicy settings round-trip", test_tool_policy_settings_round_trip)
    runner.run_test("SDK contract: ExternalSearch status model and route", test_external_search_status_model_and_route)
    runner.run_test("SDK contract: assistant tool-call trace routes", test_assistant_tool_call_trace_routes)
    runner.run_test("SDK contract: async assistant tool-call trace routes", test_async_assistant_tool_call_trace_routes)
    runner.run_test(
        "SDK contract: ChatHistory parses attached document metadata",
        test_chat_history_attached_document_metadata,
    )


# ---------------------------------------------------------------------------
# Test groups
# ---------------------------------------------------------------------------

def run_health_tests(runner: TestRunner, client: AssistantHubClient) -> None:
    def test_health_check() -> None:
        result = client.health_check()
        assert_not_none(result, "HealthCheck result")

    def test_whoami() -> None:
        identity = client.whoami()
        assert_not_none(identity, "WhoAmI result")
        assert_true(isinstance(identity, dict), "WhoAmI should return a valid JSON object")

    runner.run_test("Health: HealthCheck returns true", test_health_check)
    runner.run_test("Health: WhoAmI returns authenticated identity", test_whoami)


def run_tenant_tests(runner: TestRunner, client: AssistantHubClient) -> None:
    created_tenant_id: list[Optional[str]] = [None]
    created_user_id: list[Optional[str]] = [None]
    created_credential_id: list[Optional[str]] = [None]
    suffix = unique_suffix()

    def test_list_tenants() -> None:
        result = client.list_tenants()
        assert_not_none(result, "ListTenants result")
        assert_not_none(result.objects, "ListTenants result.objects")
        assert_gte(len(result.objects), 1, "ListTenants count")

    def test_create_tenant() -> None:
        tenant = TenantMetadata(name="test-tenant-" + suffix, active=True)
        response = client.create_tenant(tenant)
        assert_true(isinstance(response, dict), "CreateTenant should return a JSON object")
        tenant_data = response.get("Tenant", {})
        created_tenant_id[0] = tenant_data.get("Id")
        assert_not_none(created_tenant_id[0], "Created tenant ID")
        assert_starts_with(created_tenant_id[0], "ten_", "Created tenant ID prefix")

    def test_get_tenant() -> None:
        assert_not_none(created_tenant_id[0], "createdTenantId from previous test")
        tenant = client.get_tenant(created_tenant_id[0])
        assert_not_none(tenant, "GetTenant result")
        assert_equal(created_tenant_id[0], tenant.id, "Tenant ID")
        assert_equal("test-tenant-" + suffix, tenant.name, "Tenant Name")

    def test_update_tenant() -> None:
        assert_not_none(created_tenant_id[0], "createdTenantId from previous test")
        tenant = TenantMetadata(name="test-tenant-updated-" + suffix, active=True)
        updated = client.update_tenant(created_tenant_id[0], tenant)
        assert_not_none(updated, "UpdateTenant result")
        assert_equal("test-tenant-updated-" + suffix, updated.name, "Updated tenant name")

    def test_list_users() -> None:
        assert_not_none(created_tenant_id[0], "createdTenantId from previous test")
        result = client.list_users(created_tenant_id[0])
        assert_not_none(result, "ListUsers result")
        assert_not_none(result.objects, "ListUsers result.objects")

    def test_create_user() -> None:
        assert_not_none(created_tenant_id[0], "createdTenantId from previous test")
        user = UserMaster(
            first_name="Test",
            last_name="User",
            email="testuser-" + suffix + "@example.com",
            active=True,
        )
        created = client.create_user(created_tenant_id[0], user)
        assert_not_none(created, "CreateUser result")
        assert_not_none(created.id, "Created user ID")
        assert_starts_with(created.id, "usr_", "Created user ID prefix")
        created_user_id[0] = created.id

    def test_create_credential() -> None:
        assert_not_none(created_tenant_id[0], "createdTenantId from previous test")
        credential = Credential(name="test-cred-" + suffix, active=True)
        created = client.create_credential(created_tenant_id[0], credential)
        assert_not_none(created, "CreateCredential result")
        assert_not_none(created.id, "Created credential ID")
        assert_starts_with(created.id, "cred_", "Created credential ID prefix")
        created_credential_id[0] = created.id

    def test_delete_credential() -> None:
        assert_not_none(created_tenant_id[0], "createdTenantId from previous test")
        assert_not_none(created_credential_id[0], "createdCredentialId from previous test")
        client.delete_credential(created_tenant_id[0], created_credential_id[0])

    def test_delete_user() -> None:
        assert_not_none(created_tenant_id[0], "createdTenantId from previous test")
        assert_not_none(created_user_id[0], "createdUserId from previous test")
        client.delete_user(created_tenant_id[0], created_user_id[0])

    def test_delete_tenant() -> None:
        assert_not_none(created_tenant_id[0], "createdTenantId from previous test")
        client.delete_tenant(created_tenant_id[0])

    runner.run_test("Tenant: List tenants returns results", test_list_tenants)
    runner.run_test("Tenant: Create tenant with unique name", test_create_tenant)
    runner.run_test("Tenant: Get tenant by ID", test_get_tenant)
    runner.run_test("Tenant: Update tenant name", test_update_tenant)
    runner.run_test("Tenant: List users in tenant", test_list_users)
    runner.run_test("Tenant: Create user in tenant", test_create_user)
    runner.run_test("Tenant: Create credential in tenant", test_create_credential)
    runner.run_test("Tenant: Delete credential", test_delete_credential)
    runner.run_test("Tenant: Delete user", test_delete_user)
    runner.run_test("Tenant: Delete tenant", test_delete_tenant)


def run_assistant_tests(runner: TestRunner, client: AssistantHubClient) -> None:
    created_assistant_id: list[Optional[str]] = [None]
    suffix = unique_suffix()

    def test_create() -> None:
        assistant = Assistant(
            name="test-assistant-" + suffix,
            description="Test assistant created by SDK tests",
        )
        created = client.create_assistant(assistant)
        assert_not_none(created, "CreateAssistant result")
        assert_not_none(created.id, "Created assistant ID")
        assert_starts_with(created.id, "asst_", "Created assistant ID prefix")
        assert_equal("test-assistant-" + suffix, created.name, "Created assistant name")
        created_assistant_id[0] = created.id

    def test_list() -> None:
        assert_not_none(created_assistant_id[0], "createdAssistantId from previous test")
        result = client.list_assistants()
        assert_not_none(result, "ListAssistants result")
        assert_not_none(result.objects, "ListAssistants result.objects")
        found = any(a.id == created_assistant_id[0] for a in result.objects)
        assert_true(found, "Created assistant should appear in list")

    def test_get() -> None:
        assert_not_none(created_assistant_id[0], "createdAssistantId from previous test")
        assistant = client.get_assistant(created_assistant_id[0])
        assert_not_none(assistant, "GetAssistant result")
        assert_equal(created_assistant_id[0], assistant.id, "Assistant ID")
        assert_equal("test-assistant-" + suffix, assistant.name, "Assistant name")

    def test_update() -> None:
        assert_not_none(created_assistant_id[0], "createdAssistantId from previous test")
        assistant = Assistant(
            name="test-assistant-updated-" + suffix,
            description="Updated description",
        )
        updated = client.update_assistant(created_assistant_id[0], assistant)
        assert_not_none(updated, "UpdateAssistant result")
        assert_equal("test-assistant-updated-" + suffix, updated.name, "Updated assistant name")

    def test_delete() -> None:
        assert_not_none(created_assistant_id[0], "createdAssistantId from previous test")
        client.delete_assistant(created_assistant_id[0])

    def test_verify_deleted() -> None:
        assert_not_none(created_assistant_id[0], "createdAssistantId from previous test")
        result = client.list_assistants()
        assert_not_none(result, "ListAssistants result")
        assert_not_none(result.objects, "ListAssistants result.objects")
        found = any(a.id == created_assistant_id[0] for a in result.objects)
        assert_false(found, "Deleted assistant should not appear in list")

    runner.run_test("Assistant: Create assistant with name and description", test_create)
    runner.run_test("Assistant: List assistants includes created one", test_list)
    runner.run_test("Assistant: Get assistant by ID", test_get)
    runner.run_test("Assistant: Update assistant name", test_update)
    runner.run_test("Assistant: Delete assistant", test_delete)
    runner.run_test("Assistant: Verify assistant no longer in list", test_verify_deleted)


def run_collection_tests(runner: TestRunner, client: AssistantHubClient) -> None:
    created_collection_id: list[Optional[str]] = [None]
    suffix = unique_suffix()

    def test_create() -> None:
        collection = {"Name": "test-collection-" + suffix}
        created = client.create_collection(collection)
        assert_not_none(created, "CreateCollection result")
        assert_not_none(created.get("Id"), "Created collection ID")
        assert_equal("test-collection-" + suffix, created.get("Name"), "Created collection name")
        created_collection_id[0] = created["Id"]

    def test_list() -> None:
        assert_not_none(created_collection_id[0], "createdCollectionId from previous test")
        result = client.list_collections()
        assert_not_none(result, "ListCollections result")
        objects = result.get("objects") or result.get("Objects") or []
        assert_not_none(objects, "ListCollections objects")
        found = any(
            (c.get("Id") or c.get("id")) == created_collection_id[0] for c in objects
        )
        assert_true(found, "Created collection should appear in list")

    def test_get() -> None:
        assert_not_none(created_collection_id[0], "createdCollectionId from previous test")
        collection = client.get_collection(created_collection_id[0])
        assert_not_none(collection, "GetCollection result")
        assert_equal(created_collection_id[0], collection.get("Id"), "Collection ID")
        assert_equal("test-collection-" + suffix, collection.get("Name"), "Collection name")

    def test_update() -> None:
        assert_not_none(created_collection_id[0], "createdCollectionId from previous test")
        collection = {"Name": "test-collection-updated-" + suffix}
        updated = client.update_collection(created_collection_id[0], collection)
        assert_not_none(updated, "UpdateCollection result")
        assert_equal(
            "test-collection-updated-" + suffix, updated.get("Name"), "Updated collection name"
        )

    def test_delete() -> None:
        assert_not_none(created_collection_id[0], "createdCollectionId from previous test")
        client.delete_collection(created_collection_id[0])

    def test_verify_deleted() -> None:
        assert_not_none(created_collection_id[0], "createdCollectionId from previous test")
        result = client.list_collections()
        assert_not_none(result, "ListCollections result")
        objects = result.get("objects") or result.get("Objects") or []
        found = any(
            (c.get("Id") or c.get("id")) == created_collection_id[0] for c in objects
        )
        assert_false(found, "Deleted collection should not appear in list")

    runner.run_test("Collection: Create collection with name", test_create)
    runner.run_test("Collection: List collections includes created one", test_list)
    runner.run_test("Collection: Get collection by ID", test_get)
    runner.run_test("Collection: Update collection name", test_update)
    runner.run_test("Collection: Delete collection", test_delete)
    runner.run_test("Collection: Verify collection no longer in list", test_verify_deleted)


def run_document_tests(runner: TestRunner, client: AssistantHubClient) -> None:
    created_collection_id: list[Optional[str]] = [None]
    ingestion_rule_id: list[Optional[str]] = [None]
    uploaded_document_id: list[Optional[str]] = [None]
    second_document_id: list[Optional[str]] = [None]
    suffix = unique_suffix()

    def test_create_collection() -> None:
        collection = {"Name": "test-doc-collection-" + suffix}
        created = client.create_collection(collection)
        assert_not_none(created, "CreateCollection result")
        assert_not_none(created.get("Id"), "Created collection ID")
        created_collection_id[0] = created["Id"]

    def test_get_ingestion_rule() -> None:
        assert_not_none(created_collection_id[0], "createdCollectionId from previous test")
        rules = client.list_ingestion_rules()
        assert_not_none(rules, "ListIngestionRules result")
        assert_not_none(rules.objects, "ListIngestionRules result.objects")
        rule = next(
            (r for r in rules.objects if r.collection_id == created_collection_id[0]),
            None,
        )
        assert_not_none(rule, "Ingestion rule for created collection")
        assert_starts_with(rule.id, "irule_", "Ingestion rule ID prefix")
        ingestion_rule_id[0] = rule.id

    def test_upload() -> None:
        assert_not_none(ingestion_rule_id[0], "ingestionRuleId from previous test")
        content = b"This is a test document for SDK testing. It contains sample text content."
        document = client.upload_document(
            ingestion_rule_id[0],
            content,
            name="test-document-" + suffix,
            original_filename="test-document-" + suffix + ".txt",
            content_type="text/plain",
        )
        assert_not_none(document, "UploadDocument result")
        assert_not_none(document.id, "Uploaded document ID")
        assert_starts_with(document.id, "adoc_", "Document ID prefix")
        uploaded_document_id[0] = document.id

    def test_list_documents() -> None:
        assert_not_none(uploaded_document_id[0], "uploadedDocumentId from previous test")
        result = client.list_documents()
        assert_not_none(result, "ListDocuments result")
        assert_not_none(result.objects, "ListDocuments result.objects")
        found = any(d.id == uploaded_document_id[0] for d in result.objects)
        assert_true(found, "Uploaded document should appear in list")

    def test_get_document() -> None:
        assert_not_none(uploaded_document_id[0], "uploadedDocumentId from previous test")
        document = client.get_document(uploaded_document_id[0])
        assert_not_none(document, "GetDocument result")
        assert_equal(uploaded_document_id[0], document.id, "Document ID")

    def test_delete_document() -> None:
        assert_not_none(uploaded_document_id[0], "uploadedDocumentId from previous test")
        client.delete_document(uploaded_document_id[0])

    def test_verify_deleted() -> None:
        assert_not_none(uploaded_document_id[0], "uploadedDocumentId from previous test")
        result = client.list_documents()
        assert_not_none(result, "ListDocuments result")
        assert_not_none(result.objects, "ListDocuments result.objects")
        found = any(d.id == uploaded_document_id[0] for d in result.objects)
        assert_false(found, "Deleted document should not appear in list")

    def test_upload_two_for_bulk() -> None:
        assert_not_none(ingestion_rule_id[0], "ingestionRuleId from previous test")
        content1 = b"Bulk delete test document one."
        content2 = b"Bulk delete test document two."

        doc1 = client.upload_document(
            ingestion_rule_id[0],
            content1,
            name="bulk-doc-1-" + suffix,
            original_filename="bulk-doc-1-" + suffix + ".txt",
            content_type="text/plain",
        )
        doc2 = client.upload_document(
            ingestion_rule_id[0],
            content2,
            name="bulk-doc-2-" + suffix,
            original_filename="bulk-doc-2-" + suffix + ".txt",
            content_type="text/plain",
        )
        assert_not_none(doc1, "First bulk document")
        assert_not_none(doc1.id, "First bulk document ID")
        assert_not_none(doc2, "Second bulk document")
        assert_not_none(doc2.id, "Second bulk document ID")
        uploaded_document_id[0] = doc1.id
        second_document_id[0] = doc2.id

    def test_bulk_delete() -> None:
        assert_not_none(uploaded_document_id[0], "uploadedDocumentId from previous test")
        assert_not_none(second_document_id[0], "secondDocumentId from previous test")
        client.bulk_delete_documents([uploaded_document_id[0], second_document_id[0]])

    def test_verify_bulk_deleted() -> None:
        assert_not_none(uploaded_document_id[0], "uploadedDocumentId from previous test")
        assert_not_none(second_document_id[0], "secondDocumentId from previous test")
        result = client.list_documents()
        assert_not_none(result, "ListDocuments result")
        assert_not_none(result.objects, "ListDocuments result.objects")
        found_first = any(d.id == uploaded_document_id[0] for d in result.objects)
        found_second = any(d.id == second_document_id[0] for d in result.objects)
        assert_false(found_first, "First bulk deleted document should not appear in list")
        assert_false(found_second, "Second bulk deleted document should not appear in list")

    def test_cleanup_collection() -> None:
        assert_not_none(created_collection_id[0], "createdCollectionId from previous test")
        client.delete_collection(created_collection_id[0])

    runner.run_test("Document: Create collection for document tests", test_create_collection)
    runner.run_test("Document: Get ingestion rule for collection", test_get_ingestion_rule)
    runner.run_test("Document: Upload text document", test_upload)
    runner.run_test("Document: List documents includes uploaded one", test_list_documents)
    runner.run_test("Document: Get document by ID", test_get_document)
    runner.run_test("Document: Delete document", test_delete_document)
    runner.run_test("Document: Verify document no longer in list", test_verify_deleted)
    runner.run_test("Document: Upload two documents for bulk delete", test_upload_two_for_bulk)
    runner.run_test("Document: Bulk delete documents", test_bulk_delete)
    runner.run_test("Document: Verify bulk deleted documents no longer in list", test_verify_bulk_deleted)
    runner.run_test("Document: Clean up test collection", test_cleanup_collection)


def run_thread_tests(runner: TestRunner, client: AssistantHubClient) -> None:
    created_assistant_id: list[Optional[str]] = [None]
    created_thread_id: list[Optional[str]] = [None]
    suffix = unique_suffix()

    def test_create_assistant() -> None:
        assistant = Assistant(
            name="test-thread-assistant-" + suffix,
            description="Assistant created for thread tests",
        )
        created = client.create_assistant(assistant)
        assert_not_none(created, "CreateAssistant result")
        assert_not_none(created.id, "Created assistant ID")
        assert_starts_with(created.id, "asst_", "Assistant ID prefix")
        created_assistant_id[0] = created.id

    def test_create_thread() -> None:
        assert_not_none(created_assistant_id[0], "createdAssistantId from previous test")
        thread_id = client.create_thread(created_assistant_id[0])
        assert_not_none(thread_id, "CreateThread result")
        created_thread_id[0] = thread_id

    def test_list_threads() -> None:
        assert_not_none(created_assistant_id[0], "createdAssistantId from previous test")
        assert_not_none(created_thread_id[0], "createdThreadId from previous test")
        result = client.list_threads(created_assistant_id[0])
        assert_not_none(result, "ListThreads result")
        # list_threads returns list[dict] -- find the created thread
        if isinstance(result, list):
            found = any(
                (t.get("Id") or t.get("id") or t) == created_thread_id[0]
                for t in result
            )
        else:
            # If it returns an EnumerationResult-like dict
            objects = getattr(result, "objects", None) or result
            found = created_thread_id[0] in str(objects)
        assert_true(found, "Created thread should appear in list")

    def test_get_thread_history() -> None:
        assert_not_none(created_assistant_id[0], "createdAssistantId from previous test")
        assert_not_none(created_thread_id[0], "createdThreadId from previous test")
        messages = client.get_thread(created_assistant_id[0], created_thread_id[0])
        assert_not_none(messages, "GetThread result")

    def test_delete_thread() -> None:
        assert_not_none(created_thread_id[0], "createdThreadId from previous test")
        client.delete_thread(created_thread_id[0])

    def test_cleanup_assistant() -> None:
        assert_not_none(created_assistant_id[0], "createdAssistantId from previous test")
        client.delete_assistant(created_assistant_id[0])

    runner.run_test("Thread: Create assistant for thread tests", test_create_assistant)
    runner.run_test("Thread: Create thread for assistant", test_create_thread)
    runner.run_test("Thread: List threads includes created one", test_list_threads)
    runner.run_test("Thread: Get thread history", test_get_thread_history)
    runner.run_test("Thread: Delete thread", test_delete_thread)
    runner.run_test("Thread: Clean up assistant", test_cleanup_assistant)


def run_endpoint_tests(runner: TestRunner, client: AssistantHubClient) -> None:
    created_embedding_id: list[Optional[str]] = [None]
    created_completion_id: list[Optional[str]] = [None]
    suffix = unique_suffix()

    # --- Embedding Endpoint Tests ---

    def test_create_embedding() -> None:
        endpoint = PartioEndpointRequest(
            name="test-embedding-" + suffix,
            model="test-model",
            endpoint="http://localhost:8321",
            api_format="OpenAI",
            active=True,
        )
        created = client.create_embedding_endpoint(endpoint)
        assert_not_none(created, "CreateEmbeddingEndpoint result")
        assert_not_none(created.id, "Created embedding endpoint ID")
        assert_equal("test-embedding-" + suffix, created.name, "Created embedding endpoint name")
        created_embedding_id[0] = created.id

    def test_list_embedding() -> None:
        assert_not_none(created_embedding_id[0], "createdEmbeddingId from previous test")
        result = client.list_embedding_endpoints()
        assert_not_none(result, "ListEmbeddingEndpoints result")
        assert_not_none(result.objects, "ListEmbeddingEndpoints result.objects")
        found = any(e.id == created_embedding_id[0] for e in result.objects)
        assert_true(found, "Created embedding endpoint should appear in list")

    def test_get_embedding() -> None:
        assert_not_none(created_embedding_id[0], "createdEmbeddingId from previous test")
        endpoint = client.get_embedding_endpoint(created_embedding_id[0])
        assert_not_none(endpoint, "GetEmbeddingEndpoint result")
        assert_equal(created_embedding_id[0], endpoint.id, "Embedding endpoint ID")
        assert_equal("test-embedding-" + suffix, endpoint.name, "Embedding endpoint name")

    def test_update_embedding() -> None:
        assert_not_none(created_embedding_id[0], "createdEmbeddingId from previous test")
        endpoint = PartioEndpointRequest(
            name="test-embedding-updated-" + suffix,
            model="test-model-updated",
            endpoint="http://localhost:8321",
            api_format="OpenAI",
            active=True,
        )
        updated = client.update_embedding_endpoint(created_embedding_id[0], endpoint)
        assert_not_none(updated, "UpdateEmbeddingEndpoint result")
        assert_equal(
            "test-embedding-updated-" + suffix, updated.name, "Updated embedding endpoint name"
        )

    def test_check_embedding_health() -> None:
        health_statuses = client.check_embedding_health()
        assert_not_none(health_statuses, "CheckEmbeddingHealth result")

    def test_delete_embedding() -> None:
        assert_not_none(created_embedding_id[0], "createdEmbeddingId from previous test")
        client.delete_embedding_endpoint(created_embedding_id[0])

    # --- Completion Endpoint Tests ---

    def test_create_completion() -> None:
        endpoint = PartioEndpointRequest(
            name="test-completion-" + suffix,
            model="test-model",
            endpoint="http://localhost:8321",
            api_format="OpenAI",
            active=True,
        )
        created = client.create_completion_endpoint(endpoint)
        assert_not_none(created, "CreateCompletionEndpoint result")
        assert_not_none(created.id, "Created completion endpoint ID")
        assert_equal(
            "test-completion-" + suffix, created.name, "Created completion endpoint name"
        )
        created_completion_id[0] = created.id

    def test_list_completion() -> None:
        assert_not_none(created_completion_id[0], "createdCompletionId from previous test")
        result = client.list_completion_endpoints()
        assert_not_none(result, "ListCompletionEndpoints result")
        assert_not_none(result.objects, "ListCompletionEndpoints result.objects")
        found = any(e.id == created_completion_id[0] for e in result.objects)
        assert_true(found, "Created completion endpoint should appear in list")

    def test_get_completion() -> None:
        assert_not_none(created_completion_id[0], "createdCompletionId from previous test")
        endpoint = client.get_completion_endpoint(created_completion_id[0])
        assert_not_none(endpoint, "GetCompletionEndpoint result")
        assert_equal(created_completion_id[0], endpoint.id, "Completion endpoint ID")
        assert_equal("test-completion-" + suffix, endpoint.name, "Completion endpoint name")

    def test_update_completion() -> None:
        assert_not_none(created_completion_id[0], "createdCompletionId from previous test")
        endpoint = PartioEndpointRequest(
            name="test-completion-updated-" + suffix,
            model="test-model-updated",
            endpoint="http://localhost:8321",
            api_format="OpenAI",
            active=True,
        )
        updated = client.update_completion_endpoint(created_completion_id[0], endpoint)
        assert_not_none(updated, "UpdateCompletionEndpoint result")
        assert_equal(
            "test-completion-updated-" + suffix,
            updated.name,
            "Updated completion endpoint name",
        )

    def test_check_completion_health() -> None:
        health_statuses = client.check_completion_health()
        assert_not_none(health_statuses, "CheckCompletionHealth result")

    def test_delete_completion() -> None:
        assert_not_none(created_completion_id[0], "createdCompletionId from previous test")
        client.delete_completion_endpoint(created_completion_id[0])

    runner.run_test("Endpoint: Create embedding endpoint", test_create_embedding)
    runner.run_test("Endpoint: List embedding endpoints includes created one", test_list_embedding)
    runner.run_test("Endpoint: Get embedding endpoint by ID", test_get_embedding)
    runner.run_test("Endpoint: Update embedding endpoint", test_update_embedding)
    runner.run_test("Endpoint: Check embedding health", test_check_embedding_health)
    runner.run_test("Endpoint: Delete embedding endpoint", test_delete_embedding)
    runner.run_test("Endpoint: Create completion endpoint", test_create_completion)
    runner.run_test("Endpoint: List completion endpoints includes created one", test_list_completion)
    runner.run_test("Endpoint: Get completion endpoint by ID", test_get_completion)
    runner.run_test("Endpoint: Update completion endpoint", test_update_completion)
    runner.run_test("Endpoint: Check completion health", test_check_completion_health)
    runner.run_test("Endpoint: Delete completion endpoint", test_delete_completion)


def run_inference_tests(runner: TestRunner, client: AssistantHubClient) -> None:
    def test_list_models() -> None:
        models = client.list_models()
        assert_not_none(models, "ListModels result")

    runner.run_test("Inference: List models returns results", test_list_models)


def run_eval_tests(runner: TestRunner, client: AssistantHubClient) -> None:
    created_fact_id: list[Optional[str]] = [None]
    assistant_id: list[Optional[str]] = [None]
    suffix = unique_suffix()

    def test_create_assistant() -> None:
        assistant = Assistant(
            name="test-eval-assistant-" + suffix,
            description="Temporary assistant for eval tests",
        )
        created = client.create_assistant(assistant)
        assert_not_none(created, "CreateAssistant result")
        assert_not_none(created.id, "Created assistant ID")
        assistant_id[0] = created.id

    def test_create_fact() -> None:
        assert_not_none(assistant_id[0], "assistantId from previous test")
        fact = EvalFact(
            assistant_id=assistant_id[0],
            category="test-category-" + suffix,
            question="What is the test question?",
            expected_facts='["fact1", "fact2"]',
        )
        created = client.create_eval_fact(fact)
        assert_not_none(created, "CreateEvalFact result")
        assert_not_none(created.id, "Created eval fact ID")
        assert_starts_with(created.id, "ef_", "Created eval fact ID prefix")
        assert_equal(assistant_id[0], created.assistant_id, "Eval fact assistant ID")
        created_fact_id[0] = created.id

    def test_list_facts() -> None:
        assert_not_none(created_fact_id[0], "createdFactId from previous test")
        result = client.list_eval_facts()
        assert_not_none(result, "ListEvalFacts result")
        assert_not_none(result.objects, "ListEvalFacts result.objects")
        found = any(f.id == created_fact_id[0] for f in result.objects)
        assert_true(found, "Created eval fact should appear in list")

    def test_default_judge_prompt() -> None:
        prompt = client.get_default_judge_prompt()
        assert_true(isinstance(prompt, str), "Default judge prompt should be a string")

    def test_delete_fact() -> None:
        assert_not_none(created_fact_id[0], "createdFactId from previous test")
        client.delete_eval_fact(created_fact_id[0])

    def test_cleanup_assistant() -> None:
        assert_not_none(assistant_id[0], "assistantId from previous test")
        client.delete_assistant(assistant_id[0])

    runner.run_test("Eval: Create assistant for eval tests", test_create_assistant)
    runner.run_test("Eval: Create eval fact", test_create_fact)
    runner.run_test("Eval: List eval facts includes created one", test_list_facts)
    runner.run_test("Eval: Default judge prompt returns string", test_default_judge_prompt)
    runner.run_test("Eval: Delete eval fact", test_delete_fact)
    runner.run_test("Eval: Cleanup assistant", test_cleanup_assistant)


def run_request_history_tests(runner: TestRunner, client: AssistantHubClient) -> None:
    captured_request_id: list[Optional[str]] = [None]
    start_utc = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime(time.time() - 5))

    def test_capture_and_list() -> None:
        from assistanthub_sdk.models import RequestHistorySearchFilter

        client.whoami()

        for _ in range(20):
            result = client.list_request_history(
                RequestHistorySearchFilter(
                    max_results=25,
                    path_contains="/v1.0/whoami",
                    start_utc=start_utc,
                )
            )
            assert_not_none(result, "ListRequestHistory result")
            assert_not_none(result.objects, "ListRequestHistory result.objects")

            entry = next(
                (
                    item
                    for item in result.objects
                    if item.request_path and "/v1.0/whoami" in item.request_path
                ),
                None,
            )
            if entry is not None and entry.id is not None:
                captured_request_id[0] = entry.id
                return

            time.sleep(0.5)

        raise AssertionError("Timed out waiting for request-history capture of /v1.0/whoami")

    def test_get_request_history() -> None:
        assert_not_none(captured_request_id[0], "capturedRequestId from previous test")
        entry = client.get_request_history(captured_request_id[0])
        assert_not_none(entry, "GetRequestHistory result")
        assert_equal(captured_request_id[0], entry.id, "RequestHistory ID")
        assert_true(
            entry.request_path is not None and "/v1.0/whoami" in entry.request_path,
            "Request path should reference whoami",
        )

    def test_get_request_history_detail() -> None:
        assert_not_none(captured_request_id[0], "capturedRequestId from previous test")
        entry = client.get_request_history_detail(captured_request_id[0])
        assert_not_none(entry, "GetRequestHistoryDetail result")
        assert_equal(captured_request_id[0], entry.id, "RequestHistory detail ID")

    def test_get_request_history_summary() -> None:
        from assistanthub_sdk.models import RequestHistorySearchFilter

        summary = client.get_request_history_summary(
            RequestHistorySearchFilter(
                path_contains="/v1.0/whoami",
                start_utc=start_utc,
                bucket_seconds=60,
            )
        )
        assert_not_none(summary, "GetRequestHistorySummary result")
        assert_true(summary.total_count >= 1, "RequestHistory summary should include at least one entry")

    runner.run_test("RequestHistory: Capture and list whoami request", test_capture_and_list)
    runner.run_test("RequestHistory: Get request-history entry by ID", test_get_request_history)
    runner.run_test("RequestHistory: Get detailed request-history entry by ID", test_get_request_history_detail)
    runner.run_test("RequestHistory: Get request-history summary", test_get_request_history_summary)


def run_crawl_plan_tests(runner: TestRunner, client: AssistantHubClient) -> None:
    created_plan_id: list[Optional[str]] = [None]
    created_cifs_plan_id: list[Optional[str]] = [None]
    created_nfs_plan_id: list[Optional[str]] = [None]
    suffix = unique_suffix()

    def test_create() -> None:
        plan = CrawlPlan(
            name="test-crawlplan-" + suffix,
            repository_type=RepositoryType.WEB,
            repository_settings=WebCrawlRepositorySettings(
                authentication_type=WebAuthType.NONE,
                start_url="https://example.com",
                max_depth=1,
                max_parallel_tasks=1,
                crawl_delay_ms=1000,
                follow_links=False,
                follow_redirects=True,
                ignore_robots_txt=False,
                restrict_to_child_urls=True,
            ),
            schedule=CrawlScheduleSettings(
                interval_type=ScheduleInterval.ONE_TIME,
                interval_value=1,
            ),
            process_additions=True,
            process_updates=True,
            process_deletions=False,
            max_drain_tasks=1,
            retention_days=7,
        )
        created = client.create_crawl_plan(plan)
        assert_not_none(created, "CreateCrawlPlan result")
        assert_not_none(created.id, "Created crawl plan ID")
        assert_starts_with(created.id, "cplan_", "Created crawl plan ID prefix")
        assert_equal("test-crawlplan-" + suffix, created.name, "Created crawl plan name")
        created_plan_id[0] = created.id

    def test_create_cifs() -> None:
        plan = CrawlPlan(
            name="test-cifs-crawlplan-" + suffix,
            repository_type=RepositoryType.CIFS,
            repository_settings=CifsCrawlRepositorySettings(
                cifs_hostname="fileserver.example.com",
                cifs_username="crawler",
                cifs_password="secret",
                cifs_share_name="content",
                include_subdirectories=True,
            ),
            schedule=CrawlScheduleSettings(
                interval_type=ScheduleInterval.ONE_TIME,
                interval_value=1,
            ),
            process_additions=True,
            process_updates=True,
            process_deletions=False,
            max_drain_tasks=1,
            retention_days=7,
        )
        created = client.create_crawl_plan(plan)
        assert_not_none(created, "Create CIFS CrawlPlan result")
        assert_not_none(created.id, "Created CIFS crawl plan ID")
        assert_equal(RepositoryType.CIFS, created.repository_type, "Created CIFS repository type")
        assert_true(
            isinstance(created.repository_settings, CifsCrawlRepositorySettings),
            "Created CIFS repository settings type",
        )
        created_cifs_plan_id[0] = created.id

    def test_create_nfs() -> None:
        plan = CrawlPlan(
            name="test-nfs-crawlplan-" + suffix,
            repository_type=RepositoryType.NFS,
            repository_settings=NfsCrawlRepositorySettings(
                nfs_hostname="nfs.example.com",
                nfs_user_id=1000,
                nfs_group_id=1000,
                nfs_share_name="/exports/content",
                nfs_version=NfsVersion.V3,
                include_subdirectories=True,
            ),
            schedule=CrawlScheduleSettings(
                interval_type=ScheduleInterval.ONE_TIME,
                interval_value=1,
            ),
            process_additions=True,
            process_updates=True,
            process_deletions=False,
            max_drain_tasks=1,
            retention_days=7,
        )
        created = client.create_crawl_plan(plan)
        assert_not_none(created, "Create NFS CrawlPlan result")
        assert_not_none(created.id, "Created NFS crawl plan ID")
        assert_equal(RepositoryType.NFS, created.repository_type, "Created NFS repository type")
        assert_true(
            isinstance(created.repository_settings, NfsCrawlRepositorySettings),
            "Created NFS repository settings type",
        )
        created_nfs_plan_id[0] = created.id

    def test_list() -> None:
        assert_not_none(created_plan_id[0], "createdPlanId from previous test")
        assert_not_none(created_cifs_plan_id[0], "createdCifsPlanId from previous test")
        assert_not_none(created_nfs_plan_id[0], "createdNfsPlanId from previous test")
        result = client.list_crawl_plans()
        assert_not_none(result, "ListCrawlPlans result")
        assert_not_none(result.objects, "ListCrawlPlans result.objects")
        assert_true(
            any(p.id == created_plan_id[0] for p in result.objects),
            "Created web crawl plan should appear in list",
        )
        assert_true(
            any(p.id == created_cifs_plan_id[0] for p in result.objects),
            "Created CIFS crawl plan should appear in list",
        )
        assert_true(
            any(p.id == created_nfs_plan_id[0] for p in result.objects),
            "Created NFS crawl plan should appear in list",
        )

    def test_get() -> None:
        assert_not_none(created_plan_id[0], "createdPlanId from previous test")
        plan = client.get_crawl_plan(created_plan_id[0])
        assert_not_none(plan, "GetCrawlPlan result")
        assert_equal(created_plan_id[0], plan.id, "Crawl plan ID")
        assert_equal("test-crawlplan-" + suffix, plan.name, "Crawl plan name")

    def test_update() -> None:
        assert_not_none(created_plan_id[0], "createdPlanId from previous test")
        plan = CrawlPlan(
            name="test-crawlplan-updated-" + suffix,
            repository_type=RepositoryType.WEB,
            repository_settings=WebCrawlRepositorySettings(
                authentication_type=WebAuthType.NONE,
                start_url="https://example.com/updated",
                max_depth=2,
                max_parallel_tasks=1,
                crawl_delay_ms=500,
                follow_links=True,
                follow_redirects=True,
                ignore_robots_txt=False,
                restrict_to_child_urls=True,
            ),
            schedule=CrawlScheduleSettings(
                interval_type=ScheduleInterval.ONE_TIME,
                interval_value=1,
            ),
            process_additions=True,
            process_updates=True,
            process_deletions=True,
            max_drain_tasks=2,
            retention_days=14,
        )
        updated = client.update_crawl_plan(created_plan_id[0], plan)
        assert_not_none(updated, "UpdateCrawlPlan result")
        assert_equal(
            "test-crawlplan-updated-" + suffix, updated.name, "Updated crawl plan name"
        )

    def test_delete() -> None:
        assert_not_none(created_plan_id[0], "createdPlanId from previous test")
        client.delete_crawl_plan(created_plan_id[0])
        if created_cifs_plan_id[0] is not None:
            client.delete_crawl_plan(created_cifs_plan_id[0])
        if created_nfs_plan_id[0] is not None:
            client.delete_crawl_plan(created_nfs_plan_id[0])

    runner.run_test("CrawlPlan: Create crawl plan", test_create)
    runner.run_test("CrawlPlan: Create CIFS crawl plan", test_create_cifs)
    runner.run_test("CrawlPlan: Create NFS crawl plan", test_create_nfs)
    runner.run_test("CrawlPlan: List crawl plans includes created one", test_list)
    runner.run_test("CrawlPlan: Get crawl plan by ID", test_get)
    runner.run_test("CrawlPlan: Update crawl plan", test_update)
    runner.run_test("CrawlPlan: Delete crawl plan", test_delete)


def run_config_tests(runner: TestRunner, client: AssistantHubClient) -> None:
    def test_get_config() -> None:
        config = client.get_config()
        assert_true(isinstance(config, dict), "Config should be a JSON object")

    runner.run_test("Config: Get config returns valid response", test_get_config)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> int:
    print("==========================================================")
    print("  AssistantHub Python SDK Test Suite")
    print("==========================================================")
    print()

    base_url = os.environ.get("ASSISTANTHUB_URL", "http://localhost:6600")
    api_key = os.environ.get("ASSISTANTHUB_API_KEY", "default")
    local_only = local_only_requested()

    print("  Base URL:  {}".format(base_url))
    print("  API Key:   {}".format(api_key))
    print("  LocalOnly: {}".format(local_only))
    print()

    runner = TestRunner()
    total_start = time.perf_counter()

    try:
        run_sdk_contract_tests(runner)

        if local_only:
            total_ms = (time.perf_counter() - total_start) * 1000.0
            runner.print_summary(total_ms)
            for r in runner.results:
                if not r.passed:
                    return 1
            return 0

        with AssistantHubClient(base_url=base_url, api_key=api_key) as client:
            run_health_tests(runner, client)
            run_tenant_tests(runner, client)
            run_assistant_tests(runner, client)
            run_collection_tests(runner, client)
            run_document_tests(runner, client)
            run_thread_tests(runner, client)
            run_endpoint_tests(runner, client)
            run_inference_tests(runner, client)
            run_eval_tests(runner, client)
            run_request_history_tests(runner, client)
            run_crawl_plan_tests(runner, client)
            run_config_tests(runner, client)
    except Exception as ex:
        print("Unhandled exception during test execution: {}".format(ex))
        traceback.print_exc()

    total_ms = (time.perf_counter() - total_start) * 1000.0
    runner.print_summary(total_ms)

    for r in runner.results:
        if not r.passed:
            return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
