"""Exception types for the AssistantHub SDK."""

from __future__ import annotations

from typing import Any, Optional


class AssistantHubError(Exception):
    """Base exception for all AssistantHub SDK errors."""

    def __init__(
        self,
        message: str,
        status_code: Optional[int] = None,
        response_body: Optional[Any] = None,
    ) -> None:
        super().__init__(message)
        self.status_code = status_code
        self.response_body = response_body


class NotFoundError(AssistantHubError):
    """Raised when a requested resource is not found (HTTP 404)."""

    def __init__(
        self,
        message: str = "Resource not found",
        response_body: Optional[Any] = None,
    ) -> None:
        super().__init__(message, status_code=404, response_body=response_body)


class AuthenticationError(AssistantHubError):
    """Raised when authentication fails (HTTP 401)."""

    def __init__(
        self,
        message: str = "Authentication failed",
        response_body: Optional[Any] = None,
    ) -> None:
        super().__init__(message, status_code=401, response_body=response_body)


class ValidationError(AssistantHubError):
    """Raised when the server rejects a request due to validation errors (HTTP 400)."""

    def __init__(
        self,
        message: str = "Validation error",
        response_body: Optional[Any] = None,
    ) -> None:
        super().__init__(message, status_code=400, response_body=response_body)
