# Changelog

## Unreleased

### Added
- **Cross-encoder reranking**: assistants can set `RerankerType` to `CrossEncoder` (default `Llm`) and `RerankEndpointId` to a reranker from the new server `Rerankers` settings (`Id`, `Name`, `Format` `Tei` or `Cohere`, `Endpoint`, `Model`, `ApiKey`, `TimeoutMs` default 10000, `MaxPassageCharacters` default 2000). `ApiKey` is redacted by `GET /v1.0/configuration` and preserved when `PUT` sends it back redacted. `RerankCandidateCount` (1-200, default 20) sets how many candidates are retrieved and scored before `RerankerTopK` are kept, and `RerankMinScore` (0-1, optional) injects no context and tells the answer model that nothing relevant was found when every candidate scores below it (`no_relevant_context`). Saving settings with a cross-encoder that is not configured returns 400. New routes `GET /v1.0/rerankers` and `POST /v1.0/rerankers/{rerankerId}/test` (admin or tenant admin). The benchmark stack runs TEI with `cross-encoder/ms-marco-MiniLM-L-6-v2`.
- **Conversation rewrite (`EnableConversationRewrite`, `ConversationRewritePrompt`)**: a follow-up question is rewritten into a standalone query from the last six turns and searched alongside the original message. A custom prompt must contain `{question}`; `{conversation}` is optional; the model replies with `{"query": "..."}`. Reported as `conversation_rewrite`.
- **Document supersession**: documents carry `Supersedes` and `SupersededBy`. Links are set at upload with `SupersedesDocumentIds` or with the new `PUT /v1.0/documents/{documentId}/supersedes` route (400 on unknown IDs, self-reference or a cycle), and are released when a document is deleted. The assistant setting `SupersessionMode` controls retrieval: `Demote` (default) swaps each superseded chunk for the best-matching chunk of the newest replacement, `Hide` drops it, and `Include` keeps it marked as outdated in the prompt. Chat and retrieve report `superseded_chunks`, and the retrieve route records a `supersession` stage. The dashboard shows supersession and duplicate badges and edits links from the documents view.
- **Duplicate detection**: documents record `ContentSha256` and `NearDuplicates`. The ingestion rule's `Extraction.DuplicatePolicy` (`Allow`, `Warn` or `Reject`) handles exact duplicates at upload (`Reject` returns 409), and after ingestion other documents in the collection at or above `Extraction.NearDuplicateThreshold` (default 0.85) are recorded as near-duplicates.
- **Ingestion rule `Extraction` settings**: `OcrEmbeddedImages`, `CsvHasHeaderRow`, `CsvRowsPerAtom`, `ExcelHeaderRowScoreThreshold`, `DuplicatePolicy` and `NearDuplicateThreshold`, passed to DocumentAtom or applied at intake. Replaces the unused `Atomization` field (stored in the same column).
- **Structured chunking (`Chunking.CellMode` `Structured`)**: each section's text, each table and each list is sent to Partio as its own cell in one `/v1.0/process/batch` call, with `TableStrategy` (default `RowGroupWithHeaders`) and `ListStrategy` (default `WholeList`), so chunks never span sections. `ContextHeader` (`None`, `Title`, `TitleAndHeadings`) embeds the document name and heading path with each chunk without changing its stored text.
- **Page, sheet and section provenance**: chunks from structured ingestion carry `page_start`, `page_end`, `sheet` and `section`, returned on retrieved chunks and (except `section`) on citation sources. Citation labels in the prompt include the pages or sheet, for example `pp. 3-4`.
- **Embedding task prefixes**: `Embedding.TaskPrefixes` on ingestion rules prepends the model's document prefix (for example `search_document: ` for nomic-embed-text, `passage: ` for E5) and the assistant setting `EmbeddingTaskPrefixes` prepends the matching query prefix.
- **`settings_override` on `POST /v1.0/assistants/{assistantId}/retrieve`**: runs retrieval with unsaved assistant settings. The dashboard's new Retrieval Inspector uses it.
- **`EvalJudgeInferenceEndpointId`**: optional completion endpoint that judges eval runs instead of the assistant's own endpoint.
- **Answer-model retries and utility-step protection**: new `Inference` settings `RequestTimeoutMs` (300000), `UtilityTimeoutMs` (30000), `MaxRetries` (2), `RetryDelayMs` (500), `CircuitBreakerFailures` (3) and `CircuitBreakerOpenMs` (30000). The answer call retries transient 408/429/502/503/504 failures (streaming only before any token is sent). The retrieval gate, rewrites and LLM rerank are bounded by `UtilityTimeoutMs` and skipped by a per-step, per-endpoint circuit breaker after repeated failures (`rerank_skipped` when reranking is skipped).
- **Degenerate cited answers are regenerated**: a non-streaming cited answer that is only citation markers or a few words is regenerated once without the citation instructions (`answer_regenerated`).
- **TSV files** are extracted as tab-delimited tables.
- **New chat and retrieve fields**: `conversation_rewrite`, `reranker` (`llm` or `cross_encoder`), `rerank_skipped`, `no_relevant_context`, `superseded_chunks`, and on chat `answer_regenerated`.
- The new assistant settings and document columns are added by the v0.17.0 migrations for all four database providers, and the new fields and routes are in the REST/OpenAPI docs, the Postman collection and the C#/TypeScript/Python SDKs.
- **Hybrid fusion settings**: assistants now carry `FusionStrategy` (`Rrf` or `Linear`), `RrfK`, `FusionCandidatePool` and `RecencyWeight`, and hybrid search sends them to RecallDB as an explicit `Hybrid` block instead of relying on the store's defaults. Defaults (`Rrf`, k 60, store pool, no recency) match what ran before. Exposed in the dashboard's hybrid settings, REST/OpenAPI docs, database migrations for all four providers, and the C#/TypeScript/Python SDK models. The `collection_search` tool uses the assistant's fusion settings too.
- **Prompt context order (`ContextOrder`)**: `ReadingOrder` groups retrieved chunks by document (documents ordered by their best chunk), puts them in document order, and merges touching or overlapping chunks, removing repeated overlap text. `Score` (the default) keeps ranking order. Applies to every chat path; the retrieve route keeps ranking order.
- **Query-embedding cache**: query embeddings are cached in process by endpoint and query text (`Chunking.QueryEmbeddingCacheSize`, default 10000, 0 disables), with a `retrieval.query_embedding.cache` hit/miss metric.
- **`RecallDb.FilteredEfSearch`** (default 400): vector and hybrid searches that carry a label, tag or document filter raise HNSW `ef_search`, because pgvector applies filters after the index scan and a restrictive filter could otherwise return a short page.
- **Verbex index options**: `Verbex.EnableLemmatizer`, `EnableStopWordRemover`, `MinTokenLength` and `MaxTokenLength` are applied to indexes AssistantHub creates (ingestion and tenant provisioning). All default to off. The `verbex_full_text_search` tool accepts `labels`.
- **`keyword_fallback_ran`** on chat and retrieve responses: set when a hybrid search could not embed the query and ran its full-text leg alone.
- **`INGEST_AND_RETRIEVAL_IMPROVEMENTS.md`** (archived to `archive/` after round 5): improvement plan drawn from Pneuma, Isis and the services AssistantHub depends on, scored by integration simplicity and value.
- **Run ledger for benchmarks**: every benchmark report is summarized into `benchmarks/history/runs.jsonl` with a configuration fingerprint; `history` shows each metric's change against the previous run of the same configuration, and `compare --baseline previous` finds that run's report.
- **Retrieval-only diagnostics route and benchmark suite**: `POST /v1.0/assistants/{assistantId}/retrieve` runs an assistant's retrieval stages (gate, rewrite, search and fusion, attachment filter, rerank, and optionally the answerability check) without final inference, and returns every stage's ranked chunks. Chat and the route share one stage implementation (`AssistantChatService.RunRetrievalStagesAsync`). Retrieved chunks now carry `vector_score`, `vector_rank` and `text_rank` from RecallDB. Chat and retrieve responses flag `hybrid_fallback_ran`, `embedding_failed`, `rerank_parse_failed` and `answerability_parse_failed`. Added the black-box benchmark harness `src/Test.Benchmark`, the isolated stack, datasets, and docs under `benchmarks/`, plus `BENCHMARKING.md` and `RETRIEVAL_IMPROVEMENTS.md` (archived as `archive/RETRIEVAL_IMPROVEMENTS_2026-09.md` after round 5).
- **`Chunking.RequestTimeoutMs`** (default 900000): per-request timeout for Partio calls.
- **Per-endpoint request queueing passthrough (`MaxQueueDepth`)**: Embedding and completion endpoints now expose a `MaxQueueDepth` field that is forwarded to Partio alongside `MaxConcurrentRequests` and `MaximumTimeoutMs`. `0` (the default) keeps today's behavior -- requests over the concurrency limit are rejected immediately with `429`; a positive value lets that many requests queue for a slot before Partio returns `429`, and a queued request that waits past `MaximumTimeoutMs` returns `504`. Added to the `PartioEndpointConfig`/`PartioEndpointRequest` models, the embedding and inference endpoint dashboard modals, the C#/TypeScript/Python SDK endpoint models, and the REST/MCP documentation. The endpoint proxy re-serializes request bodies through the typed model, so the field is only forwarded because it is now a first-class property.
- **Transient-failure retries for Partio calls**: Document ingestion now retries the Partio `/v1.0/process` and `/v1.0/embed` calls on transient HTTP failures (`408`, `429`, `502`, `503`, `504`) with exponential backoff, so bursts that trip Partio v0.5.0's per-endpoint concurrency/queue limits no longer fail documents outright. Controlled by new `Chunking.MaxRetries` (default `3`) and `Chunking.RetryDelayMs` (default `1000`) settings.

- **OpenAPI metadata on every route**: All 196 REST routes now carry Watson OpenAPI route metadata (`src/AssistantHub.Server/OpenApi/*ApiDocs.cs`), passed at registration with `openApiMetadata:`. Each operation documents a summary, description, tag, query and header parameters, a realistic request example, response structures derived from the real model types (emitted once under `components/schemas` and referenced with `$ref`, with descriptions taken from the XML documentation), response examples, and error responses. The runtime `/openapi.json` renders this metadata; routes without metadata keep the previous generated fallback. New API tests require every route registration to carry metadata, validate each operation's examples and schemas, cover the builder's rejection of incomplete operations, and check the runtime rendering and the committed `openapi.json`.

### Changed
- **Query rewrite is additive**: the original query is always searched at full weight, and up to three rewritten variants are fused with it at weight 0.5, so a poor rewrite can no longer displace the original results. Utility steps (gate, rewrites, LLM rerank) run at temperature 0.
- **Retrieval defaults for new assistants**: `SearchMode` `Hybrid` (was `Vector`), `TextWeight` 0.5 (was 0.3), `RetrievalIncludeNeighbors` 1 (was 0) and `RerankerTopK` 10 (was 5). Stored settings keep their values.
- **Default chunking strategy is `ParagraphBased`** (was `FixedTokenCount`) for new ingestion rules.
- **Reprocessing removes the previous chunk records** after the new ones are stored, instead of leaving them in the collection.
- **Admin API key tenant resolution**: the admin API key, which has no tenant, now acts on the `tenantId` query parameter or the default tenant on list and create routes and on the collection routes, instead of an empty tenant.
- **Shipped Partio configs queue requests**: the embedding endpoint allows 4 concurrent requests (was 2) with `MaxQueueDepth` 16, and the completion endpoint `MaxQueueDepth` 16, so bursts wait for a slot instead of failing with 429.
- **`RetrievalScoreThreshold` is a vector-similarity threshold in every mode**: in `Hybrid` mode it now drops only chunks the vector leg found on its own with a similarity below it, and keeps every chunk the full-text leg found (previously the fused score was compared, which kept at most the top full-text-only chunk, since such a chunk scores at most `TextWeight`). `FullText` results are no longer held to it; `FullTextMinimumScore` is their cutoff. A `collection_search` tool call that passes an explicit `score_threshold` still applies it to full-text scores.
- **Hybrid search falls back to full-text when the query embedding fails**, instead of returning no context.
- **Extracted text keeps block and heading structure**: DocumentAtom blocks are separated by a blank line (so `ParagraphBased` chunking finds paragraphs) and headings are rendered as markdown headings using the atom's `HeaderLevel`. Affects newly ingested or reprocessed documents; reprocess existing documents to pick it up.
- **Invalid assistant settings return 400**: an out-of-range value (for example `RrfK` 0 or an unknown `FusionStrategy`) now returns `400 BadRequest` with the reason instead of `500`.
- **Voltaic 2.0.0 for the MCP server**: `AssistantHub.McpServer` and the test harness moved from Voltaic 0.6.1 to 2.0.0. `tools/list` now returns only AssistantHub tools (Voltaic's `ping`/`echo`/`getTime`/`getSessions` demo tools are gone; `getSessions` exposed other clients' session IDs). `ping` returns `{}` per the MCP specification. HTTP tools are callable only through `tools/call` with input-schema validation; bare JSON-RPC calls to a tool name now return `-32601`. TCP and WebSocket keep their bare JSON-RPC methods. Stateless `2026-07-28` clients such as Claude Code 2.1.x now see the full tool list on `/rpc`. The MCP test suite calls tools through `tools/call` and adds positive and negative protocol cases (ping shape, tools/list contents, bare-method rejection, removed demo tools, missing required arguments, TCP methods).
- **NuGet dependencies updated**: AWSSDK.S3 4.0.103.4, Blobject.* 5.1.0, Microsoft.Data.Sqlite 10.0.12, Microsoft.Data.SqlClient 7.1.0, Watson 7.2.0, System.Text.Json 10.0.12 (C# SDK), Microsoft.NET.Test.Sdk 18.10.1, NUnit3TestAdapter 6.3.0.

### Fixed
- **Authenticated web crawls sent no credentials**: CrawlSharp only authenticates when `Authentication.Type` is set, and the web crawler filled in the username and password, API key or bearer token without it. The type now follows the crawl plan's `AuthenticationType`.
- **Web crawls ignored the crawl delay**: `CrawlDelayMs` was mapped to CrawlSharp's `ThrottleMs`, which is only the backoff after a 429 response, so every crawl ran at CrawlSharp's 2.5-second default between requests. It now sets `RequestDelayMs`.
- **A redirect loop hung a web crawl**: CrawlSharp's own redirect following never ends on a cycle (A → B → A). It is now off; the HTTP client still follows ordinary redirects (the page is listed under the linking address with the target's content) and a cycle ends as an error page. Known limitation until CrawlSharp is fixed: a redirect target on an authenticated site is requested without credentials, because .NET drops the `Authorization` header when following a redirect. `WebCrawlRepositorySettings.FollowRedirects` is informational for now.
- **Supersession overrode an explicit scope**: a question scoped to an older version (an attached document, or a metadata filter the replacement does not match) had that version replaced or hidden. Such chunks are now kept and marked outdated.
- **Legacy Office formats failed without a useful error**: `.doc`, `.xls` and `.ppt` files were sent to DocumentAtom's OpenXML processors. They are now rejected with a status message asking for `.docx`, `.xlsx` or `.pptx`.
- **C# SDK `RetrievalChunk` never deserialized**: its JSON property names were PascalCase while the server sends snake_case (`document_id`, `score`, `rerank_score`, ...). They are now snake_case, and `Position` is `int?`.
- **Query embedding could hold a chat request for minutes**: query embedding used the ingestion timeout (`Chunking.RequestTimeoutMs`, 15 minutes), so a stalled embedding model stalled retrieval; benchmark runs against a load-shedding model proxy sat for 50 minutes. Query embedding now has its own `Chunking.QueryEmbeddingTimeoutMs` (default 30000) and a timed-out attempt is not retried.
- **Long queries were silently truncated before embedding**: queries were embedded through Partio's chunking route and only the first chunk (256 tokens by default) was used. Queries now go through `/v1.0/embed` and are embedded whole.
- **`verbex_full_text_search` ignored `required_terms` and `excluded_terms`**: they were sent as request fields Verbex does not have. The tool now resolves them with side queries and filters the results.
- **`TextWeight` documentation described a linear blend**, while hybrid search runs weighted reciprocal rank fusion. The settings, REST and dashboard descriptions now give the actual formula.
- **`openapi.json` coverage and validity**: The committed document was missing `GET /v1.0/analytics/ingestion`, `GET /v1.0/documents/{documentId}/performance`, and `POST /v1.0/documents/{documentId}/reprocess`. It had 53 path items nested inside `/v1.0/endpoints/embedding`, which made it invalid OpenAPI, and 439 unresolved `$ref`s. It was regenerated from the route metadata and merged with the curated schemas, and now passes OpenAPI 3.0 validation. It also corrects 34 documented success statuses (for example, creates return 201) and removes request bodies from three routes that read none. The three routes above were also added to `REST_API.md` and the Postman collection.
- **Large documents failed ingestion after 100 seconds**: Partio calls used `HttpClient`'s default 100-second timeout, so chunking and embedding any large document (for example the 200 KB `REST_API.md`) was cancelled. Found by the benchmark suite. The timeout is now `Chunking.RequestTimeoutMs`.
- **Query embedding rejected under concurrency returned no context**: A `429` from Partio's embedding endpoint (default concurrency 2, queue depth 0) made retrieval return nothing, and chat silently answered without context. At 4 concurrent requests this hit more than half of all queries in the benchmark. Query embedding now retries transient statuses (408/429/502/503/504) with short, jittered backoff, and reports `embedding_failed` when retries are exhausted.
- **Single attached document did not scope retrieval**: For one document id, retrieval sent RecallDB a `DocumentId` property it does not recognise, so the search ran across the whole collection and the attachment safety filter then removed every out-of-scope chunk. Attached-document chat often got no context at all: 40% of Qasper questions, each scoped to one paper. Retrieval now always sends `DocumentIds`. Found by the benchmark suite.
- **Upstream model overload reported by Partio as HTTP 500 was not retried**: When an embedding provider behind Partio shed load with `429`, Partio returned `500 InternalError` wrapping it, so document ingestion failed permanently (up to 5% of documents against a load-shedding model proxy) and query embedding gave up. Ingestion and query-embedding retries now treat a Partio `500` whose body reports an upstream 429/502/503/504 as transient.
- **Platform-dependent line endings in extracted text**: DocumentAtom text reassembly used `Environment.NewLine`, so extracted text had CRLF on Windows hosts and LF on Linux. It now always uses `\n`.
- **Single-chunk embedding against Partio v0.5.0**: The `Chunking.Strategy = "None"` ingestion path called Partio's standalone `/v1.0/embed` with the pre-v0.4.0 request/response shape (`Text`/`EmbeddingEndpointId` and a single `Embeddings` vector). It now sends the current `{ EndpointId, Input[], L2Normalization }` body, reads the batched `EmbedResponse` (one vector per input, plus `Success`/`Error`), and honors a body-level failure -- fixing empty or missing single-chunk embeddings.

## 0.16.0

### Added
- **OpenTelemetry metrics and tracing**: Instrumented the REST API (all routes), the MCP server (all tools across HTTP/TCP/WebSocket), and the application/service layer (inference, retrieval, ingestion, storage, chat, crawl, eval, auth) with `System.Diagnostics.Metrics.Meter` and `System.Diagnostics.ActivitySource` under a single `AssistantHub` meter/activity source, exported over OTLP via the Radiant telemetry host. Added a `Telemetry` settings section (with `ASSISTANTHUB_TELEMETRY_ENABLED`/`ASSISTANTHUB_OTLP_ENDPOINT` env overrides), a docker observability stack (OpenTelemetry Collector, Prometheus, Tempo, Loki, Grafana) with a pre-provisioned `AssistantHub` Grafana dashboard folder, an Observability links section on the dashboard Configuration page, and `TELEMETRY.md`. The published `AssistantHub.Core` package stays exporter-free (BCL emit only).
- **CIFS and NFS crawler support**: Added CIFS/SMB and NFS repository types, file-server repository settings mapped from View's `DataRepository`, shared Blobject-backed crawler infrastructure, and lazy file-byte retrieval through `CrawlerBase`.
- **File-server crawler product coverage**: Added dashboard create/edit support, REST/OpenAPI/Postman examples, C#/TypeScript/Python SDK models and tests, and archived the implementation checklist at `archive/FILE_CRAWLERS.md` for v0.16.0.
- **Attached-document chat**: Added public assistant document listing, dashboard document selection, `attached_document_ids` chat requests, retrieval metadata for applied document filters, SDK/OpenAPI/Postman/REST coverage, and server validation that attached documents are completed records in the assistant tenant and collection.
- **Local chat file attachments**: Added dashboard file upload chips and `local_attachments` chat request support so users can attach files from their machines to a chat turn. The server decodes or text-extracts local files for prompt context without adding them to the assistant collection.
- **DocumentAtom extraction tool**: Added disabled-by-default `document_atom_extract` assistant tool support so models can request bounded text extraction from a completed assistant document or a per-turn local chat attachment.
- **Attached-document retrieval fallback**: Added `RecallDb.SupportsMultiDocumentFilter`; when disabled, AssistantHub logs a warning and loops over single-document RecallDB searches instead of sending a native `DocumentIds` filter.
- **Tool-call policy foundation**: Added disabled-by-default assistant tool policy settings, policy validation, admin dry-run diagnostics, effective-tool endpoints, dashboard controls, SDK/OpenAPI/Postman/MCP coverage, and initial server executors for collection search, collection document enumeration, Verbex full-text search, and Tavily web search.
- **Dedicated tool-routing endpoints**: Added optional `ToolRoutingInferenceEndpointId` assistant setting with dashboard selector, REST/Postman/SDK documentation, startup migrations, and provider scripts for SQLite, PostgreSQL, MySQL, and SQL Server. When configured, model tool-decision turns use the router endpoint while final answers use the response inference endpoint.
- **Assistant tool-call traces**: Added redacted `AssistantToolCallRecord` persistence for non-streaming tool-call chat, provider data access, retention pruning, chat/request-history linkage, admin REST routes including filtered bulk deletion, OpenAPI/Postman coverage, dashboard API client methods, and C#/TypeScript/Python SDK methods.
- **Streaming tool-call progress**: Added safe named SSE lifecycle and heartbeat events for tool-enabled streaming chat, optional safe `tool_calls` chat response metadata, dashboard stream parsing with interrupted-stream handling, OpenAPI/REST documentation, and SDK contract coverage.
- **Assistant thinking exposure setting**: Added default-off `ExposeThinking` assistant setting, provider parsing for Ollama/OpenAI-compatible thinking fields, gated REST/SSE chat response support, dashboard rendering, migrations, SDK/OpenAPI/Postman/docs, and tests.
- **Tool-call telemetry stage**: Added safe aggregate `tools` performance telemetry with call counts, duration totals, per-tool status counts, provider dimensions, truncation flags, and result counts when available.
- **Tool-call analytics diagnostics**: Assistant analytics slowest-request rows and the dashboard now show safe aggregate tool-call counts, failures, denials, truncation counts, slowest tool duration, and failing tool names for admin triage.
- **Structured tool-call errors**: Added stable model-visible, admin-trace, and tool-policy validation error codes for malformed arguments, policy denials, tool limits, provider configuration failures, provider HTTP errors, timeouts, cancellations, unavailable tool policies, and generic tool failures.
- **S3 tool-call safety**: Added aggregate per-turn model-visible S3 object byte enforcement with structured `object_byte_limit` errors, complementing existing per-call object read limits.
- **Provider usage telemetry**: OpenAI-compatible usage parsing now preserves optional reasoning-token and tool-definition-token counters and maps them into normalized assistant performance telemetry and SDK models.
- **Collection search query variants**: Added an opt-in `EnableServerGeneratedQueryVariants` assistant tool-policy flag for deterministic server-side collection-search variants that remain bounded by `MaxSearchQueriesPerCall`.
- **Collection search diagnostics**: Added safe `DocumentsConsidered` metadata to `collection_search` output so models and admins can distinguish visible document scope from raw result counts.
- **Collection search work caps**: Added assistant tool-policy caps for `MaxDocumentsConsideredPerSearch` and `MaxResultsConsideredPerSearch`; capped exhaustive searches now report explicit incomplete reasons while timed-out tool calls fail with `ErrorCode=timeout`.
- **Tool-derived citations**: Collection, Verbex, S3, and web tool outputs can now contribute citation sources when assistant citations are enabled; web citation sources include URLs and dashboard citation cards link them directly.
- **Tavily external search configuration**: Added server JSON settings for external search providers with Tavily-compatible defaults, environment-variable expansion for endpoint/API-key values, safe secret handling in committed examples, response redaction for provider API keys, startup status logging, and a redacted admin status route/SDK method.
- **Retrieval answerability diagnostics**: Added optional post-retrieval answerability classification, query-class telemetry, dropped-candidate summaries, final citation counts, chat-rail evaluation execution, dashboard controls, SDK/OpenAPI/Postman coverage, and provider migrations for retained chat history diagnostics.

### Changed
- **Product and package version**: Updated active product, Docker image, dashboard, SDK, OpenAPI, REST, and documentation metadata to `0.16.0`.
- **Crawl-plan model**: Repository settings now deserialize polymorphically for Web, CIFS, and NFS while preserving legacy web settings that omit `RepositoryType`.

### Fixed
- **Tool-routing continuation**: If a dedicated tool router stops but the final response model asks for another server-side tool, AssistantHub now executes that tool under policy and continues the loop instead of returning a diagnostic no-text response.

## v0.14.0 - Search, MCP, and Verbex

### Added
- **Verbex text search integration**: Added Verbex server/dashboard Docker services, PostgreSQL database provisioning, factory reset handling, settings/UI configuration, and first-run tenant/default-index provisioning.
- **Inverted-index artifact APIs**: Added AssistantHub REST, OpenAPI, Postman, SDK, and MCP coverage for Verbex indices, index records, and index search, with all requests marshaled through AssistantHub.
- **Document text indexing**: Added Verbex ingestion after DocumentAtom text extraction, including ingestion-rule/document label and tag propagation, stable document-backed record IDs, duplicate-record replacement, document delete garbage collection, and admin reindex/backfill APIs.
- **Search dashboard surfaces**: Added `ARTIFACTS > Indices`, `ARTIFACTS > Indices > Records`, `ARTIFACTS > Indices > Search`, and `ARTIFACTS > Collections > Search` for Verbex text search and RecallDB collection search.
- **Search dashboard completion**: Expanded index, index-record, Verbex search, and RecallDB search pages with metadata editing, filters, detail modals, top terms, score/timing display, empty/error states, and raw JSON inspection.
- **Verbex search result enrichment**: Updated `ARTIFACTS > Indices > Search` to request Verbex matched terms, per-term details, and document term statistics, then render unique terms, total term occurrences, matched query terms, and richer result metadata in the table and detail modal.
- **Search result UI polish**: Simplified the index-records table, aligned record/search result detail modals into three-column layouts, widened the index search result modal, and added copy-to-clipboard controls for JSON payloads in index and collection search result details.
- **RecallDB collection search proxy**: Added AssistantHub-marshaled RecallDB collection search APIs across REST, MCP, SDKs, OpenAPI, Postman, and dashboard.
- **External service contracts**: Added interface/implementation wrappers for Less3 object storage, DocumentAtom atomization, Partio chunking/endpoint management, RecallDB vector storage, Verbex inverted indexing, and aggregate subordinate service health checks.
- **AssistantHub MCP server**: Added standalone `AssistantHub.McpServer` with Voltaic HTTP, TCP, and WebSocket transports plus install support for Claude/Cursor MCP clients.
- **Endpoint model loading**: Added AssistantHub proxy routes and dashboard actions for loading or warming Partio-managed embedding and inference endpoint models, with a custom status modal for Partio's load result payload.
- **MCP management surface**: Added MCP tools for system/runtime inspection, authentication, tenants, users, credentials, buckets, bucket objects, collections, assistants, assistant settings, documents, ingestion rules, feedback, history, request history, endpoints, models, crawl plans, crawl operations, evaluation, and runtime configuration.
- **MCP integration coverage**: Added shared MCP host/test harness support and automated MCP transport, CRUD, configuration-redaction, request-history, and install-path coverage.
- **MCP docs and Docker assets**: Added `MCP_API.md`, `docs/CLAUDE_MCP.md`, `src/AssistantHub.McpServer/Dockerfile`, `build-mcp.bat`, and Docker compose/config assets for running the MCP server alongside the platform.
- **Docker status scripts**: Added `docker/status.bat` and `docker/status.sh` for a concise `docker ps -a` view of container ID, name, creation time, status, and ports.

### Changed
- **Product and package version**: Updated active product, Docker image, dashboard, SDK, OpenAPI, Postman, and documentation metadata to `0.14.0`.
- **Verbex Docker database**: Added Verbex to the shared PostgreSQL deployment model alongside AssistantHub, Less3, Partio, and RecallDB.
- **Document management**: Document deletion now also removes associated Verbex text-search records, and the dashboard exposes a completed-document reindex action for admin users.
- **Docker PostgreSQL defaults**: The local Docker deployment now defaults AssistantHub, Less3, Partio, RecallDB, and Verbex to a shared PostgreSQL/pgvector container with separate service databases and application roles, plus a one-shot `postgres-init` verifier before services start.
- **Archived PostgreSQL migration plan**: Moved the completed Docker PostgreSQL migration plan to `archive/POSTGRES_MIGRATION.md`.
- **SDK parity for MCP-backed routes**: C#, JavaScript, and Python SDKs now expose request-history APIs and align the eval judge-prompt/eval-results contracts used by the MCP server.
- **Testing documentation and runners**: Root test docs and wrappers document the Touchstone-backed `Test.Automated`, `Test.Xunit`, and `Test.Nunit` layout plus MCP-focused environment controls.

### Fixed
- **Verbex configuration validation**: Added validation for Verbex endpoint, access key when ingestion is enabled, default index ID, and dashboard URL.
- **Verbex large-document controls**: Added optional `Verbex.MaxContentCharacters` to cap normalized document text sent to Verbex per record; the default `0` keeps full-text indexing unlimited.
- **Request-history query parsing**: Request-history filter parsing now URL-decodes reserved characters before building filters, fixing MCP and REST summary/list queries that include encoded path or timestamp values.
- **Assistant analytics database portability**: Retained-chat analytics scoping now uses the active database driver's boolean formatting instead of a hard-coded literal, keeping the fix valid for SQLite, MySQL, PostgreSQL, and SQL Server.

## v0.13.0 - Assistant Analytics

### Added
- **Assistant Analytics dashboard**: Added `Assistants > Analytics` with assistant selector, independent last-hour/day/week/month chart ranges, overview metrics, request volume/outcome, latency percentiles, hot-path stage duration, provider timing, provider throughput, utility-call activity, retrieval fanout/chunk flow, endpoint/model usage, slowest requests, and feedback trend.
- **Assistant Analytics scoping**: Analytics now treats Assistant History as the ownership boundary and joins Request History only for supporting timing/status telemetry, so deleting assistant history removes those turns from analytics without deleting the request audit log.
- **Assistant analytics REST API**: Added authenticated `GET /v1.0/assistants/{assistantId}/analytics/overview`, `/timeseries`, `/stages`, `/endpoints`, `/slowest`, and `/feedback`.
- **Assistant-scoped telemetry indexing**: Added `assistant_id` to `chat_history_performance_events`, startup backfill, and assistant/time/stage/endpoint indexes for SQLite, PostgreSQL, MySQL, and SQL Server.
- **Migration scripts**: Added `migrations/011_upgrade_to_v0.13.0.*.sql` provider scripts for existing installations.
- **Archived implementation plan**: Moved the completed Assistant Analytics implementation plan to `archive/ASSISTANT_ANALYTICS.md`.
- **SDK/Postman/MCP coverage**: C#, JavaScript/TypeScript, Python, Postman, OpenAPI, and MCP now expose assistant analytics read APIs.
- **Analytics tests**: Added Touchstone shared coverage for analytics range capping, aggregation, endpoint summaries, slowest request detection, feedback analytics, and telemetry assistant ID projection.

### Changed
- Dashboard history and request-history filters can deep-link with `assistantId` so analytics drill-downs open directly to related entries.
- Docker compose image tags and package/product versions updated to `0.13.0`.

### Breaking
- Database schema changes require running the matching `migrations/011_upgrade_to_v0.13.0.*.sql` provider script for existing installations when startup migrations are not used.

## v0.12.0 - Assistant Performance Telemetry

### Added
- **Provider-agnostic chat telemetry**: Chat history now captures `TraceId`, `RequestHistoryId`, `PerformanceSchemaVersion`, and `PerformanceJson` with versioned assistant hot-path telemetry.
- **Queryable performance events**: Added `chat_history_performance_events` table and data access implementations for SQLite, PostgreSQL, MySQL, and SQL Server.
- **Request/history correlation**: Request history now stores `TraceId` and `ChatHistoryId` so HTTP request details can be correlated with assistant chat history and logs.
- **Inference timing detail**: Final inference telemetry captures endpoint limiter wait, request-to-headers, headers-to-first-token, first-token-to-last-token, total client time, HTTP status, endpoint/model metadata, token counts, and provider-native metrics when available.
- **Dashboard performance drill-down**: Assistant history details and request-history details show expanded stage timing, token, endpoint, and provider metric tables.
- **Migration scripts**: Added `migrations/010_upgrade_to_v0.12.0.*.sql` provider scripts for existing installations.

### Changed
- Chat history writes now persist telemetry and normalized performance events as part of the hot path instead of dropping timing detail after response completion.
- Request-history capture preserves preassigned request IDs and trace IDs for assistant chat requests.
- C#, JavaScript, and Python SDKs include the new history correlation fields and assistant telemetry DTOs.
- Docker compose image tags and package/product versions updated to `0.12.0`.

### Breaking
- Database schema changes require running the matching `migrations/010_upgrade_to_v0.12.0.*.sql` provider script for existing installations when startup migrations are not used.

## v0.11.0 - Assistant Utility Endpoint Routing

### Added
- **Specialized assistant inference endpoints**: Assistant settings now include optional `RetrievalGateInferenceEndpointId`, `QueryRewriteInferenceEndpointId`, and `RerankInferenceEndpointId` fields for routing RAG utility LLM calls to dedicated completion endpoints.
- **Dashboard endpoint selectors**: Assistant Settings exposes dropdowns for retrieval gate, query rewrite, and re-rank endpoints alongside the required response inference endpoint.
- **Schema migration**: Added startup migrations and standalone provider migration scripts for SQLite, PostgreSQL, MySQL, and SQL Server for the new assistant settings columns.

### Changed
- Retrieval gate, query rewrite, and re-ranking now honor their dedicated endpoint settings while falling back to `InferenceEndpointId` when unset.
- C#, JavaScript, and Python SDK models include the new assistant settings fields.
- .NET test projects now use Touchstone NuGet packages with shared suites, a console runner, xUnit adapter, and NUnit adapter.
- OpenAPI, Postman, REST API docs, and package/product versions updated to `0.11.0`.

### Breaking
- Database schema changes require running the matching `migrations/009_upgrade_to_v0.11.0.*.sql` provider script for existing installations when startup migrations are not used.

## v0.10.0 - API Explorer And Request History

### Added
- **HTTP request history subsystem**: AssistantHub now captures searchable request and response metadata for system APIs and assistant-facing APIs, including headers, bodies, status, duration, assistant ID, thread ID, and tenant context with redaction and truncation controls.
- **Dashboard Request History view**: New operator surface for filtering traffic, summarizing success and failure rates, inspecting request and response detail, and replaying captured calls into the explorer.
- **Dashboard API Explorer**: New live API explorer driven by runtime route metadata, with request editing, response inspection, code snippets, recent requests, and assistant-specific helper flows for public assistant APIs.
- **Runtime OpenAPI exposure**: AssistantHub now exposes `/openapi.json` from the running server for explorer consumption and route-surface verification.
- Migration script: `migrations/008_upgrade_to_v0.10.0.sql`

### Changed
- Docker image tags updated to `v0.10.0`
- Endpoint test workflows can now open directly in the shared API Explorer with prebuilt request presets
- The dashboard now includes `API Explorer` and `Request History` as built-in `Monitoring` tools for system and assistant API traffic
- Product and package versions updated to `0.10.0`

### Breaking
- Database schema changes require running `migrations/008_upgrade_to_v0.10.0.sql` for existing installations

## v0.9.0 - Slack Support Added

### Added
- **Per-assistant Slack integration**: Slack configuration now lives on assistant settings with `EnableSlack`, `SlackAppToken`, `SlackBotToken`, `SlackChannelId`, and `SlackMessagePrefix`
- **Slack connectivity verification**: New `POST /v1.0/assistants/{assistantId}/settings/slack/verify` endpoint and dashboard flow for testing draft Slack settings before saving
- **Slack worker runtime**: AssistantHub now starts one Socket Mode worker per Slack-enabled assistant, supports configured-channel traffic and direct messages, suppresses self-messages, and posts replies back into Slack threads
- **Shared chat execution service**: Non-streaming chat execution is now reusable by web chat and Slack so retrieval, compaction, inference, citations, and persistence stay aligned
- **Chat history origin tracking**: `chat_history.origin` records request source values such as `web` and `slack`
- Migration script: `migrations/007_upgrade_to_v0.9.0.sql`

### Changed
- Docker image tags updated to `v0.9.0`
- Assistant settings API, OpenAPI spec, Postman collection, and dashboard updated for Slack configuration and verification
- Slack responses are transport-shaped only at delivery time while canonical response text is persisted to history

### Breaking
- Database schema changes require running `migrations/007_upgrade_to_v0.9.0.sql` for existing installations

## v0.8.0

### Added
- **RAG Evaluation**: Automated evaluation framework for measuring RAG pipeline quality. Define expected facts per assistant, run evaluations that send questions through the inference pipeline, and use an LLM judge to score responses against expected facts with PASS/FAIL verdicts and reasoning.
- New models: `EvalFact` (prefix `ef_`), `EvalRun` (prefix `erun_`), `EvalResult` (prefix `eres_`), `FactVerdict`, `EvalStatusEnum`
- New database tables: `eval_facts`, `eval_runs`, `eval_results` with indexes for tenant, assistant, status, and temporal queries
- New assistant setting: `EvalJudgePrompt` — custom judge prompt template per assistant (supports `{QUESTION}`, `{RESPONSE}`, `{EXPECTED_FACT}` placeholders). Falls back to built-in default when not configured.
- New `EvalService` — orchestrates evaluation runs in the background, sends questions through inference, judges each expected fact, tracks progress, and stores results
- New `EvalHandler` with 13 API endpoints:
  - `PUT /v1.0/eval/facts` — create eval fact
  - `GET /v1.0/eval/facts` — list eval facts (with assistant filter)
  - `GET /v1.0/eval/facts/{factId}` — get eval fact
  - `PUT /v1.0/eval/facts/{factId}` — update eval fact
  - `DELETE /v1.0/eval/facts/{factId}` — delete eval fact
  - `POST /v1.0/eval/runs` — start eval run (with optional judge prompt override)
  - `GET /v1.0/eval/runs` — list eval runs
  - `GET /v1.0/eval/runs/{runId}` — get eval run
  - `DELETE /v1.0/eval/runs/{runId}` — delete eval run and results
  - `GET /v1.0/eval/runs/{runId}/results` — get all results for a run
  - `GET /v1.0/eval/runs/{runId}/stream` — SSE stream of run progress (2-second polling)
  - `GET /v1.0/eval/results/{resultId}` — get single eval result
  - `GET /v1.0/eval/judge-prompt/default` — get the built-in default judge prompt
- Dashboard: "Evaluation" page in the Chat (Assistants) sidebar section with:
  - Facts sub-tab: DataTable with category, question, expected facts count; create/edit modal with dynamic expected facts list; delete with confirmation
  - Runs sub-tab: DataTable with status badges, pass/fail counts, pass rate, duration; start new run modal with optional judge prompt override; SSE progress streaming modal; results modal with per-fact detail view showing LLM response and judge verdicts with reasoning
- OpenAPI spec updated with Evaluation tag and all 13 endpoint definitions plus schemas
- Migration script: `migrations/006_upgrade_to_v0.8.0.sql` (SQLite, PostgreSQL, MySQL, SQL Server)

### Changed
- Docker image tags updated to v0.8.0
- Database schema updated with `eval_judge_prompt` column on `assistant_settings` and three new eval tables
- `DatabaseDriverBase` extended with `EvalFact`, `EvalRun`, `EvalResult` entity method interfaces
- `SqliteDatabaseDriver` wired with eval method implementations
- `TableQueries` updated with eval table creation and index statements
- `Constants` updated with eval identifier prefixes and table names
- `IdGenerator` updated with `NewEvalFactId()`, `NewEvalRunId()`, `NewEvalResultId()` methods

### Breaking
- Database schema changes require running `migrations/006_upgrade_to_v0.8.0.sql` for existing installations

## v0.7.0

### Added
- **Metadata filtering for RAG retrieval**: Filter chat retrieval to only return documents matching specified labels (strings) and/or tags (key-value pairs with conditional operators). Filters can be configured as defaults on an assistant and/or supplied per-conversation at chat time.
- New assistant settings: `RetrievalLabelFilter`, `RetrievalTagFilter` — JSON-serialized default filters applied during every retrieval for that assistant
- New `metadata_filter` field on `ChatCompletionRequest` — per-request filter that is merged with assistant defaults
- Filter merging: assistant-level and request-level filters are unioned (required labels/tags combined, excluded labels/tags combined)
- Effective metadata filter stored in `ChatHistory.MetadataFilter` for auditing
- Metadata filter display in the History View modal
- New proxy API endpoints for discovering available filter values:
  - `GET /v1.0/collections/{collectionId}/labels/distinct` (admin, authenticated)
  - `GET /v1.0/collections/{collectionId}/tags/distinct` (admin, authenticated)
  - `GET /v1.0/assistants/{assistantId}/labels/distinct` (public, no auth)
  - `GET /v1.0/assistants/{assistantId}/tags/distinct` (public, no auth)
- Dashboard: Retrieval Filters section in assistant settings (JSON editor for label and tag filters)
- Dashboard: Collapsible metadata filter panel in ChatPanel for per-session filtering
- Migration script: `migrations/005_upgrade_to_v0.7.0.sql`

### Changed
- Docker image tags updated to v0.7.0
- Database schema updated with metadata filtering columns for `assistant_settings` and `chat_history` tables
- `RetrievalService.BuildSearchBody()` now uses Dictionary-based construction to support conditional `LabelFilter` and `TagFilter` properties
- `RetrievalSearchOptions` extended with `MetadataFilter` property
- Chat completion request body now accepts an optional `metadata_filter` object (AssistantHub extension to the OpenAI-compatible schema; omitting it preserves existing behavior)

### Breaking
- Database schema changes require running `migrations/005_upgrade_to_v0.7.0.sql` for existing installations

## v0.6.0

### Added
- **LLM-based re-ranking**: After initial retrieval, an LLM scores each chunk's relevance (0-10) to the user's query. Low-scoring chunks are filtered out and results are re-sorted by relevance before context injection.
- New assistant settings: `EnableReranking`, `RerankerTopK`, `RerankerScoreThreshold`, `RerankPrompt`
- New chat history telemetry: `RerankDurationMs`, `RerankInputCount`, `RerankOutputCount`
- Re-ranking TimingBar in history detail view
- Re-rank score display on citation cards and chunk details
- Migration script: `migrations/004_upgrade_to_v0.6.0.sql`

### Changed
- Docker image tags updated to v0.6.0
- Database schema updated with re-ranking columns for `assistant_settings` and `chat_history` tables

### Breaking
- Database schema changes require running `migrations/004_upgrade_to_v0.6.0.sql` for existing installations

## v0.5.0

### Native Crawlers

- **Web crawler engine** -- Built-in web crawling powered by CrawlSharp. Automatically discovers, retrieves, and ingests content from websites. Supports link following, sitemap extraction, robots.txt compliance, and configurable crawl depth, parallelism, and throttling
- **Crawl plans** -- Persistent crawler configurations that define what to crawl, how to crawl it, and what to do with the results. Each plan specifies a target URL, authentication, schedule, filters, ingestion rule, and processing options
- **Scheduled crawling** -- Automatic recurring crawls on configurable intervals (one-time, minutes, hours, days, weeks). Background scheduler service checks all plans every 60 seconds and launches crawls when due
- **Crawl operations** -- Each crawl execution is tracked as a separate operation with full lifecycle state (NotStarted, Starting, Enumerating, Retrieving, Success, Failed, Stopped, Canceled) and detailed counters for objects/bytes enumerated, added, updated, deleted, succeeded, and failed
- **Delta-based crawling** -- Crawl enumerations are persisted to disk as JSON files. Subsequent crawls compare against the previous enumeration to identify new, changed, deleted, and unchanged objects, processing only the delta
- **Document traceability** -- Crawled documents are linked back to their crawl plan and operation via `CrawlPlanId`, `CrawlOperationId`, and `SourceUrl` fields on `AssistantDocument`. Filter the Documents view by crawler to see all documents from a specific crawl plan
- **Web authentication** -- Support for None, Basic (username/password), API Key (custom header), and Bearer Token authentication when crawling protected sites
- **Crawl filters** -- Optional content type whitelist, object prefix/suffix matching, and minimum/maximum file size constraints to control which discovered resources are ingested
- **Configurable processing** -- Per-plan control over whether additions, updates, and deletions are processed. Configurable maximum concurrent drain tasks (1-64) for parallel ingestion
- **Operation retention** -- Per-plan retention period (0-14 days) with automatic cleanup of expired operations and their enumeration files by a background service running hourly
- **Startup recovery** -- The scheduler service detects any crawl plans left in a Running state from a previous unclean shutdown and resets them to Stopped with the last operation marked as Failed
- **On-demand controls** -- Start and stop crawls immediately via API or dashboard, independent of the schedule
- **Connectivity testing** -- Test crawl plan connectivity before running a full crawl. The API performs a single-page fetch against the configured URL and reports success/failure
- **Content enumeration** -- Preview what a crawl plan would discover without ingesting anything. The API returns the list of discovered resources with metadata
- **Crawl operations statistics** -- Aggregate and per-operation statistics including run counts, success/failure rates, runtime min/max/avg, total objects and bytes crawled, and next scheduled run time
- **Dashboard: Crawlers view** -- Full management UI for crawl plans with DataTable listing, create/edit form modal with collapsible sections (General, Ingestion, Repository Settings, Schedule, Filter, Processing, Retention), row actions (Start, Stop, Edit, View Operations, View JSON, Verify Connectivity, Enumerate Contents, Delete), and bulk delete
- **Dashboard: Operations modal** -- Statistics panel with aggregate metrics, operations table with status badges, per-operation actions (View Enumeration, Delete)
- **Dashboard: Enumeration viewer** -- Collapsible sections for All Files, New Files, Changed Files, Deleted Files, Successfully Crawled, and Failed, each with count/size summary and expandable file table
- **Dashboard: Documents integration** -- Crawler filter dropdown in Documents view, "Crawled" badge on crawler-produced documents, "View Crawl Operation" context menu action
- **Extensible crawler architecture** -- Abstract `CrawlerBase` class with `CrawlerFactory` pattern. Web is the first implementation; future repository types (S3, SFTP, etc.) can be added by implementing `CrawlerBase` and extending `RepositoryTypeEnum`
- **API endpoints** -- 16 new authenticated routes: CRUD for crawl plans (`/v1.0/crawlplans`), start/stop/connectivity/enumerate actions, and crawl operations sub-resource with statistics and enumeration file access
- **Database support** -- Full schema for all 4 database drivers (SQLite, PostgreSQL, MySQL, SQL Server) with new `crawl_plans` and `crawl_operations` tables plus indexes
- **Breaking change** -- v0.5.0 includes schema changes to add `crawl_plans` and `crawl_operations` tables and add `crawl_plan_id`, `crawl_operation_id`, `source_url` columns to `assistant_documents`. A migration script is provided at `migrations/003_upgrade_to_v0.5.0.sql`

## v0.4.0

### Query Rewrite

- **LLM-based query rewrite** -- Optionally rewrite user queries into multiple semantically varied phrasings before retrieval, broadening recall by capturing synonyms, alternate phrasing, and conceptual restatements that a single query would miss
- **Customizable rewrite prompt** -- User-editable prompt template with `{prompt}` placeholder; dashboard textarea with tooltips for easy customization. Falls back to a built-in default when not customized
- **Multi-query retrieval** -- All rewritten queries are sent to RecallDB independently; results are deduplicated by document ID and position, re-sorted by score, and capped at the configured Top K
- **Rewrite metrics in chat history** -- Query rewrite results and duration are persisted in chat history for auditability and performance monitoring
- **Breaking change** -- v0.4.0 includes schema changes to `assistant_settings` and `chat_history` tables. A migration script is provided at `migrations/002_upgrade_to_v0.4.0.sql`

## v0.3.0

### Multi-Tenant Platform

- **Full multi-tenancy** -- Row-level tenant isolation across all entities (users, credentials, assistants, documents, ingestion rules, chat history, feedback). Each tenant operates in complete isolation within a shared deployment
- **Tenant management** -- CRUD API and dashboard view for creating, updating, and deleting tenants (global admin only)
- **Auto-provisioning** -- New tenants are automatically provisioned with a RecallDB tenant, default collection, S3 bucket, admin user, credential, and ingestion rule
- **Tenant deletion cascade** -- Deleting a tenant cleanly removes all child rows, S3 buckets, the RecallDB tenant, and the tenant record
- **Three-tier authorization** -- Global Admin (admin API keys or users with `IsAdmin=true`), Tenant Admin (`IsTenantAdmin`), and Tenant User roles with appropriate access controls throughout all handlers
- **Per-tenant S3 bucket isolation** -- Each tenant's S3 buckets are prefixed with `{tenantId}_` (e.g. `ten_abc123_default`). Bucket and object operations enforce tenant bucket prefix for non-global-admin users. Auto-provisioning creates a default bucket per tenant; deprovisioning deletes all tenant buckets
- **Per-tenant processing logs** -- Processing log files are namespaced by tenant ID in subdirectories, with backward-compatible fallback for pre-v0.3.0 logs
- **Per-tenant RecallDB mapping** -- Each AssistantHub tenant maps 1:1 to its own RecallDB tenant for vector isolation
- **Tenant-scoped API routes** -- Users and credentials are accessed via `/v1.0/tenants/{tenantId}/users` and `/v1.0/tenants/{tenantId}/credentials`
- **WhoAmI endpoint** -- `GET /v1.0/whoami` returns the current authentication context including tenant, role, and user details
- **Login with tenant context** -- Email/password authentication accepts optional `TenantId` parameter; dashboard login form includes Tenant ID field
- **Dashboard tenant awareness** -- Topbar shows tenant name and role badge; sidebar conditionally shows admin-only sections; 6 data views show Tenant column for global admins
- **Tenants dashboard view** -- Full CRUD interface with provisioning result modal showing auto-generated credentials
- **Breaking change** -- v0.3.0 is a new deployment; existing v0.2.0 databases require the manual migration script (`migrations/001_upgrade_to_v0.3.0.sql`) which adds `tenant_id` columns, creates the default tenant, promotes existing admins to tenant admins, and inserts default records. Scripts provided for SQLite, PostgreSQL, SQL Server, and MySQL
- **Protected records** -- Default tenant, admin user, and credential are marked `IsProtected = true` and cannot be deleted via API (returns 403). Per-tenant provisioned users and credentials are also protected. Protected status is visible and editable in the dashboard. Deactivate protected records by setting `Active = false` instead
- **Default credentials** -- Fresh deployments are pre-seeded with a default tenant, admin user (`admin@assistanthub` / `password`), bearer token (`default`), and ingestion rule. First-run auto-provisioning creates these records if no tenants exist
- **Database test suite updated** -- All 9 test classes (including new TenantTests) explicitly set TenantId and use tenant-scoped enumeration

### Other Changes

- **Neighbor chunk retrieval** -- Optionally retrieve surrounding chunks for each search match to provide broader document context to the model, configurable per assistant (0-10 neighbors)
- Removed `RecallDb.TenantId` from configuration (now derived from authenticated user context)
- Added `AdminApiKeys` and `DefaultTenant` settings sections

## v0.2.0

- **Initial release**
- **Multi-assistant platform** -- Create and manage multiple AI assistants, each with independent configuration, personality, knowledge base, and appearance
- **Automated document ingestion pipeline** -- Upload documents (PDF, text, HTML, and more); automatic text extraction via DocumentAtom, chunking and embedding via Partio, and storage in RecallDB
- **Ingestion rules** -- Define reusable ingestion configurations specifying target S3 buckets, RecallDB collections, chunking strategies, optional summarization, and embedding settings
- **Flexible search modes** -- Vector (semantic similarity), full-text (keyword matching), and hybrid search with tunable scoring weights for optimal retrieval
- **LLM-based retrieval gate** -- Optional per-assistant retrieval gate that classifies whether each user message requires new document retrieval or can be answered from existing conversation context
- **Conversation compaction** -- Automatic summarization of older messages when the conversation approaches the context window limit, preserving conversation continuity
- **Streaming chat responses** -- Real-time Server-Sent Events (SSE) streaming for token-by-token response delivery
- **Configurable inference endpoints** -- Support for Ollama (local) and OpenAI (cloud) inference providers, with per-assistant endpoint overrides via managed Partio endpoints
- **Document summarization** -- Optional pre-chunking or post-chunking summarization of document content using configurable completion endpoints
- **Public chat API** -- Unauthenticated OpenAI-compatible chat endpoint for embedding assistants into external applications
- **Feedback collection** -- Thumbs-up/thumbs-down feedback and free-text comments on assistant responses for quality monitoring
- **Chat history and performance metrics** -- Per-turn history with detailed timing measurements: retrieval duration, time to first token, time to last token, tokens per second, compaction duration, and more
- **Browser-based dashboard** -- Full management UI for assistants, documents, ingestion rules, endpoints, feedback, history, collections, buckets, users, and live chat testing
- **Multi-tenant user management** -- Admin and standard user roles with per-user assistant ownership
- **Multiple database backends** -- SQLite (default), PostgreSQL, SQL Server, and MySQL for the application database
- **One-command Docker deployment** -- Fully orchestrated Docker Compose stack with health checks, dependency ordering, and persistent volumes
- **Citation metadata in chat responses** -- When enabled per-assistant, the system instructs the model to cite source documents using bracket notation [1], [2] and returns a structured `citations` object in the response mapping references to source document names, IDs, relevance scores, and text excerpts
- **Citation document linking** -- Configurable `CitationLinkMode` setting (`None`, `Authenticated`, `Public`) that populates `download_url` on citation sources. All downloads are server-proxied (no direct S3 exposure). Public mode provides unauthenticated download gated by the assistant setting. Citation cards in the dashboard are clickable when a download URL is available
