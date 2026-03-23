"""Synchronous client for the AssistantHub API."""

from __future__ import annotations

import base64
import os
from typing import Any, Generator, Iterator, Optional, Union

from ._base_client import BaseClient
from .models import (
    Assistant,
    AssistantDocument,
    ChatCompletionMessage,
    ChatCompletionRequest,
    ChatCompletionResponse,
    ChatHistory,
    EndpointHealthStatus,
    EnumerationQuery,
    EnumerationResult,
    IngestionRule,
    PartioEndpointConfig,
    PartioEndpointRequest,
)


class AssistantHubClient(BaseClient):
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

    def __enter__(self) -> AssistantHubClient:
        """Support use as a context manager."""
        return self

    def __exit__(self, *args: Any) -> None:
        self.close()
