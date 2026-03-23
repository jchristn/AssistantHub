"""Base HTTP client for the AssistantHub SDK."""

from __future__ import annotations

from typing import Any, Optional

import httpx

from .exceptions import (
    AssistantHubError,
    AuthenticationError,
    NotFoundError,
    ValidationError,
)


class BaseClient:
    """Low-level HTTP client that handles authentication and error mapping.

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
        headers: dict[str, str] = {}
        if api_key is not None:
            headers["Authorization"] = f"Bearer {api_key}"

        self._client = httpx.Client(
            base_url=base_url.rstrip("/"),
            headers=headers,
            timeout=timeout,
        )

    def _request(
        self,
        method: str,
        path: str,
        *,
        params: Optional[dict[str, Any]] = None,
        json: Optional[Any] = None,
        headers: Optional[dict[str, str]] = None,
    ) -> httpx.Response:
        """Send an HTTP request and return the response.

        Raises typed exceptions for common error status codes.
        """
        response = self._client.request(
            method,
            path,
            params=params,
            json=json,
            headers=headers,
        )
        self._raise_for_status(response)
        return response

    @staticmethod
    def _raise_for_status(response: httpx.Response) -> None:
        """Map HTTP error responses to typed SDK exceptions."""
        if response.is_success:
            return

        try:
            body = response.json()
        except Exception:
            body = response.text

        status = response.status_code

        if status == 401:
            raise AuthenticationError(response_body=body)
        if status == 404:
            raise NotFoundError(response_body=body)
        if status == 400:
            raise ValidationError(response_body=body)

        raise AssistantHubError(
            message=f"HTTP {status} error",
            status_code=status,
            response_body=body,
        )

    def close(self) -> None:
        """Close the underlying HTTP client."""
        self._client.close()

    def __enter__(self) -> BaseClient:
        return self

    def __exit__(self, *args: Any) -> None:
        self.close()
