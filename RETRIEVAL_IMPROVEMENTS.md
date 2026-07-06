# Retrieval Improvements Plan

This document tracks product-wide retrieval improvements for AssistantHub. It is designed to be annotated in place by developers as work progresses.

Status legend:

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete
- `[!]` Blocked

Scope legend:

- Backend: server, core models, database, ingestion, chat execution, MCP
- Frontend: dashboard and public chat UI
- SDKs: C#, Python, JavaScript/TypeScript if present in the active release surface
- Testing: unit, integration, automated, manual validation
- Postman/OpenAPI: generated schema, examples, collections
- Docs: README, REST API, changelog, operational notes

## Objectives

- Improve production retrieval quality without destabilizing existing RAG behavior.
- Add observable, testable stages before making larger routing changes.
- Keep every improvement configurable per assistant where runtime behavior changes.
- Ensure admin users can see what happened: query class, filters, retrieval, rerank, answerability, citations, latency, tokens, feedback, and eval outcomes.
- Keep SDK, dashboard, Postman, OpenAPI, and documentation in sync with backend changes.

## Current Baseline

AssistantHub already supports:

- Vector, full-text, and hybrid retrieval.
- Metadata filters using labels and tags.
- Attached-document retrieval scoping.
- Optional retrieval gate: `RETRIEVE` or `SKIP`.
- Optional query rewrite.
- Optional LLM reranking.
- Citations.
- Chat history, request history, performance telemetry, analytics, feedback, and eval facts.
- Tool policies for collection, Verbex, S3, DocumentAtom, bucket, and web-search tools.

Known gaps addressed by this plan:

- No rich query classification beyond `RETRIEVE` or `SKIP`.
- No explicit answerability gate before generation.
- Eval runs do not clearly exercise the full chat/RAG execution rail.
- Eval failure-mode coverage is not formalized.
- Telemetry does not yet capture all fields needed for retrieval regression analysis.
- Structured data routing is available only indirectly through tools, not as a deterministic retrieval policy.
- Section-aware ingestion and retrieval metadata is limited.

## Concepts, Definitions, And Rationale

This section defines the major terms used in the plan. Each definition includes why the item exists, so implementers can evaluate tradeoffs instead of only following a task list.

### Query Class

What it is:

`QueryClass` is a durable label for the user's latest request, such as `lookup`, `comparison`, `summarization`, `numeric_aggregate`, `troubleshooting`, `policy_business_rule`, `metadata_discovery`, `conversation_only`, or `unknown`.

Why it matters:

AssistantHub currently has a retrieval gate that decides only `RETRIEVE` or `SKIP`. That is useful for avoiding unnecessary searches, but it does not explain what kind of work the user asked for. A lookup, a comparison, a table-heavy numeric question, and a policy question often need different retrieval settings and different eval expectations. Persisting `QueryClass` on `ChatHistory` makes the decision inspectable after the fact, enables analytics by query type, and creates a stable join point for eval failures and later routing work.

Why nullable on `ChatHistory`:

The field should be nullable because old chat rows will not have it, classification will be disabled by default at first, and classifier failures should not break chat. Null means "not classified", while `unknown` means "classification ran but could not confidently map the query." Keeping the field on `ChatHistory` ties it to the exact turn, metadata filter, retrieval results, answer, feedback, and telemetry that operators already inspect.

### Answerability Decision

What it is:

`AnswerabilityDecision` is a pre-generation judgment made after retrieval and rerank. It answers: "Does the available evidence support a response to this user request?" Suggested values are `answerable`, `needs_clarification`, `unsupported`, and `not_checked`.

Why it matters:

RAG failures often happen after retrieval: the system finds weak, partial, stale, or mismatched evidence and still asks the model to produce an answer. An answerability check gives AssistantHub a deliberate place to stop, ask a clarifying question, or return an unsupported response before the final model writes a confident answer. Starting with log-only mode provides measurement without changing user-visible behavior.

Why nullable on `ChatHistory`:

The check is optional and will not exist for historical rows. Persisting it on the chat turn lets admins compare answerability decisions with final answers, citations, feedback, and eval outcomes.

### Candidate Drop Summary

What it is:

Candidate-drop telemetry records how many retrieval candidates were removed and why. Reasons may include score threshold, rerank threshold, attached-document scope, metadata filters, context-window trimming, answerability rejection, or policy denial.

Why it matters:

When a response is poor, developers need to know whether the evidence was never retrieved, retrieved and filtered out, retrieved but trimmed, or retrieved but ignored. A safe summary gives enough information to debug without exposing hidden storage details or unbounded source text.

### Final Citation Count

What it is:

`FinalCitationCount` is the number of citation sources that survived through final answer extraction.

Why it matters:

AssistantHub can retrieve and prepare citation sources, but the final answer may cite none, cite one, or cite many. Tracking final citation count makes citation regressions measurable and helps identify answers that appear grounded internally but fail to expose evidence to users.

### Eval Failure Mode

What it is:

An eval failure mode is a stable category for a regression fact, such as `stale_docs`, `permission_bound`, `ambiguous_acronym`, `table_heavy`, `numeric`, `should_refuse`, `citation_required`, `metadata_filter`, `attached_document`, or `tool_required`.

Why it matters:

General pass rate hides product risk. A release can improve simple lookup while breaking table questions or permission-bounded retrieval. Failure-mode categories let AssistantHub report where retrieval is improving or regressing.

### Chat-Rail Eval

What it is:

A chat-rail eval sends eval questions through the same assistant chat execution path used by users, including retrieval gate, query rewrite, retrieval, rerank, context building, citations, answerability, tools, telemetry, and persistence.

Why it matters:

An inference-only eval judges a model response but does not test AssistantHub's retrieval product. For RAG quality, evals should exercise the same path that production chat uses.

### Retrieval Profile

What it is:

A retrieval profile is a named set of retrieval settings selected for a query class: search mode, top K, score threshold, full-text options, neighbor count, query rewrite, rerank, and answerability requirements.

Why it matters:

Different query types need different retrieval behavior. A summarization question may need broader coverage, a troubleshooting question may need more neighbors, and an exact policy clause may need stronger full-text behavior. Profiles make those choices explicit and testable.

### Structured Data Routing

What it is:

Structured data routing sends numeric, count, status, filter, or ranking questions to permissioned structured sources such as APIs, databases, or bounded tools instead of treating the question as normal document chunk retrieval.

Why it matters:

Chunk retrieval is a poor fit for many aggregate questions. If a user asks "how many", "which records match", or "what changed since", the best answer often comes from structured data with explicit filters and limits. This needs stronger permissioning and auditing than normal retrieval.

### Section-Aware Retrieval Metadata

What it is:

Section-aware metadata records source structure on chunks, such as page number, heading path, section title, table caption, row group, or chunk kind.

Why it matters:

Document ID plus chunk position is often not enough for precise citations, table questions, policies, manuals, or answerability checks. Section metadata helps users and admins understand where evidence came from and helps prompts avoid mixing unrelated chunks.

## Workstreams

## Phase 1: Low-Intrusion Observability And Eval Foundations

Goal: improve measurement and regression support before changing retrieval behavior.

### 1.1 Retrieval Telemetry Contract

Implementation status: partially complete in this pass. Core chat history, response telemetry, persistence, SDK models, History detail UI, REST docs, OpenAPI snippets, Postman examples, and focused tests have been added. Deeper analytics rollups and separate Request History modal rendering remain follow-up work.

What this phase adds:

New nullable fields and telemetry projections that explain how the retrieval pipeline made decisions for a chat turn.

Why this comes first:

These fields make later behavior changes measurable. They are intentionally nullable and mostly observational, so they can be added without changing default answers.

Backend:

- [x] Add nullable `QueryClass` to `ChatHistory`.
  - What: The classified intent of the latest user message for this chat turn.
  - Why: Enables analytics, eval slicing, and later routing by query type while preserving historical rows.
  - Notes: Added to core model, database schemas, provider insert paths, SDKs, and history detail display.
- [x] Add nullable `AnswerabilityDecision` to `ChatHistory`.
  - Suggested values: `answerable`, `needs_clarification`, `unsupported`, `not_checked`.
  - What: The result of the optional pre-generation evidence sufficiency check.
  - Why: Lets operators see whether the system believed the retrieved evidence was enough before it generated the answer.
  - Notes: Added as optional persisted telemetry and response metadata.
- [x] Add nullable `AnswerabilityReason` to `ChatHistory`.
  - What: A short safe explanation for the answerability decision.
  - Why: A decision without a reason is hard to debug and hard to improve.
  - Notes: Added as safe persisted rationale text.
- [x] Add nullable `DroppedCandidateCount` to `ChatHistory`.
  - What: Count of retrieved candidates removed before final context or final answer.
  - Why: Distinguishes "nothing was retrieved" from "useful evidence was retrieved and later filtered or trimmed."
  - Notes: Counts attachment-filter, rerank, and prompt-budget drops.
- [x] Add nullable `DroppedCandidateSummaryJson` to `ChatHistory`.
  - Include safe IDs, score, reason, and stage when available.
  - What: Safe, bounded details about dropped candidates.
  - Why: Gives developers enough information to debug retrieval loss without storing raw unbounded content.
  - Notes: Stores bounded stage/reason/count summaries rather than dropped raw text.
- [x] Add nullable `FinalCitationCount` to `ChatHistory` or telemetry projection.
  - What: Number of citations that appear in or are extracted from the final answer.
  - Why: Makes citation coverage observable and catches answers that had sources available but did not cite them.
  - Notes: Captured from extracted final citations when citations are active.
- [x] Extend `AssistantPerformanceTelemetryBuilder` to project the new fields into performance events.
  - What: Include new fields in normalized stage/event metadata.
  - Why: Analytics should not require parsing raw chat history JSON.
  - Notes: Adds answerability stage metadata with query class, decision, reason, drops, and final citation count.
- [x] Add migration scripts for SQLite, PostgreSQL, MySQL, and SQL Server.
  - Why: AssistantHub supports multiple database providers; telemetry must stay provider-compatible.
  - Notes: Added `014_upgrade_to_v0.17.0.*` plus startup guards/fresh schema updates.
- [x] Backfill defaults for existing rows without changing historical behavior.
  - Why: Old rows should remain readable and should not be misreported as classified or checked.
  - Notes: Implemented with nullable columns and default `not_checked` runtime behavior; no destructive backfill required.

Frontend:

- [x] Add fields to History detail retrieval/performance sections.
  - Notes:
- [ ] Add fields to Request History linked chat detail view where applicable.
  - Notes:
- [ ] Add fields to Assistant Analytics slowest-request drill-down if useful.
  - Notes:

SDKs:

- [x] Add new fields to C# SDK models.
  - Notes:
- [x] Add new fields to Python SDK models.
  - Notes:
- [x] Add new fields to JavaScript/TypeScript SDK models if this SDK surface is active.
  - Notes:
- [x] Preserve backward compatibility for older servers returning no fields.
  - Notes:

Testing:

- [x] Add model serialization tests for new telemetry fields.
  - Notes:
- [~] Add database CRUD tests for new chat history columns across supported providers.
  - Notes:
- [x] Add analytics projection tests for new metadata.
  - Notes:

Postman/OpenAPI:

- [x] Regenerate OpenAPI.
  - Notes:
- [x] Update Postman examples for chat history and analytics responses.
  - Notes:

Docs:

- [x] Update REST API chat history and analytics sections.
  - Notes:
- [ ] Update README observability/analytics bullets.
  - Notes:
- [ ] Add changelog entry.
  - Notes:

Acceptance criteria:

- [ ] A chat turn can be inspected and show query class, answerability status, candidate-drop summary, final citation count, latency, token usage, and effective metadata filter when available.
- [ ] Missing fields on old data are displayed as absent or `not_checked`, not as failures.

### 1.2 Formal Eval Failure Modes

Implementation status: partially complete in this pass. Recommended categories, category-filtered run requests, persisted run filters, frontend run filtering, SDKs, REST docs, OpenAPI snippets, and Postman examples are in place. Warning UX and category-level reporting remain follow-up work.

What this phase adds:

A shared vocabulary for the kinds of retrieval failures AssistantHub should prevent.

Why this comes before new routing:

If evals are not grouped by failure mode, later routing changes may look successful overall while silently regressing important categories.

Backend:

- [x] Define recommended eval categories using existing `EvalFact.Category`.
  - Suggested initial values: `stale_docs`, `permission_bound`, `ambiguous_acronym`, `table_heavy`, `numeric`, `should_refuse`, `citation_required`, `metadata_filter`, `attached_document`, `tool_required`.
  - What: A controlled recommendation list, not a hard enum unless the product later needs one.
  - Why: Reuses existing schema while making eval reports comparable across assistants and releases.
  - Notes: Added `EvalFact.RecommendedCategories`; custom categories remain valid.
- [ ] Add server-side validation warnings for unknown categories without blocking custom categories.
  - Why: Teams should be nudged toward shared categories without losing flexibility.
  - Notes:
- [x] Add optional category filtering to eval run requests if not already covered by enumeration filters.
  - Why: Developers need to run targeted regressions quickly while working on a specific retrieval issue.
  - Notes:
- [x] Persist selected category filters on `EvalRun` if eval runs support scoped execution.
  - Why: Later readers need to know whether a run measured the full suite or only a category subset.
  - Notes:

Frontend:

- [ ] Add category picker/filter to eval fact management.
  - Notes:
- [x] Add category filter to eval run creation.
  - Notes:
- [ ] Show pass rate by category in eval run details.
  - Notes:

SDKs:

- [x] Add category filter/request support to SDK eval APIs.
  - Notes:

Testing:

- [ ] Add tests for eval fact category create/update/list.
  - Notes:
- [ ] Add tests for category-filtered eval runs.
  - Notes:

Postman/OpenAPI:

- [ ] Add category examples to eval fact requests.
  - Notes:
- [x] Add category-filtered run example.
  - Notes:

Docs:

- [ ] Document recommended failure-mode categories and when to use each.
  - Notes:
- [ ] Add a sample regression set structure.
  - Notes:

Acceptance criteria:

- [ ] Admins can maintain small regression sets by failure mode.
- [ ] Eval results can be reviewed by category without custom database queries.

### 1.3 Full Chat/RAG Eval Execution

Implementation status: complete for the core product path. Eval runs default to `ChatRail`, keep `InferenceOnly` for compatibility, persist chat/eval artifacts, use `Origin = eval`, and expose mode selection in the dashboard and SDKs.

What this phase adds:

Eval questions go through the same execution path as user chat instead of only calling the model with a system prompt and user message.

Why this matters:

The product is a RAG system, not only an inference wrapper. Retrieval gate, query rewrite, retrieval, rerank, citations, tools, and telemetry are all part of the behavior that eval should measure.

Backend:

- [x] Refactor `EvalService` to call the same shared assistant chat execution rail used by non-streaming chat.
  - Why: Makes evals representative of production assistant behavior.
  - Notes:
- [x] Preserve an option for inference-only evals if needed for backwards compatibility.
  - Suggested setting: `ExecutionMode = ChatRail | InferenceOnly`.
  - Why: Existing users may rely on model-only eval semantics; compatibility avoids a surprise behavior change.
  - Notes:
- [x] Store retrieval metadata, citations, answer text, tool traces, and telemetry references with each `EvalResult`.
  - Why: A failed eval should be diagnosable from the eval result without manually correlating logs.
  - Notes:
- [x] Ensure eval runs use assistant settings for RAG, retrieval gate, query rewrite, rerank, citations, and tools.
  - Why: Eval should test the configured assistant, not a simplified approximation.
  - Notes:
- [x] Add safeguards so eval runs cannot accidentally post to Slack or external user channels.
  - Why: Eval execution should be observable internally but should never create user-visible side effects outside the eval context.
  - Notes:
- [x] Decide whether eval chat turns should persist in normal chat history.
  - Recommendation: persist with `Origin = eval`, or store isolated eval history with trace links.
  - Why: Persisting with a clear origin improves debugging and analytics, but the team must decide whether eval traffic should affect normal history views.
  - Decision: Persist with `Origin = eval` and trace/chat-history links on `EvalResult`.

Frontend:

- [x] Add eval run mode selection if both modes remain supported.
  - Notes:
- [ ] Show retrieval chunks and citations in eval result detail.
  - Notes:
- [ ] Show linked trace/request/chat history when available.
  - Notes:

SDKs:

- [x] Add eval run execution mode fields.
  - Notes:
- [x] Add eval result retrieval metadata fields.
  - Notes:

Testing:

- [ ] Add unit tests that eval calls `AssistantChatService` in chat-rail mode.
  - Notes:
- [ ] Add integration test for RAG-enabled eval with a small indexed document.
  - Notes:
- [ ] Add integration test for metadata-filtered eval.
  - Notes:
- [ ] Add integration test for should-refuse or unsupported category once answerability exists.
  - Notes:

Postman/OpenAPI:

- [x] Update eval run request/response schema.
  - Notes:
- [x] Add chat-rail eval examples.
  - Notes:

Docs:

- [ ] Update RAG evaluation docs to explain execution modes.
  - Notes:
- [ ] Document how eval data appears in history/analytics.
  - Notes:

Acceptance criteria:

- [ ] A RAG eval measures the same retrieval and generation path a user would hit.
- [ ] Eval results include enough retrieval context to debug failures.

## Phase 2: Optional Runtime Gates

Goal: add behavior-changing checks behind assistant settings.

### 2.1 Answerability Check

Implementation status: complete for the configurable product gate in this pass. The check runs after retrieval/rerank/context trimming, defaults to log-only, supports strict clarification/unsupported modes, persists history/response telemetry, and appears in settings/history/SDK/API docs. Further prompt calibration and analytics rollups remain follow-up work.

What this phase adds:

An optional gate that checks whether retrieved evidence is sufficient before the final answer is generated.

Why this matters:

Retrieval quality is not only about finding chunks. The system also needs to know when found evidence is insufficient, ambiguous, filtered out, stale, or outside the user's selected document scope. This reduces confident unsupported answers.

Why it starts in log-only mode:

Answerability prompts will need tuning against real traffic. Logging decisions first allows calibration before AssistantHub changes user-visible behavior.

Backend:

- [x] Add assistant settings:
  - [x] `EnableAnswerabilityCheck`
  - [x] `AnswerabilityInferenceEndpointId`
  - [x] `AnswerabilityPrompt`
  - [x] `AnswerabilityMode`
  - Suggested modes: `LogOnly`, `AskClarifyingQuestion`, `ReturnUnsupported`.
  - Notes:
- [x] Add default answerability prompt.
  - It should inspect query, retrieved chunks, citations, metadata filters, attached documents, and required fields.
  - Why: The prompt must evaluate the actual evidence boundary, not just the user question.
  - Notes:
- [x] Execute the check after retrieval/rerank/context trimming and before final inference.
  - Why: This is the point where AssistantHub knows what evidence the final model will actually see.
  - Notes:
- [x] Output structured JSON from the check.
  - Suggested fields: `decision`, `reason`, `missing_entities`, `missing_dates`, `missing_fields`, `required_clarification`.
  - Why: Structured output is easier to validate, persist, expose through SDKs, and test.
  - Notes:
- [x] In `LogOnly`, record the decision but do not alter the answer.
  - Why: Provides a safe rollout path and calibration data.
  - Notes:
- [x] In `AskClarifyingQuestion`, return a model or server-generated clarification response.
  - Why: Some failures are ambiguous rather than impossible; the best next action is to ask for missing constraints.
  - Notes:
- [x] In `ReturnUnsupported`, return a grounded unsupported response and skip final generation.
  - Why: Strict deployments may prefer no answer over an unsupported answer.
  - Notes:
- [x] Include answerability telemetry stage and duration.
  - Notes:
- [ ] Ensure attached-document and metadata-filter failures are represented clearly.
  - Notes:

Frontend:

- [x] Add answerability settings controls to Assistant Settings.
  - Notes:
- [x] Show answerability decision in History detail.
  - Notes:
- [ ] Show answerability stage in Analytics if telemetry events are emitted.
  - Notes:
- [ ] Ensure public chat handles clarification/unsupported responses cleanly.
  - Notes:

SDKs:

- [x] Add answerability settings to assistant settings models.
  - Notes:
- [x] Add answerability result fields to chat responses and history models.
  - Notes:

Testing:

- [ ] Unit test JSON parsing for answerability outputs.
  - Notes:
- [ ] Unit test invalid answerability output fallback.
  - Notes:
- [ ] Integration test `LogOnly` mode.
  - Notes:
- [ ] Integration test clarification response.
  - Notes:
- [ ] Integration test unsupported response.
  - Notes:
- [ ] Regression tests for empty retrieval, low rerank scores, and metadata-filtered no-match cases.
  - Notes:

Postman/OpenAPI:

- [ ] Add settings fields to schemas and examples.
  - Notes:
- [ ] Add chat response examples showing answerability metadata.
  - Notes:

Docs:

- [ ] Document modes, defaults, and operational tradeoffs.
  - Notes:
- [ ] Add prompt customization guidance.
  - Notes:

Acceptance criteria:

- [ ] Admins can enable answerability in log-only mode without changing user-visible answers.
- [ ] In strict modes, AssistantHub can ask for clarification or return unsupported before generation.

### 2.2 Query Classification V1

What this phase adds:

A richer classifier that labels the user's latest request by work type, initially for logging and later for retrieval routing.

Why this is separate from retrieval profiles:

Classification is useful even before it changes behavior. Separating classification from routing lets the team measure classifier quality before trusting it to alter retrieval.

Backend:

- [ ] Add query classes:
  - [ ] `lookup`
  - [ ] `comparison`
  - [ ] `summarization`
  - [ ] `numeric_aggregate`
  - [ ] `troubleshooting`
  - [ ] `policy_business_rule`
  - [ ] `metadata_discovery`
  - [ ] `conversation_only`
  - [ ] `unknown`
- [ ] Add assistant settings:
  - [ ] `EnableQueryClassification`
  - [ ] `QueryClassificationInferenceEndpointId`
  - [ ] `QueryClassificationPrompt`
  - [ ] `QueryClassificationMode`
  - Suggested modes: `LogOnly`, `RouteRetrieval`.
  - Why: Log-only mode provides a safe calibration stage; route mode enables later behavior changes when classification is trusted.
  - Notes:
- [ ] Keep existing retrieval gate behavior compatible.
  - Option: map `conversation_only` to `SKIP`, all other classes to `RETRIEVE`.
  - Why: Existing assistants already support retrieval gating; classification should not break that mental model.
  - Notes:
- [ ] Store query class in chat history and telemetry.
  - Why: Enables audit, analytics, eval slicing, and feedback correlation by query type.
  - Notes:
- [ ] Add fallback class `unknown` on classifier failure.
  - Why: A classifier failure should not fail chat or silently invent a class.
  - Notes:

Frontend:

- [ ] Add settings controls for query classification.
  - Notes:
- [ ] Show query class in History and Analytics.
  - Notes:

SDKs:

- [ ] Add settings and history fields.
  - Notes:

Testing:

- [ ] Unit test classifier parsing and fallbacks.
  - Notes:
- [ ] Integration test log-only classification.
  - Notes:
- [ ] Integration test compatibility with retrieval gate.
  - Notes:

Postman/OpenAPI:

- [ ] Add fields and examples.
  - Notes:

Docs:

- [ ] Document query classes and recommended eval categories for each.
  - Notes:

Acceptance criteria:

- [ ] Query class is recorded for chat turns without changing retrieval when in log-only mode.
- [ ] `conversation_only` can replace or augment the older `SKIP` retrieval gate behavior.

## Phase 3: Retrieval Routing And Tool Routing

Goal: use measured query classes to select safer and more effective retrieval paths.

### 3.1 Class-Based Retrieval Profiles

What this phase adds:

Per-query-class retrieval settings that can override the assistant defaults when routing is enabled.

Why this matters:

The same retrieval settings are rarely optimal for every query. Profiles let AssistantHub deliberately broaden, narrow, rerank, or neighbor-expand retrieval based on what the user is trying to do.

Backend:

- [ ] Add configurable retrieval profiles per query class.
  - Fields: search mode, top K, score threshold, full-text options, neighbor count, query rewrite enabled, rerank enabled, answerability required.
  - Notes:
- [ ] Add profile inheritance from current assistant settings.
  - Why: Profiles should be compact and should not require duplicating every existing retrieval setting.
  - Notes:
- [ ] Apply profiles only when `QueryClassificationMode = RouteRetrieval`.
  - Why: Existing assistants must keep current behavior unless admins explicitly opt into routing.
  - Notes:
- [ ] Record selected profile in telemetry.
  - Why: Operators need to know which settings were active for a given answer.
  - Notes:
- [ ] Add candidate drop reasons for profile filtering and rerank filtering.
  - Why: Profile changes can alter recall; dropped-candidate telemetry explains those changes.
  - Notes:

Frontend:

- [ ] Add retrieval profile editor to Assistant Settings.
  - Notes:
- [ ] Provide reset-to-default profile actions.
  - Notes:
- [ ] Show selected profile in History detail.
  - Notes:

SDKs:

- [ ] Add retrieval profile models.
  - Notes:

Testing:

- [ ] Unit test profile resolution defaults.
  - Notes:
- [ ] Integration test profile-specific search mode/top K/rerank behavior.
  - Notes:
- [ ] Regression test current behavior when routing is disabled.
  - Notes:

Postman/OpenAPI:

- [ ] Add profile schemas and examples.
  - Notes:

Docs:

- [ ] Document default profile recommendations.
  - Notes:

Acceptance criteria:

- [ ] Existing assistants behave the same until routing is enabled.
- [ ] Query classes can alter retrieval settings predictably and visibly.

### 3.2 Structured Data Routing

What this phase adds:

A controlled path for questions that are better answered from structured sources than from retrieved text chunks.

Why this matters:

Questions involving counts, filters, rankings, statuses, or "latest changed records" are fragile when answered from document snippets. Structured sources can answer them directly, but only if access is bounded, permissioned, audited, and grounded.

Backend:

- [ ] Define supported structured-data sources.
  - Initial candidates: AssistantHub management database, configured external REST APIs, tool-call APIs, user-provided connector metadata.
  - Decision:
- [ ] Add policy settings for structured data access.
  - Include allow-lists, read-only guarantees, tenant scoping, row limits, and timeout limits.
  - Why: Structured routing can expose sensitive operational data if it is not explicitly scoped.
  - Notes:
- [ ] Implement route for numeric/count/filter/ranking/status questions.
  - Why: These are the query shapes most likely to be wrong when forced through normal chunk retrieval.
  - Notes:
- [ ] Add result grounding format for structured outputs.
  - Why: The final answer needs a compact, inspectable source summary comparable to citations.
  - Notes:
- [ ] Add audit logging for structured queries and tool/API calls.
  - Why: Admins need traceability for data access and policy enforcement.
  - Notes:
- [ ] Ensure sensitive schema, credentials, and raw SQL are not exposed to public users.
  - Notes:

Frontend:

- [ ] Add structured data policy controls.
  - Notes:
- [ ] Add diagnostics for available structured routes.
  - Notes:
- [ ] Show structured route details in History and tool traces.
  - Notes:

SDKs:

- [ ] Add policy models and diagnostics response models.
  - Notes:

Testing:

- [ ] Unit test safe query planning and argument validation.
  - Notes:
- [ ] Integration test count/filter/ranking/status flows.
  - Notes:
- [ ] Security tests for tenant isolation and policy denial.
  - Notes:
- [ ] Eval categories for numeric and structured questions.
  - Notes:

Postman/OpenAPI:

- [ ] Add diagnostics and policy examples.
  - Notes:

Docs:

- [ ] Document supported structured routes and limitations.
  - Notes:
- [ ] Document how structured answers cite or explain their source.
  - Notes:

Acceptance criteria:

- [ ] Numeric and aggregate questions can be answered from structured sources without forcing them through document chunk retrieval.
- [ ] Every structured route is permissioned, audited, and bounded.

## Phase 4: Section-Aware Ingestion And Retrieval

Goal: make retrieved evidence more precise for structured documents.

### 4.1 Chunk Metadata Contract

What this phase adds:

A durable metadata shape for chunks so retrieval results can identify where evidence came from inside a document.

Why this matters:

Chunk text alone loses source structure. Page, heading, section, and table metadata improve citations, reranking, answerability checks, and user trust.

Backend:

- [ ] Define chunk metadata fields.
  - Suggested fields: `source_id`, `section_title`, `parent_section_title`, `heading_path`, `page_number`, `table_caption`, `figure_caption`, `row_group`, `chunk_kind`.
  - Notes:
- [ ] Extend ingestion output contract to carry metadata per chunk.
  - Why: Metadata must originate at ingestion; adding it only at retrieval time is too late for most document structures.
  - Notes:
- [ ] Extend RecallDB storage payloads to persist chunk metadata if supported.
  - Why: Retrieval needs the metadata beside the chunk.
  - Notes:
- [ ] Extend Verbex indexing payloads to persist searchable metadata where useful.
  - Why: Full-text search should be able to match or filter by safe source structure when available.
  - Notes:
- [ ] Extend `RetrievalChunk` and citations to surface safe metadata.
  - Why: The model, final citation cards, SDKs, and history UI need a consistent representation.
  - Notes:
- [ ] Add migration or compatibility layer for existing chunks without metadata.
  - Why: Existing installations should not require immediate reingestion.
  - Notes:

Frontend:

- [ ] Show section/page/table metadata in citation cards.
  - Notes:
- [ ] Show metadata in collection search and index search detail modals.
  - Notes:

SDKs:

- [ ] Add chunk metadata to retrieval and citation models.
  - Notes:

Testing:

- [ ] Unit test metadata serialization and redaction.
  - Notes:
- [ ] Integration test retrieval metadata round trip.
  - Notes:
- [ ] Document ingestion tests for PDFs, HTML, tables, and plain text.
  - Notes:

Postman/OpenAPI:

- [ ] Add metadata fields to retrieval/citation schemas.
  - Notes:

Docs:

- [ ] Document chunk metadata behavior and limitations.
  - Notes:

Acceptance criteria:

- [ ] Retrieved chunks can carry source structure needed for citation and answerability checks.
- [ ] Existing documents still work without reingestion.

### 4.2 Section Retrieval Behavior

What this phase adds:

Retrieval and context-building behavior that can use section metadata, not only isolated chunks.

Why this matters:

Some questions require the surrounding section or table context to answer correctly. Section-aware behavior gives AssistantHub a way to include enough local structure without dumping whole documents into the prompt.

Backend:

- [ ] Add retrieval mode for section-aware documents.
  - Options: retrieve chunk, retrieve section, retrieve chunk plus parent section summary.
  - Why: Different document types need different context granularity.
  - Notes:
- [ ] Add section-aware context building.
  - Why: The final model should see source structure in a predictable format.
  - Notes:
- [ ] Add answerability use of page/section/table metadata.
  - Why: A check can be stricter when it knows the relevant section or table is missing.
  - Notes:
- [ ] Add rerank prompt updates to include metadata.
  - Why: Rerank can make better decisions when it sees headings, pages, and chunk kind.
  - Notes:

Frontend:

- [ ] Add assistant setting for section-aware context behavior.
  - Notes:
- [ ] Show section context in History detail.
  - Notes:

SDKs:

- [ ] Add section retrieval settings and result fields.
  - Notes:

Testing:

- [ ] Integration test section-level retrieval on a structured document.
  - Notes:
- [ ] Regression test fixed chunk behavior remains unchanged when disabled.
  - Notes:

Postman/OpenAPI:

- [ ] Add examples for section-aware retrieval settings.
  - Notes:

Docs:

- [ ] Document recommended settings for PDFs, manuals, policies, and tables.
  - Notes:

Acceptance criteria:

- [ ] Admins can opt into section-aware context where document structure is available.
- [ ] Citations identify source structure more precisely than document plus chunk position.

## Cross-Cutting Requirements

Security and privacy:

- [ ] Preserve tenant isolation for every new retrieval, eval, and tool path.
  - Notes:
- [ ] Redact credentials, S3 keys, internal bucket names, hidden policy details, and raw system prompts.
  - Notes:
- [ ] Ensure public APIs expose only safe metadata.
  - Notes:
- [ ] Add policy-denial telemetry that is useful but non-secret.
  - Notes:

Backward compatibility:

- [ ] New behavior-changing settings default to disabled.
  - Notes:
- [ ] Existing chat API clients keep working without request changes.
  - Notes:
- [ ] Existing SDK users can ignore new nullable fields.
  - Notes:
- [ ] Existing documents do not require immediate reingestion.
  - Notes:

Operational readiness:

- [ ] Add dashboard diagnostics for classifier, answerability, and structured routing endpoints.
  - Notes:
- [ ] Add analytics filters for query class and answerability decision.
  - Notes:
- [ ] Add migration notes for every schema change.
  - Notes:
- [ ] Add configuration examples for a conservative production setup.
  - Notes:

Release process:

- [ ] Update migrations.
  - Notes:
- [ ] Update OpenAPI.
  - Notes:
- [ ] Update Postman collection.
  - Notes:
- [ ] Update C# SDK.
  - Notes:
- [ ] Update Python SDK.
  - Notes:
- [ ] Update JavaScript/TypeScript SDK if present.
  - Notes:
- [ ] Update MCP registrations if new management APIs are added.
  - Notes:
- [ ] Update README.
  - Notes:
- [ ] Update REST_API.md.
  - Notes:
- [ ] Update CHANGELOG.md.
  - Notes:
- [ ] Run full test suite.
  - Notes:

## Suggested Implementation Order

1. Phase 1.1: telemetry contract.
2. Phase 1.2: eval failure-mode categories.
3. Phase 1.3: full chat/RAG eval execution.
4. Phase 2.1: answerability check in log-only mode.
5. Phase 2.1: answerability strict modes.
6. Phase 2.2: query classification in log-only mode.
7. Phase 3.1: class-based retrieval profiles.
8. Phase 3.2: structured data routing.
9. Phase 4.1: chunk metadata contract.
10. Phase 4.2: section retrieval behavior.

## Phase Exit Checklist

Before closing any phase:

- [ ] Backend behavior is implemented and covered by tests.
- [ ] Dashboard exposes settings and diagnostics where applicable.
- [ ] SDK models and methods are updated.
- [ ] OpenAPI and Postman are regenerated and examples are updated.
- [ ] README, REST_API.md, and CHANGELOG.md are updated.
- [ ] Migration scripts exist for every supported database provider.
- [ ] Existing behavior is regression tested with new settings disabled.
- [ ] Security review items are checked for tenant isolation and redaction.
