# AssistantHub Chat Data Flow

This document describes the current public assistant chat path for v0.17.0. The archived historical version remains in `archive/CHAT_DATA_FLOW.md`.

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
- `Hybrid` (the default for new assistants)

The retrieval stages run in this order:

1. **Retrieval gate** (`EnableRetrievalGate`): on turns after the first, a utility model decides whether new retrieval is needed (`RETRIEVE`) or the conversation already answers it (`SKIP`). A message that references attached documents always retrieves.
2. **Conversation rewrite** (`EnableConversationRewrite`): when there is earlier conversation, a utility model rewrites the latest message into a standalone question from the last six turns, replying with `{"query": "..."}`. A rewrite that differs from the message is searched alongside it at full weight and returned as `retrieval.conversation_rewrite`.
3. **Query rewrite** (`EnableQueryRewrite`): a utility model produces up to three variants of the message (or of the conversation rewrite, when one ran). The rewrite is additive: the original message is always searched at full weight, and the variants are fused with it at weight 0.5, so they re-rank results rather than displace them.
4. **Search and fusion**: each query is searched, and several queries are fused with weighted reciprocal rank fusion. When re-ranking is on, each search fetches a larger candidate pool, `max(RetrievalTopK, RerankCandidateCount)`.
5. **Attachment filter**: candidates outside `attached_document_ids` are removed.
6. **Supersession** (`SupersessionMode`): candidates from a document another document supersedes are handled. `Demote` (the default) drops each such chunk and puts the best-matching chunk of the newest document in the supersedes chain in its place, found with a search scoped to that document (at most one per replacement, and none when the replacement is already present or is not `Completed`). `Hide` drops them. `Include` keeps them with `superseded_by`. Chains are followed up to 5 hops, and the count is reported as `retrieval.superseded_chunks`.
7. **Re-ranking** (`EnableReranking`): `RerankerType` `CrossEncoder` scores the candidates 0-1 with a reranker from the server's `Rerankers` settings (`RerankEndpointId`); `Llm` scores them 0-10 with the rerank completion endpoint and drops those below `RerankerScoreThreshold`. Either keeps the top `RerankerTopK`. When re-ranking produces no scores, the retrieval order is kept and only the top `RetrievalTopK` candidates are injected. A reranker skipped by its circuit breaker, or a cross-encoder that is not configured, sets `retrieval.rerank_skipped`.
8. **Minimum score** (`RerankMinScore`, cross-encoder only): when every candidate scores below it, no context is injected, the system prompt tells the model that nothing relevant was found and to say so instead of guessing, and `retrieval.no_relevant_context` is set.
9. **Answerability check** (`EnableAnswerabilityCheck`), when enabled.

The gate, rewrite and LLM re-rank steps run at temperature 0 and are bounded by `Inference.UtilityTimeoutMs`. Each step, per endpoint, and each cross-encoder reranker has a circuit breaker: after `Inference.CircuitBreakerFailures` consecutive failures it is skipped for `Inference.CircuitBreakerOpenMs`. A step that times out, fails or is skipped falls back to its default: retrieve, search the original message only, or keep the retrieval order.

The query is embedded through Partio's `/v1.0/embed` route, so it is embedded whole rather than chunked. Each attempt is bounded by `Chunking.QueryEmbeddingTimeoutMs`, and transient statuses are retried. Embeddings are cached in process by endpoint and query text (`Chunking.QueryEmbeddingCacheSize`). If the embedding still fails, vector search returns nothing, and hybrid search runs its full-text leg alone and reports `keyword_fallback_ran`.

Hybrid search sends the assistant's fusion settings to RecallDB explicitly: `FusionStrategy`, `RrfK`, `FusionCandidatePool` and `RecencyWeight`. `RetrievalScoreThreshold` is a vector-similarity threshold. In hybrid mode it drops only chunks the vector leg found on its own with a similarity below it, and keeps every chunk the full-text leg found; full-text search is filtered by `FullTextMinimumScore` instead. Vector and hybrid searches that carry a label, tag or document filter raise HNSW `ef_search` to `RecallDb.FilteredEfSearch`.

Assistant-level retrieval label/tag filters and request-level `metadata_filter` are merged before search.

When `attached_document_ids` is present, every RecallDB search receives the document filter. This applies to:

- Single-query retrieval
- Multi-query retrieval from query rewrite
- Hybrid fallback retrieval

Attached documents narrow retrieval scope. They do not request whole-document summarization and do not grant object-storage access.

The same retrieval stages (gate, conversation rewrite, query rewrite, search and fusion, attachment filter, supersession, rerank) back `POST /v1.0/assistants/{assistantId}/retrieve`, an admin route that runs them without final inference and returns each stage's ranked list. It accepts `settings_override` to try unsaved settings, which the dashboard's Retrieval Inspector uses. The benchmark harness in `benchmarks/` measures retrieval through it.

## Utility LLM Steps

When enabled in assistant settings, the chat flow may run these utility model calls:

- Retrieval gate: decides whether a new retrieval is needed
- Conversation rewrite: turns a follow-up into a standalone query
- Query rewrite: produces additional retrieval queries
- Reranking: scores retrieved chunks before context injection (LLM reranking; cross-encoder reranking calls a rerank service instead of a completion endpoint)
- Answerability check: decides whether the retrieved context can answer the question
- Context compaction: compresses long conversations before final inference

Attached document state is not stored as a chat message and is not injected into title generation or feedback history by default.

## Final Inference

The server builds the final prompt from:

- Assistant system prompt
- Conversation messages
- Retrieved context, when present
- Citation instructions, when enabled

When citations are enabled, each context chunk is labeled with its citation index, source document name and, when known, its pages or sheet, for example `(Source: "guide.pdf", pp. 3-4)`. A chunk kept from a superseded document is also prefixed and labeled `outdated: superseded by "<replacement name>"`. Citation sources carry the same `page_start`, `page_end`, `sheet` and `superseded_by` values.

Retrieved context is injected in ranking order by default. With `ContextOrder` set to `ReadingOrder`, chunks are grouped by document (documents ordered by their best chunk), placed in document order, and chunks that touch or overlap are merged into one passage with repeated overlap text removed. The retrieve route always returns chunks in ranking order.

The answer call is bounded by `Inference.RequestTimeoutMs`. A transient failure (`408`, `429`, `502`, `503` or `504`) is retried up to `Inference.MaxRetries` times with jittered exponential backoff starting at `Inference.RetryDelayMs`; a streaming answer is retried only before any token has been sent. On non-streaming chat, a cited answer that is degenerate (fewer than four words once citation markers are removed, or only a lead-in such as "According to the sources") is regenerated once with the same context but without the citation instructions, and `retrieval.answer_regenerated` is set when the regenerated answer is used.

The response may be returned as plain JSON or SSE stream depending on assistant settings and route behavior. Existing SSE status-message handling remains compatible with future tool-progress events. The browser can render safe pending-tool status text, but backend model tool-loop orchestration and emitted tool-progress events are still tracked in `TOOL_CALLS.md`.

## Response Metadata

When retrieval runs, the chat response can include:

- `retrieval.collection_id`
- `retrieval.duration_ms`
- `retrieval.chunks_returned`
- `retrieval.attached_document_ids`
- `retrieval.attached_documents`
- `retrieval.document_filter_applied`
- `retrieval.hybrid_fallback_ran`, `retrieval.embedding_failed`, `retrieval.keyword_fallback_ran`, `retrieval.rerank_parse_failed`, `retrieval.answerability_parse_failed`, `retrieval.query_count`
- `retrieval.conversation_rewrite`, `retrieval.reranker`, `retrieval.rerank_skipped`, `retrieval.no_relevant_context`, `retrieval.superseded_chunks`, `retrieval.answer_regenerated`
- `retrieval.chunks`, each with `page_start`, `page_end`, `sheet`, `section` and `superseded_by` when known

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
