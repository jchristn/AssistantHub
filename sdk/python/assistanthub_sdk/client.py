"""Synchronous client for the AssistantHub API."""

from __future__ import annotations

from typing import Any, Generator, Iterator, Optional

from ._base_client import BaseClient
from .models import (
    Assistant,
    ChatCompletionMessage,
    ChatCompletionRequest,
    ChatCompletionResponse,
    ChatHistory,
    EnumerationQuery,
    EnumerationResult,
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

    def __enter__(self) -> AssistantHubClient:
        """Support use as a context manager."""
        return self

    def __exit__(self, *args: Any) -> None:
        self.close()
