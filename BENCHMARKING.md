# AssistantHub benchmarking plan

This plan brings AssistantHub to parity with the benchmark suite in the Isis repository (`c:\code\agentmemory\benchmarks`
and `src/Test.Benchmark`), and adapts it to what AssistantHub is: a document RAG platform with an ingestion pipeline,
not an agent-memory store. The goal is the same as it was for Isis. We want numbers we can trust, compared with public
reference points, broken down finely enough to show where effort will improve retrieval quality.

> **Status (2026-09-24).** Phases 0–5 are implemented: the `/retrieve` route and per-leg scores, the harness in
> `src/Test.Benchmark`, the isolated stack and datasets in `benchmarks/`, and the CI gate script. Round 0 has been run.
> How to run it is in [benchmarks/README.md](benchmarks/README.md). The numbers are in
> [benchmarks/RESULTS.md](benchmarks/RESULTS.md), and the ranked fixes are in
> [RETRIEVAL_IMPROVEMENTS.md](RETRIEVAL_IMPROVEMENTS.md). Phase 6 (agent-in-the-loop) remains optional and unbuilt.

The plan covers:

1. What the Isis suite does, and which parts of its method carry over.
2. What AssistantHub has today, and the gaps.
3. The harness to build: its commands, datasets, metrics and reports.
4. Hypotheses from reading the code, to confirm or reject in round 0.
5. A phased delivery plan with exit criteria.
6. How the results turn into a ranked list of retrieval improvements.

---

## 1. What the Isis suite does

Isis (`c:\code\agentmemory`) shares most of its stack with AssistantHub: RecallDB 0.2.1 on pgvector, Ollama embeddings
and inference, OpenTelemetry/Prometheus, and a .NET 10 server with REST and MCP surfaces. Its benchmark suite has these
properties, and they are what AssistantHub should copy:

| Property | How Isis does it |
|---|---|
| **Black-box harness** | `src/Test.Benchmark` talks to the server only over REST and MCP. It never references server assemblies, so the same commands can measure a working tree or a deployment. |
| **Isolated stack** | `benchmarks/docker/compose.yaml` runs its own pgvector and RecallDB on non-default ports. It never touches the dev stack. |
| **One neutral dataset format** | `BenchmarkDataset` holds corpora, documents and labelled queries (`relevant` ids, optional graded relevance, `type`, `category`, gold `answer`). An empty `relevant` list marks an unanswerable question. Converters bring BEIR and LongMemEval into the format (`prepare`). |
| **Mixed datasets** | Two committed, purpose-built datasets (real project memories and a synthetic corpus with superseded facts, near-duplicates and long documents) plus two public ones: SciFact as a sanity check against published numbers, and LongMemEval-S as a domain benchmark. |
| **Standard IR metrics** | Hit@1, Recall@1/5/10, All@5/10, MRR@10 and nDCG@10 with linear gain (pytrec_eval/BEIR compatible), per search mode and per query type. |
| **Abstention signal** | AUROC of the top-hit score (fused and raw vector) as a classifier of answerable versus unanswerable questions. |
| **End-to-end chat** | An independent LLM judge, called directly rather than through the server, scores accuracy and abstention. The report also covers citation precision and recall, and splits accuracy into "evidence reached the prompt" versus "evidence missed", which separates retrieval failures from generation failures. |
| **Agent-in-the-loop** | Claude Code runs headless with and without the MCP server. The no-memory arm is the floor. |
| **Load** | Closed-loop throughput and latency percentiles per concurrency level, optionally against a stub embedding server so the model is out of the picture. |
| **Stage breakdown** | Prometheus histograms are scraped before and after each phase to get server-side time per stage. |
| **Regression gate** | `compare` diffs two reports and exits non-zero on regression, so it can gate CI. |
| **Reproducibility** | Deterministic scope names allow reuse of ingested data (`--reingest` forces a rebuild). Each report records the git commit (`-dirty` when uncommitted), machine, endpoints and config. Stratified sampling uses a fixed seed. |
| **Rounds and ablations** | `RESULTS.md` records numbers round by round. `RETRIEVAL_IMPROVEMENTS.md` scores every candidate fix for value and simplicity and records the measured result. Sweeps (recency weight, chunk overlap) reuse ingested scopes. |

Only the Isis methodology carries over. Its results measure a different system on different data, so this plan does not
use them as baselines, targets or evidence. AssistantHub's own round 0 sets its baseline. Expect that first run to be a
bug hunt as much as a measurement.

## 2. Where AssistantHub stands

### 2.1 What exists

| Area | Today |
|---|---|
| Retrieval | `RetrievalService` calls RecallDB with `Vector`, `FullText` (TsRank/TsRankCd) or `Hybrid` (RecallDB's single-call search with a `TextWeight`, default 0.3). It applies a raw-score `RetrievalScoreThreshold` (default 0.3) and optional neighbor expansion (`RetrievalIncludeNeighbors`). Hybrid falls back to vector-only when it returns nothing. |
| Pipeline stages | Optional retrieval gate, query rewrite (multi-query fused with RRF in `RetrievalFusionHelper`, k=60), LLM listwise rerank (0–10 scores, `RerankerScoreThreshold`, `RerankerTopK`), answerability check (`LogOnly` or strict modes), context compaction, citations. |
| Ingestion | Upload → DocumentAtom extraction → optional summarization → Partio chunking (default `FixedTokenCount` 256, configurable overlap, strategy, `ContextPrefix`) → Partio embedding → RecallDB. Verbex runs alongside for inverted-index search. |
| Evaluation | In-product **Eval**: `EvalFact` (question + expected facts + category from `EvalFact.RecommendedCategories`), `EvalRun`/`EvalResult` with a PASS/FAIL LLM judge per expected fact. It records `RetrievalJson`, citations, query class and answerability decision. `ChatRail` and `InferenceOnly` execution modes are available from the dashboard, REST and MCP. |
| Telemetry | OTel spans and metrics (`assistanthub.operation.duration`, `retrieval.results`, `inference.tokens`, `ingestion.*`) exported to Prometheus/Tempo/Loki. Per-request `PerformanceJson` with stage timings sits on `ChatHistory`. The dashboard has Assistant Analytics. |
| Tests | Touchstone suites (`Test.Shared`, `Test.Automated`, xUnit/NUnit adapters) cover functional behavior, not retrieval quality. |

### 2.2 Gap analysis against Isis

| Isis capability | AssistantHub status | Gap |
|---|---|---|
| Black-box benchmark harness | None | **Build** `src/Test.Benchmark` |
| Isolated benchmark stack | Only the full dev stack in `docker/compose.yaml` | **Build** `benchmarks/docker/compose.yaml` on non-default ports |
| Labelled retrieval datasets (relevant ids) | EvalFacts have expected facts but no relevance labels | **Build** a dataset format and datasets; no nDCG/Recall/MRR is possible today |
| Public reference datasets + converters | None | **Build** BEIR converter (port from Isis) plus document-RAG converters |
| IR metrics per mode and query type | None | **Build** (port `RetrievalMetrics` directly) |
| Abstention AUROC | Answerability check exists but is never scored | **Build**: score both the retrieval scores and the answerability classifier |
| Independent LLM judge | Eval judges with the **assistant's own inference endpoint and model** | **Build** a direct judge in the harness. Optionally add a separate judge endpoint setting to Eval. |
| Evidence-in-prompt split | `RetrievalJson` is stored but not analysed | **Build** into the chat runner |
| Citation precision/recall | Citations are emitted but not scored | **Build** |
| Stage breakdown | Per-request `PerformanceJson` is richer than Isis's Prometheus scrape | **Reuse**: read per-request stage timings, with a Prometheus scrape as backup |
| Load test with stub embeddings | None | **Build** (port `LoadRunner`, `StubEmbeddingServer`) |
| `compare` regression gate | None | **Build**, and add significance testing (§3.6) |
| Round-by-round RESULTS.md and ranked improvement list | None | **Write** after round 0 |
| Agent benchmark over MCP | MCP mirrors the admin API. There is no ask/retrieve tool. | Optional, later phase (§5, Phase 6) |

AssistantHub also has pieces with **no Isis counterpart**, and these need their own measurements:

- **The ingestion pipeline.** Extraction from PDF, HTML and DOCX, summarization and chunking strategies all decide
  whether an answer is retrievable at all. Isis ingests plain text.
- **Multi-stage retrieval.** The gate, rewrite, rerank and answerability steps are each a place to gain or lose
  quality. Each must be measured on its own ("stage lift") as well as end to end.
- **Metadata and document filters.** Label/tag filters and `attached_document_ids` must return only in-scope content,
  which can be measured as precision.
- **Conversation context.** The gate and rewrite behave differently on follow-up turns, so the data needs multi-turn
  queries.

## 3. The harness

### 3.1 Layout

Mirror Isis so the two suites stay recognisably the same and code can move between them:

```
benchmarks/
  README.md                 how to stand up the stack, prepare data, run, and read results
  RESULTS.md                numbers that matter, round by round
  docker/compose.yaml       isolated stack on non-default ports
  datasets/                 committed datasets (assistanthub-docs.json, meridian/ + meridian.json)
  data/                     git-ignored downloads and converted public datasets
  results/                  git-ignored per-run reports (<utc>-<kind>-<name>.json and .md)
  run-baseline.sh / .bat    the standard suite
  start-bench-server.sh / .bat
src/Test.Benchmark/         black-box harness (no AssistantHub assembly references)
RETRIEVAL_IMPROVEMENTS.md   ranked fixes with value/simplicity scores and measured results (after round 0)
```

The isolated stack runs pgvector, RecallDB, Partio, DocumentAtom and Less3 on non-default ports. It uses the host's
Ollama, plus an AssistantHub server built from the working tree with its own settings file and SQLite database. Pin
the image **versions** to the dev compose (RecallDB 0.2.1, Partio 0.5.0, DocumentAtom 3.1.2), and have every report
record the image **digests**.

### 3.2 Commands

| Command | Measures | Isis analogue |
|---|---|---|
| `prepare` | Converts BEIR, MultiHop-RAG and Qasper downloads into the harness format | `prepare` |
| `ingest` | Document ingest on its own: success rate by content type, extraction failures, docs/s, chunks per doc, token distribution, empty or duplicate chunks, and **evidence reachability** (§3.5) | part of provisioning |
| `retrieval` | IR metrics per search mode, per pipeline stage (raw, +rewrite, +rerank) and per query type. Also latency, stage breakdown, abstention AUROC and filter precision. | `retrieval` |
| `chat` | End-to-end answers through `POST /v1.0/assistants/{id}/chat`: judged accuracy, abstention, faithfulness, citation P/R, the evidence-in-prompt split, latency and tokens | `chat` |
| `load` | Search and chat throughput and latency percentiles per concurrency level, optionally with stub embeddings | `load` |
| `compare` | Diffs two reports with a significance test and exits non-zero on regression | `compare` |
| `eval-export` / `eval-import` | Moves datasets to and from in-product EvalFacts, so a benchmark set can be run from the dashboard and customers' EvalFacts can become benchmark sets | new |
| `stub` | Runs the stub embedding server on its own | `stub` |

### 3.3 Provisioning model

Isis maps one corpus to one scope. In AssistantHub, one **corpus × variant** maps to one tenant-scoped collection, an
ingestion rule and an assistant:

- **Collection and ingestion rule.** Names are deterministic from (dataset, corpus, ingestion-rule hash, embedding
  model, suffix), so a rerun reuses already-ingested content. `--reingest` rebuilds. Chunking and embedding variants
  get separate collections through `--scope-suffix`, as in Isis.
- **Documents.** Each is uploaded with `PUT /v1.0/documents` (`Base64Content`, `ContentType`, `IngestionRuleId`,
  `Labels`, `Tags`) with the tag `bench_doc_id=<dataset id>`. The harness polls `GET /v1.0/documents/{id}` until the
  status is `Completed` or `Failed`, reads the processing log on failure, and keeps a map from AssistantHub document id
  to dataset id. Retrieval hits (`RetrievalChunk.DocumentId`) are translated back to dataset ids through that map.
- **Assistants.** The harness creates one assistant per retrieval **configuration** (mode, TextWeight, threshold,
  topK, neighbors, rewrite/rerank/gate on or off, prompts), all pointing at the same collection. A sweep therefore
  needs no re-ingest and no server-side overrides, and every assistant's settings are written into the report.
- **Dated documents.** These are written in date order, so crawler-style update semantics and any future recency
  signal can be tested, as in Isis.
- **Real extraction.** Documents are ingested from their native format (PDF, HTML, DOCX, MD) whenever the dataset has
  one, so DocumentAtom extraction is part of what is measured.

### 3.4 Dataset format

Start from the shape of the Isis `BenchmarkDataset` schema (corpora, documents, labelled queries, graded relevance,
optional gold answer, an empty `relevant` list for unanswerable questions), so its converters port with little change,
and extend it:

| Field | Addition |
|---|---|
| `document.file`, `document.contentType` | Ingest a native file (relative to the dataset) instead of `body` |
| `document.labels`, `document.tags` | Exercise metadata filters |
| `document.version`, `document.supersedes` | Superseded-document questions (policy v1 → v2) |
| `query.evidence[]` | Verbatim gold passages, used for chunk-level metrics and reachability |
| `query.metadataFilter`, `query.attachedDocuments` | Filter and attachment questions, which also give filter-precision scoring |
| `query.conversation[]` | Prior turns, for gate and rewrite testing on follow-ups |
| `query.category` | Aligned with `EvalFact.RecommendedCategories` (`factual_lookup`, `multi_hop`, `aggregation`, `temporal`, `ambiguous_query`, `unanswerable`, `citation_required`, `structured_data`) |
| `query.type` | Retrieval difficulty, reusing Isis's vocabulary where it applies: `paraphrase`, `lexical`, `multi`, `detail`, `superseded`, `confusable`, `filter`, `table`, `followup`, `negative` |

`category` describes what the question asks. `type` describes what makes retrieving its evidence hard. Reports break
results down by both.

### 3.5 Metrics

**Document-level retrieval** (comparable with published BEIR numbers). Collapse the ranked chunk list to a ranked document list
by first occurrence, the "MaxP" convention. Then compute Hit@1, Recall@1/5/10, All@5/10, MRR@10 and nDCG@10 with
linear gain. `RetrievalMetrics.cs` ports verbatim.

**Chunk-level retrieval** (new, and the level that matters for RAG):

- *Evidence recall@k.* The fraction of a query's `evidence` passages contained in the top-k chunks. Containment uses
  normalized text: case, whitespace and punctuation folded, with a token-overlap threshold of at least 0.8 for
  passages that span chunk boundaries.
- *Context precision.* The fraction of chunks actually injected into the prompt that come from relevant documents.
- *Evidence reachability.* The fraction of evidence passages present in **any** stored chunk of the collection. This
  is the ceiling set by extraction and chunking. A passage that is unreachable is lost before search even runs, and
  no ranking change will recover it.

**Stage lift** (new). For each query, record the ranked list after each stage: raw search, then multi-query RRF, then
rerank, then answerability filter. Report nDCG@10 and evidence recall@k at each stage, plus:

- *Rerank lift*: the nDCG change from rerank, and the share of relevant chunks the reranker dropped.
- *Rerank ceiling*: evidence recall@(rerank input size). The reranker can never do better than this.
- *Rewrite lift*: the nDCG change from rewrite, per query type.
- *Gate accuracy*: the rate at which the gate skipped retrieval when the query needed it, and the reverse, using
  queries labelled `followup` or `needs-retrieval`.
- *Parse-failure rate* for the rerank and answerability JSON. The rerank code falls back silently today.

**Abstention.**

- Score-AUROC (answerable vs `negative`) for the fused/combined score, the raw vector score, the text score and the
  rerank score.
- The answerability classifier's precision and recall for "unsupported", with a confusion matrix.

**Filters.** Precision of returned chunks against the filter or attachment set, which should be 1.0, and recall within
the set.

**Chat (end to end).**

- Accuracy on answerable questions and abstention accuracy on negatives, graded by the independent judge.
- Accuracy split by evidence retrieved versus evidence missed.
- Citation rate, precision and recall, taken from the response's citation sources, with no text parsing needed.
- **Faithfulness**: the share of the answer's claims supported by the injected context, graded by the judge from the
  context the response reports. This catches right answers taken from the model's own knowledge, which AssistantHub's
  "grounded" positioning rules out.
- Latency p50/p95, tokens per answer and stage timings from `PerformanceJson`.

**Ingest.** Success rate per content type, docs/s, chunks per doc, tokens-per-chunk distribution, the share of empty
or near-duplicate chunks, and time per stage from the processing log.

**Load.** ops/s, p50/p95/p99 and error rate per concurrency level, for search-only, chat-only and mixed workloads.

### 3.6 Rigor

This is where the plan goes further than Isis:

- **Confidence intervals.** Every headline metric gets a 95% bootstrap interval (1,000 resamples over queries). The
  per-type tables report `n`, and types with n < 20 are flagged as indicative.
- **Significance in `compare`.** A paired bootstrap (or paired randomization) test on per-query deltas, in addition to
  Isis's fixed tolerance. A change counts as a regression or an improvement only when it clears both the tolerance and
  p < 0.05.
- **Judge independence and calibration.** The judge is called directly (Ollama or OpenAI-compatible), never through
  AssistantHub, and never with the model under test. Headline runs use a stronger judge than the answer model. Hand-label
  a fixed set of 50 answers once and report the judge's agreement (Cohen's κ) with every headline chat result.
- **Variance.** Chat runs repeat 3 times at temperature 0 and report the mean ± sd. A retrieval run is deterministic,
  so a second run is a smoke check that must match exactly.
- **Provenance.** Each report records the git commit (with `-dirty`), image digests, Ollama model digests
  (`/api/show`), the full assistant settings and ingestion rule used (with a hash), the dataset file hash, the machine
  and the harness version.
- **Labels are good, not gold.** Record label provenance per dataset, as Isis does. Questions for the synthetic corpus
  are written by a pass that did not write the corpus, then checked against it.

### 3.7 Datasets

Three tiers. Tier A anchors AssistantHub to published numbers. Tier B is the industry-standard test for document RAG.
Tier C is committed and tailored to AssistantHub's own features.

| Tier | Dataset | Size (run) | Why |
|---|---|---|---|
| A | **BEIR SciFact** | 5,183 docs, 300 queries | Sanity check against published numbers. Reference points include BM25 (nDCG@10 about 0.665, BEIR paper) and each embedding model's own published SciFact score (MTEB). Semantic mode should land on the model's published score; a shortfall means ingestion or chunking is losing quality. |
| A | **BEIR NFCorpus** | 3,633 docs, 323 queries | Graded relevance and heavy lexical mismatch. BM25 is about 0.325. |
| A | BEIR FiQA-2018 (optional) | 57,638 docs, 648 queries | Short financial Q&A posts with a large dense-over-BM25 gap. Run it only once ingest throughput is known, since every document goes through the full upload → extract → chunk → embed path. |
| B | **MultiHop-RAG** | 609 news articles, stratified sample of 300 queries | Industry benchmark for multi-document RAG. Its query types (inference, comparison, temporal, null) map onto `multi_hop`, `temporal` and `unanswerable`, and it has gold evidence and answers. |
| B | **Qasper** | about 50 papers, about 250 questions | Question answering over long scientific papers, with evidence paragraphs and unanswerable questions. It stresses chunking, neighbor expansion and detail retrieval. |
| B | FinanceBench open subset (optional) | 150 questions over SEC filings (PDF) | Stresses PDF and table extraction (`structured_data`). The PDFs are large, so it comes last. |
| C | **`assistanthub-docs`** (committed) | About 15 real repo docs (README, REST_API, MCP_API, CHAT_DATA_FLOW, TELEMETRY, TESTING, CHANGELOG, …), about 120 questions | Real documents in the real formats, exact identifiers (routes, setting names) for `lexical`, and version history for `temporal`. |
| C | **`meridian`** (committed, synthetic) | About 150 documents in mixed formats (MD, HTML, PDF, DOCX) with labels/tags, about 300 questions | An enterprise knowledge base: a policy handbook with superseded versions, near-duplicate product manuals, tables (`structured_data`), long runbooks, label/tag-filtered and attachment-scoped questions, multi-turn follow-ups and negatives. The questions come from a separate pass. |

Public datasets are downloaded at run time into `benchmarks/data/` and never committed. Check each license upstream
before publishing derived data.

## 4. Hypotheses to test in round 0

Reading the code suggests the following. None of them has been measured. The purpose of round 0 is to confirm or
reject each one with numbers.

| # | Hypothesis | Where | How round 0 tests it |
|---|---|---|---|
| H1 | **The single score threshold is wrong for most modes.** `RetrievalScoreThreshold` (default 0.3) is applied to the store's `Score` in every mode. FullText `Score` is a TsRank value (typically 0.01–0.1), Vector `Score` is an all-MiniLM cosine similarity (often below 0.3 for relevant chunks), and Hybrid `Score` is RecallDB's rank-fused score normalized to [0, 1]. With the default `TextWeight` of 0.3, a keyword-only hybrid hit scores at most 0.3, so the threshold drops every FullText hit, many Vector hits, and keyword-only Hybrid hits below rank 1. | `RetrievalService.RetrieveAsync` | Sweep the threshold per mode. Count queries returning zero chunks. |
| H2 | **The reranker can't recover misses.** Rerank sees only the `RetrievalTopK` (10) chunks search returned, so a relevant chunk ranked 11th or lower is never considered. Common practice is to retrieve 3–10× the final k and rerank down. | `AssistantChatService` (the rerank block) | Rerank ceiling versus Recall@50 of raw search. |
| H3 | **The LLM rerank is coarse and fragile.** Integer 0–10 scores produce ties, and malformed JSON falls back silently. | `AssistantChatServiceBase._DefaultRerankPrompt` | Parse-failure rate, tie rate, rerank lift and rerank-score AUROC. |
| H4 | **Vector is the wrong default.** Hybrid search usually outperforms vector-only on mixed lexical/semantic queries, but AssistantHub has never measured it. | `AssistantSettings.SearchMode = "Vector"` | Mode comparison across all datasets, and a `TextWeight` sweep over {0.1, 0.3, 0.5, 0.7}. |
| H5 | **Chunks lack document context.** Chunks are embedded without their document's title or summary. `ContextPrefix` is a per-rule setting, not per document. Adding a header may help paraphrase queries and hurt exact-identifier queries. | `IngestionServiceBase`, Partio | Ablate summarization and prefix. Report paraphrase and lexical types separately. |
| H6 | **Eval scores aren't independent.** The in-product Eval judges with the assistant's own endpoint and model, so a weak model grades itself. | `EvalService.ExecuteRunAsync` | Agreement of the Eval verdict with the independent harness judge on the same answers. |
| H7 | **Extraction and chunking lose evidence** in PDFs and tables before search runs. | DocumentAtom, Partio | Evidence reachability per content type on `meridian` and Qasper/FinanceBench. |
| H8 | **Answerability scores barely separate** answerable from unanswerable questions, so no threshold can reliably say "nothing relevant". | Answerability check | Classifier precision/recall on `negative` queries, and score AUROCs. |

## 5. Delivery plan

Each phase ends with a report committed to `benchmarks/RESULTS.md`, or with working commands. The phases are ordered so
the cheapest, highest-signal measurements (retrieval) land first.

### Phase 0: Server hooks for measurement (small, AssistantHub side)

These are the only server changes needed to benchmark properly. Each gets a Touchstone test.

1. **A retrieval-only dry run.** Add `POST /v1.0/assistants/{assistantId}/retrieve` (admin-only). It runs the chat
   rail's gate, rewrite, search, fusion, rerank and answerability steps **without final inference**, and returns every
   stage's ranked chunks with scores, the rewritten queries, the gate decision, parse-failure flags and stage timings.
   Without it, measuring rerank or rewrite needs a full chat call per query, and the raw
   `/v1.0/collections/{id}/search` passthrough makes the caller supply embeddings. It must share its code path with
   chat, not copy it, so the benchmark measures what users get.
2. **Per-leg scores.** Add `VectorScore` to `RetrievalChunk`/`SearchResult`, next to the existing `TextScore`, so
   score AUROCs and threshold analysis work in every mode.
3. **Visible fallbacks.** Surface `HybridFallbackRan`, rerank parse failures and answerability parse failures in the
   chat response's `retrieval` block and in `PerformanceJson`.
4. **A Prometheus scrape target for the bench stack.** Either an OTel collector with a Prometheus exporter on a fixed
   port, or a direct `/metrics` on the server. It is the fallback source of stage timings for `load`.

**Exit:** the routes are documented in REST_API.md and OpenAPI, covered by tests, and the chat path is unchanged.

### Phase 1: Harness skeleton, isolated stack, retrieval, round 0

- Port from Isis as-is: `BenchmarkArguments`, the dataset classes (extended per §3.4), `RetrievalMetrics`,
  `LatencyStats`, `ReportWriter` (JSON plus Markdown), `BenchmarkEnvironment` (commit, machine, digests),
  `BeirConverter`.
- Write: `AssistantHubClient` (REST), `CollectionProvisioner` (collection, ingestion rule, upload, poll, id map,
  deterministic reuse), `AssistantProvisioner` (one assistant per configuration), and `RetrievalRunner` against
  `/retrieve`.
- Datasets: SciFact, NFCorpus, and a first cut of `assistanthub-docs`.
- **Round 0:** all three modes (Vector, FullText, Hybrid) at default settings, plus the H1/H4 sweeps. Record ingest failures and fix any
  harness-blocking defects before the numbers are published.

**Exit:** RESULTS.md round 0 has nDCG, Recall and MRR per mode and per type, with CIs, SciFact
and NFCorpus compared with their published reference points, and a defect list.

### Phase 2: Pipeline stages and ingestion

- Stage-lift reporting (rewrite, RRF, rerank, answerability), the rerank ceiling, gate accuracy and parse-failure
  rates (H2, H3, H8).
- The `ingest` command with evidence reachability, run per content type (H7).
- Ablation sweeps that reuse ingested collections: `RetrievalTopK` {5, 10, 20}; neighbors {0, 1, 2}; `TextWeight`;
  `FullTextSearchType` TsRank versus TsRankCd; rewrite and rerank on/off; rerank model choice.
- Sweeps that need separate collections (`--scope-suffix`): `FixedTokenCount` {128, 256, 512} × overlap {0, 32, 64};
  chunking strategy; summarization/`ContextPrefix` (H5); embedding model (all-minilm, nomic-embed-text,
  mxbai-embed-large, bge-m3, qwen3-embedding), each checked against its published BEIR/MTEB SciFact score so the
  pipeline is shown to lose nothing.
- Build the `meridian` dataset (native formats, filters, supersession, follow-ups).

**Exit:** RESULTS.md has a stage-lift table, an ingestion table and the sweep tables, and names the default each sweep
recommends.

### Phase 3: End-to-end chat

- A `ChatRunner` with an independent `JudgeClient` (ported, plus a faithfulness prompt), citation scoring from citation
  sources, the evidence-in-prompt split, 3× repeats and the judge-agreement report (κ on 50 hand labels).
- Datasets: `assistanthub-docs`, `meridian`, MultiHop-RAG, Qasper.
- Run the in-product Eval over the same questions (via `eval-export`) and report its agreement with the harness judge
  (H6). If agreement is poor, add an `EvalJudgeInferenceEndpointId` setting to Eval.

**Exit:** chat accuracy, abstention, faithfulness and citation P/R with CIs, per category.

### Phase 4: Load and ingest throughput

- Port `LoadRunner` and `StubEmbeddingServer`, and add a chat scenario with a stub completion endpoint so the server's
  own overhead is measured apart from the model.
- Scenarios: search-only, chat-only, mixed, plus ingest docs/s at concurrency {1, 4, 8}.

**Exit:** a throughput and latency table per concurrency level, and the bottleneck named (RecallDB, Partio,
DocumentAtom, the server itself, or the model).

### Phase 5: Regression gate and Eval bridge

- `compare` with tolerance plus significance testing, covering retrieval and chat reports.
- `run-baseline.sh`/`.bat`, and a CI job running retrieval on `assistanthub-docs`, `meridian` and SciFact with stub-free
  small models, gated by `compare` against a stored baseline.
- `eval-export` and `eval-import`, so product users and the benchmark share question sets.

**Exit:** a PR that regresses nDCG@10 beyond tolerance with p < 0.05 fails CI.

### Phase 6 (optional): Agent in the loop

Add an MCP tool that asks an assistant, or retrieves from it (`assistant/chat` or `assistant/retrieve`). Then port
`AgentRunner`, with and without AssistantHub MCP, on tasks drawn from `assistanthub-docs`. This matters only if
AssistantHub is positioned as an agent knowledge source. It is skipped by default because it spends real API credits.

## 6. From results to improvements

After round 0, write `RETRIEVAL_IMPROVEMENTS.md`. Every candidate fix gets a value score (1–10) and a simplicity score
(1–10). Everything with simplicity 8 or more goes into the next round, and each fix's measured effect is recorded
against the round it landed in. The candidates below are pre-seeded from the hypotheses and from what the AssistantHub
code suggests. They are unscored until round 0 produces numbers, and the list will change once it does.

| Candidate | Addresses |
|---|---|
| Per-mode score thresholds, or a threshold on a normalized/fused score | H1 |
| Hybrid as the default mode, with a tuned `TextWeight`, or RRF fusion instead of a linear weight | H4 |
| A rerank candidate pool (`RerankCandidateCount`, 30–50) separate from the final top-k | H2 |
| A cross-encoder rerank endpoint type (Partio or a dedicated model) with calibrated scores | H2, H3, H8 |
| Per-document chunk headers (title plus summary) in the embedded text only, leaving stored text unchanged; measure paraphrase and lexical queries separately | H5 |
| A stronger default embedding model, chosen by benchmark | paraphrase |
| Neighbor expansion default tuned per dataset type | detail, long docs |
| MMR or a per-document cap so near-duplicate chunks don't crowd out other documents | multi, confusable |
| Document versioning or supersession, where the newer version wins | superseded |
| Extraction and chunking fixes for the content types that lose evidence | H7 |
| Answerability gate on a calibrated rerank score | H8 |
| A separate judge endpoint for in-product Eval | H6 |

## 7. Parity checklist

AssistantHub is at benchmarking parity with Isis when every item is checked:

- [x] A black-box `src/Test.Benchmark` with `prepare`, `retrieval`, `chat`, `load`, `compare` and `stub`
- [x] An isolated benchmark stack and start scripts, with the dev stack untouched
- [x] A neutral dataset format; BEIR converter; at least two committed datasets
- [x] Hit@1, Recall@1/5/10, All@5/10, MRR@10 and nDCG@10 per mode and per type, BEIR-compatible
- [x] Score-separation AUROC on unanswerable questions
- [x] Chat accuracy and abstention from an independent judge, citation P/R, and the evidence-in-prompt split
- [x] Load test with stub embeddings
- [x] Stage breakdown per phase
- [x] Deterministic scope reuse, `--reingest`, and full environment provenance in every report
- [x] `RESULTS.md` round 0 and `RETRIEVAL_IMPROVEMENTS.md`

Beyond parity, the items specific to AssistantHub:

- [x] Stage lift for rewrite, rerank, gate and answerability, via the `/retrieve` dry run
- [x] Chunk-level evidence recall, context precision and evidence reachability
- [x] Ingest benchmark per content type, with native-format documents
- [x] Filter and attachment precision
- [x] Faithfulness scoring
- [x] Bootstrap CIs, significance-tested `compare`, and judge κ
- [x] Industry document-RAG datasets (MultiHop-RAG, Qasper)
- [x] Eval import/export bridge, and a CI regression gate
