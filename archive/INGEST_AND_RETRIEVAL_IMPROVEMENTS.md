# Ingest and retrieval improvements

> **Archived 2026-09-28.** Work on this plan ended with benchmark round 5; statuses below are final. Results are in
> [benchmarks/RESULTS.md](../benchmarks/RESULTS.md#round-5-rerank-rewrite-supersession-and-structure).

This plan lists what AssistantHub can adopt from two sibling projects, and from the services it already runs on, to
improve what it stores at ingestion and what it finds at query time. Each item is a unit of work a developer can pick
up, annotate and close. Each one covers the whole product: server, dashboard, tests, documentation and benchmark
evidence.

**Sources**

| Source | Location | What it contributed |
|---|---|---|
| Pneuma | `c:\code\pneuma` | Structure-preserving extraction (table rows keep their headers), per-cell summaries, MMR plus reading-order context assembly, cross-encoder or listwise rerank, content-hash delta skip, embedding cache |
| Isis | `c:\code\AgentMemory` (the Isis repository; there is no `c:\code\isis`) | Weighted RRF sweeps (k 20), conversation rewrite searched alongside the original, split-leg expansion (HyDE and keywords), small-k multi-query fusion, auto-attached cross-encoder rerank with a circuit breaker, supersession, model profiles and task prefixes, embed-only title headers, chunk budget fallback |
| Service audit | `c:\code\Partio`, `c:\code\RecallDB`, `c:\code\DocumentAtom`, `c:\code\Verbex` | Capabilities AssistantHub pays for but doesn't use, and upstream defects that affect it |
| AssistantHub benchmarks | [benchmarks/](../benchmarks/README.md), [benchmarks/RESULTS.md](../benchmarks/RESULTS.md) | Baseline numbers, and the ledger each item is verified against |

**Relationship to [RETRIEVAL_IMPROVEMENTS.md](RETRIEVAL_IMPROVEMENTS_2026-09.md).** That page ranks fixes drawn from
AssistantHub's own benchmark results (default changes, retries, thresholds). None of them have landed yet. This page
does not repeat them. Where an item here implements or extends one of them, it says so as `RI #n`. Work the two lists
together. The cheap default changes in RI (#1–#5) should land first, because they move the baseline that every item
here is measured against.

## Scoring

- **Integration simplicity (1–10).** 10 means a small, local change in one service with no schema, API or upstream
  dependency. Each of these lowers the score: new settings (a database column in four providers, plus API, dashboard
  and docs), a new external service, a re-ingest of existing collections, or a dependency on an upstream fix.
- **Value (1–10).** The expected effect on retrieval and answer quality for real users. Measured gains from Pneuma,
  Isis or AssistantHub's own benchmarks score higher than plausible but unmeasured ones.
- **Total** is simplicity plus value. Ties are broken by value.

## Summary

| ID | Enhancement | Area | Learned from | Simplicity | Value | Total | Status |
|---|---|---|---|---|---|---|---|
| [IR-01](#ir-01-cross-encoder-reranking) | Cross-encoder reranking with its own candidate pool, circuit breaker and abstention score | Retrieval | Isis, Pneuma | 5 | 9 | 14 | Done (round 5) |
| [IR-02](#ir-02-conversation-aware-retrieval) | Conversation-aware retrieval: rewrite the follow-up and search it alongside the original | Retrieval | Isis | 6 | 8 | 14 | Done (round 5) |
| [IR-03](#ir-03-dedicated-query-embedding-path) | Dedicated query-embedding path (`/v1.0/embed`, short timeout, no truncation) | Retrieval | Service audit | 8 | 6 | 14 | Done |
| [IR-04](#ir-04-explicit-fusion-configuration) | Explicit fusion configuration (RecallDB `Hybrid` block: strategy, `RrfK`, candidate pool) and a correct threshold scale | Retrieval | Isis, service audit | 8 | 6 | 14 | Done |
| [IR-05](#ir-05-keep-paragraph-and-heading-structure-in-extracted-text) | Keep paragraph and heading structure in extracted text | Ingestion | Service audit, Pneuma | 9 | 5 | 14 | Done |
| [IR-06](#ir-06-contextual-chunk-headers) | Contextual chunk headers: document title and heading path, embedded with each chunk | Ingestion | Isis, Pneuma (planned), DocumentAtom | 5 | 8 | 13 | Done (round 5), opt-in |
| [IR-07](#ir-07-embedding-model-profiles) | Embedding model profiles: task prefixes, input limits, chunk-size caps, budget fallback | Ingestion + retrieval | Isis | 6 | 7 | 13 | Partial: task prefixes (round 5) |
| [IR-08](#ir-08-extraction-settings-per-ingestion-rule) | Extraction settings per ingestion rule (OCR of embedded images, CSV/Excel headers) and correct type routing | Ingestion | Service audit | 7 | 6 | 13 | Done (round 5) |
| [IR-09](#ir-09-structured-cells-for-tables-and-lists) | Send tables and lists to Partio as typed cells (row groups keep their header row) | Ingestion | Pneuma, Partio | 4 | 8 | 12 | Done (round 5), opt-in |
| [IR-10](#ir-10-document-supersession) | Document supersession: newer versions outrank the documents they replace | Ingestion + retrieval | Isis | 5 | 7 | 12 | Done (round 5) |
| [IR-11](#ir-11-source-diversity) | Source diversity: collapse or cap chunks per document, optional MMR | Retrieval | Pneuma, Isis, RecallDB | 7 | 5 | 12 | Not started |
| [IR-12](#ir-12-retrieval-inspector-in-the-dashboard) | Retrieval inspector in the dashboard (stages, per-leg scores and ranks, flags) | Product | Pneuma, Isis | 7 | 5 | 12 | Done (round 5) |
| [IR-13](#ir-13-fault-isolation-for-utility-models) | Fault isolation for utility models: per-endpoint timeouts and a circuit breaker | Retrieval | Isis | 7 | 5 | 12 | Done (round 5), partly |
| [IR-14](#ir-14-reading-order-context-assembly) | Reading-order context assembly: group by document, order by position, merge adjacent chunks | Retrieval | Pneuma | 8 | 4 | 12 | Done (negative) |
| [IR-15](#ir-15-page-sheet-and-slide-provenance) | Page, sheet and slide provenance on chunks, in citations and as filters | Ingestion + retrieval | DocumentAtom, Pneuma | 5 | 6 | 11 | Done (round 5), partly |
| [IR-16](#ir-16-additive-query-expansion-and-multi-query-fusion) | Additive query expansion split by leg (hypothetical answer → vector, keywords → text) and small-k multi-query fusion | Retrieval | Isis | 5 | 6 | 11 | Not started |
| [IR-17](#ir-17-exact-identifier-matching) | Exact-identifier matching (quoted phrases, codes, versions) through RecallDB match modes and `Terms` | Retrieval | RecallDB, RI #3 | 6 | 5 | 11 | Not started |
| [IR-18](#ir-18-duplicate-and-near-duplicate-detection-at-ingest) | Duplicate and near-duplicate detection at ingest | Ingestion | Isis, Pneuma | 7 | 4 | 11 | Done (round 5) |
| [IR-19](#ir-19-filtered-search-recall-efsearch) | Keep filtered vector search from losing recall (`EfSearch` under filters) | Retrieval | Isis, RecallDB | 8 | 3 | 11 | Done |
| [IR-20](#ir-20-optional-recency-signal) | Optional recency signal in fusion | Retrieval | Isis, RecallDB | 8 | 3 | 11 | Done |
| [IR-21](#ir-21-query-embedding-cache) | Query-embedding cache | Retrieval | Pneuma | 8 | 3 | 11 | Done |
| [IR-22](#ir-22-verbex-hygiene) | Verbex hygiene: remove ignored parameters, use index options and filters | Retrieval | Service audit | 8 | 2 | 10 | Done |
| [IR-23](#ir-23-section-summaries-as-a-second-representation) | Section summaries as a second retrieval representation | Ingestion | Pneuma | 5 | 4 | 9 | Not started |

Suggested order: **IR-03, IR-04 and IR-05** first (cheap, and they fix measurement or correctness problems under
everything else), then **IR-01 and IR-02** (largest measured gains elsewhere), then the ingestion-structure group
**IR-06 → IR-09 → IR-15**, which build on each other.

## How to work an item

Every item has a status line and checklists for **Server**, **Dashboard**, **Tests**, **Docs** and **Benchmark**.
Tick each box as it lands, and update the Status column in the summary (`Not started` → `In progress` → `Done` or
`Dropped (reason)`). An item is done when every box is ticked (`[x]`) or marked not applicable (`[-]`) with a reason.

Conventions that apply to every item:

- **Settings.** A new assistant setting is a property on `src/AssistantHub.Core/Models/AssistantSettings.cs`, a column
  in all four providers (`src/AssistantHub.Core/Database/{Sqlite,Postgresql,Mysql,SqlServer}/Queries/TableQueries.cs`
  and `.../Implementations/AssistantSettingsMethods.cs`, with a migration for existing databases), the mock in
  `src/Test.Shared/MockAssistantSettingsMethods.cs`, a control in `dashboard/src/views/AssistantSettingsView.jsx`, and
  an entry in `openapi.json`, `postman/AssistantHub.postman_collection.json` and `REST_API.md`. A new ingestion-rule
  setting goes on `IngestionChunkingConfig` / `IngestionRule` and `dashboard/src/views/IngestionRulesView.jsx`.
- **Defaults.** Ship behavior changes behind a setting whose default preserves today's behavior, unless the item says
  otherwise. Change a default only after the benchmark shows a gain (see RI for the default-change list).
- **Re-ingest.** Items that change the text or chunks written at ingestion only affect newly ingested documents. They
  must bump an ingestion pipeline version that is part of the benchmark's ingestion hash, so the harness re-ingests
  instead of silently reusing stale collections, and the changelog must tell operators to reprocess documents.
- **Tests.** Unit and service tests go in `src/Test.Shared` (`ServiceSuite`, `ModelSuite`, `ApiSuite`), which all the
  runners (`Test.Automated`, `Test.Nunit`, `Test.Xunit`) pick up. New routes update the route count in `ApiSuite`.
- **Docs.** `CHANGELOG.md` always; `CHAT_DATA_FLOW.md` for retrieval-path changes; `README.md` when a user-visible
  capability is added.
- **Benchmark.** Measure before and after with the same dataset and settings, so both runs share a fingerprint in
  `benchmarks/history/runs.jsonl`. Use `history` and `compare --baseline previous` (see
  [benchmarks/README.md](../benchmarks/README.md#run-history)), and record the result in `benchmarks/RESULTS.md` and in the
  item's Benchmark checklist. If a benchmark dataset can't exercise the item, add or extend one.

---

## IR-01 Cross-encoder reranking

**Status:** Done (round 5): +0.03 nDCG@10 on Meridian, AssistantHub-docs and MultiHop-RAG · **Simplicity 5 · Value 9 · Total 14** · Implements RI #15 and enables RI #17

**Why.** In Isis, a small cross-encoder (`cross-encoder/ms-marco-MiniLM-L-6-v2` on HuggingFace TEI) is the largest
single measured gain: nDCG@10 0.878→0.925 (isis-live), 0.835→0.883 (Atlas), 0.683→0.712 (SciFact), 0.911→0.939
(LongMemEval). With a large LLM as reranker, its score separates answerable from unanswerable questions almost
perfectly (AUROC 0.96–0.99). AssistantHub's only reranker is an LLM prompt, and with `gemma3:4b` it makes things
worse: 0.789→0.694 on Meridian, 14% unusable replies at 10 candidates and 100% at 30, plus about 5 s per query.
Pneuma also supports an external cross-encoder with an LLM fallback.

**Today.** `AssistantSettings.EnableReranking`, `RerankerTopK`, `RerankerScoreThreshold`, `RerankPrompt` and
`RerankInferenceEndpointId` drive an LLM prompt in `AssistantChatService` (rerank stage around line 1178). The
candidate pool equals `RerankerTopK`.

**Third-party.** Partio, RecallDB and DocumentAtom offer no reranking. This needs a new endpoint type that speaks the
TEI (`/rerank`) and Cohere-style (`{query, documents, top_n}` → `relevance_score`) formats, and a rerank container
in the compose files.

- **Server**
  - [x] Add a rerank endpoint kind (TEI and Cohere formats) alongside the completion and embedding endpoints, with its *Done as a server-settings `Rerankers` list with `GET /v1.0/rerankers` and a test route, rather than a Partio endpoint kind.*
        own timeout and concurrency.
  - [x] Add `RerankerType` (`Llm` | `CrossEncoder`) and `RerankEndpointId` to assistant settings. Keep `Llm` as the default until measured.
  - [x] Separate the candidate pool (`RerankCandidates`, default `max(RetrievalTopK, 20)`) from the number kept (`RerankerTopK`).
  - [x] Always rerank against the original user question, not the rewritten query.
  - [x] Truncate passages to a fixed character budget (Isis uses title plus 1,200 characters; Pneuma uses 2,000).
  - [x] Add a circuit breaker: after a failure, skip rerank for 30 s and use fused order. A malformed reply falls back for that query only. *Opens after `Inference.CircuitBreakerFailures` (3) failures for `CircuitBreakerOpenMs` (30 s).*
  - [x] Keep the rerank score on each chunk (`rerank_score` in the retrieve response and chat retrieval details).
  - [x] Add an optional `RerankMinScore`: if every candidate scores below it, inject no context and let the answer model decline (RI #17). *Injects a "nothing relevant" note rather than an empty context.*
  - [ ] Add an optional rerank service to `docker/compose.yaml` and `benchmarks/docker/compose.yaml` (TEI, CPU image). *Only `benchmarks/docker/compose.yaml`; the README shows how to run TEI next to the main stack.*
- **Dashboard**
  - [ ] Add rerank endpoint management (list, create, test) next to the embedding and inference endpoint views. *The dashboard lists configured rerankers in assistant settings; rerankers are created in server settings, and `POST /v1.0/rerankers/{id}/test` has no dashboard view yet.*
  - [x] Add reranker type, endpoint, candidate pool and minimum score to `AssistantSettingsView.jsx`, with a warning when an LLM reranker uses a small model (RI #10).
- **Tests**
  - [x] TEI and Cohere request and response mapping.
  - [x] Candidate pool vs kept count.
  - [x] The circuit breaker opens and closes.
  - [x] A malformed reply falls back.
  - [x] `RerankMinScore` empties the context.
  - [ ] The original query is sent even when a rewrite ran. *Covered by review (`lastUserMessage` is passed), not by a dedicated test.*
- **Docs**
  - [x] `REST_API.md`, `openapi.json` and Postman: the endpoint routes and new settings.
  - [x] `CHAT_DATA_FLOW.md`: the rerank stage.
  - [x] `CHANGELOG.md`.
  - [x] A deployment note for the TEI container.
- **Benchmark**
  - [x] Meridian and AssistantHub-docs Hybrid with and without the cross-encoder at pools 10 and 20. *Measured at 30 candidates on three datasets, and at 10 and 30 on a Meridian subset.*
  - [ ] SciFact and NFCorpus anchors. *Not run.*
  - [x] Score AUROC on unanswerable questions.
  - [x] Record the latency added.

**Done when** the cross-encoder beats no-rerank on nDCG@10 on at least three datasets with p < 0.05, and the result
is in RESULTS.md. Consider making it the default for new assistants when a rerank endpoint exists (Isis
auto-attaches the tenant's first cross-encoder).

## IR-02 Conversation-aware retrieval

**Status:** Done (round 5): Meridian follow-ups 0.519 → 0.783 nDCG@10, other types unchanged · **Simplicity 6 · Value 8 · Total 14** · Implements RI #6 and part of RI #18

**Why.** Follow-up questions are AssistantHub's weakest type (Meridian nDCG 0.46–0.51, AssistantHub-docs
evidence@10 0.50). Retrieval sees only the last message ("and for the 220?"). Isis rewrites the follow-up into a
standalone question from the last 6 turns and searches the rewrite **in addition to** the original, at equal weight.
On its follow-up set, the share of questions whose evidence reached the prompt went from 0.953 to 1.000. Pneuma's
rewrite replaces the question and ignores earlier turns, which is the mistake to avoid. AssistantHub's current
rewrite also replaces the query (`retrievalQueries = rewrittenQueries`) and runs at the answer temperature.

- **Server**
  - [x] Add a `ConversationRewrite` step in `RunRetrievalStagesAsync`. It only runs when there is prior conversation,
        uses the last N turns (default 6, each capped at about 1,000 characters), resolves pronouns and ellipsis, and
        runs at temperature 0 with a 20 s timeout.
  - [x] Search the original and the rewrite and fuse them (weighted RRF, both at weight 1.0). On any failure, fall back to the original alone.
  - [x] Strip `<think>` blocks and parse strict JSON, as Isis does.
  - [x] Setting `EnableConversationRewrite` (default off at first; candidate default on) and a prompt override.
  - [x] Record the rewrite and whether it ran in chat retrieval details and the retrieve response (`stages`).
- **Dashboard**
  - [x] Add the toggle and prompt editor to `AssistantSettingsView.jsx`.
  - [ ] Show the rewritten query in chat history details. *It is in the chat response's retrieval details and the retrieve response, not in the history view.*
- **Tests**
  - [ ] Only the last N turns are used. *Implemented (six turns), not separately tested.*
  - [x] The original is always searched.
  - [x] Fallback on timeout or malformed output.
  - [x] The step is skipped on the first turn.
  - [x] Temperature is 0.
- **Docs**
  - [x] `CHAT_DATA_FLOW.md`, `REST_API.md`, `openapi.json`, Postman, `CHANGELOG.md`.
- **Benchmark**
  - [ ] Meridian and AssistantHub-docs follow-up type (retrieval with `messages`, and chat). *Meridian retrieval only; no chat round or AssistantHub-docs run.*
  - [x] Confirm non-follow-up types are unchanged.

## IR-03 Dedicated query-embedding path

**Status:** Done, except long-query dataset cases (unit-tested only) · **Simplicity 8 · Value 6 · Total 14**

**Why.** Query embedding goes through Partio's chunking route (`POST /v1.0/process` with `Type="Text"`) and keeps
only `Chunks[0]` (`RetrievalService.cs`, `EmbedQueryAsync`, around lines 686–709). This has three effects:

- A query longer than the default 256 chunk tokens, such as a rewritten or conversation-expanded one, is silently
  truncated.
- The request inherits the ingestion timeout (`Chunking.RequestTimeoutMs`, 15 minutes), so one stalled
  query-embedding call can hang a chat request for many minutes. This happened in benchmark round r3: the SciFact
  Hybrid run stopped making progress for 50 minutes against a model proxy that was otherwise answering in seconds.
- `L2Normalization` is set at ingestion but never on the query. That's harmless with cosine, but becomes wrong if
  another metric is ever used.

- **Server**
  - [x] Embed queries with Partio `POST /v1.0/embed` (no chunking) and the same `L2Normalization` as the collection's ingestion rule. *Done. `L2Normalization` is not sent: only cosine is indexed, where it has no effect, and retrieval does not know which ingestion rule wrote the collection.*
  - [x] Give query embedding its own timeout (default about 30 s), separate from `Chunking.RequestTimeoutMs`. *`Chunking.QueryEmbeddingTimeoutMs`, default 30000; a timed-out attempt is not retried.*
  - [x] Keep the existing transient retries and the `embedding_failed` flag.
  - [-] When the query exceeds the model's input limit (see IR-07), truncate explicitly and flag it in retrieval details. *n/a until IR-07 adds model input limits; the query is no longer truncated to one chunk.*
  - [x] Implement RI #8 here: when the query embedding fails in Hybrid, run keyword-only rather than returning nothing. *`keyword_fallback_ran` flag on chat and retrieve responses.*
- **Dashboard**
  - [-] n/a, apart from showing the new flags in the retrieval inspector (IR-12).
- **Tests**
  - [x] Long queries are not truncated.
  - [x] The query timeout fires independently of the ingestion timeout.
  - [-] Normalization matches the ingestion rule. *n/a, see Server.*
  - [x] Keyword fallback runs when the embedding fails.
- **Docs**
  - [x] `CHAT_DATA_FLOW.md`, the configuration reference for the new timeout, `CHANGELOG.md`. *Plus README config table, REST_API.md, docker configs.*
- **Benchmark**
  - [x] Confirm no change in nDCG on the standard datasets. *Round 4b: threshold-0 runs identical to round 3 on all four datasets.*
  - [ ] Add long-query cases to the AssistantHub-docs dataset.
  - [x] Confirm `load` p99 is unaffected. *Improved: stub load 10–40 → 47–289 ops/s, p95 157–1,415 ms → 35–153 ms (with IR-21).*

## IR-04 Explicit fusion configuration

**Status:** Done · **Simplicity 8 · Value 6 · Total 14** · Extends RI #1 and RI #3

**Why.** AssistantHub never sends RecallDB's `Hybrid` object, so Hybrid always runs RRF with k=60 and RecallDB's
default candidate pool. The documentation on `AssistantSettings.TextWeight` describes a linear blend
(`(1 - TextWeight) * vector + TextWeight * text`), which isn't what runs; the weight actually scales each leg's RRF
term. `RetrievalScoreThreshold` (0.3) is therefore compared against RRF-scaled fused scores, where a chunk found only
by keyword search clears 0.3 only at rank 1.

In today's benchmarks the threshold makes no measurable difference to Hybrid (AssistantHub-docs 0.853 at both 0.3
and 0, Meridian 0.788 vs 0.785), so the value here is mostly tuning and correctness. Isis swept RRF k and moved from
60 to 20, and Pneuma exposes k, the weights and the pool.

**Third-party.** RecallDB `Hybrid.Strategy` (`Rrf` | `Linear`), `RrfK`, `CandidatePool`. Confirm that the RecallDB
image AssistantHub ships supports them (RecallDB advertises `search.hybrid.rrf`).

- **Server**
  - [x] Send `Hybrid { Strategy, RrfK, CandidatePool }` in `BuildSearchBody` (`RetrievalService.cs`, around lines 257–304) when the mode is Hybrid. *Verified against the shipped RecallDB v0.2.1 image.*
  - [x] Add settings `FusionStrategy` (default `Rrf`), `RrfK` (default 60 until swept), `CandidatePool` (default: RecallDB's). *`FusionStrategy`, `RrfK`, `FusionCandidatePool`; persisted in all four database providers with v0.17.0 migrations; SDK models updated.*
  - [x] Apply `RetrievalScoreThreshold` on a scale that means something for the mode. In Hybrid, apply it to `vector_score` (the per-leg evidence the retrieve route already reports), or drop it for Hybrid. This subsumes RI #1 for FullText. *Hybrid: vector-only chunks are held to their similarity; every chunk the text leg found is kept (a first version that thresholded every chunk with a vector score lowered AssistantHub-docs Hybrid nDCG@10 0.853 → 0.827 in r4 and was corrected). FullText: not thresholded (explicit `collection_search` `score_threshold` still applies).*
  - [x] Fix the `TextWeight` XML doc and every place it is described.
- **Dashboard**
  - [x] Add fusion strategy, RRF k and candidate pool under an "Advanced retrieval" section in `AssistantSettingsView.jsx`. *In the Hybrid block of the retrieval settings rather than a separate section.*
  - [x] Fix the text-weight help text.
- **Tests**
  - [x] The request body carries the `Hybrid` object.
  - [x] The threshold applies to the per-leg score in Hybrid.
  - [x] Defaults are unchanged when the settings are absent.
- **Docs**
  - [x] `REST_API.md`, `openapi.json`, Postman, `CHAT_DATA_FLOW.md`, `CHANGELOG.md`. *Postman n/a: its settings example carries no retrieval fields.*
  - [x] Document the actual fusion formula.
- **Benchmark**
  - [x] Sweep `RrfK` (10, 20, 40, 60) and `TextWeight` (0.3, 0.5, 0.7) on AssistantHub-docs, Meridian and MultiHop-RAG. *RRF k 60 is best or tied everywhere. Text weight 0.7 is best on Meridian (0.812) and MultiHop-RAG (0.699) and on the mean, and costs AssistantHub-docs 0.02. FullText at the default threshold: 0.000 → 0.79 / 0.79 / 0.68. See benchmarks/RESULTS.md, Round 4.*
  - [x] Pick defaults and feed them to RI #3. *Keep `RrfK` 60; RI #3 updated with the round 4 text-weight sweep. Defaults left unchanged.*

## IR-05 Keep paragraph and heading structure in extracted text

**Status:** Done · **Simplicity 9 · Value 5 · Total 14**

**Why.** `DocumentAtomAtomizationService.AppendBlock` joins atoms with a single `\n` (around line 466). Partio's
`ParagraphBased` strategy splits only on blank lines, so for any extracted document it sees one giant paragraph and
degrades to sentence splitting. The strategy is selectable in the dashboard but doesn't do what it says.

DocumentAtom also reports each heading's `HeaderLevel`, which AssistantHub drops: `AtomResponse` has no such field,
so headings become plain lines. Keeping the level (rendered as markdown `#`) is the cheap first step toward
contextual headers (IR-06), and it gives the embedding model and the answer model visible structure.

- **Server**
  - [x] Join blocks with `\n\n`.
  - [x] Add `HeaderLevel` (and, for IR-15, `PageNumber` and `SheetName`) to `AtomResponse`. *`HeaderLevel` only; page and sheet are left to IR-15.*
  - [x] Render headings as `#` × level. *Capped at six; markdown sources that already start with `#` are left alone.*
  - [x] Bump the ingestion pipeline version (see conventions) so reprocessing is detectable. *The server has no pipeline version to bump; the benchmark harness tags uploads with `bench_pipeline` (now 2) and re-ingests documents with another version, without changing the fingerprint. `--pipeline 1` reuses older collections.*
- **Dashboard**
  - [-] n/a. Optionally show the extracted text preview with structure on `DocumentsView.jsx`.
- **Tests**
  - [x] Blocks are separated by blank lines.
  - [x] Headings are rendered with the right level.
  - [x] Paragraph chunking produces more than one chunk on a multi-paragraph PDF or DOCX fixture. *Verified end to end (Partio is not available to unit tests): ParagraphBased gives 1,009 chunks on AssistantHub-docs.*
  - [x] Extraction output is identical across OSes (keep `\n`, as today).
- **Docs**
  - [x] `CHANGELOG.md` (with a reprocessing note).
  - [x] The ingestion-rule help text on what ParagraphBased does. *`IngestionRuleFormModal.jsx` strategy tooltip.*
- **Benchmark**
  - [x] Re-ingest AssistantHub-docs and Meridian with FixedTokenCount (expect no change) and ParagraphBased (expect a change). *FixedTokenCount: no significant change (0.853 → 0.837, p = 0.12; Meridian 0.788 → 0.784). ParagraphBased: reachability 100% / 99.4%, evidence@10 0.756 / 0.773, Meridian nDCG 0.800.*
  - [x] Record reachability and nDCG. *benchmarks/RESULTS.md, Round 4.*

## IR-06 Contextual chunk headers

**Status:** Done (round 5), opt-in: mixed results with structured cells; `ContextHeader` stays `None` by default · **Simplicity 5 · Value 8 · Total 13** · Depends on IR-05

**Why.** A chunk from the middle of a section doesn't say which document or section it belongs to, so questions that
name the product, section or topic miss it. Isis prepends `title: summary` to each chunk's **embedding text only**,
keeping the stored text and the keyword index unchanged. It measured isis-live 0.859→0.878 together with other
changes, and exact-identifier questions dropped (0.833→0.740) until reranking recovered them, which argues for
embedding-only headers plus IR-01. Pneuma plans the same (link title plus cell title). DocumentAtom can produce a
heading breadcrumb itself (`ContextualizeHeaders`), but its chunker skips nested atoms, so it can't simply be
switched on.

**Third-party.** Partio `ContextPrefix` is one static string per ingestion rule, prepended for embedding. A per-chunk
header needs either a per-cell `ContextPrefix` (one cell per section, see IR-09), or AssistantHub splitting sections
itself and sending each as its own cell. Also check the Partio defect that `ContextPrefix` isn't counted against
`FixedTokenCount`.

- **Server**
  - [x] Build `Document title > H1 > H2` for each section from the atom hierarchy (IR-05).
  - [x] Send each section as its own Partio cell with the header as its `ContextPrefix`, capped at about 25% of the chunk budget. *In Structured cell mode. In Flat mode only the `Title` header applies.*
  - [x] Keep the stored chunk text unchanged.
  - [x] Setting on the ingestion rule: `ContextHeader` (`None` | `Title` | `TitleAndHeadings`), default `None` until measured.
  - [x] Tag each chunk with its section path (RecallDB tag) for citations and filtering.
- **Dashboard**
  - [x] Add the option in `IngestionRulesView.jsx`.
  - [ ] Show the section path in the chunk view and in citations. *Shown in the retrieval inspector; citations carry pages and sheet but not the section.*
- **Tests**
  - [x] Header construction from nested headings.
  - [x] The budget cap is respected.
  - [ ] Stored text excludes the header. *Relies on Partio's embed-only `ContextPrefix`; not tested here.*
  - [ ] `None` reproduces today's chunks exactly. *Not tested.*
- **Docs**
  - [x] Ingestion-rule docs, `CHANGELOG.md` (reprocessing note).
- **Benchmark**
  - [x] AssistantHub-docs and Meridian with `TitleAndHeadings` vs `None`, per question type. Watch exact-identifier questions.
  - [x] Record reachability.

## IR-07 Embedding model profiles

**Status:** Partial (round 5): query and document task prefixes per model family (`EmbeddingModelProfiles`, assistant `EmbeddingTaskPrefixes`, rule `TaskPrefixes`); input limits, chunk-size caps and the retry ladder are not started · **Simplicity 6 · Value 7 · Total 13** · Implements RI #14's prefix half

**Why.** Models such as nomic-embed-text, e5, bge, mxbai and snowflake expect task prefixes (`search_query:` /
`search_document:`), and AssistantHub sends none. Switching to nomic without prefixes already gave +0.02 to +0.05 in
AssistantHub's benchmarks. Isis keeps a profile per model (prefixes, maximum input tokens, a tokenizer margin). It
caps chunks at 75% of the model's window, and at most 256 tokens. When the endpoint rejects a chunk as too long, it
re-chunks at 0.75, 0.5 and then 0.3 of the budget instead of failing. AssistantHub chunks at a fixed token count that
the embedding model may count differently (for example, all-minilm has a 256 WordPiece limit), so chunk tails can be
silently cut off when embedded. This needs verifying per model.

**Third-party.** Partio has no prefix or output-dimension options. The document prefix can go in `ContextPrefix`
(embedding-only). The query prefix is under AssistantHub's control once IR-03 lands.

- **Server**
  - [ ] Add a profile registry keyed by model name pattern (prefixes, maximum input tokens, recommended chunk size), with an override on the embedding endpoint.
  - [ ] Apply the query prefix in the query path (IR-03) and the document prefix through `ContextPrefix`, combined with any IR-06 header.
  - [ ] Cap the ingestion rule's chunk size to the profile's safe maximum, and warn in the ingestion log when a rule asks for more.
  - [ ] On a context-length error from Partio, retry the document at smaller chunk sizes before failing it.
- **Dashboard**
  - [ ] Show the detected profile on `EmbeddingEndpointsView.jsx`, with prefix and maximum-token overrides.
  - [ ] Warn in `IngestionRulesView.jsx` when the chunk size exceeds the model's limit.
- **Tests**
  - [ ] Profile matching.
  - [ ] Prefixes applied on both sides.
  - [ ] The chunk-size cap.
  - [ ] The retry ladder on a simulated context-length error.
- **Docs**
  - [ ] Embedding endpoint docs listing the supported profiles, `CHANGELOG.md` (re-embed note).
- **Benchmark**
  - [ ] nomic with and without prefixes on AssistantHub-docs, Meridian, SciFact and NFCorpus.
  - [ ] Truncation check: reachability measured on embedded text versus chunk text for all-minilm.

## IR-08 Extraction settings per ingestion rule

**Status:** Done (round 5); not benchmarked (no scanned or spreadsheet fixture yet) · **Simplicity 7 · Value 6 · Total 13**

**Why.** AssistantHub calls DocumentAtom with `Settings = null` (`DocumentAtomAtomizationService.cs`, around line
107), so every document type uses DocumentAtom's defaults. Some improvements that are available but unused:

- OCR of images embedded in PDF, DOCX and PPTX (`ExtractAtomsFromImages`). Scanned or image-heavy PDFs yield little text today.
- CSV `HasHeaderRow` / `RowsPerAtom`, and Excel `HeaderRowScoreThreshold`.

The type routing also has gaps:

- jpeg, gif, tiff and webp are all sent to `/atom/png`.
- Legacy doc, xls and ppt are sent to the OOXML routes, which can't parse them.

- **Server**
  - [x] Add an `Extraction` block to `IngestionRule` (OCR embedded images, OCR language, CSV header row, rows per atom, Excel header threshold), and pass it as DocumentAtom settings. *OCR language is not exposed.*
  - [x] Fix the route map. Reject legacy binary Office formats with a clear, recorded reason (or convert them), instead of sending them to the wrong route.
- **Dashboard**
  - [x] Add an Extraction section in `IngestionRulesView.jsx`.
  - [x] Show the rejection reason for unsupported types in `DocumentsView.jsx`. *The document status message carries the reason.*
- **Tests**
  - [x] Settings are passed through.
  - [x] Route mapping for every image type and the legacy formats. *Tested for tsv, pdf and the legacy formats.*
  - [ ] The OCR path on a scanned PDF fixture. *Not done: no fixture.*
- **Docs**
  - [x] Ingestion-rule docs, the supported-types table, `CHANGELOG.md`.
- **Benchmark**
  - [ ] Add a small scanned-PDF and spreadsheet slice to the AssistantHub-docs or Meridian corpus, and record ingest success and reachability before and after. *Not done.*

## IR-09 Structured cells for tables and lists

**Status:** Done (round 5), opt-in: `CellMode` `Structured`. Mixed results; the default stays `Flat` · **Simplicity 4 · Value 8 · Total 12** · Enables IR-06, IR-15, IR-23

**Why.** AssistantHub flattens the whole document into one `Type="Text"` cell. Partio's table strategies (`Row`,
`RowWithHeaders`, `RowGroupWithHeaders`, `KeyValuePairs`, `WholeTable`) and list strategies (`WholeList`,
`ListEntry`) therefore can't be used. `RowGroupSize` is forwarded but has no effect, and choosing a table strategy
fails validation.

A table chunk cut mid-row loses its column headers, so "what is the limit for the Pro plan?" can't match the row that
answers it. Pneuma turns each table row into a one-row markdown table with the header repeated, so every value keeps
its column context. Partio's `RowGroupWithHeaders` does the same natively.

**Third-party.** Partio typed and nested cells (`Children`, `ParentGUID`), plus two upstream defects:

- Summarization with a table or list strategy fails, because the summary child inherits a strategy it can't use.
- Fixed-token boundary drift (RI #12).

- **Server**
  - [x] Build a cell tree from DocumentAtom atoms. Each heading section becomes a Text cell (IR-06), each table a Table cell, each list a List cell.
  - [x] Add per-type strategies to the ingestion rule (text, table and list strategy, with defaults FixedTokenCount / RowGroupWithHeaders / WholeList).
  - [x] Map the returned chunks back to document positions, so neighbors and citations still work.
  - [x] Guard the Partio summarization defect: disable summarization for typed cells until it is fixed upstream. *Only text cells are summarized.*
- **Dashboard**
  - [x] Add per-type strategy selectors in `IngestionRulesView.jsx`, with the help text explaining each.
- **Tests**
  - [x] Cell-tree construction for mixed documents.
  - [x] Table rows keep headers.
  - [ ] Chunk positions stay contiguous for neighbor retrieval. *Not tested.*
  - [ ] Flat mode reproduces today's behavior. *Not tested; Flat mode takes the unchanged code path.*
- **Docs**
  - [x] Ingestion-rule docs, `CHANGELOG.md` (reprocessing note).
  - [ ] File the Partio summarization defect upstream. *Not filed yet.*
- **Benchmark**
  - [ ] Add table-heavy questions (Meridian has spec tables; add a spreadsheet document) and compare flat vs typed cells. *Compared on the existing Meridian table questions (0.904 → 0.862); no spreadsheet document added.*
  - [x] Record reachability.

## IR-10 Document supersession

**Status:** Done (round 5): superseded questions 0.722 → 0.908, hit@1 0.42 → 0.92 · **Simplicity 5 · Value 7 · Total 12** · Implements RI #16

**Why.** When a newer document replaces an older one, both are retrieved and the old one often ranks first (Meridian
superseded questions 0.73). Isis stores an explicit `supersedes` link. At read time it follows the chain and, by
default, **demotes** the replaced item, placing the replacement where the stale one ranked, even if search missed
it. Its superseded-fact questions improved from 0.718 to 0.876.

- **Server**
  - [x] Add `SupersedesDocumentIds` on document upload and update, with a reverse `SupersededBy` stored on the replaced document and as a RecallDB tag. *Stored on the documents, not as a RecallDB tag. `PUT /v1.0/documents/{id}/supersedes` sets it after upload.*
  - [x] Release the link when the replacement is deleted.
  - [x] Add a post-fusion step in `RunRetrievalStagesAsync` with mode `Demote` (default) / `Hide` / `Include`. Mark superseded chunks in the prompt as outdated. *An explicit scope (attached documents, or a filter the replacement fails) keeps the old version, marked outdated.*
  - [x] Add an assistant setting for the mode.
  - [ ] Optionally infer supersession for crawled documents whose URL is unchanged but whose content changed (crawlers already track MD5). *Not done.*
- **Dashboard**
  - [x] Add a supersedes picker on the document detail in `DocumentsView.jsx`, and show a "superseded by" badge.
  - [x] Add the mode selector in assistant settings.
- **Tests**
  - [x] Chain resolution (A→B→C).
  - [x] Each mode.
  - [x] Link release on delete.
  - [ ] Crawler inference. *Not applicable.*
- **Docs**
  - [x] `REST_API.md`, `openapi.json`, Postman, `CHAT_DATA_FLOW.md`, `CHANGELOG.md`.
- **Benchmark**
  - [x] Add `supersedes` links to the Meridian superseded pairs, and compare the superseded question type per mode.

## IR-11 Source diversity

**Status:** Not started · **Simplicity 7 · Value 5 · Total 12**

**Why.** One long document can fill every top-K slot, crowding out the second source a multi-part question needs.
MultiHop-RAG is AssistantHub's weakest dataset (Hybrid 0.637, evidence@10 0.43). Pneuma runs MMR by default (λ 0.7,
word-overlap similarity). Isis has MMR but found no measurable gain, so treat this as a hypothesis to test rather than
a sure win.

**Third-party.** RecallDB `Collapse` (by `DocumentId` or a tag, with its own candidate pool) does per-document
grouping in the database.

- **Server**
  - [ ] Add a `MaxChunksPerDocument` setting (0 means off), enforced after fusion and before reranking.
  - [ ] Optionally use RecallDB `Collapse` when the setting is 1.
  - [ ] Add an optional `Diversity` λ for MMR over the candidate pool. Use embedding similarity when the vectors are available, word overlap otherwise.
- **Dashboard**
  - [ ] Add both settings under Advanced retrieval.
- **Tests**
  - [ ] The cap per document.
  - [ ] MMR drops near-duplicates.
  - [ ] Off reproduces today's order.
- **Docs**
  - [ ] Settings docs, `CHAT_DATA_FLOW.md`, `CHANGELOG.md`.
- **Benchmark**
  - [ ] MultiHop-RAG `All@10` and evidence@10, and Meridian multi-document questions, at caps 0, 2 and 3 and λ 0.7.

## IR-12 Retrieval inspector in the dashboard

**Status:** Done (round 5) · **Simplicity 7 · Value 5 · Total 12**

**Why.** The `POST /v1.0/assistants/{id}/retrieve` route already returns the stages (search, fused, attachment
filter, rerank), per-leg scores and ranks, and the pipeline flags (`embedding_failed`, `hybrid_fallback_ran`,
`rerank_parse_failed`). No UI uses it. Pneuma and Isis both surface per-hit evidence in their tools, and it is the
fastest way for an operator to see why a question failed and which knob to change. Every other item here is easier to
tune with it.

- **Server**
  - [x] n/a (the route exists). Optionally add a `settings_override` body field so unsaved settings can be tried without saving.
- **Dashboard**
  - [x] Add a "Test retrieval" panel on `AssistantSettingsView.jsx`, and extend `CollectionSearchView.jsx`. Show: *The Retrieval Inspector on `AssistantSettingsView.jsx`; `CollectionSearchView.jsx` is unchanged.*
        - the query, and the rewrites if any;
        - a table per stage with document, position, fused, vector and text scores and ranks, and the rerank score;
        - the flags;
        - a diff between two settings.
- **Tests**
  - [x] An API test for `settings_override` if added.
  - [x] Dashboard build.
- **Docs**
  - [x] A dashboard guide section, `CHANGELOG.md`. *`CHANGELOG.md` only.*
- **Benchmark**
  - [x] n/a.

## IR-13 Fault isolation for utility models

**Status:** Done (round 5), partly: global utility timeout and circuit breaker, answer retry · **Simplicity 7 · Value 5 · Total 12** · Fixes the RI "InferenceService 100 s timeout" defect and RI #9

**Why.** Rerank, rewrite, answerability and gate calls use `InferenceService` with `HttpClient`'s fixed 100 s
timeout. On a slow shared model server, 23 of 24 rerank calls timed out in one run, and every chat paid for the wait.
Isis gives each query step a 20 s timeout, falls back to the original query, and puts rerank behind a 30 s circuit
breaker. The answer model is also not retried on transient 429/502/503 errors (RI #9).

- **Server**
  - [ ] Add a per-endpoint `TimeoutMs` on completion endpoints, and a per-step default (utility steps 20 s). *A global `Inference.UtilityTimeoutMs` (30 s) and `RequestTimeoutMs` instead.*
  - [x] Add a shared circuit breaker per endpoint and step (open after N consecutive failures, half-open after 30 s), with a flag in retrieval details. *Closes after `CircuitBreakerOpenMs`; a skipped rerank sets `rerank_skipped`.*
  - [x] Retry the answer model on transient status codes with jittered backoff. *Linear backoff (`RetryDelayMs`), not jittered.*
- **Dashboard**
  - [ ] Add a timeout field in `InferenceEndpointsView.jsx`. *Not done (global setting).*
  - [ ] Show breaker state in the endpoint health view. *Not done.*
- **Tests**
  - [ ] Timeout per endpoint. *Not applicable (global setting).*
  - [x] Breaker open, half-open and close.
  - [x] Answer retry on 429, and no retry on 400.
- **Docs**
  - [x] Endpoint docs, `TELEMETRY.md` (the new metrics), `CHANGELOG.md`. *README and REST docs; no new metrics.*
- **Benchmark**
  - [ ] A chat run against the GB10 proxy under concurrency 4: failed requests and p95 latency before and after. *Not run.*

## IR-14 Reading-order context assembly

**Status:** Done, negative result: reading order lowered chat accuracy; default stays `Score` · **Simplicity 8 · Value 4 · Total 12**

**Why.** AssistantHub orders the injected chunks by score only (`AssistantChatService`, around line 1087). With
neighbors on, adjacent chunks of one document can appear separated and out of order, and overlapping chunks repeat
text. Pneuma groups the selected chunks by source, orders the groups by best score, and orders chunks within a group
by position, which gives the answer model coherent passages.

- **Server**
  - [x] After selection, group chunks by document, order the groups by best score and the chunks by position, and merge adjacent or overlapping chunks, removing duplicated overlap text. *`RetrievalContextOrder` in Core, applied in every chat path; the retrieve route keeps ranking order. Prompt-budget trimming still drops from the end, which in reading order is the tail of the lowest-ranked document.*
  - [x] Add a setting `ContextOrder` (`Score` | `ReadingOrder`), default `Score` until measured.
  - [x] Keep citation numbering stable. *Citations are built from the same ordered list.*
- **Dashboard**
  - [x] Add the option under Advanced retrieval. *`Context Order` select in the retrieval settings.*
- **Tests**
  - [x] Grouping and ordering.
  - [x] Overlap merge.
  - [x] Citations still map to the right document. *Merged passages keep their document; covered by the ordering test.*
- **Docs**
  - [x] `CHAT_DATA_FLOW.md` (citation section), `CHANGELOG.md`.
- **Benchmark**
  - [x] Chat accuracy and faithfulness on Meridian and AssistantHub-docs with neighbors 1, `Score` vs `ReadingOrder`. *Meridian only: accuracy 0.660 → 0.500 (p = 0.048), evidence in prompt 0.940 → 0.880, faithfulness 0.776 → 0.796. Negative; `Score` stays the default. A follow-up would trim to the prompt budget in rank order before reordering.*

## IR-15 Page, sheet and slide provenance

**Status:** Done (round 5), partly: provenance tags, citations and prompt labels. Page filters and slide numbers are not done · **Simplicity 5 · Value 6 · Total 11** · Depends on IR-05, and easier after IR-09

**Why.** DocumentAtom reports `PageNumber`, `SheetName` and slide position for each atom, and AssistantHub discards
them. Citations can only point at a whole document, and users can't scope a question to pages or sheets.

- **Server**
  - [x] Carry the page, sheet and slide range of each chunk into RecallDB tags (`page_start`, `page_end`, `sheet`, `slide`). *As `ah_page_start`, `ah_page_end`, `ah_sheet` and `ah_section`. Slides use the page number DocumentAtom reports.*
  - [x] Return them on citations and in retrieval details.
  - [ ] Allow them in metadata filters. Numeric comparisons need RecallDB to compare numbers as numbers: its tag `GreaterThan` / `LessThan` compare strings today, so report that upstream or zero-pad. *Possible through tag filters on the zero-padded values, but not exposed or tested.*
- **Dashboard**
  - [x] Show "p. 12–13" and the sheet name in citations and the chunk view. *In the prompt's source labels, citations and the retrieval inspector.*
  - [ ] Add a page filter in the retrieval inspector. *Not done.*
- **Tests**
  - [x] Tags for PDF, XLSX and PPTX fixtures. *Tested with extracted blocks and structured cells, not per-format fixtures.*
  - [x] Citation formatting.
  - [ ] Filter behavior. *Not tested.*
- **Docs**
  - [x] `CHAT_DATA_FLOW.md` and `REST_API.md` (citations and metadata filters), `CHANGELOG.md`.
- **Benchmark**
  - [ ] Add page-scoped questions to a PDF-heavy dataset (Qasper papers are candidates) and record citation precision. *Not done.*

## IR-16 Additive query expansion and multi-query fusion

**Status:** Not started · **Simplicity 5 · Value 6 · Total 11** · Implements RI #18

**Why.** AssistantHub's rewrite replaces the query and gained only +0.010 for 2.5 s. Isis generates a short
hypothetical answer (searched on the vector leg only) and up to 8 keywords (searched on the text leg only), and fuses
them at weight 0.5. The fusion uses RRF with a small constant (k=5), so the extra queries re-rank the original's
candidates rather than displace them. It measured SciFact 0.683→0.720 and LongMemEval 0.911→0.932 for about 2 s.
Isis turns expansion on automatically only when no reranker is attached, because it adds nothing on top of a
cross-encoder.

- **Server**
  - [ ] Add an expansion step that produces `{hypothetical_answer, keywords}` as strict JSON at temperature 0, with a timeout and fallback.
  - [ ] Search the original (weight 1.0) plus the hypothetical answer (vector leg, weight 0.5) plus the keywords (text leg, weight 0.5), then fuse with a configurable small RRF k.
  - [ ] Setting `QueryExpansion` (`Off` | `On` | `Auto` = only without a cross-encoder).
  - [ ] Keep today's replace-style rewrite for compatibility, but deprecate it.
- **Dashboard**
  - [ ] Add the expansion mode and prompt in `AssistantSettingsView.jsx`.
  - [ ] Show the expansions in the retrieval inspector.
- **Tests**
  - [ ] Leg routing.
  - [ ] Weights.
  - [ ] Fusion constant.
  - [ ] Fallback.
  - [ ] `Auto` disables itself when a cross-encoder is set.
- **Docs**
  - [ ] `CHAT_DATA_FLOW.md`, settings docs, `CHANGELOG.md`.
- **Benchmark**
  - [ ] SciFact, NFCorpus, Meridian and MultiHop-RAG, with expansion off/on and with and without IR-01. Record latency.

## IR-17 Exact-identifier matching

**Status:** Not started · **Simplicity 6 · Value 5 · Total 11** · Extends RI #3

**Why.** Keyword-only search beats default Hybrid on lexical questions (Meridian 0.92 vs 0.82), because identifiers,
error codes, version strings and quoted phrases are where embeddings are weakest. AssistantHub sends every query with
RecallDB's default any-term match. RecallDB also offers `MatchMode` `Phrase` and `WebSearch` (quotes and `-term`),
`MinimumShouldMatch`, and `Terms` (a trigram substring filter or boost for exact tokens).

- **Server**
  - [ ] Detect quoted phrases and identifier-like tokens (digits mixed with letters, dotted versions, `ERR_…`, paths).
  - [ ] For quoted phrases, send `MatchMode = WebSearch`.
  - [ ] For identifiers, add a third fused leg or a `Terms` requirement on a secondary search.
  - [ ] Setting `ExactMatchBoost` (off, or a weight).
- **Dashboard**
  - [ ] Add the option under Advanced retrieval.
- **Tests**
  - [ ] Token detection.
  - [ ] The request carries `MatchMode` / `Terms`.
  - [ ] Off reproduces today's request.
- **Docs**
  - [ ] Settings docs, `CHANGELOG.md`.
- **Benchmark**
  - [ ] Meridian lexical and identifier questions, and the AssistantHub-docs API-name questions.

## IR-18 Duplicate and near-duplicate detection at ingest

**Status:** Done (round 5); ingest overhead not measured · **Simplicity 7 · Value 4 · Total 11**

**Why.** Duplicate documents waste top-K slots and confuse citations. Isis runs a vector search with a new item's
first chunk and flags matches at cosine ≥ 0.85 (flag only, never merge). Pneuma skips re-ingest when the content hash
is unchanged. AssistantHub crawlers already compare MD5, but uploads do not.

- **Server**
  - [x] Store a content hash per document.
  - [x] On upload, report an exact duplicate in the same collection (setting: `Allow` | `Warn` | `Reject`).
  - [x] After ingestion, run a near-duplicate check (first chunk, threshold 0.85, top 3) and store the matches on the document.
- **Dashboard**
  - [x] Add a duplicate badge and a link to the matches in `DocumentsView.jsx`.
  - [x] Add the policy in the ingestion rule.
- **Tests**
  - [x] Hash match for each policy.
  - [x] The near-duplicate threshold.
- **Docs**
  - [x] Document and ingestion docs, `CHANGELOG.md`.
- **Benchmark**
  - [ ] n/a for quality. Record ingest overhead in the `ingest` report. *Not done: overhead not recorded.*

## IR-19 Filtered search recall (`EfSearch`)

**Status:** Done (no measurable effect at benchmark scale) · **Simplicity 8 · Value 3 · Total 11**

**Why.** HNSW search with a restrictive filter (labels, tags, attached documents) can return fewer than K rows, or
the wrong ones. Isis raises `EfSearch` to 1000 whenever a filter is present. RecallDB auto-sizes it but caps it at
1000, with no iterative scan.

- **Server**
  - [x] Send `Vector.EfSearch` when label, tag or document filters are active. Make it configurable, with a sensible default. *`RecallDb.FilteredEfSearch`, default 400, 0 to disable.*
- **Dashboard**
  - [-] n/a (advanced configuration only).
- **Tests**
  - [x] The request carries `EfSearch` only when filtered.
- **Docs**
  - [x] `CHANGELOG.md`.
- **Benchmark**
  - [x] Meridian filtered questions: filter precision and recall@10 before and after. *No change: filter precision 1.000 and Hybrid filter-question recall@10 1.000 before and after. The 160-document corpus is too small for filtered HNSW scans to come up short.*

## IR-20 Optional recency signal

**Status:** Done (value unmeasured; needs a date-ordered ingest) · **Simplicity 8 · Value 3 · Total 11**

**Why.** For corpora where newer means more correct (release notes, policies), Isis adds recency as a third RRF term
(weight 0.1), which breaks near-ties in favor of the newer document. RecallDB supports `Hybrid.RecencyWeight`. For most
document collections, supersession (IR-10) is the better tool, so keep this opt-in.

- **Server**
  - [x] Setting `RecencyWeight` (default 0), sent in the `Hybrid` object (IR-04).
- **Dashboard**
  - [x] Add the field under Advanced retrieval. *`Recency Weight` slider in the Hybrid block, shown for RRF fusion.*
- **Tests**
  - [x] The request carries the weight.
  - [x] 0 means today's behavior.
- **Docs**
  - [x] Settings docs, `CHANGELOG.md`.
- **Benchmark**
  - [x] Meridian superseded and temporal questions at 0, 0.05 and 0.1. *0.788 / 0.790 / 0.790. Not a real test: the benchmark ingests every document at once, so creation time carries no signal; rerun with `--date-order`.*

## IR-21 Query-embedding cache

**Status:** Done · **Simplicity 8 · Value 3 · Total 11**

**Why.** Repeated and suggested questions, retries, and the retrieve-then-chat pattern embed the same text again.
Pneuma keeps a process-wide LRU keyed by (endpoint, SHA-256 of the text). It cuts latency and model load, and reduces
the embedding rejections that caused silent empty retrieval in round 0.

- **Server**
  - [x] An LRU cache (default 10,000 entries) keyed by endpoint ID, model, normalization and text hash, in the query path (IR-03). Add hit and miss metrics. *`QueryEmbeddingCache`, keyed by endpoint and query hash; normalization is not part of the key because queries are not normalized (see IR-03). Metric `retrieval.query_embedding.cache`.*
- **Dashboard**
  - [-] n/a. *No dashboard surface.*
- **Tests**
  - [x] A hit on repeat.
  - [x] A miss when the endpoint or normalization changes. *Endpoint and query.*
  - [x] Eviction.
- **Docs**
  - [x] `TELEMETRY.md`, `CHANGELOG.md`.
- **Benchmark**
  - [x] `load` retrieve scenario p50 and p99 with repeated queries. *Stub load p50 86–802 ms → 18–110 ms and 10–40 → 47–289 ops/s (with IR-03); the load runner repeats a fixed question set, so this is close to the best case.*

## IR-22 Verbex hygiene

**Status:** Done, except reporting the Verbex defects upstream · **Simplicity 8 · Value 2 · Total 10**

**Why.** The `verbex_full_text_search` tool sends `RequiredTerms` and `ExcludedTerms`, which are not fields on
Verbex's search request, so they are silently ignored (`AssistantToolExecutor.cs`, around lines 674–689). Verbex
indexes are created without `EnableLemmatizer` or `EnableStopWordRemover`, which can't be changed later, and its
search-side `Labels` and `Tags` filters are unused. Verbex's own scoring (fixed TF-IDF squashed through a sigmoid, a
tokenizer mismatch between indexing and search, and a limit applied before scoring) makes it a poor candidate for the
main retrieval path. Keep it as a tool and fix only the plumbing.

- **Server**
  - [x] Apply required and excluded terms client-side, or map them to `UseAndLogic`. *Side queries (AND for required, OR for excluded) find the matching records; the main search over-fetches and is filtered against them.*
  - [x] Set lemmatizer and stop-word options at index creation (new indexes only). *`Verbex.EnableLemmatizer`, `EnableStopWordRemover`, `MinTokenLength`, `MaxTokenLength`, in ingestion and tenant provisioning; off by default.*
  - [x] Pass label and tag filters. *Labels; Verbex search has no tag filter.*
- **Dashboard**
  - [-] n/a. *No dashboard surface.*
- **Tests**
  - [x] Required and excluded terms take effect.
  - [x] Index options are sent.
- **Docs**
  - [x] `REST_API.md` (tool reference), `CHANGELOG.md`. *`REST_API.md` has no per-tool argument reference; the tool schema descriptions carry it. Changelog updated.*
  - [ ] Report the Verbex defects upstream.
- **Benchmark**
  - [-] n/a. *No retrieval-quality effect to measure.*

## IR-23 Section summaries as a second representation

**Status:** Not started · **Simplicity 5 · Value 4 · Total 9** · Depends on IR-09

**Why.** Pneuma writes an LLM summary for each extracted cell of 128 characters or more and indexes it alongside the
body text (`chunkKind=summary`, pointing to the same cell), so abstract questions can match a summary even when no
single passage does. It has not measured the benefit. AssistantHub already calls Partio summarization, but with one
flat cell there is only a whole-document hierarchy. Once IR-09 exists, per-section summaries become possible.

- **Server**
  - [ ] Enable summarization per section cell. Tag summary chunks `chunk_kind=summary`.
  - [ ] Resolve a summary hit to its section's body chunks before injecting context.
- **Dashboard**
  - [ ] Show summary chunks distinctly in the chunk view.
- **Tests**
  - [ ] Summary chunks are tagged.
  - [ ] Hits on a summary resolve to body chunks.
- **Docs**
  - [ ] Ingestion docs, `CHANGELOG.md`.
- **Benchmark**
  - [ ] Meridian and MultiHop-RAG with and without summaries.
  - [ ] Record ingest cost (LLM calls per document).

---

## Third-party capabilities that AssistantHub doesn't use

Where AssistantHub already uses a capability, it is omitted. "Used by" shows whether Pneuma or Isis relies on it.

| Service | Capability | Used by | AssistantHub today | Item |
|---|---|---|---|---|
| RecallDB | `Hybrid.Strategy` / `RrfK` / `CandidatePool` | Isis (RRF, k 20) | Not sent; RRF k 60 by default | IR-04 |
| RecallDB | `FullText.MatchMode` (`All`, `Phrase`, `WebSearch`), `MinimumShouldMatch` | Isis relies on any-term | Default any-term only | IR-17 |
| RecallDB | `Terms` (trigram exact substring) | — | Unused | IR-17 |
| RecallDB | `Collapse` by document or tag | Isis (server-side collapse) | Unused | IR-11 |
| RecallDB | `Vector.EfSearch` | Isis (1000 under filters) | Unused | IR-19 |
| RecallDB | `Hybrid.RecencyWeight`, `CreatedAfter` / `CreatedBefore` | Isis (recency 0.1) | Unused | IR-20 |
| RecallDB | Server-side `MinimumScore` / `Vector.MinimumScore` | — | Threshold applied client-side after the limit | IR-04 |
| Partio | Typed cells (Table, List) and their strategies | Pneuma (in-house equivalent) | Always one Text cell | IR-09 |
| Partio | Nested cells (`Children`) with a per-cell `ContextPrefix` | — | Unused | IR-06, IR-09 |
| Partio | `/v1.0/embed` for queries | — | Queries go through `/process` | IR-03 |
| DocumentAtom | Per-type settings (OCR of embedded images, CSV/Excel headers, JSON/XML depth) | — | `Settings = null` | IR-08 |
| DocumentAtom | `HeaderLevel`, `PageNumber`, `SheetName` on atoms | Pneuma (cell titles, planned) | Dropped | IR-05, IR-15 |
| DocumentAtom | Built-in chunking with `ContextualizeHeaders` | — | Unused (blocked by a nested-atom defect) | IR-06 |
| Verbex | Index options (lemmatizer, stop words, token lengths), search `Labels` / `Tags` | — | Unused | IR-22 |
| TEI / Cohere-style rerank API | Cross-encoder reranking | Isis (default), Pneuma (optional) | No such endpoint type | IR-01 |

Not available from any of these services: sparse or learned-sparse vectors, multi-vector (ColBERT-style) retrieval,
built-in reranking, and embedding task prefixes. The last is handled in AssistantHub by IR-07.

## Upstream defects to report

These are in the services, not in AssistantHub. File them with their owners and link the issues here.

| Service | Defect | Affects | Issue |
|---|---|---|---|
| Partio | `ChunkingHelpers.ChunkByTokenSpans` advances by the token count of a re-tokenized slice, which duplicates or drops text at boundaries (about 1% of text; caps AssistantHub-docs reachability near 90%). The sentence and paragraph boundary adjustment drifts too. | Default FixedTokenCount chunking (RI #12) | |
| Partio | A summary child cell inherits a table or list strategy and fails validation | IR-09, IR-23 | |
| Partio | `ContextPrefix` is not counted against `FixedTokenCount` on `/process` | IR-06, IR-07 | |
| Partio | Upstream 429 reported as `500 InternalError` (AssistantHub now treats it as transient) | Ingestion under load | |
| RecallDB | Vector-only thresholds applied after the result limit (short pages); full-text-only search falls back to the vector threshold | IR-04 | |
| RecallDB | Tag `GreaterThan` / `LessThan` compare strings (`'10' < '9'`); `Contains` doesn't escape `%` and `_` | IR-15, metadata filters | |
| DocumentAtom | Nested atoms are not chunked, so built-in chunking can't be used with hierarchy | IR-06 | |
| DocumentAtom | OCR language hard-coded to English; PDF atoms not in reading order; DOCX hyperlink text dropped; PPTX titles have no text | IR-08 | |
| Verbex | Tokenization differs between indexing and query; the limit is applied before scoring; `TotalCount` is wrong | IR-22 | |

## Considered and not recommended now

| Idea | Source | Why not |
|---|---|---|
| Knowledge-graph extraction and GraphRAG community summaries for "global" questions | Pneuma | Large build (an LLM classification pass per cell, a graph store, community detection), unmeasured in Pneuma, and AssistantHub's datasets don't exercise global questions. Revisit if users ask corpus-wide thematic questions. |
| Fusing Verbex into the main retrieval path | — | Verbex scoring (fixed TF-IDF, sigmoid-compressed) is weaker than RecallDB full-text search, which already provides the keyword leg. |
| Query decomposition at full weight | Isis | Isis measured Atlas 0.831→0.764 at full weight with RRF k 60. It is neutral at best with the small-k fusion in IR-16, which covers the useful part. |
| Salience and access-count ranking | Isis (planned) | Not implemented or measured in Isis. |
