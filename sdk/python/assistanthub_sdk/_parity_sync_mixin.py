"""Additional synchronous SDK surface needed for parity."""

from __future__ import annotations

from typing import Any, Optional, TypeVar

import httpx

from .models import (
    AssistantAnalyticsEndpointResult,
    AssistantAnalyticsFeedbackResult,
    AssistantAnalyticsOverviewResult,
    AssistantAnalyticsQuery,
    AssistantAnalyticsSlowestResult,
    AssistantAnalyticsStageResult,
    AssistantAnalyticsTimeSeriesResult,
    AssistantFeedback,
    AssistantChatOpenResult,
    AssistantPublicInfo,
    AssistantSettings,
    AssistantToolDescriptor,
    AssistantToolPolicyValidationRequest,
    AssistantToolPolicyValidationResult,
    AssistantToolPolicyTestResult,
    AuthenticateRequest,
    AuthenticateResult,
    BucketCreateRequest,
    ChatCompletionRequest,
    ChatHistory,
    CollectionRecord,
    DocumentReindexBatchResult,
    DocumentReindexRequest,
    DocumentReindexResult,
    EndpointExplorerCompletionRequest,
    EndpointExplorerCompletionResponse,
    EndpointExplorerEmbeddingRequest,
    EndpointExplorerEmbeddingResponse,
    EnumerationResult,
    FeedbackRequest,
    SlackVerificationRequest,
    SlackVerificationResponse,
    ThreadSummary,
)

T = TypeVar("T")


class AssistantHubClientParityMixin:
    """Mixin that adds missing sync SDK methods and fixes stale contracts."""

    def _head(self, path: str) -> bool:
        response = self._client.request("HEAD", path)
        return response.is_success

    def _parse_enumeration(self, data: dict[str, Any], model_type: type[T]) -> EnumerationResult[T]:
        result = EnumerationResult[T].model_validate(data)
        result.objects = [model_type.model_validate(obj) for obj in (data.get("objects") or [])]
        return result

    @staticmethod
    def _assistant_analytics_params(
        query: AssistantAnalyticsQuery | dict[str, Any] | None,
    ) -> Optional[dict[str, Any]]:
        if query is None:
            return None

        if isinstance(query, AssistantAnalyticsQuery):
            params = query.model_dump(by_alias=True, exclude_none=True, mode="json")
        else:
            params = {key: value for key, value in query.items() if value is not None}

        metrics = params.get("metrics")
        if isinstance(metrics, list):
            params["metrics"] = ",".join(str(metric) for metric in metrics)

        return params

    def health(self) -> dict[str, Any]:
        return self.health_check()

    def health_head(self) -> bool:
        return self._head("/")

    def authenticate(self, request: AuthenticateRequest) -> AuthenticateResult:
        response = self._request(
            "POST",
            "/v1.0/authenticate",
            json=request.model_dump(by_alias=True, exclude_none=True),
        )
        return AuthenticateResult.model_validate(response.json())

    def assistant_exists(self, assistant_id: str) -> bool:
        return self._head(f"/v1.0/assistants/{assistant_id}")

    def collection_exists(self, collection_id: str) -> bool:
        return self._head(f"/v1.0/collections/{collection_id}")

    def tenant_exists(self, tenant_id: str) -> bool:
        return self._head(f"/v1.0/tenants/{tenant_id}")

    def user_exists(self, tenant_id: str, user_id: str) -> bool:
        return self._head(f"/v1.0/tenants/{tenant_id}/users/{user_id}")

    def credential_exists(self, tenant_id: str, credential_id: str) -> bool:
        return self._head(f"/v1.0/tenants/{tenant_id}/credentials/{credential_id}")

    def ingestion_rule_exists(self, rule_id: str) -> bool:
        return self._head(f"/v1.0/ingestion-rules/{rule_id}")

    def document_exists(self, document_id: str) -> bool:
        return self._head(f"/v1.0/documents/{document_id}")

    def get_assistant_public(self, assistant_id: str) -> AssistantPublicInfo:
        response = self._request("GET", f"/v1.0/assistants/{assistant_id}/public")
        return AssistantPublicInfo.model_validate(response.json())

    def open_assistant_chat(self, assistant_id: str) -> AssistantChatOpenResult:
        response = self._request("POST", f"/v1.0/assistants/{assistant_id}/chat/open")
        return AssistantChatOpenResult.model_validate(response.json())

    def get_assistant_settings(self, assistant_id: str) -> AssistantSettings:
        response = self._request("GET", f"/v1.0/assistants/{assistant_id}/settings")
        return AssistantSettings.model_validate(response.json())

    def get_assistant_tools(self, assistant_id: str) -> list[AssistantToolDescriptor]:
        response = self._request("GET", f"/v1.0/assistants/{assistant_id}/tools")
        return [AssistantToolDescriptor.model_validate(obj) for obj in (response.json() or [])]

    def validate_assistant_tool_policy(
        self,
        assistant_id: str,
        request: AssistantToolPolicyValidationRequest,
    ) -> AssistantToolPolicyValidationResult:
        response = self._request(
            "POST",
            f"/v1.0/assistants/{assistant_id}/settings/tools/validate",
            json=request.model_dump(by_alias=True, exclude_none=True),
        )
        return AssistantToolPolicyValidationResult.model_validate(response.json())

    def test_assistant_tool_policy(
        self,
        assistant_id: str,
        request: AssistantToolPolicyValidationRequest,
    ) -> AssistantToolPolicyTestResult:
        response = self._request(
            "POST",
            f"/v1.0/assistants/{assistant_id}/settings/tools/test",
            json=request.model_dump(by_alias=True, exclude_none=True),
        )
        return AssistantToolPolicyTestResult.model_validate(response.json())

    def update_assistant_settings(self, assistant_id: str, settings: AssistantSettings) -> AssistantSettings:
        response = self._request(
            "PUT",
            f"/v1.0/assistants/{assistant_id}/settings",
            json=settings.model_dump(by_alias=True, exclude_none=True),
        )
        return AssistantSettings.model_validate(response.json())

    def verify_slack(self, assistant_id: str, request: SlackVerificationRequest) -> SlackVerificationResponse:
        response = self._request(
            "POST",
            f"/v1.0/assistants/{assistant_id}/settings/slack/verify",
            json=request.model_dump(by_alias=True, exclude_none=True),
        )
        return SlackVerificationResponse.model_validate(response.json())

    def get_assistant_analytics_overview(
        self,
        assistant_id: str,
        query: AssistantAnalyticsQuery | dict[str, Any] | None = None,
    ) -> AssistantAnalyticsOverviewResult:
        response = self._request(
            "GET",
            f"/v1.0/assistants/{assistant_id}/analytics/overview",
            params=self._assistant_analytics_params(query),
        )
        return AssistantAnalyticsOverviewResult.model_validate(response.json())

    def get_assistant_analytics_time_series(
        self,
        assistant_id: str,
        query: AssistantAnalyticsQuery | dict[str, Any] | None = None,
    ) -> AssistantAnalyticsTimeSeriesResult:
        response = self._request(
            "GET",
            f"/v1.0/assistants/{assistant_id}/analytics/timeseries",
            params=self._assistant_analytics_params(query),
        )
        return AssistantAnalyticsTimeSeriesResult.model_validate(response.json())

    def get_assistant_analytics_stages(
        self,
        assistant_id: str,
        query: AssistantAnalyticsQuery | dict[str, Any] | None = None,
    ) -> AssistantAnalyticsStageResult:
        response = self._request(
            "GET",
            f"/v1.0/assistants/{assistant_id}/analytics/stages",
            params=self._assistant_analytics_params(query),
        )
        return AssistantAnalyticsStageResult.model_validate(response.json())

    def get_assistant_analytics_endpoints(
        self,
        assistant_id: str,
        query: AssistantAnalyticsQuery | dict[str, Any] | None = None,
    ) -> AssistantAnalyticsEndpointResult:
        response = self._request(
            "GET",
            f"/v1.0/assistants/{assistant_id}/analytics/endpoints",
            params=self._assistant_analytics_params(query),
        )
        return AssistantAnalyticsEndpointResult.model_validate(response.json())

    def get_assistant_analytics_slowest(
        self,
        assistant_id: str,
        query: AssistantAnalyticsQuery | dict[str, Any] | None = None,
    ) -> AssistantAnalyticsSlowestResult:
        response = self._request(
            "GET",
            f"/v1.0/assistants/{assistant_id}/analytics/slowest",
            params=self._assistant_analytics_params(query),
        )
        return AssistantAnalyticsSlowestResult.model_validate(response.json())

    def get_assistant_analytics_feedback(
        self,
        assistant_id: str,
        query: AssistantAnalyticsQuery | dict[str, Any] | None = None,
    ) -> AssistantAnalyticsFeedbackResult:
        response = self._request(
            "GET",
            f"/v1.0/assistants/{assistant_id}/analytics/feedback",
            params=self._assistant_analytics_params(query),
        )
        return AssistantAnalyticsFeedbackResult.model_validate(response.json())

    def list_threads(
        self,
        assistant_id: Optional[str] = None,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> list[ThreadSummary]:
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token
        if assistant_id is not None:
            params["assistantId"] = assistant_id

        response = self._request("GET", "/v1.0/threads", params=params)
        data = response.json()
        if isinstance(data, list):
            return [ThreadSummary.model_validate(item) for item in data]
        return []

    def get_thread_history(self, assistant_id: str, thread_id: str) -> list[ChatHistory]:
        return self.get_thread(assistant_id, thread_id)

    def get_distinct_labels(self, assistant_id: str) -> list[str]:
        response = self._request("GET", f"/v1.0/assistants/{assistant_id}/labels/distinct")
        data = response.json()
        return data if isinstance(data, list) else []

    def get_distinct_tags(self, assistant_id: str) -> list[str]:
        response = self._request("GET", f"/v1.0/assistants/{assistant_id}/tags/distinct")
        data = response.json()
        return data if isinstance(data, list) else []

    def submit_feedback(self, assistant_id: str, request: FeedbackRequest) -> AssistantFeedback:
        response = self._request(
            "POST",
            f"/v1.0/assistants/{assistant_id}/feedback",
            json=request.model_dump(by_alias=True, exclude_none=True),
        )
        return AssistantFeedback.model_validate(response.json())

    def compact(
        self,
        assistant_id: str,
        request: ChatCompletionRequest,
        *,
        thread_id: Optional[str] = None,
    ) -> dict[str, Any]:
        headers = {"X-Thread-ID": thread_id} if thread_id else None
        response = self._request(
            "POST",
            f"/v1.0/assistants/{assistant_id}/compact",
            json=request.model_dump(by_alias=True, exclude_none=True),
            headers=headers,
        )
        return response.json()

    def list_feedback(
        self,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[AssistantFeedback]:
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token
        response = self._request("GET", "/v1.0/feedback", params=params)
        return self._parse_enumeration(response.json(), AssistantFeedback)

    def get_feedback(self, feedback_id: str) -> AssistantFeedback:
        response = self._request("GET", f"/v1.0/feedback/{feedback_id}")
        return AssistantFeedback.model_validate(response.json())

    def delete_feedback(self, feedback_id: str) -> None:
        self._request("DELETE", f"/v1.0/feedback/{feedback_id}")

    def list_history(
        self,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[ChatHistory]:
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token
        response = self._request("GET", "/v1.0/history", params=params)
        return self._parse_enumeration(response.json(), ChatHistory)

    def get_history(self, history_id: str) -> ChatHistory:
        response = self._request("GET", f"/v1.0/history/{history_id}")
        return ChatHistory.model_validate(response.json())

    def delete_history(self, history_id: str) -> None:
        self._request("DELETE", f"/v1.0/history/{history_id}")

    def get_document_processing_log(self, document_id: str) -> dict[str, Any]:
        response = self._request("GET", f"/v1.0/documents/{document_id}/processing-log")
        return response.json()

    def reindex_document(self, document_id: str) -> DocumentReindexResult:
        response = self._request("POST", f"/v1.0/documents/{document_id}/reindex", json={})
        return DocumentReindexResult.model_validate(response.json())

    def reindex_documents(
        self,
        request: DocumentReindexRequest | dict[str, Any] | None = None,
        *,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
        bucket_name: Optional[str] = None,
        collection_id: Optional[str] = None,
    ) -> DocumentReindexBatchResult:
        payload = request.model_dump(by_alias=True, exclude_none=True) if isinstance(request, DocumentReindexRequest) else (request or {})
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token
        if bucket_name is not None:
            params["bucketName"] = bucket_name
        if collection_id is not None:
            params["collectionId"] = collection_id

        response = self._request("POST", "/v1.0/documents/reindex", params=params, json=payload)
        return DocumentReindexBatchResult.model_validate(response.json())

    def download_document(self, document_id: str) -> httpx.Response:
        return self._request("GET", f"/v1.0/documents/{document_id}/download")

    def download_document_public(self, assistant_id: str, document_id: str) -> httpx.Response:
        return self._request("GET", f"/v1.0/assistants/{assistant_id}/documents/{document_id}/download")

    def test_embedding_endpoint(
        self,
        endpoint_id: str,
        request: EndpointExplorerEmbeddingRequest,
    ) -> EndpointExplorerEmbeddingResponse:
        response = self._request(
            "POST",
            f"/v1.0/endpoints/embedding/{endpoint_id}/test",
            json=request.model_dump(by_alias=True, exclude_none=True),
        )
        return EndpointExplorerEmbeddingResponse.model_validate(response.json())

    def test_completion_endpoint(
        self,
        endpoint_id: str,
        request: EndpointExplorerCompletionRequest,
    ) -> EndpointExplorerCompletionResponse:
        response = self._request(
            "POST",
            f"/v1.0/endpoints/completion/{endpoint_id}/test",
            json=request.model_dump(by_alias=True, exclude_none=True),
        )
        return EndpointExplorerCompletionResponse.model_validate(response.json())

    def create_collection_record(self, collection_id: str, record: CollectionRecord | dict[str, Any]) -> CollectionRecord:
        payload = record.model_dump(by_alias=True, exclude_none=True) if isinstance(record, CollectionRecord) else record
        response = self._request("PUT", f"/v1.0/collections/{collection_id}/records", json=payload)
        return CollectionRecord.model_validate(response.json())

    def get_collection_distinct_labels(self, collection_id: str) -> list[str]:
        response = self._request("GET", f"/v1.0/collections/{collection_id}/labels/distinct")
        data = response.json()
        return data if isinstance(data, list) else []

    def get_collection_distinct_tags(self, collection_id: str) -> list[str]:
        response = self._request("GET", f"/v1.0/collections/{collection_id}/tags/distinct")
        data = response.json()
        return data if isinstance(data, list) else []

    def list_collection_records(
        self,
        collection_id: str,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[CollectionRecord]:
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token
        response = self._request("GET", f"/v1.0/collections/{collection_id}/records", params=params)
        return self._parse_enumeration(response.json(), CollectionRecord)

    def get_collection_record(self, collection_id: str, record_id: str) -> CollectionRecord:
        response = self._request("GET", f"/v1.0/collections/{collection_id}/records/{record_id}")
        return CollectionRecord.model_validate(response.json())

    def delete_collection_record(self, collection_id: str, record_id: str) -> None:
        self._request("DELETE", f"/v1.0/collections/{collection_id}/records/{record_id}")

    def batch_delete_collection_records(self, collection_id: str, record_ids: list[str]) -> None:
        self._request("POST", f"/v1.0/collections/{collection_id}/records/delete", json={"RecordIds": record_ids})

    def search_collection(self, collection_id: str, request: dict[str, Any]) -> dict[str, Any]:
        response = self._request("POST", f"/v1.0/collections/{collection_id}/search", json=request)
        return response.json()

    def list_indices(
        self,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> dict[str, Any]:
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token
        response = self._request("GET", "/v1.0/indices", params=params)
        return response.json()

    def create_index(self, index: dict[str, Any]) -> dict[str, Any]:
        response = self._request("PUT", "/v1.0/indices", json=index)
        return response.json()

    def get_index(self, index_id: str) -> dict[str, Any]:
        response = self._request("GET", f"/v1.0/indices/{index_id}")
        return response.json()

    def update_index(self, index_id: str, index: dict[str, Any]) -> dict[str, Any]:
        response = self._request("PUT", f"/v1.0/indices/{index_id}", json=index)
        return response.json()

    def delete_index(self, index_id: str) -> None:
        self._request("DELETE", f"/v1.0/indices/{index_id}")

    def index_exists(self, index_id: str) -> bool:
        return self._head(f"/v1.0/indices/{index_id}")

    def update_index_labels(self, index_id: str, labels: Any) -> dict[str, Any]:
        response = self._request("PUT", f"/v1.0/indices/{index_id}/labels", json=labels)
        return response.json()

    def update_index_tags(self, index_id: str, tags: Any) -> dict[str, Any]:
        response = self._request("PUT", f"/v1.0/indices/{index_id}/tags", json=tags)
        return response.json()

    def update_index_custom_metadata(self, index_id: str, custom_metadata: Any) -> dict[str, Any]:
        response = self._request("PUT", f"/v1.0/indices/{index_id}/custom-metadata", json=custom_metadata)
        return response.json()

    def get_index_top_terms(self, index_id: str, max_results: Optional[int] = None) -> dict[str, Any]:
        params = {"maxResults": max_results} if max_results is not None else None
        response = self._request("GET", f"/v1.0/indices/{index_id}/terms/top", params=params)
        return response.json()

    def search_index(self, index_id: str, request: dict[str, Any]) -> dict[str, Any]:
        response = self._request("POST", f"/v1.0/indices/{index_id}/search", json=request)
        return response.json()

    def list_index_records(
        self,
        index_id: str,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> dict[str, Any]:
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token
        response = self._request("GET", f"/v1.0/indices/{index_id}/records", params=params)
        return response.json()

    def create_index_record(self, index_id: str, record: dict[str, Any]) -> dict[str, Any]:
        response = self._request("PUT", f"/v1.0/indices/{index_id}/records", json=record)
        return response.json()

    def create_index_records_batch(self, index_id: str, records: list[dict[str, Any]] | dict[str, Any]) -> dict[str, Any]:
        response = self._request("POST", f"/v1.0/indices/{index_id}/records/batch", json=records)
        return response.json()

    def check_index_records_exist(self, index_id: str, record_ids: list[str] | dict[str, Any]) -> dict[str, Any]:
        response = self._request("POST", f"/v1.0/indices/{index_id}/records/exists", json=record_ids)
        return response.json()

    def delete_index_records(self, index_id: str, record_ids: list[str]) -> None:
        self._request("POST", f"/v1.0/indices/{index_id}/records/delete", json={"RecordIds": record_ids})

    def get_index_record(self, index_id: str, record_id: str) -> dict[str, Any]:
        response = self._request("GET", f"/v1.0/indices/{index_id}/records/{record_id}")
        return response.json()

    def index_record_exists(self, index_id: str, record_id: str) -> bool:
        return self._head(f"/v1.0/indices/{index_id}/records/{record_id}")

    def delete_index_record(self, index_id: str, record_id: str) -> None:
        self._request("DELETE", f"/v1.0/indices/{index_id}/records/{record_id}")

    def update_index_record_labels(self, index_id: str, record_id: str, labels: Any) -> dict[str, Any]:
        response = self._request("PUT", f"/v1.0/indices/{index_id}/records/{record_id}/labels", json=labels)
        return response.json()

    def update_index_record_tags(self, index_id: str, record_id: str, tags: Any) -> dict[str, Any]:
        response = self._request("PUT", f"/v1.0/indices/{index_id}/records/{record_id}/tags", json=tags)
        return response.json()

    def update_index_record_custom_metadata(self, index_id: str, record_id: str, custom_metadata: Any) -> dict[str, Any]:
        response = self._request("PUT", f"/v1.0/indices/{index_id}/records/{record_id}/custom-metadata", json=custom_metadata)
        return response.json()

    def create_bucket(self, request: BucketCreateRequest | dict[str, Any]) -> dict[str, Any]:
        payload = request.model_dump(by_alias=True, exclude_none=True) if isinstance(request, BucketCreateRequest) else request
        response = self._request("PUT", "/v1.0/buckets", json=payload)
        return response.json()

    def list_buckets(self) -> dict[str, Any]:
        response = self._request("GET", "/v1.0/buckets")
        return response.json()

    def get_bucket(self, name: str) -> dict[str, Any]:
        response = self._request("GET", f"/v1.0/buckets/{name}")
        return response.json()

    def delete_bucket(self, name: str) -> None:
        self._request("DELETE", f"/v1.0/buckets/{name}")

    def bucket_exists(self, name: str) -> bool:
        return self._head(f"/v1.0/buckets/{name}")

    def put_bucket_object(self, bucket_name: str, key: str) -> dict[str, Any]:
        response = self._request("PUT", f"/v1.0/buckets/{bucket_name}/objects", params={"key": key})
        return response.json()

    def list_bucket_objects(self, bucket_name: str, prefix: Optional[str] = None, delimiter: str = "/") -> dict[str, Any]:
        params: dict[str, Any] = {"delimiter": delimiter}
        if prefix is not None:
            params["prefix"] = prefix
        response = self._request("GET", f"/v1.0/buckets/{bucket_name}/objects", params=params)
        return response.json()

    def delete_bucket_object(self, bucket_name: str, key: str) -> None:
        self._request("DELETE", f"/v1.0/buckets/{bucket_name}/objects", params={"key": key})

    def get_bucket_object_metadata(self, bucket_name: str, key: str) -> dict[str, Any]:
        response = self._request("GET", f"/v1.0/buckets/{bucket_name}/objects/metadata", params={"key": key})
        return response.json()

    def download_bucket_object(self, bucket_name: str, key: str) -> httpx.Response:
        return self._request("GET", f"/v1.0/buckets/{bucket_name}/objects/download", params={"key": key})

    def upload_bucket_object(
        self,
        bucket_name: str,
        key: str,
        data: bytes,
        content_type: str = "application/octet-stream",
    ) -> dict[str, Any]:
        response = self._client.request(
            "POST",
            f"/v1.0/buckets/{bucket_name}/objects/upload",
            params={"key": key},
            content=data,
            headers={"Content-Type": content_type},
        )
        self._raise_for_status(response)
        return response.json()

    def crawl_plan_exists(self, plan_id: str) -> bool:
        return self._head(f"/v1.0/crawlplans/{plan_id}")

    def get_crawl_operation_enumeration(self, plan_id: str, operation_id: str) -> dict[str, Any]:
        response = self._request("GET", f"/v1.0/crawlplans/{plan_id}/operations/{operation_id}/enumeration")
        return response.json()
