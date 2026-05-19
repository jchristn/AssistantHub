"""Synchronous client for the AssistantHub API."""

from __future__ import annotations

import base64
import os
from typing import Any, Generator, Iterator, Optional, Union

from ._base_client import BaseClient
from ._parity_sync_mixin import AssistantHubClientParityMixin
from .models import (
    Assistant,
    AssistantDocument,
    ChatCompletionMessage,
    ChatCompletionRequest,
    ChatCompletionResponse,
    ChatHistory,
    CrawlOperation,
    CrawlPlan,
    Credential,
    EndpointHealthStatus,
    EnumerationQuery,
    EnumerationResult,
    EvalFact,
    EvalResult,
    EvalRun,
    EvalRunRequest,
    InferenceModel,
    IngestionRule,
    PartioEndpointConfig,
    PartioEndpointRequest,
    PullProgress,
    TenantMetadata,
    UserMaster,
)


class AssistantHubClient(AssistantHubClientParityMixin, BaseClient):
    """Synchronous client for the AssistantHub REST API.

    Provides methods for managing assistants, collections, threads, and
    sending chat messages.

    Args:
        base_url: The base URL of the AssistantHub API (e.g. "http://localhost:8000").
        api_key: Optional bearer token for authentication.
        timeout: Request timeout in seconds. Defaults to 30.
    """

    def __init__(
        self,
        base_url: str,
        api_key: Optional[str] = None,
        timeout: float = 30.0,
    ) -> None:
        super().__init__(base_url, api_key=api_key, timeout=timeout)

    # ------------------------------------------------------------------
    # Assistants
    # ------------------------------------------------------------------

    def list_assistants(
        self,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[Assistant]:
        """List assistants.

        Args:
            max_results: Maximum number of results to return.
            continuation_token: Token for fetching the next page.

        Returns:
            Paginated enumeration result containing Assistant objects.
        """
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token

        response = self._request("GET", "/v1.0/assistants", params=params)
        data = response.json()
        objects = [Assistant.model_validate(obj) for obj in (data.get("objects") or [])]
        result = EnumerationResult[Assistant].model_validate(data)
        result.objects = objects
        return result

    def get_assistant(self, assistant_id: str) -> Assistant:
        """Get a single assistant by ID.

        Args:
            assistant_id: The assistant identifier.

        Returns:
            The requested Assistant.
        """
        response = self._request("GET", f"/v1.0/assistants/{assistant_id}")
        return Assistant.model_validate(response.json())

    def create_assistant(self, assistant: Assistant) -> Assistant:
        """Create a new assistant.

        Args:
            assistant: The Assistant object to create.

        Returns:
            The created Assistant with server-assigned fields populated.
        """
        response = self._request(
            "PUT",
            "/v1.0/assistants",
            json=assistant.model_dump(by_alias=True, exclude_none=True),
        )
        return Assistant.model_validate(response.json())

    def update_assistant(
        self, assistant_id: str, assistant: Assistant
    ) -> Assistant:
        """Update an existing assistant.

        Args:
            assistant_id: The assistant identifier.
            assistant: The updated Assistant object.

        Returns:
            The updated Assistant.
        """
        response = self._request(
            "PUT",
            f"/v1.0/assistants/{assistant_id}",
            json=assistant.model_dump(by_alias=True, exclude_none=True),
        )
        return Assistant.model_validate(response.json())

    def delete_assistant(self, assistant_id: str) -> None:
        """Delete an assistant.

        Args:
            assistant_id: The assistant identifier.
        """
        self._request("DELETE", f"/v1.0/assistants/{assistant_id}")

    # ------------------------------------------------------------------
    # Collections
    # ------------------------------------------------------------------

    def list_collections(
        self,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> dict[str, Any]:
        """List collections.

        Requires global admin privileges.

        Args:
            max_results: Maximum number of results to return.
            continuation_token: Token for fetching the next page.

        Returns:
            The collection enumeration response from RecallDB.
        """
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token

        response = self._request("GET", "/v1.0/collections", params=params)
        return response.json()

    def get_collection(self, collection_id: str) -> dict[str, Any]:
        """Get a single collection by ID.

        Requires global admin privileges.

        Args:
            collection_id: The collection identifier.

        Returns:
            The collection data from RecallDB.
        """
        response = self._request("GET", f"/v1.0/collections/{collection_id}")
        return response.json()

    def create_collection(self, collection: dict[str, Any]) -> dict[str, Any]:
        """Create a new collection.

        Requires global admin privileges.

        Args:
            collection: The collection data to create (proxied to RecallDB).

        Returns:
            The created collection data from RecallDB.
        """
        response = self._request(
            "PUT", "/v1.0/collections", json=collection
        )
        return response.json()

    def update_collection(
        self, collection_id: str, collection: dict[str, Any]
    ) -> dict[str, Any]:
        """Update an existing collection.

        Requires global admin privileges.

        Args:
            collection_id: The collection identifier.
            collection: The updated collection data.

        Returns:
            The updated collection data from RecallDB.
        """
        response = self._request(
            "PUT",
            f"/v1.0/collections/{collection_id}",
            json=collection,
        )
        return response.json()

    def delete_collection(self, collection_id: str) -> None:
        """Delete a collection.

        Requires global admin privileges.

        Args:
            collection_id: The collection identifier.
        """
        self._request("DELETE", f"/v1.0/collections/{collection_id}")

    # ------------------------------------------------------------------
    # Threads
    # ------------------------------------------------------------------

    def list_threads(
        self,
        assistant_id: Optional[str] = None,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> list[dict[str, Any]]:
        """List threads, optionally filtered by assistant.

        Args:
            assistant_id: Optional assistant ID to filter threads.
            max_results: Maximum number of results to return.
            continuation_token: Token for fetching the next page.

        Returns:
            A list of thread summary objects.
        """
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token
        if assistant_id is not None:
            params["assistantIdFilter"] = assistant_id

        response = self._request("GET", "/v1.0/threads", params=params)
        return response.json()

    def get_thread(
        self, assistant_id: str, thread_id: str
    ) -> list[ChatHistory]:
        """Get the message history for a thread.

        Args:
            assistant_id: The assistant identifier that owns the thread.
            thread_id: The thread identifier.

        Returns:
            A list of ChatHistory records for the thread.
        """
        response = self._request(
            "GET",
            f"/v1.0/assistants/{assistant_id}/threads/{thread_id}/history",
        )
        data = response.json()
        if isinstance(data, list):
            return [ChatHistory.model_validate(item) for item in data]
        return []

    def create_thread(self, assistant_id: str) -> str:
        """Create a new chat thread.

        Args:
            assistant_id: The assistant identifier to create the thread for.

        Returns:
            The new thread ID.
        """
        response = self._request(
            "POST", f"/v1.0/assistants/{assistant_id}/threads"
        )
        data = response.json()
        return data.get("ThreadId", data.get("threadId", ""))

    def delete_thread(self, thread_id: str) -> None:
        """Delete a thread.

        Note: Thread deletion may not be supported by all server versions.

        Args:
            thread_id: The thread identifier.
        """
        self._request("DELETE", f"/v1.0/threads/{thread_id}")

    # ------------------------------------------------------------------
    # Chat
    # ------------------------------------------------------------------

    def send_message(
        self,
        assistant_id: str,
        messages: list[ChatCompletionMessage],
        *,
        thread_id: Optional[str] = None,
        model: Optional[str] = None,
        temperature: Optional[float] = None,
        top_p: Optional[float] = None,
        max_tokens: Optional[int] = None,
    ) -> ChatCompletionResponse:
        """Send a chat message and get a complete response.

        Args:
            assistant_id: The assistant identifier.
            messages: The conversation messages to send.
            thread_id: Optional thread ID for conversation continuity.
            model: Optional model override.
            temperature: Optional temperature for generation.
            top_p: Optional top-p for generation.
            max_tokens: Optional max tokens for generation.

        Returns:
            The chat completion response.
        """
        request = ChatCompletionRequest(
            model=model,
            messages=messages,
            stream=False,
            temperature=temperature,
            top_p=top_p,
            max_tokens=max_tokens,
        )

        headers: Optional[dict[str, str]] = None
        if thread_id is not None:
            headers = {"X-Thread-ID": thread_id}

        response = self._request(
            "POST",
            f"/v1.0/assistants/{assistant_id}/chat",
            json=request.model_dump(by_alias=True, exclude_none=True),
            headers=headers,
        )
        return ChatCompletionResponse.model_validate(response.json())

    def send_message_stream(
        self,
        assistant_id: str,
        messages: list[ChatCompletionMessage],
        *,
        thread_id: Optional[str] = None,
        model: Optional[str] = None,
        temperature: Optional[float] = None,
        top_p: Optional[float] = None,
        max_tokens: Optional[int] = None,
    ) -> Iterator[str]:
        """Send a chat message and stream the response as SSE chunks.

        Args:
            assistant_id: The assistant identifier.
            messages: The conversation messages to send.
            thread_id: Optional thread ID for conversation continuity.
            model: Optional model override.
            temperature: Optional temperature for generation.
            top_p: Optional top-p for generation.
            max_tokens: Optional max tokens for generation.

        Yields:
            Raw SSE data strings as they arrive from the server.
        """
        request = ChatCompletionRequest(
            model=model,
            messages=messages,
            stream=True,
            temperature=temperature,
            top_p=top_p,
            max_tokens=max_tokens,
        )

        extra_headers: dict[str, str] = {}
        if thread_id is not None:
            extra_headers["X-Thread-ID"] = thread_id

        with self._client.stream(
            "POST",
            f"/v1.0/assistants/{assistant_id}/chat",
            json=request.model_dump(by_alias=True, exclude_none=True),
            headers=extra_headers if extra_headers else None,
        ) as stream_response:
            BaseClient._raise_for_status(stream_response)
            for line in stream_response.iter_lines():
                if line.startswith("data: "):
                    yield line[6:]

    # ------------------------------------------------------------------
    # Embedding Endpoints
    # ------------------------------------------------------------------

    def list_embedding_endpoints(
        self,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[PartioEndpointConfig]:
        """List embedding endpoints.

        Requires global admin privileges.

        Args:
            max_results: Maximum number of results to return.
            continuation_token: Token for fetching the next page.

        Returns:
            Paginated enumeration result containing endpoint configs.
        """
        body: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            body["continuationToken"] = continuation_token

        response = self._request(
            "POST", "/v1.0/endpoints/embedding/enumerate", json=body
        )
        data = response.json()
        objects = [
            PartioEndpointConfig.model_validate(obj)
            for obj in (data.get("objects") or [])
        ]
        result = EnumerationResult[PartioEndpointConfig].model_validate(data)
        result.objects = objects
        return result

    def get_embedding_endpoint(self, endpoint_id: str) -> PartioEndpointConfig:
        """Get a single embedding endpoint by ID.

        Args:
            endpoint_id: The endpoint identifier.

        Returns:
            The requested endpoint configuration.
        """
        response = self._request(
            "GET", f"/v1.0/endpoints/embedding/{endpoint_id}"
        )
        return PartioEndpointConfig.model_validate(response.json())

    def create_embedding_endpoint(
        self, endpoint: PartioEndpointRequest
    ) -> PartioEndpointConfig:
        """Create a new embedding endpoint.

        Requires global admin privileges.

        Args:
            endpoint: The endpoint request to create.

        Returns:
            The created endpoint configuration.
        """
        response = self._request(
            "PUT",
            "/v1.0/endpoints/embedding",
            json=endpoint.model_dump(by_alias=True, exclude_none=True),
        )
        return PartioEndpointConfig.model_validate(response.json())

    def update_embedding_endpoint(
        self, endpoint_id: str, endpoint: PartioEndpointRequest
    ) -> PartioEndpointConfig:
        """Update an existing embedding endpoint.

        Args:
            endpoint_id: The endpoint identifier.
            endpoint: The updated endpoint request.

        Returns:
            The updated endpoint configuration.
        """
        response = self._request(
            "PUT",
            f"/v1.0/endpoints/embedding/{endpoint_id}",
            json=endpoint.model_dump(by_alias=True, exclude_none=True),
        )
        return PartioEndpointConfig.model_validate(response.json())

    def delete_embedding_endpoint(self, endpoint_id: str) -> None:
        """Delete an embedding endpoint.

        Args:
            endpoint_id: The endpoint identifier.
        """
        self._request("DELETE", f"/v1.0/endpoints/embedding/{endpoint_id}")

    def check_embedding_health(self) -> list[EndpointHealthStatus]:
        """Check health of all embedding endpoints.

        Returns:
            A list of health status objects for each embedding endpoint.
        """
        response = self._request("GET", "/v1.0/endpoints/embedding/health")
        data = response.json()
        if isinstance(data, list):
            return [EndpointHealthStatus.model_validate(item) for item in data]
        return []

    def check_embedding_endpoint_health(
        self, endpoint_id: str
    ) -> EndpointHealthStatus:
        """Check health of a specific embedding endpoint.

        Args:
            endpoint_id: The endpoint identifier.

        Returns:
            The health status of the endpoint.
        """
        response = self._request(
            "GET", f"/v1.0/endpoints/embedding/{endpoint_id}/health"
        )
        return EndpointHealthStatus.model_validate(response.json())

    # ------------------------------------------------------------------
    # Completion Endpoints
    # ------------------------------------------------------------------

    def list_completion_endpoints(
        self,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[PartioEndpointConfig]:
        """List completion endpoints.

        Requires global admin privileges.

        Args:
            max_results: Maximum number of results to return.
            continuation_token: Token for fetching the next page.

        Returns:
            Paginated enumeration result containing endpoint configs.
        """
        body: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            body["continuationToken"] = continuation_token

        response = self._request(
            "POST", "/v1.0/endpoints/completion/enumerate", json=body
        )
        data = response.json()
        objects = [
            PartioEndpointConfig.model_validate(obj)
            for obj in (data.get("objects") or [])
        ]
        result = EnumerationResult[PartioEndpointConfig].model_validate(data)
        result.objects = objects
        return result

    def get_completion_endpoint(self, endpoint_id: str) -> PartioEndpointConfig:
        """Get a single completion endpoint by ID.

        Args:
            endpoint_id: The endpoint identifier.

        Returns:
            The requested endpoint configuration.
        """
        response = self._request(
            "GET", f"/v1.0/endpoints/completion/{endpoint_id}"
        )
        return PartioEndpointConfig.model_validate(response.json())

    def create_completion_endpoint(
        self, endpoint: PartioEndpointRequest
    ) -> PartioEndpointConfig:
        """Create a new completion endpoint.

        Requires global admin privileges.

        Args:
            endpoint: The endpoint request to create.

        Returns:
            The created endpoint configuration.
        """
        response = self._request(
            "PUT",
            "/v1.0/endpoints/completion",
            json=endpoint.model_dump(by_alias=True, exclude_none=True),
        )
        return PartioEndpointConfig.model_validate(response.json())

    def update_completion_endpoint(
        self, endpoint_id: str, endpoint: PartioEndpointRequest
    ) -> PartioEndpointConfig:
        """Update an existing completion endpoint.

        Args:
            endpoint_id: The endpoint identifier.
            endpoint: The updated endpoint request.

        Returns:
            The updated endpoint configuration.
        """
        response = self._request(
            "PUT",
            f"/v1.0/endpoints/completion/{endpoint_id}",
            json=endpoint.model_dump(by_alias=True, exclude_none=True),
        )
        return PartioEndpointConfig.model_validate(response.json())

    def delete_completion_endpoint(self, endpoint_id: str) -> None:
        """Delete a completion endpoint.

        Args:
            endpoint_id: The endpoint identifier.
        """
        self._request("DELETE", f"/v1.0/endpoints/completion/{endpoint_id}")

    def check_completion_health(self) -> list[EndpointHealthStatus]:
        """Check health of all completion endpoints.

        Returns:
            A list of health status objects for each completion endpoint.
        """
        response = self._request("GET", "/v1.0/endpoints/completion/health")
        data = response.json()
        if isinstance(data, list):
            return [EndpointHealthStatus.model_validate(item) for item in data]
        return []

    def check_completion_endpoint_health(
        self, endpoint_id: str
    ) -> EndpointHealthStatus:
        """Check health of a specific completion endpoint.

        Args:
            endpoint_id: The endpoint identifier.

        Returns:
            The health status of the endpoint.
        """
        response = self._request(
            "GET", f"/v1.0/endpoints/completion/{endpoint_id}/health"
        )
        return EndpointHealthStatus.model_validate(response.json())

    # ------------------------------------------------------------------
    # Documents
    # ------------------------------------------------------------------

    def list_documents(
        self,
        collection_id: Optional[str] = None,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[AssistantDocument]:
        """List documents, optionally filtered by collection.

        Args:
            collection_id: Optional collection ID to filter documents.
            max_results: Maximum number of results to return.
            continuation_token: Token for fetching the next page.

        Returns:
            Paginated enumeration result containing document objects.
        """
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token
        if collection_id is not None:
            params["collectionIdFilter"] = collection_id

        response = self._request("GET", "/v1.0/documents", params=params)
        data = response.json()
        objects = [
            AssistantDocument.model_validate(obj)
            for obj in (data.get("objects") or [])
        ]
        result = EnumerationResult[AssistantDocument].model_validate(data)
        result.objects = objects
        return result

    def get_document(self, document_id: str) -> AssistantDocument:
        """Get a single document by ID.

        Args:
            document_id: The document identifier.

        Returns:
            The requested document.
        """
        response = self._request("GET", f"/v1.0/documents/{document_id}")
        return AssistantDocument.model_validate(response.json())

    def upload_document(
        self,
        ingestion_rule_id: str,
        content: Union[str, bytes],
        *,
        name: Optional[str] = None,
        original_filename: Optional[str] = None,
        content_type: Optional[str] = None,
        labels: Optional[list[str]] = None,
        tags: Optional[dict[str, str]] = None,
    ) -> AssistantDocument:
        """Upload a document for ingestion.

        Args:
            ingestion_rule_id: The ingestion rule ID to process the document with.
            content: File path (str) or raw bytes to upload. If a file path is
                provided, the file is read and its basename is used as the
                original filename when *original_filename* is not set.
            name: Optional display name for the document.
            original_filename: Optional original filename override.
            content_type: Optional MIME type of the document.
            labels: Optional list of labels.
            tags: Optional key-value tags.

        Returns:
            The created document metadata.
        """
        if isinstance(content, str):
            file_path = content
            with open(file_path, "rb") as fh:
                raw_bytes = fh.read()
            if original_filename is None:
                original_filename = os.path.basename(file_path)
        else:
            raw_bytes = content

        body: dict[str, Any] = {
            "ingestionRuleId": ingestion_rule_id,
            "base64Content": base64.b64encode(raw_bytes).decode("ascii"),
        }
        if name is not None:
            body["name"] = name
        if original_filename is not None:
            body["originalFilename"] = original_filename
        if content_type is not None:
            body["contentType"] = content_type
        if labels is not None:
            body["labels"] = labels
        if tags is not None:
            body["tags"] = tags

        response = self._request("PUT", "/v1.0/documents", json=body)
        return AssistantDocument.model_validate(response.json())

    def delete_document(self, document_id: str) -> None:
        """Delete a document.

        Args:
            document_id: The document identifier.
        """
        self._request("DELETE", f"/v1.0/documents/{document_id}")

    def bulk_delete_documents(self, document_ids: list[str]) -> None:
        """Delete multiple documents at once.

        Args:
            document_ids: List of document identifiers to delete.
        """
        self._request(
            "POST",
            "/v1.0/documents/delete",
            json={"documentIds": document_ids},
        )

    # ------------------------------------------------------------------
    # Ingestion Rules
    # ------------------------------------------------------------------

    def list_ingestion_rules(
        self,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[IngestionRule]:
        """List ingestion rules.

        Args:
            max_results: Maximum number of results to return.
            continuation_token: Token for fetching the next page.

        Returns:
            Paginated enumeration result containing ingestion rules.
        """
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token

        response = self._request("GET", "/v1.0/ingestion-rules", params=params)
        data = response.json()
        objects = [
            IngestionRule.model_validate(obj)
            for obj in (data.get("objects") or [])
        ]
        result = EnumerationResult[IngestionRule].model_validate(data)
        result.objects = objects
        return result

    def get_ingestion_rule(self, rule_id: str) -> IngestionRule:
        """Get a single ingestion rule by ID.

        Args:
            rule_id: The ingestion rule identifier.

        Returns:
            The requested ingestion rule.
        """
        response = self._request("GET", f"/v1.0/ingestion-rules/{rule_id}")
        return IngestionRule.model_validate(response.json())

    def create_ingestion_rule(self, rule: IngestionRule) -> IngestionRule:
        """Create a new ingestion rule.

        Args:
            rule: The ingestion rule to create.

        Returns:
            The created ingestion rule with server-assigned fields populated.
        """
        response = self._request(
            "PUT",
            "/v1.0/ingestion-rules",
            json=rule.model_dump(by_alias=True, exclude_none=True),
        )
        return IngestionRule.model_validate(response.json())

    def update_ingestion_rule(
        self, rule_id: str, rule: IngestionRule
    ) -> IngestionRule:
        """Update an existing ingestion rule.

        Args:
            rule_id: The ingestion rule identifier.
            rule: The updated ingestion rule.

        Returns:
            The updated ingestion rule.
        """
        response = self._request(
            "PUT",
            f"/v1.0/ingestion-rules/{rule_id}",
            json=rule.model_dump(by_alias=True, exclude_none=True),
        )
        return IngestionRule.model_validate(response.json())

    def delete_ingestion_rule(self, rule_id: str) -> None:
        """Delete an ingestion rule.

        Args:
            rule_id: The ingestion rule identifier.
        """
        self._request("DELETE", f"/v1.0/ingestion-rules/{rule_id}")

    # ------------------------------------------------------------------
    # Retrieval / Search
    # ------------------------------------------------------------------

    def search(
        self,
        assistant_id: str,
        query: str,
        *,
        thread_id: Optional[str] = None,
        max_tokens: Optional[int] = None,
        temperature: Optional[float] = None,
    ) -> ChatCompletionResponse:
        """Search documents via RAG retrieval through the chat endpoint.

        Sends a single user message to the assistant and returns the full
        response including retrieval results and citations.

        Args:
            assistant_id: The assistant identifier to search through.
            query: The search query text.
            thread_id: Optional thread ID for conversation continuity.
            max_tokens: Optional max tokens for generation.
            temperature: Optional temperature for generation.

        Returns:
            The chat completion response containing retrieval results
            and citations alongside the generated answer.
        """
        messages = [ChatCompletionMessage(role="user", content=query)]
        return self.send_message(
            assistant_id,
            messages,
            thread_id=thread_id,
            max_tokens=max_tokens,
            temperature=temperature,
        )

    # ------------------------------------------------------------------
    # Inference / Models
    # ------------------------------------------------------------------

    def list_models(
        self,
        *,
        assistant_id: Optional[str] = None,
    ) -> list[InferenceModel]:
        """List available inference models.

        Args:
            assistant_id: Optional assistant ID to resolve the assistant's
                configured inference endpoint.

        Returns:
            A list of available inference models.
        """
        params: dict[str, Any] = {}
        if assistant_id is not None:
            params["assistantId"] = assistant_id

        response = self._request(
            "GET", "/v1.0/models", params=params if params else None
        )
        data = response.json()
        if isinstance(data, list):
            return [InferenceModel.model_validate(item) for item in data]
        return []

    def pull_model(self, model_name: str) -> dict[str, Any]:
        """Start pulling a model.

        Args:
            model_name: The name of the model to pull.

        Returns:
            The pull initiation response with ModelName and Status.
        """
        response = self._request(
            "POST", "/v1.0/models/pull", json={"Name": model_name}
        )
        return response.json()

    def get_pull_status(self) -> PullProgress:
        """Get the status of a model pull operation.

        Returns:
            The current pull progress.
        """
        response = self._request("GET", "/v1.0/models/pull/status")
        return PullProgress.model_validate(response.json())

    def delete_model(self, model_name: str) -> None:
        """Delete a model.

        Args:
            model_name: The name of the model to delete.
        """
        self._request("DELETE", f"/v1.0/models/{model_name}")

    def generate(
        self,
        assistant_id: str,
        messages: list[ChatCompletionMessage],
        *,
        model: Optional[str] = None,
        temperature: Optional[float] = None,
        top_p: Optional[float] = None,
        max_tokens: Optional[int] = None,
    ) -> ChatCompletionResponse:
        """Generate a response without RAG retrieval.

        Args:
            assistant_id: The assistant identifier.
            messages: The conversation messages to send.
            model: Optional model override.
            temperature: Optional temperature for generation.
            top_p: Optional top-p for generation.
            max_tokens: Optional max tokens for generation.

        Returns:
            The chat completion response.
        """
        request = ChatCompletionRequest(
            model=model,
            messages=messages,
            stream=False,
            temperature=temperature,
            top_p=top_p,
            max_tokens=max_tokens,
        )
        response = self._request(
            "POST",
            f"/v1.0/assistants/{assistant_id}/generate",
            json=request.model_dump(by_alias=True, exclude_none=True),
        )
        return ChatCompletionResponse.model_validate(response.json())

    def generate_stream(
        self,
        assistant_id: str,
        messages: list[ChatCompletionMessage],
        *,
        model: Optional[str] = None,
        temperature: Optional[float] = None,
        top_p: Optional[float] = None,
        max_tokens: Optional[int] = None,
    ) -> Iterator[str]:
        """Generate a streaming response without RAG retrieval.

        Args:
            assistant_id: The assistant identifier.
            messages: The conversation messages to send.
            model: Optional model override.
            temperature: Optional temperature for generation.
            top_p: Optional top-p for generation.
            max_tokens: Optional max tokens for generation.

        Yields:
            Raw SSE data strings as they arrive from the server.
        """
        request = ChatCompletionRequest(
            model=model,
            messages=messages,
            stream=True,
            temperature=temperature,
            top_p=top_p,
            max_tokens=max_tokens,
        )
        with self._client.stream(
            "POST",
            f"/v1.0/assistants/{assistant_id}/generate",
            json=request.model_dump(by_alias=True, exclude_none=True),
        ) as stream_response:
            BaseClient._raise_for_status(stream_response)
            for line in stream_response.iter_lines():
                if line.startswith("data: "):
                    yield line[6:]

    # ------------------------------------------------------------------
    # Eval
    # ------------------------------------------------------------------

    def list_eval_facts(
        self,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[EvalFact]:
        """List evaluation facts.

        Args:
            max_results: Maximum number of results to return.
            continuation_token: Token for fetching the next page.

        Returns:
            Paginated enumeration result containing EvalFact objects.
        """
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token

        response = self._request("GET", "/v1.0/eval/facts", params=params)
        data = response.json()
        objects = [EvalFact.model_validate(obj) for obj in (data.get("objects") or [])]
        result = EnumerationResult[EvalFact].model_validate(data)
        result.objects = objects
        return result

    def get_eval_fact(self, fact_id: str) -> EvalFact:
        """Get a single evaluation fact by ID.

        Args:
            fact_id: The eval fact identifier.

        Returns:
            The requested EvalFact.
        """
        response = self._request("GET", f"/v1.0/eval/facts/{fact_id}")
        return EvalFact.model_validate(response.json())

    def create_eval_fact(self, fact: EvalFact) -> EvalFact:
        """Create a new evaluation fact.

        Args:
            fact: The EvalFact object to create. Must include a valid
                AssistantId (not a placeholder).

        Returns:
            The created EvalFact with server-assigned fields populated.
        """
        response = self._request(
            "PUT",
            "/v1.0/eval/facts",
            json=fact.model_dump(by_alias=True, exclude_none=True),
        )
        return EvalFact.model_validate(response.json())

    def update_eval_fact(self, fact_id: str, fact: EvalFact) -> EvalFact:
        """Update an existing evaluation fact.

        Args:
            fact_id: The eval fact identifier.
            fact: The updated EvalFact object.

        Returns:
            The updated EvalFact.
        """
        response = self._request(
            "PUT",
            f"/v1.0/eval/facts/{fact_id}",
            json=fact.model_dump(by_alias=True, exclude_none=True),
        )
        return EvalFact.model_validate(response.json())

    def delete_eval_fact(self, fact_id: str) -> None:
        """Delete an evaluation fact.

        Args:
            fact_id: The eval fact identifier.
        """
        self._request("DELETE", f"/v1.0/eval/facts/{fact_id}")

    def start_eval_run(self, run_request: EvalRunRequest) -> EvalRun:
        """Start a new evaluation run.

        Args:
            run_request: The eval run request containing AssistantId and
                optional JudgePrompt.

        Returns:
            The created EvalRun.
        """
        response = self._request(
            "POST",
            "/v1.0/eval/runs",
            json=run_request.model_dump(by_alias=True, exclude_none=True),
        )
        return EvalRun.model_validate(response.json())

    def list_eval_runs(
        self,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[EvalRun]:
        """List evaluation runs.

        Args:
            max_results: Maximum number of results to return.
            continuation_token: Token for fetching the next page.

        Returns:
            Paginated enumeration result containing EvalRun objects.
        """
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token

        response = self._request("GET", "/v1.0/eval/runs", params=params)
        data = response.json()
        objects = [EvalRun.model_validate(obj) for obj in (data.get("objects") or [])]
        result = EnumerationResult[EvalRun].model_validate(data)
        result.objects = objects
        return result

    def get_eval_run(self, run_id: str) -> EvalRun:
        """Get a single evaluation run by ID.

        Args:
            run_id: The eval run identifier.

        Returns:
            The requested EvalRun.
        """
        response = self._request("GET", f"/v1.0/eval/runs/{run_id}")
        return EvalRun.model_validate(response.json())

    def delete_eval_run(self, run_id: str) -> None:
        """Delete an evaluation run.

        Args:
            run_id: The eval run identifier.
        """
        self._request("DELETE", f"/v1.0/eval/runs/{run_id}")

    def list_eval_results(self, run_id: str) -> list[EvalResult]:
        """Get all results for an evaluation run.

        Args:
            run_id: The eval run identifier.

        Returns:
            A list of EvalResult objects for the run.
        """
        response = self._request("GET", f"/v1.0/eval/runs/{run_id}/results")
        data = response.json()
        if isinstance(data, list):
            return [EvalResult.model_validate(item) for item in data]
        return []

    def get_eval_result(self, result_id: str) -> EvalResult:
        """Get a single evaluation result by ID.

        Args:
            result_id: The eval result identifier.

        Returns:
            The requested EvalResult.
        """
        response = self._request("GET", f"/v1.0/eval/results/{result_id}")
        return EvalResult.model_validate(response.json())

    def stream_eval_run(self, run_id: str) -> Iterator[str]:
        """Stream evaluation run updates via SSE.

        Args:
            run_id: The eval run identifier.

        Yields:
            Raw SSE data strings as they arrive from the server.
        """
        with self._client.stream(
            "GET", f"/v1.0/eval/runs/{run_id}/stream"
        ) as stream_response:
            BaseClient._raise_for_status(stream_response)
            for line in stream_response.iter_lines():
                if line.startswith("data: "):
                    yield line[6:]

    def get_default_judge_prompt(self) -> str:
        """Get the default judge prompt for evaluation.

        Returns:
            The default judge prompt string.
        """
        response = self._request("GET", "/v1.0/eval/judge-prompt/default")
        data = response.json()
        return data.get("Prompt", "")

    # ------------------------------------------------------------------
    # Crawl Plans
    # ------------------------------------------------------------------

    def list_crawl_plans(
        self,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[CrawlPlan]:
        """List crawl plans.

        Args:
            max_results: Maximum number of results to return.
            continuation_token: Token for fetching the next page.

        Returns:
            Paginated enumeration result containing CrawlPlan objects.
        """
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token

        response = self._request("GET", "/v1.0/crawlplans", params=params)
        data = response.json()
        objects = [CrawlPlan.model_validate(obj) for obj in (data.get("objects") or [])]
        result = EnumerationResult[CrawlPlan].model_validate(data)
        result.objects = objects
        return result

    def get_crawl_plan(self, plan_id: str) -> CrawlPlan:
        """Get a single crawl plan by ID.

        Args:
            plan_id: The crawl plan identifier.

        Returns:
            The requested CrawlPlan.
        """
        response = self._request("GET", f"/v1.0/crawlplans/{plan_id}")
        return CrawlPlan.model_validate(response.json())

    def create_crawl_plan(self, plan: CrawlPlan) -> CrawlPlan:
        """Create a new crawl plan.

        Args:
            plan: The CrawlPlan object to create.

        Returns:
            The created CrawlPlan with server-assigned fields populated.
        """
        response = self._request(
            "PUT",
            "/v1.0/crawlplans",
            json=plan.model_dump(by_alias=True, exclude_none=True),
        )
        return CrawlPlan.model_validate(response.json())

    def update_crawl_plan(self, plan_id: str, plan: CrawlPlan) -> CrawlPlan:
        """Update an existing crawl plan.

        Args:
            plan_id: The crawl plan identifier.
            plan: The updated CrawlPlan object.

        Returns:
            The updated CrawlPlan.
        """
        response = self._request(
            "PUT",
            f"/v1.0/crawlplans/{plan_id}",
            json=plan.model_dump(by_alias=True, exclude_none=True),
        )
        return CrawlPlan.model_validate(response.json())

    def delete_crawl_plan(self, plan_id: str) -> None:
        """Delete a crawl plan.

        Requires admin privileges.

        Args:
            plan_id: The crawl plan identifier.
        """
        self._request("DELETE", f"/v1.0/crawlplans/{plan_id}")

    def start_crawl(self, plan_id: str) -> CrawlPlan:
        """Start a crawl for a given plan.

        Requires admin privileges.

        Args:
            plan_id: The crawl plan identifier.

        Returns:
            The updated CrawlPlan.
        """
        response = self._request("POST", f"/v1.0/crawlplans/{plan_id}/start")
        return CrawlPlan.model_validate(response.json())

    def stop_crawl(self, plan_id: str) -> CrawlPlan:
        """Stop a running crawl.

        Requires admin privileges.

        Args:
            plan_id: The crawl plan identifier.

        Returns:
            The updated CrawlPlan.
        """
        response = self._request("POST", f"/v1.0/crawlplans/{plan_id}/stop")
        return CrawlPlan.model_validate(response.json())

    def test_crawl_connectivity(self, plan_id: str) -> dict[str, Any]:
        """Test connectivity for a crawl plan.

        Args:
            plan_id: The crawl plan identifier.

        Returns:
            A dict with a 'Success' boolean field.
        """
        response = self._request(
            "POST", f"/v1.0/crawlplans/{plan_id}/connectivity"
        )
        return response.json()

    def enumerate_crawl_contents(self, plan_id: str) -> list[dict[str, Any]]:
        """Enumerate contents available for a crawl plan.

        Args:
            plan_id: The crawl plan identifier.

        Returns:
            A list of crawled object dictionaries.
        """
        response = self._request(
            "GET", f"/v1.0/crawlplans/{plan_id}/enumerate"
        )
        data = response.json()
        if isinstance(data, list):
            return data
        return []

    # ------------------------------------------------------------------
    # Crawl Operations
    # ------------------------------------------------------------------

    def list_crawl_operations(
        self,
        plan_id: str,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[CrawlOperation]:
        """List crawl operations for a plan.

        Args:
            plan_id: The crawl plan identifier.
            max_results: Maximum number of results to return.
            continuation_token: Token for fetching the next page.

        Returns:
            Paginated enumeration result containing CrawlOperation objects.
        """
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token

        response = self._request(
            "GET", f"/v1.0/crawlplans/{plan_id}/operations", params=params
        )
        data = response.json()
        objects = [
            CrawlOperation.model_validate(obj)
            for obj in (data.get("objects") or [])
        ]
        result = EnumerationResult[CrawlOperation].model_validate(data)
        result.objects = objects
        return result

    def get_crawl_operation(
        self, plan_id: str, operation_id: str
    ) -> CrawlOperation:
        """Get a single crawl operation by ID.

        Args:
            plan_id: The crawl plan identifier.
            operation_id: The crawl operation identifier.

        Returns:
            The requested CrawlOperation.
        """
        response = self._request(
            "GET", f"/v1.0/crawlplans/{plan_id}/operations/{operation_id}"
        )
        return CrawlOperation.model_validate(response.json())

    def delete_crawl_operation(
        self, plan_id: str, operation_id: str
    ) -> None:
        """Delete a crawl operation.

        Requires admin privileges.

        Args:
            plan_id: The crawl plan identifier.
            operation_id: The crawl operation identifier.
        """
        self._request(
            "DELETE", f"/v1.0/crawlplans/{plan_id}/operations/{operation_id}"
        )

    def get_crawl_plan_statistics(self, plan_id: str) -> dict[str, Any]:
        """Get aggregate statistics for all operations in a crawl plan.

        Args:
            plan_id: The crawl plan identifier.

        Returns:
            A dict with statistics including LastRun, NextRun, FailedRunCount,
            SuccessfulRunCount, MinRuntimeMs, MaxRuntimeMs, AvgRuntimeMs,
            ObjectCount, BytesCrawled.
        """
        response = self._request(
            "GET", f"/v1.0/crawlplans/{plan_id}/operations/statistics"
        )
        return response.json()

    def get_crawl_operation_statistics(
        self, plan_id: str, operation_id: str
    ) -> dict[str, Any]:
        """Get statistics for a specific crawl operation.

        Args:
            plan_id: The crawl plan identifier.
            operation_id: The crawl operation identifier.

        Returns:
            A dict with statistics including LastRun, FailedRunCount,
            SuccessfulRunCount, RuntimeMs, ObjectCount, BytesCrawled.
        """
        response = self._request(
            "GET",
            f"/v1.0/crawlplans/{plan_id}/operations/{operation_id}/statistics",
        )
        return response.json()

    # ------------------------------------------------------------------
    # Configuration
    # ------------------------------------------------------------------

    def get_config(self) -> dict[str, Any]:
        """Get the server configuration.

        Requires global admin privileges.

        Returns:
            The full AssistantHubSettings as a dictionary.
        """
        response = self._request("GET", "/v1.0/configuration")
        return response.json()

    def update_config(self, config: dict[str, Any]) -> dict[str, Any]:
        """Update the server configuration.

        Requires global admin privileges.

        Args:
            config: The full AssistantHubSettings object to apply.

        Returns:
            The updated AssistantHubSettings as a dictionary.
        """
        response = self._request("PUT", "/v1.0/configuration", json=config)
        return response.json()

    # ------------------------------------------------------------------
    # Health / Status
    # ------------------------------------------------------------------

    def health_check(self) -> dict[str, Any]:
        """Check the health of the AssistantHub server.

        Returns:
            The server health response.
        """
        response = self._request("GET", "/")
        return response.json()

    def whoami(self) -> dict[str, Any]:
        """Get the authenticated user's identity information.

        Returns:
            The authentication context for the current user.
        """
        response = self._request("GET", "/v1.0/whoami")
        return response.json()

    # ------------------------------------------------------------------
    # Tenants
    # ------------------------------------------------------------------

    def list_tenants(
        self,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[TenantMetadata]:
        """List tenants.

        Global admin sees all tenants; regular users see only their own.

        Args:
            max_results: Maximum number of results to return.
            continuation_token: Token for fetching the next page.

        Returns:
            Paginated enumeration result containing TenantMetadata objects.
        """
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token

        response = self._request("GET", "/v1.0/tenants", params=params)
        data = response.json()
        objects = [
            TenantMetadata.model_validate(obj)
            for obj in (data.get("objects") or [])
        ]
        result = EnumerationResult[TenantMetadata].model_validate(data)
        result.objects = objects
        return result

    def get_tenant(self, tenant_id: str) -> TenantMetadata:
        """Get a single tenant by ID.

        Args:
            tenant_id: The tenant identifier.

        Returns:
            The requested TenantMetadata.
        """
        response = self._request("GET", f"/v1.0/tenants/{tenant_id}")
        return TenantMetadata.model_validate(response.json())

    def create_tenant(self, tenant: TenantMetadata) -> dict[str, Any]:
        """Create a new tenant.

        Requires global admin privileges. The response includes both
        the created tenant and provisioning results.

        Args:
            tenant: The TenantMetadata to create (Name is required).

        Returns:
            A dict with 'Tenant' (TenantMetadata) and 'Provisioning' keys.
        """
        response = self._request(
            "PUT",
            "/v1.0/tenants",
            json=tenant.model_dump(by_alias=True, exclude_none=True),
        )
        return response.json()

    def update_tenant(
        self, tenant_id: str, tenant: TenantMetadata
    ) -> TenantMetadata:
        """Update an existing tenant.

        Requires global admin privileges.

        Args:
            tenant_id: The tenant identifier.
            tenant: The updated TenantMetadata.

        Returns:
            The updated TenantMetadata.
        """
        response = self._request(
            "PUT",
            f"/v1.0/tenants/{tenant_id}",
            json=tenant.model_dump(by_alias=True, exclude_none=True),
        )
        return TenantMetadata.model_validate(response.json())

    def delete_tenant(self, tenant_id: str) -> None:
        """Delete a tenant.

        Requires global admin privileges.

        Args:
            tenant_id: The tenant identifier.
        """
        self._request("DELETE", f"/v1.0/tenants/{tenant_id}")

    # ------------------------------------------------------------------
    # Users
    # ------------------------------------------------------------------

    def list_users(
        self,
        tenant_id: str,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[UserMaster]:
        """List users in a tenant.

        Requires tenant admin privileges.

        Args:
            tenant_id: The tenant identifier.
            max_results: Maximum number of results to return.
            continuation_token: Token for fetching the next page.

        Returns:
            Paginated enumeration result containing UserMaster objects.
        """
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token

        response = self._request(
            "GET", f"/v1.0/tenants/{tenant_id}/users", params=params
        )
        data = response.json()
        objects = [
            UserMaster.model_validate(obj)
            for obj in (data.get("objects") or [])
        ]
        result = EnumerationResult[UserMaster].model_validate(data)
        result.objects = objects
        return result

    def get_user(self, tenant_id: str, user_id: str) -> UserMaster:
        """Get a single user by ID.

        Args:
            tenant_id: The tenant identifier.
            user_id: The user identifier.

        Returns:
            The requested UserMaster.
        """
        response = self._request(
            "GET", f"/v1.0/tenants/{tenant_id}/users/{user_id}"
        )
        return UserMaster.model_validate(response.json())

    def create_user(
        self, tenant_id: str, user: UserMaster
    ) -> UserMaster:
        """Create a new user in a tenant.

        Requires tenant admin privileges.

        Args:
            tenant_id: The tenant identifier.
            user: The UserMaster to create (Email is required).

        Returns:
            The created UserMaster with server-assigned fields populated.
        """
        response = self._request(
            "PUT",
            f"/v1.0/tenants/{tenant_id}/users",
            json=user.model_dump(by_alias=True, exclude_none=True),
        )
        return UserMaster.model_validate(response.json())

    def update_user(
        self, tenant_id: str, user_id: str, user: UserMaster
    ) -> UserMaster:
        """Update an existing user.

        Requires tenant admin privileges.

        Args:
            tenant_id: The tenant identifier.
            user_id: The user identifier.
            user: The updated UserMaster.

        Returns:
            The updated UserMaster.
        """
        response = self._request(
            "PUT",
            f"/v1.0/tenants/{tenant_id}/users/{user_id}",
            json=user.model_dump(by_alias=True, exclude_none=True),
        )
        return UserMaster.model_validate(response.json())

    def delete_user(self, tenant_id: str, user_id: str) -> None:
        """Delete a user.

        Requires tenant admin privileges.

        Args:
            tenant_id: The tenant identifier.
            user_id: The user identifier.
        """
        self._request(
            "DELETE", f"/v1.0/tenants/{tenant_id}/users/{user_id}"
        )

    # ------------------------------------------------------------------
    # Credentials
    # ------------------------------------------------------------------

    def list_credentials(
        self,
        tenant_id: str,
        max_results: int = 100,
        continuation_token: Optional[str] = None,
    ) -> EnumerationResult[Credential]:
        """List credentials in a tenant.

        Requires tenant admin privileges.

        Args:
            tenant_id: The tenant identifier.
            max_results: Maximum number of results to return.
            continuation_token: Token for fetching the next page.

        Returns:
            Paginated enumeration result containing Credential objects.
        """
        params: dict[str, Any] = {"maxResults": max_results}
        if continuation_token is not None:
            params["continuationToken"] = continuation_token

        response = self._request(
            "GET", f"/v1.0/tenants/{tenant_id}/credentials", params=params
        )
        data = response.json()
        objects = [
            Credential.model_validate(obj)
            for obj in (data.get("objects") or [])
        ]
        result = EnumerationResult[Credential].model_validate(data)
        result.objects = objects
        return result

    def get_credential(
        self, tenant_id: str, credential_id: str
    ) -> Credential:
        """Get a single credential by ID.

        Args:
            tenant_id: The tenant identifier.
            credential_id: The credential identifier.

        Returns:
            The requested Credential.
        """
        response = self._request(
            "GET",
            f"/v1.0/tenants/{tenant_id}/credentials/{credential_id}",
        )
        return Credential.model_validate(response.json())

    def create_credential(
        self, tenant_id: str, credential: Credential
    ) -> Credential:
        """Create a new credential in a tenant.

        Requires tenant admin privileges.

        Args:
            tenant_id: The tenant identifier.
            credential: The Credential to create (UserId is required).

        Returns:
            The created Credential with server-assigned fields populated.
        """
        response = self._request(
            "PUT",
            f"/v1.0/tenants/{tenant_id}/credentials",
            json=credential.model_dump(by_alias=True, exclude_none=True),
        )
        return Credential.model_validate(response.json())

    def update_credential(
        self, tenant_id: str, credential_id: str, credential: Credential
    ) -> Credential:
        """Update an existing credential.

        Requires tenant admin privileges.

        Args:
            tenant_id: The tenant identifier.
            credential_id: The credential identifier.
            credential: The updated Credential.

        Returns:
            The updated Credential.
        """
        response = self._request(
            "PUT",
            f"/v1.0/tenants/{tenant_id}/credentials/{credential_id}",
            json=credential.model_dump(by_alias=True, exclude_none=True),
        )
        return Credential.model_validate(response.json())

    def delete_credential(
        self, tenant_id: str, credential_id: str
    ) -> None:
        """Delete a credential.

        Requires tenant admin privileges.

        Args:
            tenant_id: The tenant identifier.
            credential_id: The credential identifier.
        """
        self._request(
            "DELETE",
            f"/v1.0/tenants/{tenant_id}/credentials/{credential_id}",
        )

    # ------------------------------------------------------------------
    # Context manager
    # ------------------------------------------------------------------

    def __enter__(self) -> AssistantHubClient:
        """Support use as a context manager."""
        return self

    def __exit__(self, *args: Any) -> None:
        self.close()
