# AssistantHub Chat Data Flow

This document describes the current public assistant chat path for v0.16.0. The archived historical version remains in `archive/CHAT_DATA_FLOW.md`.

## Scope

The primary chat route is:

```http
POST /v1.0/assistants/{assistantId}/chat
```

The same execution rail is shared by the browser chat experience, SDK chat helpers, and Slack non-streaming execution. `POST /v1.0/assistants/{assistantId}/generate` remains an inference-only helper and does not run the full chat pipeline.

## Browser Request

The browser chat panel builds a `ChatCompletionRequest` with:

- `messages`: prior user and assistant messages plus the current user message
- `metadata_filter`: optional request-level retrieval filter
- `attached_document_ids`: optional selected `AssistantDocument.Id` values

When document attachments are enabled, the browser lists selectable documents with:

```http
GET /v1.0/assistants/{assistantId}/documents
```

That route returns safe metadata only. It does not expose S3 keys, bucket names, storage paths, signed URLs, Verbex internals, or document contents.

## Server Validation

For each chat request, the server resolves:

- Assistant record and tenant
- Assistant settings
- Thread ID, when supplied
- Attached document IDs, when supplied
- Metadata filters, when supplied

Attached document validation happens before retrieval. Every attached document must:

- Belong to the assistant tenant
- Belong to the assistant configured collection
- Have `Completed` status
- Fit within `DocumentAttachmentMaxCount`
- Be selectable only when `EnableDocumentAttachments` is enabled

Blank and duplicate attached document IDs are normalized away. Invalid IDs fail the request before RecallDB is queried.

## Retrieval

If RAG is enabled and retrieval is allowed for the turn, the server searches RecallDB using the assistant collection and configured search mode:

- `Vector`
- `FullText`
- `Hybrid`

Assistant-level retrieval label/tag filters and request-level `metadata_filter` are merged before search.

When `attached_document_ids` is present, every RecallDB search receives the document filter. This applies to:

- Single-query retrieval
- Multi-query retrieval from query rewrite
- Hybrid fallback retrieval

Attached documents narrow retrieval scope. They do not request whole-document summarization and do not grant object-storage access.

## Utility LLM Steps

When enabled in assistant settings, the chat flow may run these utility model calls:

- Retrieval gate: decides whether a new retrieval is needed
- Query rewrite: produces additional retrieval queries
- Reranking: scores retrieved chunks before context injection
- Context compaction: compresses long conversations before final inference

Attached document state is not stored as a chat message and is not injected into title generation or feedback history by default.

## Final Inference

The server builds the final prompt from:

- Assistant system prompt
- Conversation messages
- Retrieved context, when present
- Citation instructions, when enabled

The response may be returned as plain JSON or SSE stream depending on assistant settings and route behavior. Existing SSE status-message handling remains compatible with future tool-progress events. The browser can render safe pending-tool status text, but backend model tool-loop orchestration and emitted tool-progress events are still tracked in `TOOL_CALLS.md`.

## Response Metadata

When retrieval runs, the chat response can include:

- `retrieval.collection_id`
- `retrieval.duration_ms`
- `retrieval.chunks_returned`
- `retrieval.attached_document_ids`
- `retrieval.attached_documents`
- `retrieval.document_filter_applied`
- `retrieval.chunks`

When citations are enabled, citation sources should correspond to retrieved context. For attached-document turns, citation sources are expected to stay inside the selected document set.

## Persistence and Observability

Chat execution records history, request history, and performance telemetry for the normal chat pipeline. Performance telemetry includes stages such as retrieval gate, query rewrite, retrieval, rerank, context compaction, and final inference.

As of v0.16.0, attached-document IDs and safe document display metadata are returned in response retrieval metadata and persisted on `ChatHistory` as `AttachedDocumentIdsJson` and `AttachedDocumentsJson`. History persistence does not store S3 keys, bucket names, signed URLs, or document contents.

## Tool Policy Foundation

Assistant settings include a disabled-by-default `ToolPolicyJson` and parsed `ToolPolicy`. Administrators can validate draft policy and inspect effective tool availability.

Implemented server-side executor support currently covers:

- `collection_search`
- `collection_enumerate_documents`
- `verbex_full_text_search`
- `web_search`

Full provider tool-call orchestration is still tracked in `TOOL_CALLS.md`. Public chat users cannot choose or broaden tool permissions.

The v0.16.0 tool foundation is read-only. It does not expose arbitrary filesystem access, arbitrary HTTP fetch, shell execution, SQL execution, credential reads, or admin management operations as model-callable tools.
