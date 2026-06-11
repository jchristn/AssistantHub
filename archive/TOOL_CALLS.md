# AssistantHub Tool Calls Plan

AssistantHub should support model-directed tool use without turning browser chat into an unbounded automation surface. The model may decide it needs more evidence, a different search strategy, a document range, an indexed full-text hit, an S3 object excerpt, or current web results. The server remains the authority for tenant scope, assistant policy, tool availability, argument validation, output limits, audit logging, and secret redaction.

The end state is an administrator-controlled assistant setting: each assistant can expose a bounded set of server-side tools to the model. Public chat users do not choose tools. The assistant configuration determines what the model may request, and every request is executed by AssistantHub on the server against the assistant's tenant, collection, index, bucket, and web-search policy.

## Progress Legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete
- `[!]` Blocked or needs a product decision

## Source Requirements

- [x] Read repository requirements from `C:\code\agents\requirements`.
- [x] Read document-writing requirements from `C:\code\agents\requirements\WRITING_DOCUMENTS.md`.
- [x] Read backend architecture and test requirements from `C:\code\agents\requirements\BACKEND_ARCHITECTURE.md` and `BACKEND_TEST_ARCHITECTURE.md`.
- [x] Read frontend and i18n requirements from `C:\code\agents\requirements\FRONTEND_ARCHITECTURE.md` and `I18N.md`.
- [x] Read authentication and multi-tenant authorization requirements from `C:\code\agents\requirements\AUTHENTICATION.md`.
- [x] Inspect the Mux Tavily integration under `C:\code\mux\src`.
- [x] Inspect current AssistantHub assistant settings, chat, retrieval, Verbex, S3, SDK, MCP, and dashboard surfaces enough to write this plan.

## Current Findings

- [x] Chat currently uses an OpenAI-compatible request/response model with `messages`, `metadata_filter`, and assistant-managed settings.
- [x] `ChatCompletionRequest` does not currently carry `tools`, `tool_choice`, `tool_calls`, or tool output messages.
- [x] `AssistantChatService` performs pre-generation retrieval, optional retrieval gate, query rewrite, reranking, citations, telemetry, and history persistence.
- [x] Streaming chat has its own path in `ChatHandler`, so tool-call orchestration must be shared before streaming/non-streaming split or factored into a common runtime.
- [x] RecallDB collection search is centralized through `RetrievalService.RetrieveAsync(...)` and `RetrievalSearchOptions`.
- [x] Retrieval supports vector, full-text, and hybrid search modes over RecallDB, plus label/tag metadata filters and neighboring chunks.
- [x] `RetrievalChunk.DocumentId` and `Position` identify the source document and chunk position.
- [x] `AssistantDocument` stores tenant, collection, status, source URL, S3 bucket/key metadata, labels, tags, and Verbex tenant/index/record metadata.
- [x] Verbex is available through `VerbexInvertedIndexService` and existing authenticated index/index-record proxy routes.
- [x] S3-compatible object reads currently exist for full object download; tool-safe partial/ranged text reads are not yet modeled.
- [x] AssistantHub MCP currently exposes management/admin surfaces, not public assistant chat/runtime tool execution.
- [x] Mux has a provider-agnostic web search abstraction, Tavily request/response models, provider fallback, env-var expansion for secrets, and a `web_search` tool schema that AssistantHub can adapt.

## Product Decisions

- [x] Decide release version. This work is tracked for `v0.16.0`; active product/package/runtime metadata has been bumped to `0.16.0` / `v0.16.0`.
- [x] Decide first-release provider support. Decision: support Ollama and OpenAI-compatible tool-capable endpoints first, with explicit capability detection/configuration required for tool calling.
- [x] Decide whether the preferred OpenAI path is Responses API or Chat Completions tool calling. Decision: implement OpenAI-compatible Chat Completions `tools`/`tool_calls` first because that path is the broader shared contract for Ollama and open model endpoints such as gpt-oss, Qwen, DeepSeek, Llama, vLLM, LM Studio, and similar OpenAI-compatible servers.
- [x] Decide whether local Ollama and Gemini endpoints are initially disabled for tool-calling chat unless their configured Partio endpoint advertises compatible tool-call support. Decision: disable tool calling unless endpoint capability is explicit; Gemini remains out of first-release tool-calling scope unless later configured as OpenAI-compatible/tool-capable.
- [x] Decide whether tool-call chat is a new assistant mode or an extension of RAG. Decision: add `EnableToolCalls` as a separate assistant master setting.
- [x] Decide whether RAG and tool calls may run together. Decision: yes. RAG can provide initial context, and tools can let the model expand or verify evidence; admins may also disable RAG and rely on model-directed search/content-discovery tools.
- [x] Decide default for existing assistants. Decision: `EnableToolCalls = false`.
- [x] Decide whether web search requires both global Tavily configuration and per-assistant enablement. Decision: yes. Tavily has system-level endpoint/API-key settings and assistant-level enablement/overrides near `EnableToolCalls`; assistant overrides may supply endpoint/API key, otherwise system-wide configuration is used. If neither system nor assistant configuration is complete, web search must not run and the server must log a warning.
- [x] Decide whether S3 object tools may read any object in the associated bucket or only objects referenced by assistant documents. Decision: document-backed objects only by default, with explicit assistant setting opt-in for bucket-wide reads.
- [x] Decide whether the model may enumerate S3 bucket objects. Decision: yes only when enabled by assistant settings, with prefix allow-lists and page limits.
- [x] Decide whether the model may enumerate Verbex index records. Decision: yes when enabled by assistant settings and scoped to the assistant tenant's mapped Verbex tenant and allowed index IDs.
- [x] Decide whether the model may enumerate RecallDB records. Decision: yes when enabled by assistant settings and scoped to the assistant tenant/collection/index policy.
- [x] Decide whether tool-call details are visible to end users. Decision: public chat users see safe running/completed/failed status and the tool name; admins see full redacted traces in Assistant Request History.
- [x] Decide whether streaming clients should receive tool progress events. Decision: yes, streaming clients receive tool progress events.
- [x] Decide whether failed tool calls should be shown to the model. Decision: yes, return structured non-secret failed tool outputs to the model so it can recover.
- [x] Decide arbitrary URL retrieval behavior. Decision: arbitrary URL retrieval is disabled by default unless an assistant setting explicitly permits ungoverned web access.
- [x] Decide Slack feedback behavior. Decision: Slack messages should be emitted when tool calls are running, completed, and failed.

## Architecture Summary

Model-directed retrieval needs a loop, not a bigger context block. AssistantHub should build a tool list from assistant settings, call the configured model with that list, execute requested tools on the server, append tool outputs to the model conversation, and continue until the model returns a final assistant message or a server limit stops the loop.

The loop should live behind a new shared runtime, not inside the dashboard and not duplicated between streaming and non-streaming chat. The runtime should accept the resolved assistant, settings, request messages, trace/thread metadata, and request origin. It should return the final response, tool trace, telemetry, citations, usage, and any errors using the same chat history path where possible.

## Target Tool Inventory

### `collection_search`

Searches the assistant's configured RecallDB collection. The model may choose vector, full-text, hybrid, or exhaustive search behavior, but the server validates all options and applies assistant-level tenant, collection, label, tag, document, and result limits.

- [x] Tool name: `collection_search`.
- [x] Purpose: let the model run one or more searches against the assistant's collection. Server executor supports this path, and the shared chat loop wires it through non-streaming and tool-enabled streaming chat.
- [x] Require `settings.CollectionId`.
- [x] Require assistant tenant to match collection tenant. AssistantHub does not keep a separate collection metadata row to validate here; executor and RecallDB calls are tenant-scoped to the assistant tenant, and all document-scoped inputs are explicitly validated against assistant tenant, collection, status, and metadata policy.
- [x] Apply assistant `RetrievalLabelFilter` and `RetrievalTagFilter`.
- [x] Accept optional user/request metadata filter only if the chat request already supports it and the server merges it with assistant filters. `ChatCompletionRequest`/`AssistantChatExecutionRequest` carry `MetadataFilter`, the shared service merges it with assistant filters, and collection tool tests cover model-supplied label/tag/source narrowing.
- [x] Validate any model-supplied `document_ids` against the assistant tenant, collection, status, and metadata-filter scope.
- [x] Reject cross-tenant, cross-collection, missing, inactive, failed, pending, or deleted documents.
- [x] Support `query`.
- [x] Support `mode`: `Vector`, `FullText`, `Hybrid`, and `Auto`. The schema exposes all four values; executor normalizes `Auto` to the assistant default unless exhaustive search intentionally fans out.
- [x] Support `strategy`: `single`, `multi_query`, `broad`, `narrow`, `exhaustive`. Executor accepts all strategy names; `single`, `multi_query`, and `exhaustive` have distinct behavior. For v0.16.0, `broad` and `narrow` intentionally resolve to the bounded multi-query path until a deeper product/ranking contract is defined.
- [x] Support `queries` for model-provided multi-query search, capped by assistant policy.
- [x] Support `top_k`, capped by assistant policy. `top_k` is exposed as a schema alias for capped `max_results`.
- [x] Support `score_threshold`, bounded by assistant policy. Executor clamps model-supplied `score_threshold` to 0-1 and also enforces the assistant's configured retrieval threshold.
- [x] Support `include_neighbors`, capped by assistant policy.
- [x] Support `fulltext_search_type`, `fulltext_language`, `fulltext_normalization`, `fulltext_minimum_score`. Executor accepts bounded model-supplied overrides, passes them to RecallDB for FullText/Hybrid modes, returns the effective values in metadata, and has service-suite coverage.
- [x] Support label filters only when they narrow the assistant policy. Model-supplied label filters are merged with assistant metadata filters and service coverage verifies RecallDB request narrowing plus executor post-filtering.
- [x] Support tag filters only when they narrow the assistant policy. Model-supplied tag filters are merged with assistant metadata filters and service coverage verifies RecallDB request narrowing plus executor post-filtering.
- [x] Support `document_ids`.
- [x] Support `source_url_contains` only if safe and available in document metadata. Executor rejects the filter unless `AllowDocumentSourceUrls` is enabled and post-filters against document metadata.
- [x] Support `content_type` filter if document metadata exposes it.
- [x] Return normalized result objects with result ID, document ID, document name, content type, score, text score, fusion score if present, position, excerpt, neighbor availability, labels/tags only if policy allows, and citation handle. Executor returns result ID, document ID/name/content type, score/text score/fusion score, position, excerpt, content, neighbors, policy-gated labels/tags, and citation handle. Provider-native rank internals are not exposed in v0.16.0.
- [x] Return enough metadata for the model to request exact chunks later without re-searching. Executor returns document ID, position, and citation/read handle per result.
- [x] Return excerpts, not entire chunks, unless policy allows full chunk text in search output. `ReturnFullSearchContent` defaults to `false`; `collection_search` emits excerpts plus `ContentOmitted=true`, and only returns full result/neighbor content when admins explicitly enable the switch. Core, SDK, dashboard, REST/OpenAPI/Postman examples, and service-suite coverage are updated.
- [x] Include `more_available` and `next_offset` if paging is supported. Collection search does not support paging yet, so output explicitly returns `MoreAvailable=false` and `NextOffset=null`.
- [x] Record which retrieval mode was used. Each pass records `SearchMode`, and the response includes `SearchedModes`.
- [x] Record whether hybrid fallback ran. `RetrievalSearchOptions.HybridFallbackRan` is set by `RetrievalService`, and `collection_search` aggregates it into `HybridFallbackRan` response metadata with service-suite coverage.
- [x] Record every query executed under the tool call.
- [x] Avoid exposing RecallDB internal request bodies in model-visible output.

### Exhaustive Collection Search Semantics

Exhaustive search should be a deliberate mode, not a synonym for a larger `top_k`. The model needs a way to ask AssistantHub to search broadly across the assigned collection, discover candidate documents, inspect multiple result bands, and then narrow into exact chunks. The server still decides how much work is allowed in one call and one chat turn.

- [x] Define `strategy = exhaustive` for `collection_search`.
- [x] Treat exhaustive search as a server-controlled multi-pass search plan.
- [x] Run lexical/full-text pass when full-text is available.
- [x] Run vector pass when embeddings are available.
- [x] Run hybrid pass when both lexical and vector paths are available.
- [x] Run exact phrase pass for quoted strings or identifiers when present. Collection search extracts quoted phrases and identifier-like tokens, runs bounded full-text exact passes when full-text is allowed, and has service-suite coverage.
- [x] Run query variants supplied by the model only up to `MaxSearchQueriesPerCall`.
- [x] Optionally add server-generated query variants only if assistant policy enables them. `EnableServerGeneratedQueryVariants` defaults to `false`; when enabled, collection search adds deterministic quote/punctuation-normalized variants only after model-supplied queries and still enforces `MaxSearchQueriesPerCall`, with searched/server-generated query metadata and model/service-suite coverage.
- [x] Fan out across selected `document_ids` when the model scopes the search to documents.
- [x] Search the whole assistant collection when no document IDs are supplied.
- [x] Deduplicate by `DocumentId + Position`.
- [x] Merge results with deterministic ranking, preserving available vector/text/rank details. Collection search dedupes document/position results across multi-query and exhaustive passes in deterministic query/mode order and preserves score/text score/fusion score when RecallDB provides them. Raw provider request/rank internals are intentionally not exposed in v0.16.0.
- [x] Return result buckets such as `exact`, `full_text`, `semantic`, `hybrid`, and `low_confidence` when useful. Collection search now adds per-result `ResultBucket` and aggregate `ResultBuckets`, with exact/full-text coverage in the service suite.
- [x] Return `searched_queries`, `searched_modes`, `documents_considered`, and `results_considered` in metadata. Executor returns `SearchedQueries`, `SearchedModes`, `ExactPhraseQueries`, `SearchPasses`, `DocumentsConsidered`, `ResultsConsidered`, `TotalResults`, incomplete reasons, and safe suggested next calls; service-suite coverage verifies visible-document counts exclude failed and out-of-collection documents.
- [x] Return `exhaustive_complete = false` when result limits or output caps stopped the search early, and fail timed-out tool calls instead of returning partial evidence. Executor returns `ExhaustiveComplete=false` with `ExhaustiveIncompleteReasons` for query, document, per-pass result, and results-considered caps; timeout/cancellation stays a failed tool result with stable `ErrorCode=timeout`/`canceled`, with service-suite coverage for exhaustive timeout failure.
- [x] Return `suggested_next_calls` with candidate `collection_read_chunks` positions when the result set is too large. Collection search now returns safe `SuggestedNextCalls` for capped/large result sets with `document_id` and exact `positions`, covered by service-suite assertions.
- [x] Cap exhaustive search by assistant policy: maximum queries, maximum documents, maximum results considered, maximum duration, and maximum output characters. Query count, per-pass result count, `MaxDocumentsConsideredPerSearch`, `MaxResultsConsideredPerSearch`, timeout, output caps, and incomplete metadata for query/document/result/results-considered caps are enforced. Document caps use a paged, ordered assistant-document scope and narrow RecallDB searches to the capped document IDs when needed. Dashboard controls, SDK policy models, REST/OpenAPI/Postman docs, and service-suite coverage are updated.
- [x] Add telemetry for each internal pass instead of treating the whole exhaustive search as opaque. Collection search output now includes `SearchPasses` with query, mode, exact-pass flag, results considered, and results returned for each internal pass.
- [x] Add tests proving exhaustive search covers vector-only, full-text-only, hybrid, and mixed candidate paths. Service-suite coverage verifies full-text, vector, hybrid pass execution, dedupe, result-limit incomplete metadata, and visible `DocumentsConsidered`; broader live provider/result-shape validation remains external.

### `collection_read_chunks`

Reads exact chunk text from the assistant collection by document ID and chunk position. This is the main precision tool after search. The model should search first, then read the exact chunk positions it needs with a bounded neighbor window.

- [x] Tool name: `collection_read_chunks`.
- [x] Purpose: return exact text for known document chunks and neighboring context. Server executor supports this path, and the shared chat loop wires it through non-streaming and tool-enabled streaming chat.
- [x] Require `document_id`.
- [x] Require document tenant to match assistant tenant.
- [x] Require document collection to match assistant `CollectionId`.
- [x] Require document status `Completed`.
- [x] Apply assistant label/tag filters before permitting reads.
- [x] Support `positions` array.
- [x] Support `ranges` array with `start_position` and `count`.
- [x] Support `neighbor_window`, capped by assistant policy.
- [x] Support `max_chunks`, capped by assistant policy.
- [x] Return chunks sorted by document ID then position.
- [x] Return exact chunk text, document metadata, position, and citation handle.
- [x] Return omitted/truncated indicators when output is clipped.
- [x] Do not let the model request arbitrary RecallDB record IDs that were not validated against `AssistantDocument`.
- [x] Add a RecallDB chunk-read route or service method if RecallDB lacks a direct chunk-position lookup.
- [x] If RecallDB cannot read by position, use the AssistantDocument chunk-record ID list to resolve positions deterministically before reading records.
- [x] Ensure neighbor chunks never cross document boundaries.

### `verbex_full_text_search`

Runs full-text search in Verbex using the assistant tenant's mapped Verbex tenant and allowed index IDs. Verbex search should complement RecallDB search because it can expose exact phrase and lexical matches over full extracted text.

- [x] Tool name: `verbex_full_text_search`.
- [x] Purpose: search indexed extracted document text through Verbex.
- [x] Require global Verbex configuration to be valid.
- [x] Resolve AssistantHub tenant to Verbex tenant using tenant tags or default mapping. Verbex executor paths resolve mapped/default index scope, and `VerbexToolService.ResolveAllowedVerbexScopeAsync` resolves `Constants.VerbexTenantIdTag`, default-index tags, policy indices, and visible document indices with service-suite coverage.
- [x] Resolve allowed index IDs from assistant tool policy, assistant settings, document metadata, and tenant defaults. Verbex tools now allow the resolved tenant/default index, assistant policy defaults/allow-lists, and explicit `VerbexIndexId` values on completed assistant documents in scope.
- [x] Default to the assistant's associated/default Verbex index.
- [x] Reject model-supplied tenant IDs.
- [x] Reject model-supplied index IDs outside the allowed list.
- [x] Support `query`.
- [x] Support `index_id` only when in policy.
- [x] Support `record_ids` only when they map to valid assistant documents. Verbex search/enumeration schemas and argument validation allow `record_ids`, and the executor validates each requested record ID against assistant-visible documents, including document IDs, explicit Verbex record IDs, and mapped chunk record IDs before filtering output; service-suite coverage verifies allowed and denied filters.
- [x] Support `max_results`, capped by assistant policy.
- [x] Support exact phrase, boolean, fuzzy, or provider-supported full-text options only after mapping them to Verbex's actual API contract. Executor supports Verbex query, AND logic, required terms, and excluded terms. Fuzzy/provider-specific switches are intentionally not exposed in v0.16.0 without a concrete Verbex API contract.
- [x] Return record ID, mapped AssistantHub document ID, document name, score/rank, excerpts, match positions if available, and citation handle. Executor returns record ID, mapped document metadata, content type, score, excerpt, matched terms, citation handles, available chunk counts, derived chunk positions when record IDs map to chunks, and safe follow-up call hints. Provider-native match positions are not exposed unless Verbex returns a stable contract for them.
- [x] Return enough metadata for a follow-up `collection_read_chunks` or `s3_object_read`. Verbex search/enumeration results now include document ID, available chunk count, derived chunk position when record IDs match collection chunks, citation handles, and safe suggested next-call arguments for chunk reads and document-backed S3 reads without exposing object keys.
- [x] Apply assistant document visibility filters after Verbex returns results.
- [x] Do not expose Verbex tenant IDs or access keys in model-visible output.
- [x] Add a redacted debug log for request path, index, result count, duration, and trace ID. `AssistantToolExecutor` logs Verbex search request shape and response metadata with the chat/request trace ID when available, without raw query text or terms.

### `s3_object_read`

Reads an entire object or a bounded part of an object from the assistant's associated S3 bucket. The tool should be conservative because raw S3 objects may be large, binary, sensitive, or unrelated to the selected assistant.

Current implementation note: `AssistantToolExecutor` supports document-backed reads by `document_id`, bucket-wide reads by `object_key` only when explicitly opted in, bucket object enumeration, storage metadata reads, and ranged object reads through `IObjectStorageService`.

- [x] Tool name: `s3_object_read`.
- [x] Purpose: read a document-backed object or approved bucket object. Document-backed reads are the default; approved bucket object reads require `AllowBucketWideObjectRead=true`, `DocumentBackedObjectsOnly=false`, and an allowed object prefix.
- [x] Require global S3 settings to be valid.
- [x] Default bucket scope to `Settings.S3.BucketName`.
- [x] Support bucket override only when assistant tool policy allows named buckets. Document-backed reads use the document bucket, and bucket-wide reads require the default bucket or a bucket listed in `AllowedBucketNames`.
- [x] Default object scope to objects referenced by `AssistantDocument`.
- [x] Support bucket-wide object reads only when `AllowBucketWideObjectRead = true`. Executor also requires `DocumentBackedObjectsOnly=false` and at least one allowed prefix.
- [x] Require `document_id` or `object_key`.
- [x] If `document_id` is supplied, resolve bucket/key from `AssistantDocument` and ignore any conflicting model-supplied bucket/key.
- [x] Validate tenant, collection, document status, and assistant filters before document-backed reads.
- [x] If `object_key` is supplied, validate bucket, prefix allow-list, suffix/content-type allow-list, and max object size.
- [x] Support `range_start` and `range_length` in bytes.
- [x] Support `text_start` and `text_length` only after decoding text safely.
- [x] Support `content_mode`: `text`, `base64`, `metadata_only`.
- [x] Default to text mode for text-like content and metadata-only for binary content.
- [x] Reject full binary reads unless the assistant policy explicitly allows base64 output.
- [x] Cap returned bytes and characters. Executor caps returned bytes/chars and uses metadata/range reads before downloading object bytes.
- [x] Return object metadata: bucket, key redacted if needed, document ID, content type, size, ETag if available, range returned, truncation flag, and citation handle.
- [x] Add `IObjectStorageService` methods for metadata, list, and ranged reads if the current abstraction cannot support them. Added safe metadata, listing, and range-read methods plus concrete S3 and test-fake implementations.
- [x] Avoid loading very large objects fully into memory when only a range is requested. Document-backed reads now use metadata plus `DownloadRangeAsync` and enforce policy caps before reading object bytes.
- [x] Return a clear model-visible error when content is binary, missing, or not allowed.

### `collection_enumerate_documents`

Enumerates documents in the assistant collection using safe metadata. The model can use this to discover candidate files before searching or reading.

- [x] Tool name: `collection_enumerate_documents`.
- [x] Purpose: list documents available to the assistant.
- [x] Require assistant `CollectionId`.
- [x] Scope by assistant tenant and collection.
- [x] Include only `Completed` documents by default.
- [x] Support optional status filter only when policy permits non-completed metadata visibility.
- [x] Apply assistant label/tag filters where possible.
- [x] Support `query` over document name, original filename, source URL if allowed, and content type.
- [x] Support `content_type`.
- [x] Support `labels` and `tags` only as narrowing filters.
- [x] Support pagination with `max_results` and `continuation_token`.
- [x] Return safe fields: document ID, name, original filename, content type, size, source URL if allowed, created UTC, last update UTC, labels/tags if allowed, collection ID, and availability flags.
- [x] Do not return S3 secret fields, raw bucket credentials, internal storage provider configuration, or processing logs.

### `index_enumerate_records`

Enumerates documents or records in the associated Verbex index. The model can inspect what is indexed before issuing a full-text query.

- [x] Tool name: `index_enumerate_records`.
- [x] Purpose: list index records in allowed Verbex indexes. Server executor supports this path, and the shared chat loop wires it through non-streaming and tool-enabled streaming chat.
- [x] Resolve allowed index IDs from assistant policy.
- [x] Default to assistant tenant default Verbex index.
- [x] Reject model-supplied tenant IDs.
- [x] Validate records against `AssistantDocument` metadata before returning them.
- [x] Support `index_id`, `query`, `record_id_prefix`, `max_results`, and `continuation_token`.
- [x] Return record ID, mapped document ID, document name, content type, index ID, last indexed metadata if available, and safe excerpts only if policy permits.
- [x] Hide records that cannot be mapped back to an allowed assistant document unless policy explicitly permits raw index enumeration.

### `bucket_enumerate_objects`

Enumerates S3 bucket objects in the associated bucket or approved buckets. Bucket enumeration has a higher leakage risk than document enumeration, so the default should be off.

- [x] Tool name: `bucket_enumerate_objects`.
- [x] Purpose: list bucket objects available under assistant policy.
- [x] Require `EnableBucketEnumerateObjectsTool`.
- [x] Default bucket to `Settings.S3.BucketName`.
- [x] Support explicit bucket name only when listed in assistant policy.
- [x] Require prefix allow-list when bucket-wide enumeration is enabled. Executor rejects enumeration unless `AllowedBucketPrefixes` is configured and the requested prefix is inside the allow-list.
- [x] Support `prefix`, `suffix`, `content_type`, `max_results`, and `continuation_token`.
- [x] Return key, bucket, size, content type if available, last modified UTC, ETag if safe, document ID if mapped, and `read_allowed`.
- [x] Redact or hash object keys if policy says object paths are sensitive. First-release executor redacts object keys by default using the existing safe key display.
- [x] Never return S3 access key, secret key, endpoint auth material, or signed URLs.
- [x] Enforce page size and total-result caps. Executor caps page size to assistant `MaxSearchResultsPerCall`, and storage caps native list requests to 1000 objects.

### `web_search`

Runs public web search through Tavily. Tavily must be configured in server JSON, and the assistant must explicitly enable the web search tool.

- [x] Tool name: `web_search`.
- [x] Purpose: search the public web for current or external information.
- [x] Require `Settings.ExternalSearch.Enabled = true`.
- [x] Require at least one enabled Tavily provider.
- [x] Require assistant `EnableWebSearchTool = true`.
- [x] Support `query`.
- [x] Support `max_results`, capped by global and assistant policy.
- [x] Support `search_depth`: `basic` or `advanced`.
- [x] Support `topic`: `general` or `news`.
- [x] Support `time_range`, `start_date`, `end_date`.
- [x] Support `include_answer`. Executor accepts true/false/basic/advanced and passes the normalized option to Tavily.
- [x] Support `include_raw_content` only when assistant policy allows raw web content.
- [x] Support `include_images` only when assistant policy allows images.
- [x] Support `include_domains` and `exclude_domains`, intersected with global and assistant domain policy.
- [x] Support `country`.
- [x] Support `safe_search`, defaulting to the stricter of global and assistant policy. Executor forces safe search when global settings or assistant policy require it and otherwise accepts model-requested safe search.
- [x] Return provider, query, answer, request ID, latency, result title, URL, snippet, score, published timestamp, favicon, and raw content only if allowed.
- [x] Return Tavily usage/credits in telemetry, not necessarily to the model. `web_search` captures Tavily credits and provider latency into tool traces, persisted summaries, and aggregate `tools` telemetry metadata, with service-suite and SDK contract coverage.
- [x] Add structured errors for disabled search, missing provider, provider timeout, provider HTTP error, and policy denial. Executor returns safe success/denied/error fields with stable `ErrorCode` values (`invalid_arguments`, `unknown_tool`, `tool_unavailable`, `policy_denial`, `provider_missing`, `provider_http_error`, `timeout`, `canceled`, and `tool_error`), model-visible tool outputs include the code, persisted admin summaries retain it, and service-suite coverage verifies invalid arguments, unknown tools, web-search limits, and timeouts.
- [x] Do not add arbitrary URL retrieval in the first release unless a separate `web_retrieve` tool is explicitly approved. Decision: arbitrary URL retrieval remains disabled by default and requires an explicit assistant setting for ungoverned web access.

## Assistant Tool Policy

The assistant setting should be expressive enough for administrators but stable enough for SDKs and migrations. A single typed policy object stored as JSON is the most practical shape because per-tool knobs will grow. The API should expose a structured object, not ask dashboard and SDK callers to hand-edit raw JSON.

- [x] Add `AssistantToolPolicy` model under `src/AssistantHub.Core/Models/`.
- [x] Store the policy in a new `assistant_settings.tool_policy_json` column.
- [x] Expose a typed `ToolPolicy` property in assistant settings API responses.
- [x] Preserve backward compatibility by treating missing or null policy as disabled.
- [x] Add validation that normalizes null nested policies to disabled defaults. First release uses the flat `AssistantToolPolicy` object by design; missing/null `ToolPolicy` and null lists normalize to disabled/safe defaults, while nested policy models are explicitly deferred.
- [x] Add XML docs on every public model, property, constructor, and method.
- [x] Keep each model class or enum in its own file.
- [x] Avoid tuples in new code.
- [x] Use explicit types instead of `var`.

### Top-Level Policy Fields

- [x] `EnableToolCalls`: master toggle, default `false`.
- [x] `ToolChoiceMode`: `Auto`, `Required`, `None`, or `AllowedOnly`, default `Auto` when enabled. Server policy normalizes the value; non-streaming chat maps `Auto`/`Required`/`None` to provider tool choice and service-suite coverage verifies `None` preserves standard inference.
- [x] `MaxToolIterations`: default `6`, min `1`, max `20`.
- [x] `MaxToolCallsPerTurn`: default `12`, min `1`, max `50`.
- [x] `MaxParallelToolCalls`: default `1` for first release. Policy normalizes this to `1` while `AllowParallelToolCalls` is false; runtime remains sequential for first release.
- [x] `AllowParallelToolCalls`: default `false`. Policy normalization keeps first-release execution sequential unless explicitly changed in future runtime work.
- [x] `ToolCallTimeoutMs`: default `30000`, min `1000`, max `300000`.
- [x] `MaxToolOutputChars`: default `12000`, min `1024`, max `200000`.
- [x] `EnableSlackToolProgressMessages`: default `true`.
- [x] `MaxToolOutputCharactersPerTurn`: default `50000`. Non-streaming chat enforces the aggregate model-visible tool-output budget, truncates the current tool output when the budget is reached, stops additional tool execution, and asks for a best-effort final answer from available evidence.
- [x] `MaxToolResultItems`: default `20`. Policy field and normalization are implemented and applied to collection search, collection chunk reads, collection document enumeration, Verbex search/enumeration, bucket enumeration, and Tavily web search with service-suite coverage.
- [x] `ExposeToolTraceToUser`: default `false`. Policy field is implemented and public chat continues to omit raw tool traces by default.
- [x] `PersistToolArguments`: default `true` with redaction. Non-streaming trace persistence redacts arguments by default and writes a suppressed marker when policy sets `PersistToolArguments = false`.
- [x] `PersistToolOutputs`: default `false` or metadata-only for first release. Non-streaming trace persistence stores summaries by default and stores redacted full outputs only when policy sets `PersistToolOutputs = true`.
- [x] `RequireCitationsForToolEvidence`: default `true`. Successful tool outputs are annotated with `CitationIndex`/`CitationReference` when assistant citations are enabled, and tool-derived document/web sources are merged into response citation metadata with service-suite coverage.
- [x] `AllowedToolNames`: normalized names generated from enabled nested policies. Resolver enforces the final allow-list and service-suite coverage verifies it narrows effective tools.

### Collection Tool Policy Fields

- [x] `EnableCollectionSearchTool`.
- [x] `EnableCollectionReadChunksTool`.
- [x] `EnableCollectionEnumerationTool`. Alias is implemented and normalized into `EnableCollectionEnumerateDocumentsTool`.
- [x] `AllowedSearchModes`: default `["Vector", "FullText", "Hybrid"]`. Registry and executor enforce allowed modes, with service coverage for disallowed mode rejection.
- [x] `DefaultSearchMode`: default from assistant `SearchMode`. Executor uses the policy override when present and falls back to the first allowed mode if the assistant default is not allowed.
- [x] `MaxSearchQueriesPerCall`: default `3`.
- [x] `EnableServerGeneratedQueryVariants`: default `false`. Collection search only adds deterministic server-generated query variants when this policy field is enabled, and dashboard/SDK/API documentation expose the flag.
- [x] `MaxSearchTopK`: default from assistant `RetrievalTopK`, capped at `50`. Policy cap is implemented and collection search/schema use the stricter of `MaxSearchResultsPerCall` and `MaxSearchTopK`.
- [x] `MaxChunksPerRead`: default `20`.
- [x] `MaxReadRangesPerCall`: default `5`. Collection chunk reads reject requests with too many ranges before RecallDB reads.
- [x] `MaxNeighborWindow`: default from assistant `RetrievalIncludeNeighbors`, capped at `10`.
- [x] `AllowModelDocumentIdFilter`: default `true`. Executor rejects model-supplied document ID filters when disabled by assistant policy.
- [x] `ReturnLabels`: default `false`. Collection search and enumeration return labels only when `ReturnLabels` or `AllowDocumentMetadataDetails` is enabled.
- [x] `ReturnTags`: default `false`. Collection search and enumeration return tags only when `ReturnTags` or `AllowDocumentMetadataDetails` is enabled.
- [x] `ReturnFullSearchContent`: default `false`. Collection search returns excerpts by default and only includes full result/neighbor content when this explicit policy switch is enabled.
- [x] `ReturnSourceUrl`: default `false`.
- [x] `AllowNonCompletedDocumentMetadata`: default `false`. Collection enumeration remains completed-only by default and accepts `status` only when the policy enables non-completed metadata visibility.

### Verbex Tool Policy Fields

- [x] `EnableVerbexSearchTool`. Alias is implemented and normalized into `EnableVerbexFullTextSearchTool`.
- [x] `EnableIndexEnumerationTool`. Alias is implemented and normalized into `EnableIndexEnumerateRecordsTool`.
- [x] `AllowedVerbexIndexIds`.
- [x] `DefaultIndexId`. Policy override is implemented for Verbex search and record enumeration.
- [x] `MaxVerbexResults`: default `20`. Verbex search and record enumeration cap results by the stricter policy value.
- [x] `AllowRawIndexRecords`: default `false`. Policy field and normalization are implemented; raw record output remains intentionally disabled in v0.16.0.
- [x] `RequireDocumentMapping`: default `true`. Policy field is implemented and current Verbex tools require assistant document mapping; allowing unmapped records is intentionally unsupported in v0.16.0.
- [x] `ReturnVerbexRecordMetadata`: default `false`. Policy field and normalization are implemented; model-visible record metadata remains intentionally disabled in v0.16.0.

### S3 Tool Policy Fields

- [x] `EnableS3ObjectReadTool`.
- [x] `EnableBucketEnumerateObjectsTool`.
- [x] `AllowedBucketNames`.
- [x] `AllowedBucketPrefixes`.
- [x] `AllowedObjectSuffixes`. S3 object reads and bucket enumeration enforce suffix allow-lists with service-suite coverage.
- [x] `AllowedContentTypes`. S3 object reads and bucket enumeration enforce content-type allow-lists with service-suite coverage.
- [x] `DocumentBackedObjectsOnly`: default `true`. Normalization keeps `AllowBucketWideObjectRead` false while document-backed-only mode is enabled.
- [x] `AllowBucketWideObjectRead`: default `false`. Bucket-wide object-key reads require this to be true and `DocumentBackedObjectsOnly` to be false.
- [x] `AllowBinaryObjectOutput`: default `false`.
- [x] `MaxObjectReadBytes`: default `131072`, max `10485760`.
- [x] `MaxObjectBytesPerTurn`: default `524288`. Policy field, normalization, executor byte telemetry, and shared chat-loop aggregate model-visible S3 byte enforcement are implemented with service-suite coverage.
- [x] `MaxBucketEnumerationResults`: default `50`. Bucket enumeration schema and executor cap results by the stricter S3 enumeration policy.
- [x] `RedactObjectKeys`: executor redacts returned object keys by default and now honors the explicit policy field; service-suite coverage verifies redacted and unredacted paths.

### Web Search Tool Policy Fields

- [x] `EnableWebSearchTool`.
- [x] `AllowedProviders`: default empty means global default Tavily provider. Executor enforces provider allow-lists before outbound web-search calls.
- [x] `TavilyEndpoint`: assistant-level Tavily endpoint override; null/blank uses system-wide configuration.
- [x] `TavilyApiKey`: assistant-level Tavily API key override; null/blank uses system-wide configuration.
- [x] `MaxWebResults`: default `5`, max `20`. Tavily request building caps results by assistant and global policy.
- [x] `SearchDepth`: default `basic`. Policy default and request normalization are implemented.
- [x] `AllowAdvancedSearchDepth`: default `false`. Tavily requests downgrade `advanced` to `basic` unless policy permits advanced depth.
- [x] `AllowNewsTopic`: default `true`. Tavily requests downgrade `news` to `general` when policy disables news.
- [x] `AllowRawWebContent`: default `false`.
- [x] `AllowImages`: default `false`.
- [x] `AllowUngovernedWebAccess`: default `false`, reserved for explicit future arbitrary URL retrieval.
- [x] `IncludeDomains`.
- [x] `ExcludeDomains`.
- [x] `RequireSafeSearch`: default `true`. Tavily request building forces safe search when assistant policy requires it.
- [x] `MaxWebSearchesPerTurn`: default `3`. Non-streaming chat counts web-search tool calls per turn and returns a model-visible denied tool output after the limit is reached.

## Server JSON Configuration

Tavily is a server-level capability. Assistant policy can expose it, but the server JSON owns provider credentials, endpoints, default safety behavior, timeouts, and provider availability.

- [x] Add `ExternalSearchSettings` under `src/AssistantHub.Core/Settings/`.
- [x] Add `ExternalSearchProviderSettings` under `src/AssistantHub.Core/Settings/`.
- [x] Add `ExternalSearch` property to `AssistantHubSettings`.
- [x] Add defaults that leave external search disabled.
- [x] Add settings validation in `AssistantHubServer.ValidateSettings(...)`.
- [x] Expand environment variables in provider endpoint/API key values, matching the Mux pattern.
- [x] Redact provider API keys in configuration responses.
- [x] Redact provider API keys in logs and request history.
- [x] Add global defaults for `MaxResults`, `TimeoutMs`, `SafeSearch`, `AllowRawContent`, and domain allow/deny lists.
- [x] Support only Tavily in the first release unless another provider is explicitly added later.
- [x] Add assistant-level Tavily endpoint/API-key override semantics with system-wide fallback and warning on incomplete configuration.
- [x] Add a startup log line saying external search is disabled, enabled with N providers, or misconfigured, without printing secrets.

Recommended server JSON shape:

```json
{
  "ExternalSearch": {
    "Enabled": false,
    "AllowFallback": false,
    "MaxResults": 10,
    "TimeoutMs": 30000,
    "SafeSearch": true,
    "AllowRawContent": false,
    "IncludeDomains": [],
    "ExcludeDomains": [],
    "Providers": [
      {
        "Name": "tavily-primary",
        "ProviderType": "tavily",
        "Endpoint": "https://api.tavily.com/search",
        "ApiKey": "${TAVILY_API_KEY}",
        "Enabled": true,
        "IsDefault": true,
        "TimeoutMs": 60000
      }
    ]
  }
}
```

- [x] Add this example to `README.md`.
- [x] Add this example to `REST_API.md` configuration docs.
- [x] Add this example to `docker/factory/assistanthub.json` with disabled defaults and env-var placeholder if appropriate.
- [x] Add `TAVILY_API_KEY` to Docker `.env` examples only as a placeholder.
- [x] Keep real keys out of committed files.

## Inference Provider and API Surface

Tool calling is an inference capability, not just a prompt. AssistantHub needs a provider abstraction that can represent tool definitions, tool calls, tool outputs, and final messages.

- [x] Add a provider-neutral `ToolCapableInferenceRequest`.
- [x] Add a provider-neutral `ToolCapableInferenceResponse`.
- [x] Add `AssistantModelToolDefinition`.
- [x] Add `AssistantModelToolCall`.
- [x] Add `AssistantModelToolOutput`.
- [x] Add `AssistantModelMessageItem` or equivalent if Responses API item shape is needed. Not needed for first release because the supported provider path uses OpenAI-compatible Chat Completions and Ollama chat tool-call message shapes rather than Responses API item arrays.
- [x] Add `FinishReason` values for `stop`, `length`, `tool_calls`, `error`, and provider-native variants. `InferenceResult.FinishReason` carries provider finish reasons as strings, supplies `tool_calls` when tool calls are present, and uses `error` for failures with mocked provider coverage.
- [x] Add usage tracking for prompt, completion, reasoning, tool definition, and total tokens when provider returns them. OpenAI-compatible `usage` parsing now preserves prompt/completion/total counters plus optional reasoning and tool-definition token details, maps them into normalized assistant telemetry, and is mirrored in C#/TypeScript/Python SDK models with model/service/SDK contract coverage.
- [x] Add endpoint capability metadata: `SupportsToolCalling`, `ToolCallingApiFormat`, `SupportsParallelToolCalls`, `SupportsStreamingToolCalls`. Shared models, SDKs, dashboard form, and server endpoint resolution now carry these fields; AssistantHub persists them to Partio completion endpoint labels/tags using reserved AssistantHub metadata keys.
- [x] Add admin UI and API exposure for endpoint tool-calling capabilities using Partio endpoint labels/tags. AssistantHub proxy DTOs and dashboard form expose the friendly fields while the proxy writes/reads reserved Partio tags, preserving operator labels/tags.
- [x] For OpenAI provider, defer Responses API unless a later endpoint configuration explicitly selects it. First release uses explicit OpenAI-compatible chat-completions tool calling for broad open-model endpoint compatibility.
- [x] For Ollama and OpenAI-compatible chat endpoints, implement model tool-call wire formats only when the endpoint is configured as compatible/tool-capable. OpenAI-compatible and Ollama `tools` request/`tool_calls` response handling is implemented and tested; live provider validation is tracked in the QA checklist.
- [x] For unsupported providers, fail fast with a clear assistant configuration error when `EnableToolCalls = true`. Tool-capable inference returns a clear unsupported-provider error and chat rejects endpoints without explicit capability; live provider validation is tracked in the QA checklist.
- [x] Do not silently downgrade tool calls to plain text prompting. Tool-capable inference does not downgrade, and chat fails configuration errors before final inference; live provider validation is tracked in the QA checklist.
- [x] Add provider tests with mocked HTTP responses for one tool call, multiple tool calls, no tool calls, malformed tool call arguments, tool-call final answer, and provider error. Service-suite coverage now includes OpenAI-compatible one-tool, multi-tool, direct final answer, malformed argument string handoff, provider HTTP error, and Ollama object-argument parsing paths.

## Tool Runtime

The runtime is the core of the feature. It should be deterministic, small enough to test, and independent of the dashboard.

- [x] Add `AssistantToolRegistry`.
- [x] Add `IAssistantToolExecutor`.
- [x] Add `AssistantToolExecutionContext`.
- [x] Add `AssistantToolExecutionRequest`.
- [x] Add `AssistantToolExecutionResult`.
- [x] Add `AssistantToolPolicyResolver`.
- [x] Add `AssistantToolDescriptor` for effective tool availability.
- [x] Add `AssistantToolArgumentValidator`.
- [x] Add `AssistantToolOutputLimiter`.
- [x] Add `AssistantToolAuditWriter`.
- [x] Add built-in tool executors for every target tool. `collection_search`, `collection_read_chunks`, `collection_enumerate_documents`, `verbex_full_text_search`, `index_enumerate_records`, document-backed and explicitly opted-in bucket-wide `s3_object_read`, `bucket_enumerate_objects`, and `web_search` are implemented in `AssistantToolExecutor`.
- [x] Build the tool registry from assistant settings and server capabilities on each chat turn. Non-streaming and tool-enabled streaming chat build policy-filtered schemas through the shared service.
- [x] Exclude disabled tools from model-visible definitions.
- [x] Exclude planned-but-unimplemented tools from model-visible definitions.
- [x] Enforce the same policy again at execution time even if a model asks for a disabled tool.
- [x] Validate JSON arguments with strongly typed request DTOs. `AssistantToolArgumentValidator` now deserializes each canonical tool argument object through focused DTOs before dispatch, with flexible converters for numeric strings, boolean strings, scalar-or-array string lists, integer lists, and tag-filter object/array forms; service-suite coverage verifies malformed scalar, list, and chunk-range payloads are rejected as `invalid_arguments`.
- [x] Reject unknown properties unless a tool explicitly allows provider-specific pass-through. Model-facing schemas set `additionalProperties: false`, and `AssistantToolArgumentValidator` rejects unknown executor-side properties with service-suite coverage.
- [x] Normalize and de-duplicate IDs, prefixes, domains, tags, labels, and positions. Tool argument helpers normalize case-insensitive property names, queries, document IDs, labels, tag conditions, domains, and positions/ranges, and typed DTO validation rejects malformed values before dispatch.
- [x] Attach trace ID, assistant ID, tenant ID, thread ID, origin, and request history ID to every tool execution. Shared tool-loop records persist these fields and link chat history by trace ID for non-streaming and tool-enabled streaming chat.
- [x] Measure duration per tool.
- [x] Capture result count, byte count, character count, truncation, and policy-denial status. Runtime captures result counts in response traces/progress, redacted input/output byte counts, output characters, truncation, and denial with service-suite coverage.
- [x] Return model-visible output as compact JSON.
- [x] Return admin-visible output as redacted structured telemetry. Persisted non-streaming records store redacted arguments and output summaries, and dashboard history/request detail panels render those redacted records.
- [x] Never return secrets to the model. Non-streaming model-visible tool JSON is redacted for common secret/token/key/password/credential field names before it is sent back to the model.
- [x] Never return raw credentials, bearer tokens, access keys, API keys, signed URLs, or internal connection strings. The same redactor covers common credential-shaped fields in model-visible output and persisted full-output traces; broader semantic leakage remains a manual/security-review concern.
- [x] Add cancellation checks before and after outbound service calls. Executor now passes linked cancellation tokens through outbound services and adds explicit checkpoints around retrieval, Verbex, S3/storage, enumeration, and Tavily calls.
- [x] Time out long-running tool calls.
- [x] Stop execution when turn-level output limits are reached. Non-streaming chat truncates the current output, skips further tool execution, and asks the model for a best-effort final answer from available evidence.
- [x] Stop execution when iteration limit is reached and ask the model for a best-effort answer from available evidence. The shared loop does this with focused service coverage; live streaming validation remains open.
- [x] Fail the chat request only for configuration errors that make tool-call chat impossible. Endpoint capability enforcement is implemented and tested; live streaming validation remains open.
- [x] Let recoverable tool errors flow back to the model as structured tool outputs.

## Chat Orchestration

The orchestration loop should work for streaming and non-streaming chat. It should preserve existing RAG behavior when tool calls are disabled and add a model-directed path when they are enabled.

- [x] Add `AssistantChatToolOrchestrator` or equivalent shared service. `AssistantChatService` is the shared non-streaming/tool-enabled streaming execution service.
- [x] Accept `AssistantChatExecutionRequest`. The shared service is used for non-streaming and tool-enabled streaming chat.
- [x] Resolve assistant and settings once. The HTTP handler passes preloaded assistant/settings into the shared service for non-streaming and tool-enabled streaming chat.
- [x] Build effective metadata filters once. The shared service builds effective filters before RAG/tool execution for non-streaming and tool-enabled streaming chat.
- [x] Resolve assistant tool policy once for non-streaming chat.
- [x] Resolve model endpoint once for non-streaming chat, including tool capability metadata.
- [x] Build initial system/developer instructions for tool behavior. Non-streaming chat adds a short tool behavior system message only when tool calls are active.
- [x] Include only safe tool descriptions and JSON schemas in non-streaming chat.
- [x] Optionally run existing pre-retrieval RAG before the first model call when `EnableRag = true`. The shared service preserves this behavior for non-streaming and tool-enabled streaming chat.
- [x] Preserve retrieval gate, query rewrite, reranking, citations, context trimming, compaction, history, and telemetry when pre-retrieval RAG is active. Shared service preserves these paths for non-streaming and tool-enabled streaming chat; live SSE/browser validation is tracked in the QA checklist.
- [x] Make the model aware that collection tools can retrieve additional evidence if initial context is insufficient. Tool behavior instructions explicitly tell the model to prefer collection tools for assigned documents when current context is insufficient.
- [x] Call the model. Tool-capable calls are implemented and tested through the shared service; live provider validation is tracked in the QA checklist.
- [x] If the model returns final content, finish. Shared service handling is implemented and tested.
- [x] If the model returns tool calls, validate and execute them. Shared service execution and strongly typed per-tool argument DTO validation are implemented and tested.
- [x] Append tool outputs in provider-native format. Tool-role messages are implemented and tested through the shared service.
- [x] Repeat until final content, provider error, cancellation, or server limit. Shared service loop and limit-specific service coverage are implemented.
- [x] Produce final `ChatCompletionResponse` compatible with existing clients. Non-streaming response compatibility and tool-enabled streaming final chunks are implemented; live SSE/browser validation is tracked in the QA checklist.
- [x] Add optional `tool_calls` or `tools` extension metadata to `ChatCompletionResponse`. `tool_calls` uses safe `ChatCompletionToolTrace` metadata when `ExposeToolTraceToUser` is enabled.
- [x] Add tool trace metadata to `ChatCompletionRetrieval` only if it is retrieval-specific; otherwise add a separate `ChatCompletionToolTrace`. Tool traces use a separate `ChatCompletionToolTrace` response extension.
- [x] Keep `POST /v1.0/assistants/{assistantId}/generate` inference-only unless explicitly changed later. Tool orchestration is only wired through assistant chat.
- [x] Keep `/compact` behavior unaffected.
- [x] Ensure Slack assistant traffic can use the same tool runtime when enabled for that assistant. Slack constructs the shared non-streaming service with storage support and policy-controlled safe progress messages for started/completed/failed/denied tool calls; live Slack workspace validation is tracked in the QA checklist.
- [x] Add a Slack-safe setting decision for tool progress/errors. Decision: Slack emits messages when tool calls are running, completed, and failed.

## Streaming Behavior

Streaming needs a careful compatibility contract because existing clients expect OpenAI-style SSE content chunks.

- [x] Preserve existing `chat.completion.chunk` content deltas for final assistant text.
- [x] Add optional SSE extension events for tool progress: `assistant.tool_call.started`, `assistant.tool_call.heartbeat`, `assistant.tool_call.completed`, `assistant.tool_call.failed`, and `assistant.tool_call.denied`. `AssistantToolProgressEvent` is emitted by the shared tool loop and written as named SSE events for tool-enabled streaming chat.
- [x] Add optional SSE extension event for iteration-level progress: `assistant.tool_iteration.started`.
- [x] Include extension events only for clients that opt in with a request header or assistant setting unless existing dashboard chat can safely consume them. Dashboard chat safely consumes named tool events, and backend emission is controlled by assistant policy `EnableToolFeedbackEvents`.
- [x] Include a stable `event_type`, `tool_call_id`, `tool_name`, `display_label`, `status_code`, `started_utc`, and safe `summary` in progress events.
- [x] Include `duration_ms`, `result_count`, `truncated`, and safe `summary` in completion events.
- [x] Include stable error codes in failure and denial events, but avoid leaking whether inaccessible private resources exist. Public events use stable status codes and generic safe summaries.
- [x] Do not stream raw tool output to public users by default. SSE tool events carry lifecycle metadata only.
- [x] Stream short status labels only when assistant tool feedback is enabled or dashboard admin diagnostics are active. `EnableToolFeedbackEvents` controls public streaming progress events; `ExposeToolTraceToUser` separately controls final `tool_calls` response metadata.
- [x] Update dashboard `requestStream(...)` parsing to ignore unknown event types safely.
- [x] Add tests proving old clients still receive final text. Service and static API coverage prove the shared loop still returns final content and dashboard parsing ignores named tool events; live SSE/browser validation is tracked in the QA checklist.
- [x] Add tests proving dashboard can render progress without overlapping UI or breaking the transcript. Pending tool status now renders inside the assistant message content column with truncation/hover text, and `ApiSuite` asserts the dashboard structure and CSS guardrails.

## Assistant Chat Tool Feedback

Users should not stare at a blank assistant bubble while the model is searching a collection, reading chunks, querying Verbex, reading an S3 object, or calling Tavily. The chat experience needs lightweight feedback that confirms work is happening without exposing raw arguments, hidden policy, confidential object paths, or noisy internal traces.

- [x] Add a request/header or assistant setting that enables client-visible tool feedback for browser chat. `EnableToolFeedbackEvents` controls safe streaming progress event emission.
- [x] Default public chat feedback to safe status text, not detailed traces.
- [x] Show an assistant pending bubble as soon as the model starts a tool-capable turn.
- [x] Replace generic pending text with tool-aware status when the first tool event arrives.
- [x] Show neutral status labels such as `Searching collection`, `Reading document chunks`, `Searching index`, `Reading source object`, and `Searching web`.
- [x] Use stable status codes from the server and localize/render user-facing text in the dashboard when i18n support is available. Server emits stable status codes; dashboard i18n is not part of the current runtime, so localization remains a future framework task.
- [x] Show tool status in the active assistant message area, not as separate chat messages that pollute conversation history.
- [x] Coalesce repeated tool calls of the same type into one visible status line with a count, such as `Searching collection (3 searches)`.
- [~] Show completion transition text only briefly, then move back to answer generation status. Browser chat clears status when final answer content arrives; timing polish remains manual QA.
- [x] Show recoverable tool failures as subdued status, such as `One search failed; trying another source`, only when useful to the user.
- [~] Show final unrecoverable tool failure in the assistant response with a clear user-facing explanation. Streaming tool-aware chat emits an error chunk; manual browser copy/UX validation remains open.
- [x] Do not show raw query text unless admin/debug mode is enabled.
- [x] Do not show raw S3 bucket names, object keys, Verbex tenant IDs, access keys, provider request IDs, or hidden policy details in public chat.
- [x] In admin/debug mode, allow expanding a tool trace panel for the current turn with redacted arguments, result summaries, duration, and denial/failure reasons.
- [x] Add a compact progress timeline in admin/debug mode for multi-step tool turns. Admin tool-call trace panels now show a responsive `ToolCallTimeline` ordered by iteration/sequence before the detailed redacted trace cards, with static dashboard coverage.
- [x] Keep feedback visible for long-running calls and update at least every few seconds if the server can emit heartbeat/progress events. The shared tool loop emits safe `assistant.tool_call.heartbeat` events every five seconds while an individual tool call is awaited, and dashboard chat renders them as status-only updates.
- [x] Add a timeout status when a tool exceeds policy or network timeout. Failed tool progress events now emit stable `tool_timeout` when the safe tool result indicates a timeout, with service-suite coverage.
- [x] Add cancellation handling so stopping generation clears pending tool status.
- [x] Add reconnect behavior for streaming interruptions: mark the current tool status as interrupted instead of leaving an endless spinner. Dashboard streaming marks tool-progress stream breaks with `assistant.tool_call.interrupted`/`tool_stream_interrupted` metadata and shows a safe interrupted response.
- [x] Ensure the pending/status bubble is not persisted as a normal assistant answer in chat history. Tool status is browser state/SSE metadata only; persisted chat history contains the final assistant answer and separate tool-call trace rows.
- [x] Persist tool trace records separately for admin review. Tool-enabled streaming chat now routes through the shared service, so redacted tool-call records persist through the same SQLite, PostgreSQL, MySQL, SQL Server, and mock implementations; live browser/provider validation remains external.
- [x] Persist and render tool-decision model timing for admin diagnostics. Tool-loop model checks are captured as `tool_iteration_model` stages, legacy tool-enabled histories estimate the missing segment from tool-loop wall time minus final inference and server tool execution, startup backfill repairs missing normalized event rows, and both chat-history and request-history detail modals render `Tool Model Checks` for existing and new records.
- [x] Persist diagnostic chat history when the model exhausts tool iterations and the final best-effort inference returns empty output or fails. The server now returns a safe diagnostic assistant answer, records provider failure metadata in telemetry, links tool-call traces to chat history, and has service-suite coverage for empty and failed post-limit final responses.
- [x] Ensure Slack and future non-browser channels can either suppress feedback or receive channel-appropriate short status messages. Slack uses `EnableSlackToolProgressMessages` and posts short safe lifecycle messages shaped from server progress events, with service/static API coverage.

## Tool Prompt and Evidence Rules

Tool calling will only improve accuracy if the model is told how to use the tools and how to handle missing evidence. The instructions should be short, stable, and server-owned.

- [x] Add a default tool-use instruction block.
- [x] Tell the model to use tools when available context is insufficient.
- [x] Tell the model to prefer collection tools for assistant-assigned collection facts.
- [x] Tell the model to use `collection_search` before `collection_read_chunks` unless the user named a known document/chunk.
- [x] Tell the model to resolve exact document filenames once, avoid repeated enumeration of the same collection pages, use returned chunk positions or `suggested_next_calls` for reads, and answer from sufficient excerpts rather than continuing discovery loops.
- [x] Tell the model to use `verbex_full_text_search` for exact phrases, identifiers, terms, and lexical matches.
- [x] Tell the model to use `s3_object_read` for source object text only when chunk/index evidence is insufficient or the user asks about file contents directly.
- [x] Tell the model to use `collection_enumerate_documents` to discover document names when the user refers to files ambiguously.
- [x] Tell the model to use `web_search` only for public/current/external information, not for private collection data.
- [x] Tell the model to cite collection, Verbex, S3, and web evidence using returned citation handles.
- [x] Tell the model to say when evidence is insufficient after exhausting reasonable tool calls.
- [x] Tell the model not to reveal hidden tool policy, internal IDs except safe document IDs, credentials, or raw system prompts.
- [x] Treat tool outputs as untrusted content that can contain prompt injection.
- [x] Add prompt-injection tests where a document or web result instructs the model to ignore policy or reveal secrets. Service-suite coverage now injects a malicious tool result and verifies the follow-up model request retains the untrusted-output and no-secret guardrails.

## Backend Data Model and Migrations

Provider matrix support is required. SQLite, PostgreSQL, MySQL, and SQL Server migrations must evolve together.

- [x] Add `tool_policy_json` to `assistant_settings`.
- [x] Add `assistant_tool_calls` table. Startup create-table paths are implemented for SQLite, PostgreSQL, MySQL, and SQL Server.
- [x] Add provider migrations for SQLite.
- [x] Add provider migrations for PostgreSQL.
- [x] Add provider migrations for MySQL.
- [x] Add provider migrations for SQL Server.
- [x] Add startup schema repair/ensure-column logic if that is the current AssistantHub pattern.
- [x] Add model `AssistantToolCallRecord`.
- [x] Add database interface `IAssistantToolCallMethods`.
- [x] Add provider implementations for all supported databases. SQLite, PostgreSQL, MySQL, SQL Server, and mock implementations are present.
- [x] Add enumeration methods with pagination and filters. SQLite, PostgreSQL, MySQL, SQL Server, and mock support assistant, thread, request-history, chat-history, trace, tool name, success, denied, and time-bound filters, with HTTP integration coverage for assistant/trace/tool/success filters.
- [x] Add prune/delete methods for retention. SQLite, PostgreSQL, MySQL, SQL Server, and mock implementations now support filtered deletion and retention pruning; server startup cleanup invokes trace pruning with request-history retention.
- [x] Add indexes on tenant ID, assistant ID, thread ID, chat history ID, trace ID, tool name, success, created UTC, plus tenant/assistant/created and tenant/assistant/tool/created composites across SQLite, PostgreSQL, MySQL, and SQL Server.
- [x] Include `active`, `created_utc`, and `last_update_utc` where consistent with local requirements. `AssistantToolCallRecord` now carries `Active`, `CreatedUtc`, and `LastUpdateUtc`; provider schemas and chat-history-link updates maintain them.
- [x] Generate IDs with an AssistantHub ID helper, for example `atc_...`.

Suggested `assistant_tool_calls` fields:

- [x] `id`.
- [x] `tenant_id`.
- [x] `assistant_id`.
- [x] `thread_id`.
- [x] `chat_history_id`.
- [x] `request_history_id`.
- [x] `trace_id`.
- [x] `origin`.
- [x] `turn_index`.
- [x] `iteration`.
- [x] `tool_call_id`. Implemented as compatibility field `provider_tool_call_id` / `ProviderToolCallId`.
- [x] `tool_name`.
- [x] `arguments_json_redacted`. Implemented as redacted compatibility field `arguments_json` / `ArgumentsJson`.
- [x] `result_json_redacted`. Implemented as redacted compatibility field `output_json` / `OutputJson`.
- [x] `result_summary_json`.
- [x] `success`.
- [x] `denied`.
- [x] `error_type`.
- [x] `error_message`.
- [x] `duration_ms`.
- [x] `input_bytes`.
- [x] `output_bytes`.
- [x] `output_characters`.
- [x] `truncated`.
- [x] `provider`.
- [x] `model`.
- [x] `created_utc`.
- [x] `last_update_utc`.
- [x] `active`.

## Backend REST API

Most tool execution should remain internal to chat. Admin and diagnostics APIs still need to manage policy, validate configuration, and inspect tool traces.

- [x] Extend `GET /v1.0/assistants/{assistantId}/settings` to return `ToolPolicy`.
- [x] Extend `PUT /v1.0/assistants/{assistantId}/settings` to accept `ToolPolicy`.
- [x] Add backend validation that rejects malformed `ToolPolicyJson` with `400 Bad Request`.
- [x] Add validation errors with stable codes for invalid tool policy. Validation returns backward-compatible `Errors` plus stable `ErrorCodes` for invalid policy JSON, unknown allowed tool names, no enabled tool switches, and no executable tools after prerequisites/allow-lists; REST/OpenAPI/SDK/dashboard contracts are updated and integration/API/SDK/dashboard checks pass.
- [x] Add `POST /v1.0/assistants/{assistantId}/settings/tools/validate`.
- [x] Add `POST /v1.0/assistants/{assistantId}/settings/tools/test` for admin-only dry-run diagnostics. The route validates a draft policy, reports effective tools, checks selected completion endpoint tool-call metadata without executing tools/model calls, redacts endpoint secrets, is exposed through REST/OpenAPI/Postman/MCP/SDKs/dashboard, and has integration/API/SDK/dashboard coverage.
- [x] Add `GET /v1.0/assistants/{assistantId}/tools` to return the effective tool list for admins.
- [x] Add `GET /v1.0/assistants/{assistantId}/tool-calls` for paginated trace history.
- [x] Add `GET /v1.0/assistants/{assistantId}/tool-calls/{toolCallRecordId}`.
- [x] Add `DELETE /v1.0/assistants/{assistantId}/tool-calls/{toolCallRecordId}` if trace deletion is supported.
- [x] Add `DELETE /v1.0/assistants/{assistantId}/tool-calls` for filtered bulk deletion if request-history style deletion is supported.
- [x] Add global configuration docs for `ExternalSearch` under existing configuration API documentation.
- [x] Ensure `/openapi.json` exposes all new management routes. Effective tool-list, validation, dry-run diagnostics, external-search status, and tool-call trace list/get/delete/bulk-delete routes are present, and API/Postman/OpenAPI route parity passes.
- [x] Ensure public assistant chat routes do not expose admin-only tool policy details. Static API-suite coverage verifies public chat request DTOs do not expose `ToolPolicy`; policy remains behind authenticated assistant settings routes.
- [x] Ensure model-facing tool schemas are not exposed to anonymous users unless there is an explicit public metadata route. Effective tool schema/list routes are authenticated management routes only.

## Authorization and Tenant Isolation

Tool calls are internal server actions triggered by public or authenticated chat traffic. They still need to be authorized as actions on the assistant's tenant and resources.

- [x] Treat every tool execution as tenant-scoped. Executor context carries assistant tenant and all collection/Verbex/document/S3 resolution is scoped through assistant settings and server policy.
- [x] Resolve tenant from assistant, not from model arguments. Model-facing schemas do not include tenant IDs, and executor paths use `context.Assistant.TenantId`.
- [x] Reject any model argument that attempts to supply or override tenant ID. Schemas set `additionalProperties:false`; executor ignores tenant override fields and tests cover tenant-bound document rejection.
- [x] Resolve collection from assistant settings unless policy explicitly allows a list of collection IDs. Current first-release tools always use the assistant `CollectionId`; model-supplied collection IDs are not accepted.
- [x] Resolve Verbex tenant from AssistantHub tenant mapping. Verbex default index resolution uses tenant tags/default mapping and tests cover mapped/default index behavior.
- [x] Resolve S3 bucket from server settings and assistant policy. Document-backed reads and bucket enumeration validate default/allowed buckets and prefix policy.
- [x] Use existing authenticated admin gates for policy management. Integration coverage verifies admin update/list routes and non-owner/no-auth denial.
- [x] Keep public chat users from broadening tool scope. Public chat cannot submit tool policy, tenant, or collection/index/bucket scope overrides; service and API coverage verify this.
- [x] Add authorization event logging for policy denials. Shared chat orchestration now writes safe metadata-only `tool policy denial` log events for pre-execution limits and executor policy denials, with static API coverage.
- [x] Add audit records for tool calls that touch S3, web search, or index enumeration. Redacted tool-call records persist every executed tool call, and metadata-only `tool audit event` logs are emitted for S3, bucket enumeration, Tavily, Verbex search, and index enumeration.
- [x] Ensure global admin bypasses are audited with bypass reason. Shared tenant-access helpers emit metadata-only audit logs with stable reasons `tenant_access_validation` and `tenant_ownership_enforcement`, and API-suite coverage guards the log phrase, reason strings, and target tenant field.
- [x] Add tests for cross-tenant document IDs.
- [x] Add tests for cross-collection document IDs.
- [x] Add tests for cross-bucket object keys. Service-suite coverage verifies cross-bucket document-backed reads are denied outside default/allowed buckets.
- [x] Add tests for disallowed Verbex index IDs.
- [x] Add tests for disabled Tavily global configuration.
- [x] Add tests for assistant-level Tavily override availability and execution.
- [x] Add tests for assistant web search disabled while global search is enabled.

## Security and Abuse Controls

Server-side tools increase the blast radius of a prompt injection or careless assistant setting. The first release should be read-only and heavily bounded.

- [x] Tool calls are read-only in the first release. Implemented model-callable tools are search, read, and enumerate only.
- [x] No write, delete, upload, reindex, crawl-start, configuration-update, credential-read, or admin-management tools are model-callable. Registry only exposes read/search/enumeration tools.
- [x] No arbitrary filesystem tools.
- [x] No arbitrary HTTP fetch tool. Tavily search is governed; arbitrary URL retrieval remains disabled/reserved.
- [x] No shell execution tool.
- [x] No SQL execution tool.
- [x] No MCP management tool surface is exposed to public assistant chat.
- [x] Apply per-call and per-turn byte limits. Per-call S3 byte caps are enforced by the executor, and the shared chat loop enforces aggregate model-visible S3 object bytes per turn with structured `object_byte_limit` errors.
- [x] Apply per-call and per-turn character limits. Per-call output caps and aggregate per-turn model-visible character budgets are enforced by the executor/chat loop, with truncation and best-effort final-answer handling.
- [x] Apply per-turn tool count limits.
- [x] Apply timeout and cancellation.
- [x] Redact secrets in arguments and outputs before persistence. Non-streaming trace persistence redacts secret-like argument fields and stores output summaries by default.
- [x] Redact secrets before model-visible tool output. `AssistantChatService` applies `AssistantToolAuditWriter.RedactToolJson` to successful and failed tool outputs before tool-role messages are sent back to the model, with service-suite coverage for model-visible secret redaction.
- [x] Block S3 object reads of known secret/config paths by default. `s3_object_read` now rejects known secret/config filenames, key suffixes, and secret directory segments before metadata or range reads, with service-suite coverage for document-backed and bucket-wide paths.
- [x] Block binary output by default.
- [x] Block web raw content by default.
- [x] Block private IP/domain web searches only if Tavily can return/fetch private network data; otherwise document Tavily's public-search boundary. `web_search` now denies query/include-domain targets for localhost, private IP ranges, and internal-only domains before Tavily is called, with service-suite coverage.
- [x] Add allow/deny domain policy for web search. Assistant/global include and exclude domains are enforced in Tavily request construction.
- [x] Add prefix allow/deny policy for S3 keys. S3 object reads and bucket enumeration enforce allowed prefixes.
- [x] Add content-type allow/deny policy for S3 reads. `AllowedContentTypes` is implemented and service-tested for S3 reads/enumeration.
- [x] Add detailed denial reasons for admins. Executor returns/logs detailed safe denial/error text, persists detailed admin trace fields, and dashboard admin trace panels surface safe trace/error state for diagnostics.
- [x] Return generic denial messages to the model when detailed reasons could leak resource existence. Model-visible failed tool outputs now include stable `ErrorCode` values and generic error text, while detailed executor errors stay in admin traces/logs; service-suite coverage verifies detailed simulated failures are not sent back to the model.

## Tavily Integration

AssistantHub can adapt the Mux design while keeping AssistantHub code style. The Mux example has `TavilySearchClient`, `TavilySearchQuery`, `TavilySearchResponse`, provider-agnostic request/response models, `WebSearchService`, `WebSearchServiceFactory`, and a `web_search` tool schema.

- [x] Add `AssistantHub.Search` namespace or keep search under `AssistantHub.Core.Services` and `Models` according to local project organization. Decision: keep first Tavily client/models under existing `AssistantHub.Core.Services` and `AssistantHub.Core.Models`.
- [x] Add provider-agnostic `WebSearchRequest`.
- [x] Add provider-agnostic `WebSearchResponse`.
- [x] Add `WebSearchResultItem`.
- [x] Add `WebSearchProviderAttempt`.
- [x] Add `WebSearchServiceOptions`.
- [x] Add `WebSearchProviderRegistration`.
- [x] Add `SearchProviderOptions`.
- [x] Add `IWebSearchService`.
- [x] Add `WebSearchService`. Tavily-backed provider-neutral service is implemented and used by `web_search`, with service-suite coverage for response mapping and usage credits.
- [x] Add `TavilySearchClient`.
- [x] Add `TavilySearchQuery`.
- [x] Add `TavilySearchResponse`.
- [x] Put `TavilyAutoParameters` and `TavilyUsage` in separate files, unlike the Mux example, to satisfy AssistantHub code style.
- [x] Implement Tavily POST to `https://api.tavily.com/search`.
- [x] Use bearer auth with configured API key.
- [x] Resolve API keys from `${VAR}`, `$VAR`, `$env:VAR`, and `%VAR%` environment-variable references.
- [x] Send Tavily fields: `query`, `search_depth`, `topic`, `max_results`, `chunks_per_source`, `time_range`, `start_date`, `end_date`, `include_answer`, `include_raw_content`, `include_images`, `include_image_descriptions`, `include_favicon`, `include_domains`, `exclude_domains`, `country`, `auto_parameters`, `exact_match`, `include_usage`, and `safe_search` as supported.
- [x] Normalize Tavily results into AssistantHub response models.
- [x] Parse `request_id`, `response_time`, `answer`, `images`, result `title`, `url`, `content`, `score`, `raw_content`, `favicon`, and published date if present.
- [x] Parse usage credits when Tavily returns them.
- [x] Support test HTTP client injection.
- [x] Add provider timeout handling. Tavily client applies provider timeout settings through `HttpClient.Timeout`, and service coverage verifies timeout behavior.
- [x] Add provider HTTP error handling with response body redaction.
- [x] Add invalid JSON handling.
- [x] Add tests using fake Tavily responses from Mux-style fixtures.

## Collection and Chunk Read Services

The model needs exact reads, not only ranked search excerpts. Existing retrieval code should be extended carefully instead of duplicating RecallDB calls throughout tool executors.

- [x] Add public methods to `RetrievalService` or a focused `CollectionToolService` for tool use. `CollectionToolService` provides typed facade methods over the canonical executor while keeping policy enforcement centralized.
- [x] Add `SearchCollectionAsync` that accepts a strongly typed tool search request. The request DTO covers query, multi-query, search mode/strategy, document IDs, metadata filters, and full-text options; service-suite coverage verifies JSON marshalling.
- [x] Add `ReadChunksAsync` that accepts document ID and positions/ranges. The typed facade accepts exact positions, ranges, chunk caps, and neighbor windows with service-suite marshalling coverage.
- [x] Add `EnumerateDocumentsAsync` that applies assistant filters. The typed facade accepts query/status/content/source/metadata filters and delegates enforcement to the policy-scoped executor with service-suite coverage.
- [x] Add multi-query reciprocal-rank fusion helper reusable from chat and tool paths. `RetrievalFusionHelper.FuseByReciprocalRank` is implemented in Core, both chat execution paths use it for multi-query retrieval, and model-suite coverage verifies dedupe, best-score retention, ordering, and `FusionScore` assignment.
- [x] Add document ID filter support to `RetrievalSearchOptions`.
- [x] Add `DocumentIds` to RecallDB search body if RecallDB supports native multi-document filters.
- [x] If RecallDB only supports single document ID, loop and merge/dedupe server-side. `RetrievalService` falls back to per-document `DocumentId` searches when `RecallDb.SupportsMultiDocumentFilter=false`, logs a warning, merges/dedupes by document/position, and preserves score ordering with service-suite coverage.
- [x] Ensure hybrid fallback carries document filters.
- [x] Ensure full-text-only mode carries document filters.
- [x] Ensure metadata filters always intersect with document filters.
- [x] Add tests for vector, full-text, hybrid, hybrid fallback, metadata filters, document filters, and neighbor reads.

## Verbex Services

Verbex tool access should use a small service wrapper that understands AssistantHub tenant/index/document mapping. Do not have the tool executor manually assemble raw paths everywhere.

- [x] Add `VerbexToolService`. The service provides typed scope, search, enumeration, and record-to-document mapping helpers while delegating execution to canonical tools.
- [x] Add `ResolveAllowedVerbexScopeAsync`. The helper resolves assistant tenant, `Constants.VerbexTenantIdTag`, `Constants.VerbexDefaultIndexIdTag`, policy indices, and visible document indices.
- [x] Add `SearchAsync` for index full-text search. The typed facade marshals `VerbexToolSearchRequest` to `verbex_full_text_search`.
- [x] Add `EnumerateRecordsAsync` for index record listing. The typed facade marshals `VerbexToolEnumerateRecordsRequest` to `index_enumerate_records`.
- [x] Add `MapRecordToAssistantDocumentAsync`. The helper maps direct assistant document IDs, document-level Verbex record IDs, and chunk record IDs back to assistant-visible completed documents.
- [x] Add post-filtering to remove records outside assistant tenant/collection. `VerbexToolService` uses the same visible-document checks as executor paths and service-suite coverage verifies hidden collection records do not map or appear in scope.
- [x] Add safe response model for Verbex search hits. `VerbexToolSearchHit` exposes only mapped document metadata, score/excerpt/matched terms, chunk position, and citation handle.
- [x] Add safe response model for Verbex record enumeration. `VerbexToolRecordItem` exposes mapped document metadata, safe optional source/excerpt fields, chunk position, and citation handle.
- [x] Add tests for tenant mapping through `Constants.VerbexTenantIdTag`. Service-suite coverage verifies tenant tag mapping, default-index tag mapping, visible document index inclusion, hidden index exclusion, typed Verbex request marshalling, and chunk-record document mapping.
- [x] Add tests for default index mapping through `Constants.VerbexDefaultIndexIdTag`. Service-suite Verbex search coverage verifies tenant default-index tag resolution and requested-index denial outside the resolved scope.
- [x] Add tests for documents with explicit `VerbexIndexId` and `VerbexRecordId`. Service-suite Verbex search coverage now maps a record returned only by `VerbexRecordId` back to the assistant document and emits safe follow-up metadata.
- [x] Add tests for unmapped records. Service-suite Verbex search coverage verifies unmapped records are excluded from model-visible output.

## S3 Object Services

Object reads need metadata and range support. If Blobject or the existing storage abstraction does not expose efficient range reads, add the narrowest extension needed.

- [x] Add `ObjectToolService`. The service provides typed `ReadObjectAsync` and `EnumerateObjectsAsync` facades over `s3_object_read` and `bucket_enumerate_objects`, with service-suite marshalling coverage.
- [x] Extend `IObjectStorageService` with metadata read if supported. Added `GetObjectMetadataAsync` plus concrete S3 and in-memory test fake implementations.
- [x] Extend `IObjectStorageService` with object enumeration if supported. Added `ListObjectsAsync` plus concrete S3 `ListObjectsV2` implementation and in-memory test fakes.
- [x] Extend `IObjectStorageService` with ranged read if supported. Added `DownloadRangeAsync`; concrete S3 uses `GetObjectRequest.ByteRange`.
- [x] If native range reads are unavailable, document the fallback and enforce small max object sizes before full download. First-release built-in storage now has native S3 range support; executor uses metadata/range reads and rejects oversized requests before object bytes are read.
- [x] Add safe text detection by content type and byte sniffing.
- [x] Add UTF-8 decoding with replacement handling. S3 text reads now decode UTF-8 with replacement characters after binary/content-type checks, and service-suite coverage verifies malformed UTF-8 bytes are returned with `U+FFFD` rather than failing the tool call.
- [x] Add binary detection.
- [x] Add object key prefix/suffix policy validation. Prefix validation is implemented for document-backed reads and bucket enumeration, and suffix/content-type allow-lists are enforced before document-backed reads or bucket enumeration output.
- [x] Add document-backed object resolution from `AssistantDocument`.
- [x] Add mapped-object result response models. `ObjectToolReadResult` and `ObjectToolEnumerationItem` define safe typed shapes for document-backed object reads and mapped bucket enumeration results.
- [x] Add tests for document-backed reads, allowed prefix reads, denied prefix reads, binary denial, secret/config path denial, truncation, missing object, and cancellation. Service-suite coverage now includes document-backed read, range/metadata reads, bucket enumeration, prefix denial, secret/config path denial, binary/base64 behavior, truncation, missing object, and pre-call cancellation.

## Frontend Dashboard

Assistant settings must let administrators understand and control tool exposure without editing JSON by hand. The UI should remain dense and operational, matching the current dashboard style.

- [x] Add a `Tool Calls` section to `dashboard/src/views/AssistantSettingsView.jsx`. Structured master, group, per-tool, limit, allow-list, reset, Tavily, and policy preview controls are implemented.
- [x] Add a master `Enable tool calls` toggle.
- [x] Add a concise warning when enabled for a public assistant.
- [x] Add tool group toggles: Collection, Verbex, S3 Objects, Web Search.
- [x] Add individual toggles for each tool.
- [x] Add per-tool limit controls using number inputs, sliders, checkboxes, and domain/prefix list editors.
- [x] Add a generated effective tool list preview.
- [x] Add a policy validation button.
- [x] Add a test tool policy button for admins. The Validate Policy action exercises the draft policy through the backend validation route.
- [x] Add clear disabled states when prerequisites are missing, such as no collection, no Verbex, no S3, no Tavily provider, or endpoint lacks tool-call support. Settings UI warns for missing collection, endpoint capability, global Tavily readiness, and enabled-but-unavailable tool descriptors such as Verbex/S3 prerequisites.
- [x] Add redacted status display for global Tavily configuration. Assistant settings now loads `GET /v1.0/configuration/external-search/status`, displays configured/disabled/incomplete system Tavily readiness without provider names, endpoints, or secrets, and has API-suite plus dashboard build coverage.
- [x] Add tool-call capability indicators to endpoint selection if endpoint metadata supports it. Endpoint edit/create forms expose capability flags, and assistant endpoint selectors label endpoints as tool-capable with their tool-call API format or as no-tools endpoints.
- [x] Add JSON view support for `ToolPolicy`.
- [x] Validate `ToolPolicyJson` in the dashboard before saving assistant settings.
- [x] Add reset-to-disabled policy action.
- [x] Add safe defaults when loading older assistant settings with no policy.
- [x] Ensure all new user-facing strings are routed through i18n if/when the dashboard i18n runtime exists. AssistantHub has no dashboard i18n runtime today, so new tool-call strings follow the existing English-only dashboard convention; README documents this baseline and server feedback uses stable `status_code` values for future localization.
- [~] Ensure long labels and domain/prefix chips fit on desktop, tablet, and mobile. CSS now gives tool-policy grids, warnings, status chips, artifact chips, trace cards, and validation text responsive min-width/wrapping/ellipsis behavior; manual desktop/tablet/mobile viewport QA remains open.
- [x] Avoid nested cards inside cards.
- [x] Use existing form, tooltip, modal, password, table, copy button, and action menu patterns.
- [x] Add tool-call trace display to chat history/request detail views for admins.
- [x] Add filters for tool name, success, denied, trace ID, and time range in any new tool-call history UI.
- [x] Add `ApiClient` methods for policy validation, effective tool list, and tool-call trace APIs.
- [x] Update API Explorer examples through OpenAPI rather than custom hard-coded forms where possible. Tool-call trace routes render from OpenAPI, readable labels are present, and remaining API Explorer verification is tracked as live/manual QA.

## Browser Chat UX

The public chat experience should stay simple. Tool use should improve answers without exposing confusing internal mechanics.

- [x] Keep final answer as the primary visible output. Browser chat uses tool events only to update pending status and still renders final assistant content in the transcript.
- [x] Show citations from tool-derived evidence when citation mode is enabled. The chat loop annotates citation-capable tool outputs, merges document and web sources into the final `citations` manifest, and dashboard citation cards render document downloads or web URLs.
- [x] Preserve existing SSE status-message handling in browser chat so future tool-progress events have a UI path.
- [x] Show a subtle tool-aware status while server-side tool calls are running when browser chat feedback is enabled. Backend emits safe named SSE events and dashboard chat maps them to pending status text; live viewport/provider validation remains external.
- [x] Do not display raw tool arguments or raw outputs to public users by default.
- [x] Add admin/debug mode in dashboard chat drawer if operators need to inspect tool calls live. Browser chat now renders a compact safe `tool_calls`/`tool_events` activity summary below assistant responses when the server returns safe trace metadata, while admins retain the redacted full trace panels in chat history and request detail.
- [~] Ensure tool-progress labels do not overlap with input controls or transcript content. CSS truncation/wrapping is implemented for pending status and final safe tool summaries; manual viewport QA remains open.
- [~] Ensure mobile chat drawer handles tool status without layout shifts. CSS truncation/wrapping is implemented for pending status and final safe tool summaries; manual viewport QA remains open.
- [x] Ensure Slack responses do not dump tool traces into channels unless explicitly enabled. Slack progress messages are built from safe display labels/status/result counts only, and final Slack answers continue using canonical assistant text rather than trace payloads.

## SDK Updates

SDK parity matters because assistant settings are not only edited through the dashboard.

### C# SDK

- [x] Add `AssistantToolPolicy`.
- [x] Add nested policy models. Explicitly deferred for first release; SDKs expose the flat `AssistantToolPolicy` contract selected for v0.16.0.
- [x] Add `AssistantToolTrace` or `ChatCompletionToolTrace`. `ChatCompletionToolTrace` is implemented in Core and the C# SDK.
- [x] Add `AssistantToolCallRecord`.
- [x] Update assistant settings model with `ToolPolicyJson` and typed `ToolPolicy`.
- [x] Add effective tool list method.
- [x] Add tool policy validation method.
- [x] Add tool-call trace enumeration methods.
- [x] Add tests in `sdk/csharp/Test.Sdk`.
- [x] Add endpoint capability fields to C# SDK endpoint models and local contract tests.
- [x] Update C# SDK README.
- [x] Bump package version if release version changes.

### JavaScript/TypeScript SDK

- [x] Add TypeScript types for `AssistantToolPolicy` and nested policies.
- [x] Add response/tool trace types.
- [x] Update assistant settings API methods/types with `ToolPolicyJson` and typed `ToolPolicy`.
- [x] Add effective tool list method.
- [x] Add tool policy validation method.
- [x] Add tool-call trace methods.
- [x] Add test harness coverage.
- [x] Add assistant Tavily override and ungoverned-web-access fields to TypeScript policy types.
- [x] Update JS SDK README.
- [x] Bump package version if release version changes.

### Python SDK

- [x] Add model classes or typed dictionaries for `AssistantToolPolicy`.
- [x] Update sync and async clients with current settings/model parity, including typed `tool_policy`.
- [x] Add effective tool list method.
- [x] Add tool policy validation method.
- [x] Add tool-call trace methods.
- [x] Add sync and async tests. Local model/contract coverage passes in `test_sdk.py --local-only`, including sync and async assistant tool-call trace route helpers; live server SDK validation is tracked in the QA checklist.
- [x] Add assistant Tavily override and endpoint capability fields to Python SDK models and local contract tests.
- [x] Update Python SDK README.
- [x] Bump package version if release version changes.

## MCP Surface

AssistantHub MCP should expose management parity, not silently add public runtime tool execution.

- [x] Update MCP assistant settings tools to include `ToolPolicy`. Existing `assistant/settings/get` and `assistant/settings/update` use the SDK `AssistantSettings` model with `ToolPolicy`/`ToolPolicyJson`, and MCP redaction now covers `TavilyApiKey` plus nested `ToolPolicyJson`.
- [x] Add MCP tool to get effective assistant tools for admins. `assistant/settings/tools/list` calls the SDK effective-tool route.
- [x] Add MCP tool to validate assistant tool policy for admins. `assistant/settings/tools/validate` validates draft `AssistantToolPolicyValidationRequest` payloads without persisting them.
- [x] Add MCP tools to enumerate/read assistant tool-call traces if management parity requires it. `assistant/tool-calls/list`, `assistant/tool-calls/get`, `assistant/tool-calls/delete`, and `assistant/tool-calls/delete-bulk` expose redacted trace management through the SDK.
- [x] Do not expose model runtime tools like `s3_object_read` directly through MCP unless an explicit admin-only diagnostic tool is added. MCP exposes management and trace inspection only; runtime tools remain server-side chat tools.
- [x] Update `MCP_API.md`.
- [x] Update MCP registration tests. Static API-suite coverage verifies assistant document listing, effective tools, validation, and trace registrations plus MCP docs.
- [x] Update `docker/assistanthub-mcp/assistanthub-mcp.json` version if release version changes. Version is already `v0.16.0`.

## Postman

Postman should demonstrate admin configuration and chat behavior without requiring users to reverse-engineer JSON.

- [x] Perform a thorough Postman parity pass for tool-call workflows:
  - [x] Assistant tool policy settings and validation examples.
  - [x] Effective tool list examples.
  - [x] Chat with tool calls enabled and a tool-capable endpoint.
  - [x] Disabled/default posture examples.
  - [x] Tool trace/history examples when trace routes land.
- [x] Environment variables and examples match `README.md`, `REST_API.md`, `MCP_API.md`, `openapi.json`, SDK READMEs, and dashboard payloads. Tool-policy examples now include collection, Verbex, S3, and Tavily limit fields, and API-suite Postman/OpenAPI parity passes. Live streaming/manual UI workflow validation is tracked in the QA checklist.
- [x] Add request: get assistant settings with tool policy.
- [x] Add request: update assistant settings to enable collection tools.
- [x] Add request: update assistant settings to enable Verbex search tool.
- [x] Add request: update assistant settings to enable S3 object read for document-backed objects.
- [x] Add request: update assistant settings to enable Tavily web search.
- [x] Add/update request guidance for server-level Tavily `ExternalSearch` configuration and redacted key round trips.
- [x] Add request: validate tool policy.
- [x] Add request: get effective tool list.
- [x] Add request: chat with tool calls enabled.
- [x] Add request: enumerate tool-call history.
- [x] Add request: get one tool-call record.
- [x] Add environment variables for assistant ID, collection ID, document ID, and optional Tavily API key placeholder.
- [x] Keep secrets out of committed Postman examples.

## OpenAPI

- [x] Regenerate or update `openapi.json`.
- [x] Ensure `AssistantToolPolicy` schema is present.
- [x] Ensure nested policy schemas are present. Not applicable for v0.16.0 because nested policy models are deferred; `openapi.json` exposes the flat `AssistantToolPolicy` schema.
- [x] Ensure tool trace schemas are present.
- [x] Ensure validation and effective tool list routes are present.
- [x] Ensure response examples include disabled defaults. `openapi.json` includes an `AssistantToolPolicy` example with tool calls and individual tools disabled plus empty allow-lists, and `ApiSuite` asserts it remains present.
- [x] Ensure API Explorer can render the nested policy request body. Not applicable for v0.16.0; API Explorer renders the flat `AssistantToolPolicy` request/response schema and dashboard form controls.
- [x] Verify streaming routes remain documented. `REST_API.md` documents standard content chunks, named tool progress events, and final `tool_calls` metadata.

## Documentation

- [x] Perform a thorough documentation parity pass across `README.md`, `CHANGELOG.md`, `REST_API.md`, and `MCP_API.md`:
  - [x] Tool-call overview, disabled defaults, built-in tools, Tavily settings, and provider capability metadata are documented.
  - [x] REST settings, validation, and effective-tool routes are documented.
  - [x] Non-streaming tool-loop behavior, tool progress visibility, and model-visible failed tool outputs are documented after implementation lands.
  - [x] Assistant Request History/admin trace behavior is documented when trace persistence lands.
- [x] Postman, OpenAPI, SDK README, API Explorer, dashboard, and docs examples use identical paths, JSON names, and default values. Tool-policy field examples, trace list/get/delete/bulk-delete artifacts, and API parity are aligned; manual API Explorer/dashboard workflow QA is tracked in the QA checklist.
- [x] Update `README.md` with tool-call overview.
- [x] Explain the difference between RAG, attached documents, and model-directed tools.
- [x] Document every built-in tool and its scope.
- [x] Document default disabled posture.
- [x] Document server-side enforcement and tenant isolation.
- [x] Document Tavily server JSON configuration.
- [x] Document Docker/env-var setup for Tavily.
- [x] Document provider support and limitations.
- [x] Document streaming tool status behavior.
- [x] Document admin UI workflow. README and REST docs now describe endpoint capability setup, disabled-default policy editing, validation/effective-tool inspection, chat validation, and redacted trace review.
- [x] Update `REST_API.md`.
- [x] Update `MCP_API.md`.
- [x] Update `TESTING.md`.
- [x] Update `CHANGELOG.md`.
- [x] Add or update architecture docs if this plan graduates from planning into implemented behavior.
- [x] Document known limitations: no write tools, no arbitrary filesystem, no arbitrary URL fetch, provider support limited by endpoint capability, and S3 binary output disabled by default.

## Analytics, History, and Observability

Operators need to know when tools are helping and when they are expensive or failing.

- [x] Add tool-call counts to assistant performance telemetry. The shared chat service passes safe tool traces into `AssistantPerformanceTelemetryBuilder`, which writes a `tools` stage with total, success, denied, error, and truncated counts.
- [x] Add tool-call duration totals to telemetry. The `tools` stage stores summed tool-call duration in `DurationMs` and `tool_call_duration_ms` metadata.
- [x] Add per-tool counts and error counts to assistant analytics if useful. Performance telemetry includes `per_tool` count/success/error/denied/truncated/duration/output/result metadata, slowest-request rows expose safe aggregate tool diagnostics, and dashboard analytics shows tool failures/denials/truncation/slowest tool summaries with service-suite coverage.
- [x] Add dimensions: tool name, success, denied, provider, result count, truncated. Tool telemetry dimensions now include safe provider mapping, result count when known, and per-call status/truncation fields.
- [x] Link tool-call records to chat history and request history by trace ID and IDs. Records include request-history IDs, are linked to chat history by trace ID after persistence, and dashboard admin/history panels surface linked traces for diagnostics.
- [x] Add request-history redaction for tool call arguments and outputs. Tool-call records redact persisted arguments and store output summaries; route/UI responses use those redacted records.
- [x] Add logs for tool start/end/denial/error with safe metadata. Shared orchestration logs safe tool lifecycle failures, policy denials, success/failure metadata, and persistence/linking failures without raw secrets.
- [x] Add health/status signal for Tavily configuration. Added global-admin `GET /v1.0/configuration/external-search/status`, OpenAPI/REST/Postman/README/MCP docs, dashboard client usage, and C#/TypeScript/Python SDK helpers with local contract coverage.
- [x] Add metrics for Tavily request count, failures, latency, and credits when available. The `tools` telemetry stage now includes Tavily request/failure counts, summed provider latency, credits used, per-tool metrics, and per-call dimensions.
- [x] Add pruning policy for tool-call records. Tool-call traces are pruned by the request-history cleanup loop using `RequestHistory.RetentionDays`, and README/REST docs now state the retention policy.
- [x] Add dashboard display for slowest/failing tool calls if the analytics page is extended. The Assistant Analytics slowest-request table now includes aggregate tool call count, failures, denials, truncations, slowest tool, and failing tool names; REST docs and C#/JS/Python SDK models are updated.

## Testing Plan

### Model and DTO Tests

- [x] `AssistantToolPolicy` defaults disabled.
- [x] Null nested policies normalize. Not applicable for v0.16.0 nested models; null/missing flat `ToolPolicy` normalizes to disabled defaults.
- [x] Invalid limits clamp or reject as documented.
- [x] Tool names normalize and dedupe. The registry canonicalizes implemented names, definition generation dedupes canonical names, the chat loop normalizes model-returned tool names, executor dispatch uses canonical names, and service-suite coverage verifies mixed-case model tool names execute through the canonical path.
- [x] Server JSON `ExternalSearch` defaults disabled.
- [x] Tavily provider settings validate endpoint, key, timeout, default provider.
- [x] Tool result DTOs serialize with expected JSON names.
- [x] Provider-neutral tool-call DTOs serialize and deserialize OpenAI-compatible tool-call shapes.
- [x] Provider-neutral tool-call DTOs accept Ollama-style raw JSON object arguments.

### Service Tests

- [x] Effective tool resolver includes only policy-enabled and prerequisite-available tools by default.
- [x] Effective tool resolver can include disabled/unavailable descriptors for admin diagnostics.
- [x] Registry includes only enabled tools.
- [x] Registry excludes globally unavailable tools.
- [x] Registry excludes tools whose executor is not implemented yet.
- [x] Runtime rejects disabled tool call.
- [x] Runtime rejects unknown tool name.
- [x] Runtime rejects malformed arguments.
- [x] Runtime enforces per-call timeout.
- [x] Runtime enforces per-turn iteration limit. The shared chat loop enforces `MaxToolIterations`, and tool-enabled streaming chat routes through that same loop.
- [x] Runtime enforces output truncation.
- [x] Runtime redacts secrets from persisted arguments. Non-streaming trace persistence masks common secret/token/key fields and can suppress argument persistence entirely by policy; broader policy review remains a release security-review task.
- [x] Runtime returns structured model-visible errors. The result DTO carries success, denied, safe error text, and stable `ErrorCode` values; model-visible tool outputs and persisted admin summaries include the code, with service-suite coverage for malformed JSON, unknown arguments, unknown tools, web-search turn limits, and timeouts.
- [x] Collection search applies assistant metadata filters.
- [x] Collection search applies document filters.
- [x] Collection search merges multi-query results. Service-suite coverage verifies duplicate document/position hits across model-provided queries are returned once while preserving searched-query metadata.
- [x] Collection read chunks validates tenant and collection.
- [x] Verbex search validates index policy.
- [x] Verbex record enumeration validates index policy and assistant document mapping.
- [x] S3 read validates object policy.
- [x] Effective tool resolver validates global Tavily and assistant web-search policy before exposing `web_search`.
- [x] Tool executor applies Tavily allowed/blocked domains and raw-content/image policy.
- [x] Tool executor uses assistant-level Tavily endpoint/API-key override before falling back to system-wide Tavily.
- [x] Tool executor can enumerate completed documents scoped to the assistant collection.
- [x] Tool executor applies assistant metadata filters to document enumeration.
- [x] Tool executor can read exact collection chunks by document position.
- [x] Tool executor sends collection search through the assistant tenant and collection.
- [x] Tool executor searches Verbex through the mapped/default assistant tenant index and filters results through assistant documents.
- [x] Tool executor enumerates Verbex records through the mapped/default assistant tenant index and filters results through assistant documents.
- [x] Tool executor can read document-backed S3 object text within byte/text limits.
- [x] Tool executor rejects binary S3 text output unless base64 output is explicitly enabled.
- [x] Model-facing schemas include bounded parameters for implemented tools.
- [x] Tavily client parses normalized response.
- [x] Tavily client expands endpoint and API-key environment-variable references.
- [x] Tavily client handles HTTP error.
- [x] Tavily client handles timeout.
- [x] Tavily client handles invalid JSON.
- [x] OpenAI-compatible non-streaming inference sends tool definitions and parses model tool calls.
- [x] Ollama non-streaming inference sends tool definitions and parses model tool calls with object arguments.

### Database Provider Tests

- [x] SQLite migration adds `tool_policy_json`. Startup table/create-column paths and SQLite-backed integration coverage are in place.
- [~] PostgreSQL migration adds `tool_policy_json`. Startup create/alter query is implemented; external PostgreSQL validation remains open.
- [~] MySQL migration adds `tool_policy_json`. Startup create/alter query is implemented; external MySQL validation remains open.
- [~] SQL Server migration adds `tool_policy_json`. Startup create/alter query is implemented; external SQL Server validation remains open.
- [x] SQLite migration adds `assistant_tool_calls`.
- [~] PostgreSQL migration adds `assistant_tool_calls`. Startup create-table and index queries are implemented; external PostgreSQL validation remains open.
- [~] MySQL migration adds `assistant_tool_calls`. Startup create-table and index queries are implemented; external MySQL validation remains open.
- [~] SQL Server migration adds `assistant_tool_calls`. Startup create-table and index queries are implemented; external SQL Server validation remains open.
- [x] CRUD tests for `AssistantToolCallRecord`. Non-streaming chat persistence, mock read/list/delete paths, and an in-process SQLite HTTP list/get/delete round trip are covered; external provider CRUD validation is tracked with database migration QA.
- [x] Enumeration/pagination tests for tool-call records. API/OpenAPI/Postman route parity and SQLite/mock filter support are covered, including in-process HTTP assistant/trace/tool/success filtering and `maxResults` pagination assertions; external provider-matrix validation is tracked with database migration QA.
- [x] Prune/delete tests for retention. HTTP filtered bulk deletion and direct SQLite retention-age pruning are covered by the integration suite; external provider retention validation is tracked with database migration QA.
- [x] Provider matrix tests use `Test.Shared` where possible. OpenAI-compatible and Ollama tool-definition/tool-call parsing, SQLite-backed HTTP trace routes, model serialization, service orchestration, and SDK local contract coverage all run through `Test.Shared` or shared SDK harnesses; external PostgreSQL/MySQL/SQL Server and live provider validation remain tracked separately.

### Route and Auth Tests

- [x] Admin can update tool policy.
- [x] Non-owner/non-admin cannot update tool policy.
- [x] Public chat cannot override tool policy. Static API-suite coverage asserts `ChatCompletionRequest` exposes no tool-policy override field; tool policy remains behind authenticated assistant settings routes.
- [x] Effective tools route requires authenticated admin/owner.
- [x] Tool-call trace route enforces tenant and assistant ownership. Handler enforces admin and tenant/assistant ownership, and the integration suite verifies assistant-scoped filtering/get/delete/bulk-delete plus cross-assistant get/delete denial and non-admin list denial.
- [x] Cross-tenant document ID is denied. Service-suite coverage verifies collection search and chunk-read requests reject other-tenant assistant document IDs before any RecallDB read/search.
- [x] Cross-collection document ID is denied. Service-suite coverage verifies collection search, collection chunk reads, Verbex search normalization, and Verbex enumeration exclude/reject other-collection documents.
- [x] Cross-index Verbex request is denied. Service-suite coverage verifies requested `index_id` values outside assistant policy/default scope fail before any Verbex provider call.
- [x] Cross-bucket object read is denied. Executor denies document buckets outside the default bucket unless listed in `AllowedBucketNames`, with service-suite coverage for document-backed and bucket-wide S3 policy paths; live browser/provider validation is tracked in the QA checklist.
- [x] Disabled Tavily global config prevents web tool exposure in the effective tool resolver.
- [x] Disabled assistant web tool prevents web tool exposure even when global config exists. Service-suite coverage verifies assistant policy suppresses `web_search` even when global Tavily is configured.

### Chat Orchestration Tests

- [x] Tool calls disabled preserves existing RAG behavior. Service-suite coverage verifies RAG retrieval still runs and no model-facing `tools` payload is sent when `EnableToolCalls=false`.
- [x] Tool calls enabled with no tool call returns final answer in non-streaming chat.
- [x] One tool call returns final answer in non-streaming chat.
- [x] Multiple sequential tool calls return final answer. Service-suite coverage verifies two model-requested tools across sequential provider turns produce the final answer.
- [x] Tool error can be recovered by model. Service-suite coverage verifies a failed tool result is returned to the model and the model can produce a final answer.
- [x] Tool iteration limit returns controlled final behavior in non-streaming chat.
- [x] Streaming final answer works after tool calls. `HandleToolAwareStreamingChatAsync` emits tool progress events and final answer chunks through the shared tool loop; live SSE/browser validation is tracked in the QA checklist.
- [x] Existing SSE clients are not broken. Dashboard parser ignores/handles named tool events and local static/API checks cover the parser contract; live client validation is tracked in the QA checklist.
- [x] Chat history includes tool telemetry. Tool-call records are persisted and linked to chat history, and `PerformanceJson`/performance events include the safe aggregate `tools` telemetry stage.
- [x] Request history links to tool-call records. Records carry `RequestHistoryId`, and dashboard request-history/admin detail panels surface linked tool-call traces for diagnostics.
- [x] Citations include tool-derived collection evidence. Collection/Verbex/S3-style `CitationHandle` values are mapped to assistant documents, assigned bracket references in model-visible tool output, and returned in `citations.sources` with service-suite coverage.
- [x] Web citations include URL evidence. Web-search result URLs are assigned bracket references in model-visible tool output, returned as `source_type=web` citation sources with `url`, and rendered as clickable dashboard citation cards.

### Frontend Tests

- [x] Assistant settings loads disabled tool policy for old assistants. Model-suite coverage verifies null `ToolPolicyJson` materializes a disabled `AssistantToolPolicy`.
- [x] Tool Calls section toggles master enable.
- [x] Tool group controls enable/disable nested settings.
- [~] Validation errors render without overlapping. Alert, document attachment, tool policy, and admin trace error containers now wrap long validation/provider text safely; manual browser viewport QA remains open.
- [x] Effective tool list preview updates.
- [x] Tavily missing configuration warning renders. Backend logs startup/execution warnings for incomplete configuration, and assistant settings renders safe global Tavily configured/disabled/incomplete status from the admin status route.
- [x] Tool trace admin view paginates and filters. Chat-history and request-history detail panels filter by tool name, success, denied, trace ID, and time range; integration coverage verifies tool-call trace `maxResults` pagination.
- [x] Chat drawer handles tool-progress events.
- [x] Chat drawer shows a pending assistant bubble while a tool call is running.
- [x] Chat drawer coalesces repeated tool status events without adding transcript messages.
- [x] Chat drawer clears pending tool status when final assistant text arrives.
- [x] Chat drawer clears pending tool status when generation is cancelled.
- [~] Chat drawer displays a safe failure status for unrecoverable tool failures. Recoverable failed tool events use a safe status; final error-copy/manual browser validation remains open.
- [~] Public chat does not show raw tool arguments, object keys, provider request IDs, or hidden policy details. Server events and response metadata omit these fields; manual browser/live trace review remains open.
- [x] Admin/debug tool trace panel shows redacted arguments and summaries.
- [~] Desktop viewport check around 1280px. Responsive wrapping/truncation CSS is implemented and dashboard build passes; manual browser viewport QA remains.
- [~] Tablet viewport check around 768px. Responsive wrapping/truncation CSS is implemented and dashboard build passes; manual browser viewport QA remains.
- [~] Mobile viewport check around 390px. Responsive wrapping/truncation CSS is implemented and dashboard build passes; manual browser viewport QA remains.
- [~] Long labels, empty states, loading states, and errors are checked. Defensive wrapping/ellipsis styles are implemented for tool policy, trace, alert, and chat status surfaces; manual browser QA remains.
- [x] i18n-ready string handling is documented or implemented according to dashboard baseline. README documents the current English-only dashboard baseline and the stable-code boundary for future localization; full `i18next` runtime work remains outside the tool-calls feature scope.

### SDK Tests

- [x] C# SDK settings round-trip with `ToolPolicy`.
- [x] JS SDK settings round-trip with `ToolPolicy`.
- [x] Python SDK settings round-trip with `ToolPolicy`.
- [x] C# SDK local contract harness passes with tool-policy coverage.
- [x] JS SDK local contract harness passes with tool-policy coverage.
- [x] Python SDK local contract harness passes with tool-policy coverage.
- [x] C# SDK tool trace enumeration. List/get/delete/bulk-delete methods and model are implemented, README examples are updated, and local SDK contracts verify route shape and response parsing.
- [x] JS SDK tool trace enumeration. List/get/delete/bulk-delete methods and types build with `npm.cmd run build`, README examples are updated, and local SDK contracts verify route shape and response parsing.
- [x] Python SDK tool trace enumeration. Sync/async list/get/delete/bulk-delete methods and casing-compatible delete result model are implemented, README examples are updated, and local SDK contracts verify route shape and response parsing.
- [x] Sync and async Python paths covered. Local SDK contracts verify sync and async assistant tool-call trace list/get/delete/bulk-delete route helpers.

### Integration and Docker Tests

- [~] Local Docker stack starts with tool calls disabled. Factory defaults and model-suite coverage verify disabled-by-default policy; Docker startup validation remains environment-specific.
- [~] Local Docker stack starts with disabled Tavily placeholder. Factory and runtime Docker server JSON configuration plus docs include disabled placeholder settings; Docker startup validation remains environment-specific.
- [~] Tavily env-var expansion works when `TAVILY_API_KEY` is supplied. Client/config-level expansion is implemented and tested for endpoint and API-key values, and both Docker server JSON files contain `${TAVILY_API_KEY}`; Docker/server JSON end-to-end validation remains.
- [~] Upload or crawl sample documents into a collection. Upload/crawler ingestion paths and collection tool policy contracts are implemented; live Docker/sample-data validation remains.
- [~] Enable collection tools for an assistant. Assistant policy persistence, API, dashboard controls, SDKs, and executor registry are implemented; live Docker validation remains.
- [~] Ask a question requiring model-directed search and chunk read. Tool-enabled chat orchestration, progress events, and service coverage are implemented; live provider/Docker validation remains.
- [~] Enable Verbex tool and ask for exact phrase lookup. Verbex tool policy, executor routing, and docs are implemented; live Verbex/Docker validation remains.
- [~] Enable S3 object read and ask for source-file excerpt. Document-backed S3 object read policy/executor behavior is implemented; live S3/Docker validation remains.
- [~] Enable web search and ask for current public information. Tavily configuration, assistant policy, executor behavior, SDKs, and docs are implemented; live Tavily/Docker validation remains.
- [~] Validate request history, chat history, citations, and tool-call records. In-process SQLite HTTP coverage validates persisted tool-call records and delete behavior; full chat/request-history/citation Docker workflow remains open.

## Release Checklist

- [x] All implementation checklist sections completed or explicitly deferred. Remaining open rows are manual/live validation or explicit product decisions for future behavior.
- [!] All provider migrations committed. Startup migration code is implemented for SQLite, PostgreSQL, MySQL, and SQL Server; creating the final release commit remains a human/release-process action.
- [x] OpenAPI regenerated.
- [x] Postman collection updated.
- [x] REST docs updated.
- [x] MCP docs updated.
- [x] README updated.
- [x] TESTING updated.
- [x] CHANGELOG updated.
- [x] Thorough README, CHANGELOG, REST_API, MCP_API, and Postman parity pass incorporated; manual API Explorer/browser/provider validation is tracked in the QA checklist.
- [x] SDKs updated and versioned. Effective tool-list, validation, tool-call trace list/get/delete/bulk-delete, external-search status helpers, and `ChatCompletionToolTrace` SDK models are updated and build/local contracts pass; live server SDK validation is tracked in the QA checklist.
- [x] Docker factory settings updated.
- [x] Secrets redaction reviewed. Persisted tool arguments redact common secret fields, output storage uses summaries, model-visible failed tool outputs use generic text plus stable codes, and public progress events omit raw arguments, raw outputs, object keys, provider request IDs, and hidden policy details. Manual browser/Slack trace review remains tracked in the QA checklist.
- [x] Tool policy default disabled verified on fresh install. Model-suite coverage verifies null/default assistant settings keep `EnableToolCalls=false`.
- [~] Upgrade from previous settings database verified. Startup migration code and local SQLite/in-process coverage exercise missing tool-policy and tool-call trace schema paths; release upgrade validation against a real previous deployment database remains.
- [x] Build succeeds with no warnings introduced. `dotnet build src\AssistantHub.sln /p:UseSharedCompilation=false` passes with 0 warnings after typed facade, analytics, provider-test, UTF-8, dashboard trace-summary, endpoint indicator, search-content policy, `DocumentsConsidered` metadata, collection work caps, generic model-visible tool-error, trace-route authorization, trace pagination, and SDK route-test additions; dashboard build passes with existing Vite warnings about `/config.js` and chunk size.
- [x] `run-tests.ps1` or equivalent passes. Focused v0.16.0 verification passes with `ASSISTANTHUB_TEST_SUITES=model,service,api dotnet run --project src\Test.Automated\Test.Automated.csproj --no-build`, covering tool-policy normalization, OpenAPI examples, collection cap behavior, exhaustive timeout failure, and model-visible tool-limit denial output.
- [x] Dashboard build passes. `npm.cmd run build` passes in `dashboard` with existing Vite warnings about `/config.js` and chunk size.
- [~] Manual QA completed for desktop, tablet, and mobile. Responsive implementation and build verification are complete; human browser QA remains.

## Acceptance Criteria

- [x] An administrator can enable or disable tool calls per assistant. Assistant settings accepts typed/JSON policy, dashboard exposes the master switch, and model/service/API coverage verifies disabled defaults and policy updates.
- [x] An administrator can choose exactly which server-side tools the model may request. Dashboard per-tool switches, `AllowedToolNames`, effective-tool routes, registry filtering, and executor revalidation are implemented with service/API coverage.
- [x] An administrator can set per-tool limits and policy constraints in assistant settings. Dashboard controls, typed policy models, SDK types, REST docs, validation, normalization, and service coverage cover collection, Verbex, S3, Tavily, output, timeout, and allow-list limits.
- [x] A model can search the assistant collection through `collection_search`. Tool schemas, executor dispatch, chat-loop orchestration, document/metadata filters, exhaustive/multi-query modes, server query variants, and service tests are implemented.
- [x] A model can read exact chunks through `collection_read_chunks`. Tool schemas, executor dispatch, document visibility checks, position/range expansion, neighbor limits, and service coverage are implemented.
- [x] A model can perform full-text search through Verbex using `verbex_full_text_search`. Tool schemas, mapped/default index resolution, record filters, post-filtering through assistant documents, safe output, and service coverage are implemented.
- [x] A model can read an entire or partial S3 object through `s3_object_read` within policy. Document-backed and explicit bucket-wide executor/schema paths, metadata/range reads, output caps, and shared chat-loop orchestration are implemented; live S3/Docker validation is tracked in the QA checklist.
- [x] A model can enumerate documents in a collection through `collection_enumerate_documents`. Tool schemas, executor dispatch, assistant collection/status/metadata/source filters, safe output, and service coverage are implemented.
- [x] A model can enumerate associated Verbex index records through `index_enumerate_records`. Tool schemas, mapped/default index resolution, record filters, post-filtering through assistant documents, safe output, and service coverage are implemented.
- [x] A model can enumerate associated S3 bucket objects through `bucket_enumerate_objects` when enabled. Executor/schema, prefix-policy enforcement, redacted keys, mapped-document output, shared chat-loop orchestration, and service-suite coverage are implemented; live S3/Docker validation is tracked in the QA checklist.
- [x] A model can search the web through Tavily using `web_search` when globally configured and assistant-enabled. Tavily settings, assistant overrides, effective-tool gating, executor limits, fake-provider coverage, and SDK/REST docs are implemented; live Tavily validation remains external.
- [x] Public chat users cannot broaden tool permissions. Public chat DTOs expose no tool-policy fields, policy is loaded server-side from assistant settings, and executor revalidates every tool request against effective policy.
- [x] Model-supplied tenant, collection, index, bucket, or object identifiers cannot bypass server policy. Executor ignores or validates model-supplied scope identifiers against assistant tenant/collection/index/bucket policy, with service coverage for cross-tenant, cross-collection, cross-index, document-backed S3, bucket-prefix, web-domain, and disabled-tool cases.
- [x] Tool calls are audited, redacted, linked to chat history, and visible to administrators. Records are redacted, request/chat linked, available through admin REST APIs, visible in dashboard history/request detail panels, and covered by in-process SQLite HTTP trace tests; full live audit workflow validation is tracked in the QA checklist.
- [x] Browser chat gives users safe real-time feedback while server-side tool calls are running. Client support, backend SSE event emission, and Slack-safe lifecycle messages are implemented and covered locally; live browser/Slack validation is tracked in the QA checklist.
- [x] Tool feedback never exposes raw tool outputs, secrets, hidden policy, raw S3 object keys, or internal provider identifiers to public chat users. Server progress events and Slack messages use safe labels/status/result counts only; manual browser/Slack trace review is tracked in the QA checklist.
- [x] Existing assistants remain disabled for tool calls after upgrade. Null/absent `ToolPolicyJson` defaults to a disabled typed policy.
- [x] Existing non-tool chat behavior remains unchanged when tool calls are disabled. Service-suite coverage verifies standard RAG/non-tool inference omits tool schemas and does not invoke tool execution.
