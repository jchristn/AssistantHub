"""Enum types for the AssistantHub SDK."""

from enum import Enum


class ApiError(str, Enum):
    """API error codes."""

    AUTHENTICATION_FAILED = "AuthenticationFailed"
    AUTHORIZATION_FAILED = "AuthorizationFailed"
    BAD_REQUEST = "BadRequest"
    NOT_FOUND = "NotFound"
    CONFLICT = "Conflict"
    INTERNAL_ERROR = "InternalError"


class CrawlOperationState(str, Enum):
    """State of a crawl operation."""

    NOT_STARTED = "NotStarted"
    STARTING = "Starting"
    ENUMERATING = "Enumerating"
    RETRIEVING = "Retrieving"
    SUCCESS = "Success"
    FAILED = "Failed"
    STOPPED = "Stopped"
    CANCELED = "Canceled"


class CrawlPlanState(str, Enum):
    """State of a crawl plan."""

    STOPPED = "Stopped"
    RUNNING = "Running"


class DatabaseType(str, Enum):
    """Database types."""

    SQLITE = "Sqlite"
    POSTGRESQL = "Postgresql"
    SQL_SERVER = "SqlServer"
    MYSQL = "Mysql"


class DocumentStatus(str, Enum):
    """Status of a document during ingestion."""

    PENDING = "Pending"
    UPLOADING = "Uploading"
    UPLOADED = "Uploaded"
    TYPE_DETECTING = "TypeDetecting"
    TYPE_DETECTION_SUCCESS = "TypeDetectionSuccess"
    TYPE_DETECTION_FAILED = "TypeDetectionFailed"
    PROCESSING = "Processing"
    STORING_TEXT = "StoringText"
    PROCESSING_CHUNKS = "ProcessingChunks"
    SUMMARIZING = "Summarizing"
    STORING_EMBEDDINGS = "StoringEmbeddings"
    COMPLETED = "Completed"
    FAILED = "Failed"


class EnumerationOrder(str, Enum):
    """Ordering for enumeration queries."""

    CREATED_ASCENDING = "CreatedAscending"
    CREATED_DESCENDING = "CreatedDescending"


class EvalStatus(str, Enum):
    """Status of an evaluation run."""

    PENDING = "Pending"
    RUNNING = "Running"
    COMPLETED = "Completed"
    FAILED = "Failed"


class FeedbackRating(str, Enum):
    """User feedback rating."""

    THUMBS_UP = "ThumbsUp"
    THUMBS_DOWN = "ThumbsDown"


class InferenceProvider(str, Enum):
    """Inference provider types."""

    OPENAI = "OpenAI"
    OLLAMA = "Ollama"
    GEMINI = "Gemini"


class RepositoryType(str, Enum):
    """Repository types for crawling."""

    WEB = "Web"
    CIFS = "CIFS"
    NFS = "NFS"


class NfsVersion(str, Enum):
    """NFS protocol versions."""

    V2 = "V2"
    V3 = "V3"
    V4 = "V4"


class ScheduleInterval(str, Enum):
    """Interval types for crawl scheduling."""

    ONE_TIME = "OneTime"
    MINUTES = "Minutes"
    HOURS = "Hours"
    DAYS = "Days"
    WEEKS = "Weeks"


class SummarizationOrder(str, Enum):
    """Order for summarization processing."""

    BOTTOM_UP = "BottomUp"
    TOP_DOWN = "TopDown"


class WebAuthType(str, Enum):
    """Authentication types for web crawling."""

    NONE = "None"
    BASIC = "Basic"
    API_KEY = "ApiKey"
    BEARER_TOKEN = "BearerToken"
