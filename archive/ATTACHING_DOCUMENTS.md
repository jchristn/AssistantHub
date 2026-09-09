# AssistantHub Attached Documents Retrieval Plan

AssistantHub chat already supports assistant-level retrieval filters, request-level metadata filters, retrieval gate classification, query rewrite, RecallDB vector/full-text/hybrid search, reranking, citations, chat history, telemetry, API Explorer, Postman, and SDK parity. Attached documents should fit into that path instead of creating a second chat pipeline.

The goal is direct: a user can pick one or more completed documents from the collection associated with the assistant, ask a question such as "give me a summary of this document", and every RecallDB retrieval query for that turn is constrained to the selected document IDs. Retrieval gate, query rewrite, metadata filters, reranking, citations, history persistence, and performance telemetry still run; document attachment narrows where RecallDB searches, it does not bypass the rest of the chat rail.

## Progress Legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete
- `[!]` Blocked or needs a decision

## Current Findings

- [x] `ChatCompletionRequest` already carries `metadata_filter` and the backend merges it with assistant-level retrieval filters.
- [x] `AssistantChatService` and streaming `ChatHandler` both construct `RetrievalSearchOptions` before calling `RetrievalService.RetrieveAsync(...)`.
- [x] `RetrievalSearchOptions` already carries search mode, full-text settings, neighbor count, and metadata filters.
- [x] `RetrievalService.BuildSearchBody(...)` centralizes the RecallDB search body for vector, full-text, and hybrid modes.
- [x] `RetrievalChunk.DocumentId` maps RecallDB search results back to `AssistantDocument.Id`.
- [x] Ingestion stores chunks in RecallDB with `DocumentId = AssistantDocument.Id`.
- [x] `AssistantDocument` stores `TenantId`, `CollectionId`, `Status`, `OriginalFilename`, `Name`, labels, tags, source URL, and storage metadata needed to validate selectable documents.
- [x] Dashboard chat already has a metadata filter modal and passes request-level filter state into `ApiClient.chat(...)`.
- [x] Dashboard documents and collection search views already have document ID display/search patterns that can be reused.
- [x] Root `SEARCH.md` was moved to `archive/SEARCH.md` as requested.
- [x] Root `FILE_CRAWLERS.md` was moved to `archive/FILE_CRAWLERS.md` as requested.

## Product Decisions

- [x] Decide release version. This work is tracked for `v0.16.0`; active product/package/runtime metadata has been bumped to `0.16.0` / `v0.16.0`.
- [x] Decide whether public chat users may enumerate assistant documents. Decision: yes, but only when `EnableDocumentAttachments` is enabled and only for safe metadata.
- [x] Decide default for the assistant setting. Decision: `EnableDocumentAttachments = false` for existing public assistants, with admins opting in.
- [x] Decide maximum selected document count. Decision: assistant-specific `DocumentAttachmentMaxCount`, default `10`, clamped to `1..100`.
- [x] Decide whether selection is sticky across turns or scoped to a single message. Decision: sticky within the chat panel until the user removes selected documents.
- [x] Decide whether selected documents should be persisted in browser storage by thread. Decision: no for first release; keep selection in memory.
- [x] Decide whether a "summarize the whole document" request needs a separate whole-document summarization mode. Decision: first release constrains standard retrieval; whole-document summary support remains future work.

## User Experience Contract

The chat UI needs to make document scope visible without turning the chat box into a document manager. A user should be able to attach "foo.pdf", see a chip beside the input, send "summarize this document", and trust that retrieval was restricted to "foo.pdf". The selected scope must be obvious enough that a user knows why later answers are still focused on that document.

- [x] Add an attach-documents button to `dashboard/src/components/ChatPanel.jsx`.
- [x] Use a paperclip/document icon button with a tooltip such as "Attach documents".
- [x] Add an `AttachedDocumentsModal` or `DocumentAttachmentModal` under `dashboard/src/components/modals/`.
- [x] Load only documents from the assistant's configured collection.
- [x] Show safe document fields:
  - [x] Display name or original filename.
  - [x] Content type.
  - [x] Size.
  - [x] Status. Public selector returns completed documents only, so row-level status is implicit and the empty state names completed documents.
  - [x] Created/updated time.
  - [x] Optional source URL or crawl source only if the assistant setting allows it.
- [x] Default the list to completed documents only.
- [x] Hide failed, pending, deleted, and documents from other collections.
- [x] Add search by filename/name/source URL where allowed.
- [x] Add pagination or continuation support for large collections.
- [x] Show selected documents as removable chips near the chat input.
- [x] Add a clear-all control when one or more documents are selected.
- [x] Include the selected document count in the chat status bar.
- [x] Disable send only for existing reasons; selected documents do not add a new send-disabled condition.
- [x] Keep selected documents sticky across turns until removed.
- [x] Reset selected documents when the assistant changes or the thread is cleared.
- [x] Do not render hidden summary system messages as attachments.
- [x] Exclude attachment selector UI from the empty landing state when no assistant is loaded.
- [x] Ensure selected chip labels truncate cleanly on mobile.
- [x] Ensure keyboard users can open the modal, select documents, remove chips, and close the modal.

## Public Document Listing API

Document selection requires a safe way for the chat client to discover documents in the assistant's collection. The existing authenticated document admin APIs are too broad for public chat; the selector should receive only the metadata needed to identify a document.

- [x] Add `GET /v1.0/assistants/{assistantId}/documents` as an unauthenticated public assistant route.
- [x] Gate the route by active assistant and new assistant setting `EnableDocumentAttachments`.
- [x] Return `404` for missing/inactive assistants.
- [x] Return `403` or an empty list when document attachments are disabled. Decision: `403` for API clarity; hidden button in UI.
- [x] Require configured assistant settings and `CollectionId`.
- [x] Scope results to `assistant.TenantId`.
- [x] Scope results to `assistantSettings.CollectionId`.
- [x] Include only `DocumentStatusEnum.Completed` by default.
- [x] Support `maxResults`, `continuationToken`, `ordering`, and optional `query` parameters.
- [x] Support optional content type filters if the UI needs them. Implemented `contentType`/`content_type` query filtering with exact MIME and `type/*` matching.
- [x] Apply assistant-level label/tag retrieval filters when possible so the picker does not expose documents that default assistant filters would exclude.
- [x] Do not require `CitationLinkMode = Public`; selection and public download are separate capabilities.
- [x] Do not include S3 keys, bucket names, Verbex tenant/index IDs, credentials, or raw labels/tags unless explicitly needed.
- [x] Response model should use a dedicated safe DTO such as `AssistantDocumentSelectionItem`.
- [x] Include fields:
  - [x] `Id`
  - [x] `Name`
  - [x] `OriginalFilename`
  - [x] `ContentType`
  - [x] `SizeBytes`
  - [x] `SourceUrl` only if allowed
  - [x] `CreatedUtc`
  - [x] `LastUpdateUtc`
- [x] Add an authenticated admin variant only if the public route cannot satisfy dashboard chat needs. Decision: not needed for first release because the public route is gated by assistant settings and returns only safe metadata.

## Chat Request Contract

Attached documents belong in the chat request, because they are a per-turn retrieval constraint. The payload should be simple, stable, and usable from every SDK.

- [x] Add `AttachedDocumentIds` to `ChatCompletionRequest`.
- [x] JSON name: `attached_document_ids`.
- [x] Type: `List<string>`.
- [x] Default: `null` or empty list.
- [x] Add XML documentation explaining that the IDs are `AssistantDocument.Id` values and are used to constrain retrieval.
- [x] Add the same property to SDK request models:
  - [x] C# `ChatCompletionRequest`
  - [x] JavaScript/TypeScript `ChatCompletionRequest`
  - [x] Python `ChatCompletionRequest`
- [x] Add request validation:
  - [x] Remove null/blank IDs.
  - [x] De-duplicate IDs while preserving order.
  - [x] Reject more than the configured maximum selected documents.
  - [x] Reject invalid IDs with `400 Bad Request`.
  - [x] Reject IDs that do not exist.
  - [x] Reject IDs outside the assistant tenant.
  - [x] Reject IDs outside the assistant collection.
  - [x] Reject documents not in `Completed` status.
- [x] Decide whether one invalid ID fails the whole chat request. Decision: fail the request so the user can correct the selection.
- [x] Include the accepted attached document IDs in the chat response retrieval metadata.
- [x] Include selected document names in response metadata if safe and useful for UI/history.

## Backend Chat Pipeline

Document attachments should be resolved once per chat request, then threaded through the existing retrieval path. Retrieval gate and query rewrite should remain part of the request, but they need enough context to make good decisions when the user's text is intentionally vague.

- [x] Add `AttachedDocumentIds` to `AssistantChatExecutionRequest`.
- [x] Pass `chatReq.AttachedDocumentIds` from non-streaming `ChatHandler.PostChatAsync(...)` into `AssistantChatService.ExecuteNonStreamingAsync(...)`.
- [x] Pass `chatReq.AttachedDocumentIds` through the streaming path in `ChatHandler.PostChatAsync(...)`.
- [x] Add a shared helper, for example `ResolveAttachedDocumentsAsync(...)`, in the chat service base or a small dedicated service.
- [x] Resolve attachment IDs before retrieval gate.
- [x] Build a safe list of attachment display names for prompts and telemetry.
- [x] Add attached document context to retrieval gate prompt:
  - [x] Include selected document names.
  - [x] Explain that requests like "this document" or "the attached file" require retrieval.
  - [x] Keep the gate prompt short.
- [x] Add deterministic override logic: if attached documents exist and the latest user message refers to attached/current/selected document(s), set `shouldRetrieve = true` even if the gate result says `SKIP`.
- [x] Add attached document context to query rewrite prompt only when it improves query clarity.
- [x] Keep query rewrite output as query text; do not ask the model to emit document IDs.
- [x] Pass the normalized document ID list into every chat `RetrievalSearchOptions` instance.
- [x] Apply the document filter to:
  - [x] Single-query retrieval.
  - [x] Multi-query query rewrite retrieval.
  - [x] Hybrid fallback to vector-only.
  - [x] Reranking input.
  - [x] Citation source resolution.
- [x] Preserve existing metadata label/tag filters. Document ID filters intersect with metadata filters and do not replace them.
- [x] Log safe attachment metadata, not document contents or secret fields. Chat paths log attachment filter counts and defensive filter events without document contents or storage details.
- [x] Surface a clear error when all selected documents are inaccessible or not in the assistant collection.
- [x] Ensure `generate` remains inference-only and does not accept attachments.
- [x] Ensure manual `/compact` behavior is unaffected; compacted context does not add or remove attached document state.

## Retrieval and RecallDB Contract

RecallDB must receive a document constraint for every search call. The implementation should prefer a native multi-document filter. If RecallDB only supports a single `DocumentId` today, AssistantHub can loop over selected documents and merge results until RecallDB adds `DocumentIds`.

- [!] Verify RecallDB search request support for `DocumentId` and `DocumentIds`. AssistantHub sends `DocumentId`/`DocumentIds` as the primary exact selector and already propagates labels/tags to RecallDB chunk records during ingestion, so tags can be used as a reserved fallback if native document filters are unavailable. Live RecallDB validation outside the local unit/integration harness remains required before deciding whether a RecallDB enhancement is needed.
- [x] If RecallDB supports `DocumentIds`, add `DocumentIds` to the search body when more than one document is selected.
- [x] If RecallDB only supports `DocumentId`, issue one search per selected document and merge/dedupe results. Implemented behind `RecallDb.SupportsMultiDocumentFilter=false`, which preserves the native `DocumentIds` default while enabling a local fallback loop with service-suite coverage.
- [x] Add `DocumentIds` to `RetrievalSearchOptions`.
- [x] Add validation in `RetrievalSearchOptions` or `RetrievalService` for null/blank IDs.
- [x] Extend `RetrievalService.BuildSearchBody(...)` to include document filters.
- [x] Extend the hybrid vector fallback body to include document filters.
- [x] Ensure full-text-only, vector-only, and hybrid modes all apply the same filter.
- [x] Preserve `IncludeNeighbors` behavior. A defensive post-retrieval filter removes neighbor chunks outside selected document scope before prompt/citation use.
- [x] Dedupe merged results by `DocumentId + Position`.
- [x] Keep reciprocal-rank fusion logic stable when multiple rewritten queries and multiple attached documents are both used.
- [x] Add telemetry metadata:
  - [x] `attached_document_count`
  - [x] `attached_document_ids`
  - [x] `document_filter_mode` such as `none`, `single`, `multi-native`, or `multi-loop`
- [x] Add logging around RecallDB request bodies only in redacted debug form. `RetrievalService` now logs a per-search trace ID, RecallDB search path, redacted request-body summary, status, result count, and duration without raw query text, embeddings, document IDs, labels, or tags.
- [x] Add fallback warnings when native multi-document filtering is unavailable. `RetrievalService` logs a warning before using the single-document fallback loop when `RecallDb.SupportsMultiDocumentFilter=false`.

## Whole-Document Summary Behavior

A user asking "summarize this document" expects coverage beyond the best few matching chunks. Standard top-k retrieval may miss sections of a long file, even when the document filter works perfectly. The first release can stay with constrained retrieval, but the plan should keep the whole-document case visible.

- [x] Add tests showing "summarize this document" with one selected document constrains retrieval to that document.
- [x] Decide whether to add a document-summary intent detector in the first release. Decision: do not add a separate summary intent path in this release; attached documents constrain normal RAG retrieval, and exhaustive/whole-document discovery belongs to model-directed tools/future summarization work.
- [x] If adding summary intent: deferred out of first release.
  - [x] Detect summary-like prompts with simple deterministic rules before model inference. Deferred out of first release.
  - [x] When exactly one document is selected, increase `RetrievalTopK` up to a configured summary cap. Deferred out of first release.
  - [x] Keep token budgeting strict; never push every chunk blindly into the prompt. Current implementation preserves strict RAG token budgeting.
  - [x] Consider a RecallDB document chunk enumeration call if native search cannot cover a whole document. Deferred to model-directed collection tools/future summarization work.
  - [x] Consider map-reduce summarization as a later feature for very large documents. Deferred to later feature work.
- [x] If not adding summary intent in the first release, document that attached documents focus retrieval but do not guarantee exhaustive whole-document summarization.

## Chat History, Telemetry, and Analytics

History should make attachment-scoped turns explainable. A later operator viewing a chat request should see which documents constrained retrieval and why no other documents appeared in citations.

- [x] Add `AttachedDocumentIdsJson` to `ChatHistory`.
- [x] Add `AttachedDocumentsJson` if names/content types are needed for display.
- [x] Add DB columns across:
  - [x] SQLite
  - [x] PostgreSQL
  - [x] MySQL
  - [x] SQL Server
- [x] Add table migration/backfill logic for existing deployments.
- [x] Update `ChatHistory.FromDataRow(...)`.
- [x] Update each database implementation's create/read/enumerate SQL.
- [x] Persist attached document metadata for streaming and non-streaming chat.
- [x] Add attachment metadata to `AssistantPerformanceTelemetryBuilder`.
- [x] Add UI display in `HistoryViewModal` and request history linked chat timing. History and request-history detail modals render persisted attached-document metadata from linked chat history.
- [x] Include attachment count in assistant analytics if useful. Decision: include attachment count in assistant analytics.
- [x] Do not store document contents in history.

## Citations and Response Metadata

Citations already map chunks to document IDs. Attached documents should strengthen that contract rather than introduce new citation behavior.

- [x] Ensure every citation source returned for an attached-document turn has a `DocumentId` in the selected set.
- [x] Include selected document metadata in `ChatCompletionRetrieval`, for example:
  - [x] `AttachedDocumentIds`
  - [x] `AttachedDocuments`
  - [x] `DocumentFilterApplied`
- [x] Add SDK fields for the retrieval metadata.
- [x] Add frontend display in the response details or status bar showing that retrieval was constrained.
- [x] Add tests that citation sources do not leak chunks from unselected documents.
- [x] Preserve public document download authorization behavior. Attachment selection does not change public download authorization.

## Frontend Implementation

The UI should reuse existing dashboard patterns: modals, restrained form controls, copyable IDs where needed, and dense but readable tables. The document selector should not introduce a marketing-style layout or a new visual language.

- [x] Add API client method `getAssistantDocuments(serverUrl, assistantId, options)`.
- [x] Extend `ApiClient.chat(...)` signature to accept `attachedDocumentIds`.
- [x] Include `attached_document_ids` in the request body only when the list is non-empty.
- [x] Add state to `ChatPanel`:
  - [x] `attachedDocuments`
  - [x] `showDocumentAttachmentModal`
  - [x] loading/error state for the document selector
- [x] Add modal component:
  - [x] Search input.
  - [x] Paginated table/list.
  - [x] Checkbox selection.
  - [x] Selected count.
  - [x] Apply button.
  - [x] Clear button.
  - [x] Empty state for no completed documents.
  - [x] Disabled state when attachments are not enabled.
- [x] Add selected chips in the input area.
- [x] Add per-chip remove buttons.
- [x] Add a "Clear attached documents" action.
- [x] Pass selected IDs into `ApiClient.chat(...)`.
- [x] Keep attachments out of title generation.
- [x] Keep attachments out of feedback `MessageHistory` unless a new field is added for them.
- [x] Ensure automatic compaction does not drop or mutate attached document state.
- [x] Ensure `/clear` clears attached documents.
- [x] Ensure changing assistant clears attached documents.
- [x] Ensure loaded thread history does not imply old attachments are still selected.
- [x] Add responsive CSS for mobile and desktop.
- [~] Validate no text overlap in compact chat drawer and full chat view. Responsive chip/input/error wrapping CSS is implemented; manual browser viewport QA remains open.

## Assistant Settings UI

Admins need control over whether public chat users can see document names. A document selector can leak document titles even when content download is disabled.

- [x] Add `EnableDocumentAttachments` to `AssistantSettings`.
- [x] Add `DocumentAttachmentMaxCount` if the limit should be assistant-specific.
- [x] Add `ExposeDocumentSourceUrls` only if source URLs are safe enough to show.
- [x] Update assistant settings modal/dashboard form.
- [x] Add help text in admin UI only where existing form conventions allow it.
- [x] Update settings validation and defaults.
- [x] Add DB columns/defaults across supported databases.
- [x] Add tests for defaults and persistence.

## Backend API Surface

Every new public or protected route must appear consistently across the runtime route table, API Explorer, OpenAPI, REST docs, Postman, and SDKs.

- [x] Register `GET /v1.0/assistants/{assistantId}/documents`.
- [x] Add handler method in `ChatHandlerRouteBase` or a focused assistant public handler.
- [x] Add route to `AssistantHubServer`.
- [x] Update `OpenApiDocumentService` route tagging and summaries if needed.
- [x] Ensure `/swagger` shows the document listing route.
- [x] Ensure route is unauthenticated only when intentionally public.
- [x] Add request/response examples to `REST_API.md`.
- [x] Add API Explorer template for the route.
- [x] Add Postman request for the route.
- [x] Add Postman chat request with `attached_document_ids`.
- [x] Update `openapi.json` static/generated artifact.

## SDKs

SDKs should expose the feature without making users manually build raw JSON. The chat request model carries attachments; helper methods discover selectable documents.

### C# SDK

- [x] Add `AttachedDocumentIds` to `ChatCompletionRequest`.
- [x] Add `AssistantDocumentSelectionItem` model.
- [x] Add list assistant documents method.
- [x] Add retrieval metadata fields for attachments.
- [x] Update JSON serialization attributes.
- [x] Update SDK README examples.
- [x] Update `Test.Sdk` coverage.

### JavaScript/TypeScript SDK

- [x] Add `attached_document_ids?: string[]` or `AttachedDocumentIds?: string[]` based on current naming conventions.
- [x] Add document selection item type.
- [x] Add `getAssistantDocuments(...)`.
- [x] Update `chatCompletion(...)` and streaming method helpers if they have typed request overloads.
- [x] Update README examples.
- [x] Update `test_sdk.mjs`.

### Python SDK

- [x] Add `attached_document_ids` to `ChatCompletionRequest` with alias `attached_document_ids`.
- [x] Add document selection item model.
- [x] Add sync and async `get_assistant_documents(...)`.
- [x] Update package exports.
- [x] Update README examples.
- [x] Update `test_sdk.py`.

## MCP

Current MCP docs may keep public chat flows REST-only. The feature still needs explicit MCP documentation so API parity is not ambiguous.

- [x] Decide whether MCP should expose assistant public document listing. Decision: MCP should expose assistant public document lists.
- [x] If MCP remains REST-only for public assistant chat submission:
  - [x] Update `MCP_API.md` to state attached-document chat is REST-only in this release.
  - [x] Include the REST route and request field in the deferred/public assistant section.
- [x] If MCP exposes it:
  - [x] Add MCP tool registration for assistant document listing. `assistant/documents/list` mirrors the public REST/SDK document list contract.
  - [x] Add chat request attachment support if chat is exposed through MCP. Not applicable for the first release because MCP chat submission remains intentionally deferred.
  - [x] Update MCP server SDK/client parity tests. Static API-suite coverage verifies registration, SDK call usage, and MCP docs.
  - [x] Update `docker/assistanthub-mcp/assistanthub-mcp.json` version if release version changes. Version is already `v0.16.0`.

## Postman and API Explorer

API Explorer and Postman should make the feature easy to exercise without guessing field names.

- [x] Perform a thorough Postman parity pass for attached-document workflows:
  - [x] Listing selectable assistant documents.
  - [x] Chat requests with `attached_document_ids`.
  - [x] Optional `contentType` filter example.
  - [x] Error cases for disabled attachments, invalid IDs, cross-collection IDs, and too many selected documents.
  - [~] Environment variables and examples match `REST_API.md`, `openapi.json`, SDK examples, and dashboard request payloads. Route, chat body, `contentType`, and error-case examples are aligned; live Postman execution remains open.
- [x] Add API Explorer route template for `GET /v1.0/assistants/{assistantId}/documents`. Decision: a custom API Explorer page/template is desired rather than relying only on the OpenAPI-rendered route.
- [x] Add chat body template:
  ```json
  {
    "messages": [
      { "role": "user", "content": "Give me a summary of this document." }
    ],
    "attached_document_ids": ["adoc_example"]
  }
  ```
- [x] Add API Explorer docs/description explaining that attachment IDs constrain RecallDB retrieval.
- [x] Add Postman request for listing selectable documents.
- [x] Add Postman request for attached-document chat.
- [x] Keep auth settings consistent with backend route auth.
- [x] Confirm API Explorer, Postman, REST docs, OpenAPI, and backend routes all use identical paths and JSON names. Static API-suite coverage passes for routes, attachment JSON names, OpenAPI schemas, Postman examples, and `contentType`; live Swagger/API Explorer QA is tracked in the validation checklist.

## Documentation

The docs should explain both the user feature and the retrieval mechanics. Avoid overselling it as full-document summarization unless a full-document summarization mode is implemented.

- [x] Perform a thorough documentation parity pass across `README.md`, `CHANGELOG.md`, `REST_API.md`, and `MCP_API.md`:
  - [x] Public assistant document listing route and request field are documented.
  - [x] Attached-document retrieval metadata is documented.
  - [x] Limitations for standard retrieval versus exhaustive whole-document summarization are stated consistently.
  - [~] Postman, OpenAPI, SDK README, API Explorer, and docs examples use identical paths, JSON names, and default values. Static API-suite and Postman JSON validation pass; live API Explorer/browser validation remains open.
  - [x] `archive/SEARCH.md` and `archive/FILE_CRAWLERS.md` are referenced only as archived implementation plans, not current root docs.
- [x] Update `README.md` feature list.
- [x] Update `README.md` chat/RAG explanation.
- [x] Update `CHANGELOG.md`.
- [x] Update `REST_API.md`:
  - [x] Public assistant documents route.
  - [x] `attached_document_ids` request field.
  - [x] Retrieval response metadata.
  - [x] Error responses.
  - [x] Security notes.
- [x] Update `MCP_API.md`.
- [x] Update `TESTING.md`.
- [x] Update SDK READMEs.
- [x] Update `archive/CHAT_DATA_FLOW.md` or add a new current chat data flow doc if the archive should remain historical.
- [x] Keep `archive/SEARCH.md` as the archived copy of the old root search plan.

## Tests

Coverage should prove the constraint reaches RecallDB and that invalid document IDs cannot leak cross-tenant or cross-collection data.

### Model Tests

- [x] `ChatCompletionRequest` serializes and deserializes `attached_document_ids`.
- [x] Empty/null attachment IDs are handled consistently.
- [x] SDK model serialization matches backend JSON.
- [x] Retrieval metadata fields serialize as expected.
- [x] `ChatHistory` attachment JSON fields serialize and hydrate from database rows.

### Service Tests

- [x] Attachment validation accepts completed documents in assistant tenant and collection.
- [x] Attachment validation rejects missing documents.
- [x] Attachment validation rejects documents in another tenant.
- [x] Attachment validation rejects documents in another collection.
- [x] Attachment validation rejects failed/pending documents.
- [x] Attachment validation rejects documents excluded by assistant label/tag filters.
- [x] Retrieval search body includes the document filter.
- [x] Hybrid fallback includes the document filter.
- [x] Multi-query retrieval includes the document filter on every query.
- [x] Performance telemetry includes attachment count and document-filter-applied metadata.
- [x] Multi-document loop/native mode dedupes results.
- [x] Reranking receives only chunks from selected documents. Service-suite coverage now verifies an out-of-scope chunk returned by RecallDB is filtered before the rerank prompt is built.
- [x] Citation sources stay within selected documents.

### API Tests

- [x] Public assistant documents route returns only safe metadata.
- [x] Route is hidden/forbidden when attachments are disabled.
- [x] Route returns only completed docs in assistant collection.
- [x] Chat with one attached document sends filtered RecallDB request.
- [x] Chat with multiple attached documents sends filtered RecallDB request.
- [x] Chat rejects invalid document IDs with clear error text.
- [~] Chat history persists attachment metadata. Backend model, SQL provider, migration, write-path, and SDK contract coverage are complete; live chat API validation remains open.
- [x] OpenAPI route surface includes new fields/routes.

### Frontend Tests

- [x] Selector opens from chat panel.
- [x] Selector lists completed documents.
- [x] Search filters selector results.
- [x] Selection chips render and can be removed.
- [x] Chat request includes selected IDs.
- [x] `/clear` clears selected documents.
- [x] Changing assistant clears selected documents.
- [~] Mobile layout has no overlap. Responsive chip/input/error wrapping CSS is implemented; manual mobile browser QA remains open.
- [~] Chat drawer layout has no overlap. Drawer-specific chip/status wrapping CSS is implemented; manual drawer browser QA remains open.

### SDK and Postman Tests

- [x] C# SDK can list assistant documents.
- [x] C# SDK can submit chat with attachments.
- [x] JS SDK can list assistant documents.
- [x] JS SDK can submit chat with attachments.
- [x] Python SDK can list assistant documents.
- [x] Python SDK can submit chat with attachments.
- [x] C# SDK local contract tests pass with `dotnet run --project sdk/csharp/Test.Sdk/Test.Sdk.csproj -- localonly=true`.
- [x] JS SDK local contract tests pass with `node sdk/js/test_sdk.mjs --local-only`.
- [x] Python SDK local contract tests pass with `python sdk/python/test_sdk.py --local-only`.
- [x] C# SDK parses `ChatHistory` attachment metadata.
- [x] JS SDK parses `ChatHistory` attachment metadata.
- [x] Python SDK parses `ChatHistory` attachment metadata.
- [x] Postman collection request bodies match REST docs.

### Docker and Integration Tests

- [~] Local Docker deployment can upload or crawl two documents into the same assistant collection. Upload/crawler routes and document metadata flow are implemented; local Docker sample-data validation remains.
- [~] Chat without attachments can retrieve either document. Backend retrieval behavior and tests preserve unfiltered chat when no documents are attached; live Docker validation remains.
- [~] Chat with `foo.pdf` attached retrieves only `foo.pdf` chunks. Backend retrieval filters and tests constrain selected-document searches; live Docker validation remains.
- [~] Chat with `bar.pdf` attached retrieves only `bar.pdf` chunks. Backend retrieval filters and tests constrain selected-document searches; live Docker validation remains.
- [x] Logs/telemetry show attachment count and document filter mode.
- [~] Swagger at `http://localhost:8800/swagger` shows the new public document route and chat field. OpenAPI/static API parity validation passes; live Swagger validation remains.

## Security and Privacy

Document selection creates a new metadata exposure path. The selector must never become a public document inventory unless an admin explicitly allows that behavior.

- [x] Add assistant setting to enable/disable document attachments.
- [x] Keep default disabled unless product owner decides otherwise.
- [x] Return only safe metadata in public document listing.
- [x] Validate tenant and collection ownership server-side.
- [x] Do not trust client-provided document names.
- [x] Do not allow arbitrary RecallDB `DocumentId` values to pass through without `AssistantDocument` validation.
- [x] Limit selected document count.
- [x] Rate-limit or rely on existing request controls for document listing. First release relies on existing route controls plus bounded document-list pagination and disabled-by-default attachment settings.
- [x] Redact IDs/names in logs if deployment policy requires it. New retrieval debug logging records counts/lengths and trace metadata rather than raw queries, embeddings, document IDs, labels, tags, or storage identifiers.
- [x] Do not expose S3 keys, bucket names, internal index IDs, or storage paths.
- [x] Document the difference between selecting a document for retrieval and downloading a document.

## Rollout Plan

- [x] Implement backend models and validation first.
- [x] Add RecallDB document filter support in retrieval service.
- [x] Add public document listing endpoint.
- [x] Add tests proving RecallDB request bodies are filtered.
- [x] Add frontend selector and chat request integration.
- [x] Add SDK and Postman updates.
- [x] Update REST, MCP, README, CHANGELOG, TESTING, and OpenAPI.
- [x] Complete the explicit README, CHANGELOG, REST_API, MCP_API, and Postman parity pass requested for release readiness. Static API-suite route/schema/Postman/doc parity passes; live Swagger/API Explorer/browser validation is tracked separately.
- [x] Run full build/test suite.
- [~] Validate in local Docker with two known documents. Static and in-process coverage passes; local Docker sample-data validation remains.
- [~] Verify Swagger/API Explorer parity. OpenAPI/Postman/API-suite parity passes; live Swagger/API Explorer browser validation remains.
- [x] Prepare release notes. `CHANGELOG.md` includes attached-document chat coverage for v0.16.0.

## Acceptance Criteria

- [~] A user can open chat for an assistant and select `foo.pdf` from documents in the assistant's collection. Document picker and request integration are implemented; manual browser validation remains.
- [~] The selected document appears as a removable chip in the chat UI. Chip rendering/removal behavior is implemented; manual browser validation remains.
- [~] Sending "give me a summary of this document" includes `attached_document_ids` in the chat API request. Chat request serialization is implemented and SDK/API contracts are updated; manual browser/network validation remains.
- [x] Retrieval gate and query rewrite still run unless disabled by assistant settings. Service-suite coverage verifies attached-document prompts flow through gate/rewrite helpers and can override a gate skip when the latest prompt references selected documents.
- [x] Every RecallDB search performed for that chat turn is constrained to `foo.pdf`'s `AssistantDocument.Id`. Retrieval-service and chat-orchestration coverage verifies single/multiple document filters are included in RecallDB search bodies.
- [x] Reranking receives only chunks from selected documents. Chat execution filters retrieval chunks by attached document IDs before the rerank stage, and service-suite coverage verifies the rerank prompt excludes out-of-scope chunks.
- [~] Citations point only to selected documents. Retrieval and rerank inputs are constrained to selected document IDs in service coverage; live citation rendering validation remains.
- [x] Invalid, cross-tenant, cross-collection, pending, or failed document IDs are rejected. Service-suite coverage verifies attachment resolution rejects missing, cross-tenant, cross-collection, failed, and disabled/over-limit document selections.
- [x] API Explorer, Postman, REST docs, MCP docs, SDKs, and OpenAPI all describe the same contract. Static route/schema/Postman/API parity passes; live API Explorer and Swagger validation is tracked in the validation checklist.
- [~] Local Docker validation proves selected-document chat does not retrieve from an unselected document in the same collection. Backend filtering coverage is complete; live Docker validation remains.
