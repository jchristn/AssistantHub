#!/usr/bin/env python3
"""AssistantHub Python SDK Test Suite."""

from __future__ import annotations

import os
import sys
import time
import traceback
import uuid
from dataclasses import dataclass, field
from typing import Any, Callable, Optional

# Add the SDK to the path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from assistanthub_sdk import AssistantHubClient
from assistanthub_sdk.models import (
    Assistant,
    AssistantDocument,
    CrawlPlan,
    CrawlScheduleSettings,
    Credential,
    EvalFact,
    PartioEndpointConfig,
    PartioEndpointRequest,
    TenantMetadata,
    UserMaster,
    WebCrawlRepositorySettings,
)
from assistanthub_sdk.enums import (
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

    def test_delete_fact() -> None:
        assert_not_none(created_fact_id[0], "createdFactId from previous test")
        client.delete_eval_fact(created_fact_id[0])

    def test_cleanup_assistant() -> None:
        assert_not_none(assistant_id[0], "assistantId from previous test")
        client.delete_assistant(assistant_id[0])

    runner.run_test("Eval: Create assistant for eval tests", test_create_assistant)
    runner.run_test("Eval: Create eval fact", test_create_fact)
    runner.run_test("Eval: List eval facts includes created one", test_list_facts)
    runner.run_test("Eval: Delete eval fact", test_delete_fact)
    runner.run_test("Eval: Cleanup assistant", test_cleanup_assistant)


def run_crawl_plan_tests(runner: TestRunner, client: AssistantHubClient) -> None:
    created_plan_id: list[Optional[str]] = [None]
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

    def test_list() -> None:
        assert_not_none(created_plan_id[0], "createdPlanId from previous test")
        result = client.list_crawl_plans()
        assert_not_none(result, "ListCrawlPlans result")
        assert_not_none(result.objects, "ListCrawlPlans result.objects")
        found = any(p.id == created_plan_id[0] for p in result.objects)
        assert_true(found, "Created crawl plan should appear in list")

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

    runner.run_test("CrawlPlan: Create crawl plan", test_create)
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

    print("  Base URL:  {}".format(base_url))
    print("  API Key:   {}".format(api_key))
    print()

    runner = TestRunner()
    total_start = time.perf_counter()

    try:
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
