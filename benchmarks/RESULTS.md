# AssistantHub benchmark results

This page records what the suite in this directory measured, round by round, and what changed in AssistantHub between
rounds. How to run it is in [README.md](README.md). The plan is [BENCHMARKING.md](../BENCHMARKING.md), and the ranked
fixes these numbers point to are in [RETRIEVAL_IMPROVEMENTS.md](../archive/RETRIEVAL_IMPROVEMENTS_2026-09.md). Per-run reports are
written to the git-ignored `results/` directory.

**The short version.** The first run found that retrieval was silently failing, not just ranking poorly:

- Under 4 concurrent requests, a `429` from the query-embedding endpoint returned no context for 41–57% of questions.
- A single attached document never actually scoped retrieval.
- Large documents could not be ingested at all.

With those three defects fixed, Hybrid retrieval reaches nDCG@10 of 0.85 on AssistantHub's own docs, 0.77–0.79 on
the Meridian enterprise corpus, 0.63 on MultiHop-RAG and 0.99 on Qasper (document level).

The largest remaining wins are defaults:

- The shipped score threshold turns keyword search off entirely. FullText scores **0.000** at the default and 0.79 at
  threshold 0.
- Vector is the default mode, although Hybrid beats it by 0.07–0.35.
- Neighbor expansion, off by default, adds 6–11 points of evidence to the prompt.

The weakest question types are follow-ups (0.46–0.51), which are retrieved without their conversation, and superseded
documents (0.73).

In chat, answer generation, not retrieval, is now the bottleneck. The evidence reached the prompt for 96–100% of
questions, yet a 4B answer model got 0.47–0.67 of answers right. Its citation instructions alone cost 16 points: on
Meridian, 0.472 with citations and 0.633 without. A 20B answer model reached 0.741. On this 4B-class setup, the LLM
reranker (0.789 → 0.694) and the answerability check (0 of 12 unanswerable questions caught) make things worse or add
nothing. In-product Eval agrees with an independent judge (κ 0.83).

**Round 4 update.** The `RetrievalScoreThreshold` fix makes FullText usable at the default threshold: it now scores
0.79 on AssistantHub-docs and Meridian, and 0.68 on MultiHop-RAG, where it scored 0.000 before. Hybrid is unchanged.
Query embedding no longer hangs: the BEIR runs that stalled for 50 minutes on a slow model proxy now finish, with
4–6% of queries falling back to keyword search. The query-embedding cache and the standalone embed route lift stub
throughput from 10–40 to 47–289 ops/s. Paragraph chunking now works; it reaches 99–100% evidence reachability and
the best evidence@10 measured (0.756 / 0.773). Two knobs did not transfer from Isis: a smaller RRF k is never better
here, and reading-order context lowered chat accuracy. See [Round 4](#round-4-the-simplicity-8-items).

**Round 5 update.** A cross-encoder reranker adds 0.03 nDCG@10 on every dataset (Meridian 0.784 → 0.816,
AssistantHub-docs 0.834 → 0.866, MultiHop-RAG 0.631 → 0.664), where the LLM reranker lost 0.095. Rewriting follow-ups
into standalone questions lifts them from 0.52 to 0.78. Supersession links lift superseded-document questions from 0.72
to 0.91 (hit@1 0.42 → 0.92). Structured cells with heading headers are mixed and stay opt-in. See
[Round 5](#round-5-rerank-rewrite-supersession-and-structure).

## Setup

- **Machine:** one laptop, an AMD Ryzen AI 9 HX PRO 370 (12 cores) with a Radeon 890M iGPU and 92 GB RAM, running
  Windows 11.
- **Isolated stack** (`docker/compose.yaml`): pgvector (PostgreSQL 17), RecallDB 0.2.1 (the `d8ce32c` image: any-term
  full text and RRF hybrid), Partio 0.5.0, DocumentAtom 3.1.2, Less3 4.0.0 and Verbex 0.2.1. AssistantHub was built
  from the working tree and run natively.
- **Models:** retrieval rounds 0–2 used Ollama on the host, with `all-minilm` (384 dims) embeddings through Partio's
  seeded endpoint (concurrency 2, queue 0).
- **Model endpoints:** the embedding-model comparison, the BEIR runs, the LLM stages and chat used Ollama-compatible
  endpoints on an NVIDIA GB10 (DGX Spark), registered as Partio endpoints with a bearer token and concurrency 2:
  - `nomic-embed-text`
  - `gemma3:4b`: answers and utility calls
  - `gpt-oss:20b`: the independent judge, a larger model from a different family than the answer model
- **Settings:** every assistant starts from AssistantHub's defaults for a new assistant — Vector search, topK 10,
  threshold 0.3, text weight 0.3, no neighbors, no rewrite, rerank, gate or answerability — except that responses
  are non-streaming. Ingestion rules use the dashboard defaults: FixedTokenCount, 256 tokens, no overlap.

The Ollama instance was shared with two other benchmark stacks running on the same machine. That shows up three ways:

- Embedding throughput is low: about 3 embeddings per second, and about 0.2 s per call when idle.
- A 5-token `gemma3:4b` completion took 30–90 s at the busiest.
- Latencies are inflated. Compare them within this page only.

Because of the contention, the LLM-dependent measurements (rerank, rewrite, answerability, chat, Eval agreement) were
run at concurrency 1 on small stratified subsets. They are marked indicative.

## Datasets

| Dataset | Documents | Questions (answerable / unanswerable) | Evidence reachable at round 0 |
|---|---|---|---|
| assistanthub-docs (committed) | 18 real docs, md | 105 / 15 | 89.9% |
| meridian (committed) | 160 synthetic docs: md 56, pdf 46, docx 27, html 20, txt 11 | 274 / 34 | 96.2% (pdf 96.7%, docx 97.8%, html 96.2%, md 94.4%, txt 94.1%) |
| Qasper, 50 papers | 50 long papers, md | 149 / 22 | 71.9% |
| MultiHop-RAG, 300 questions | 609 news articles | 225 / 75 | 90.6% |
| BEIR SciFact | 5,183 abstracts | 300 / 0 | – (no evidence labels) |
| BEIR NFCorpus | 3,633 documents | 323 / 0 | – (no evidence labels) |

DocumentAtom extraction held up across formats. PDF and DOCX lost no more evidence than Markdown. Most of the missing
reachability comes from Partio's fixed-token chunker, which drops or duplicates a few characters at every chunk
boundary (see "Defects"). Qasper's evidence units are whole paragraphs, often longer than one 256-token chunk, so its
rate is also a matching artifact.

## The rounds

| Round | What changed |
|---|---|
| 0 | First full run. Two ingest fixes found by the smoke run were already in: the Partio 100 s timeout and platform newlines. |
| 1 | Query embedding retries transient `429`/`5xx` and reports `embedding_failed`. |
| 2 | Single-document scoping sends `DocumentIds`. Defaults unchanged. |
| 3 | Restart: round 2's configurations rerun on unchanged code, recorded in the run ledger (`benchmarks/history/runs.jsonl`) so later rounds compare against it. |
| 4 | The simplicity ≥ 8 items from [INGEST_AND_RETRIEVAL_IMPROVEMENTS.md](../archive/INGEST_AND_RETRIEVAL_IMPROVEMENTS.md): standalone query embedding with its own timeout and cache, keyword fallback, explicit fusion options, a mode-aware threshold, filtered `ef_search`, recency, reading-order context, blank-line and heading extraction, Verbex fixes. The first run (label `r4`) had a hybrid threshold bug; `r4b` is the corrected run. |
| 5 | The rest of both plans: cross-encoder rerank, conversation rewrite, additive query rewrite, utility timeouts and circuit breakers, answer retry and regeneration, document supersession, structured cells and context headers, provenance, extraction settings and duplicate detection. New assistants default to Hybrid, text weight 0.5, one neighbor and rerank top K 10. `r5c` is the corrected supersession run. |

## Round 4: the simplicity ≥ 8 items

Round 3 reran round 2's configurations on unchanged code; its retrieval numbers matched round 2 within noise
(AssistantHub-docs Hybrid 0.853, Meridian 0.788, MultiHop-RAG 0.637, Qasper 1.000). Round 4 changed the items below.
Collections were reused (`--pipeline 1`) except where stated, so each run shares a fingerprint with its round 3 twin
and `history` shows the change directly.

**Default threshold (0.3), round 3 → round 4b, nDCG@10:**

| Dataset | FullText r3 | FullText r4b | Hybrid r3 | Hybrid r4b |
|---|---|---|---|---|
| AssistantHub-docs | 0.000 | 0.787 | 0.853 | 0.853 |
| Meridian | 0.000 | 0.790 | 0.788 | 0.788 |
| MultiHop-RAG | 0.000 | 0.680 | 0.634 | 0.637 |
| Qasper | 0.000 | 0.987 | 1.000 | 0.987 |

- The threshold is now a vector-similarity threshold. FullText is no longer held to it, and in Hybrid it only drops
  chunks the vector leg found on its own. Qasper loses one question to that rule: its only in-paper chunk was a
  weak vector-only match.
- The first version (`r4`) applied the threshold to every hybrid chunk with a vector score, which dropped strong
  keyword matches with low embedding similarity: AssistantHub-docs Hybrid 0.853 → 0.827, evidence@10 0.721 → 0.644.
  It was corrected before `r4b`.
- Threshold-0 runs are unchanged from round 3, so the explicit `Hybrid` fusion block (RRF, k 60, the store's pool)
  reproduces what RecallDB ran by default.

**Query embedding (nomic on the GB10, threshold 0).** In round 3, SciFact and NFCorpus Hybrid stalled for 50 minutes
and were killed. They now finish. On SciFact 6.3% of queries, and on NFCorpus 4.3%, hit the 30 s
`QueryEmbeddingTimeoutMs` on the loaded model proxy and fell back to keyword search:

| Dataset | Hybrid (earlier nomic run) | Hybrid r4b | Queries on keyword fallback |
|---|---|---|---|
| SciFact | 0.715 | 0.704 | 6.3% |
| NFCorpus | 0.321 | 0.328 | 4.3% |

The SciFact gap is the fallback queries, which score at keyword level (0.59). Median latency was 6.9–7.0 s per
query, all of it the model proxy.

**Fusion sweeps (Hybrid, default threshold), nDCG@10:**

| Setting | AssistantHub-docs | Meridian | MultiHop-RAG | Mean |
|---|---|---|---|---|
| RRF k 10 | 0.826 | 0.777 | 0.618 | 0.740 |
| RRF k 20 | 0.833 | 0.784 | 0.622 | 0.746 |
| RRF k 40 | 0.845 | 0.788 | 0.631 | 0.755 |
| RRF k 60 (default) | **0.853** | **0.788** | **0.637** | 0.759 |
| Text weight 0.5 | 0.840 | 0.797 | 0.665 | 0.767 |
| Text weight 0.7 | 0.833 | **0.812** | **0.699** | **0.781** |

- A smaller RRF k never helps here, unlike Isis (k 20); keep 60.
- A higher text weight helps Meridian and MultiHop-RAG, where Hybrid at 0.7 beats keyword-only search (0.699 vs
  0.680), and costs AssistantHub-docs 0.02. It is the best mean. The best weight depends on the corpus.
- Recency weight 0.05 or 0.1 on Meridian: 0.790 (vs 0.788). The benchmark ingests every document at once, so creation
  time carries no signal; a date-ordered ingest (`--date-order`) is needed to measure it.

**Reading-order context (chat, Meridian, Hybrid, threshold 0, neighbors 1, 60 questions).** Against round 3's
score-order run of the same configuration, accuracy fell from 0.660 to 0.500 (paired p = 0.048), and the evidence
that reached the prompt fell from 0.940 to 0.880. Prompt-budget trimming drops chunks from the end of the list,
which in reading order is the tail of the lowest-ranked document, and the 4B answer model may do better with the most
relevant passage first. `ContextOrder` stays `Score` by default.

**Extraction with blank-line blocks and markdown headings (re-ingested, `--pipeline 2`):**

| Corpus and chunking | Reachability | Evidence@10 | Hybrid nDCG@10 |
|---|---|---|---|
| AssistantHub-docs, FixedTokenCount 256, before | 89.9% (round 2) | 0.721 | 0.853 |
| AssistantHub-docs, FixedTokenCount 256, after | 88.0% | 0.706 | 0.837 (p = 0.12 vs before) |
| AssistantHub-docs, ParagraphBased, after | **100%** | **0.756** | 0.830 |
| Meridian, FixedTokenCount 256, before | 96.2% (round 2) | 0.725 | 0.788 |
| Meridian, FixedTokenCount 256, after | 96.4% | 0.712 | 0.784 |
| Meridian, ParagraphBased, after | **99.4%** | **0.773** | **0.800** |

(ParagraphBased runs use threshold 0; the others use the default 0.3, which no longer matters for Hybrid.)
Fixed-token chunking is unaffected, as expected. ParagraphBased, which saw each extracted document as one paragraph
before, now keeps nearly every evidence passage intact. On AssistantHub-docs it produces 71 tiny chunks (headings
and short items), which contextual chunk headers (IR-06) would attach to the following text.

## Round 5: rerank, rewrite, supersession and structure

Round 5 measured the remaining retrieval-plan items. All runs use the harness defaults (Hybrid, threshold 0.3, text
weight 0.3, no neighbors) and the pipeline 2 collections, so each compares directly with the `r5-baseline` run on
the same code. The baseline matched round 4b exactly (Meridian 0.784, before and after the supersession runs), so
nothing regressed underneath.

**Cross-encoder rerank** (`cross-encoder/ms-marco-MiniLM-L-6-v2` on the CPU TEI container, 30 candidates, keep 10):

| Dataset | Hybrid nDCG@10 | + cross-encoder | Recall@10 | Evidence@10 |
|---|---|---|---|---|
| Meridian | 0.784 | **0.816** | 0.895 → 0.928 | 0.712 → 0.778 |
| AssistantHub-docs | 0.834 | **0.866** | 0.938 → 0.935 | 0.705 → 0.697 |
| MultiHop-RAG | 0.631 | **0.664** | 0.698 → 0.708 | 0.434 → 0.420 |

- It gains 0.03 on every dataset. The LLM reranker (`gemma3:4b`) lost 0.095 on the same Meridian questions.
- On Meridian it lifts lexical (0.784 → 0.841), table (0.800 → 0.855) and detail (0.892 → 0.929) questions most.
- Cost on the CPU container, concurrency 1, 40 Meridian questions: p50 1.25 s at 30 candidates (nDCG 0.837) and 0.46 s
  at 10 (0.827). At concurrency 4 the container saturates and p50 reaches 4–6 s. Use a GPU TEI image, or 10–20
  candidates, for interactive use.
- The rerank score does not separate unanswerable questions better than the fused score (AUROC 0.769 vs 0.771), so
  `RerankMinScore` stays off by default.

**Conversation rewrite** (`gemma3:4b` on the GB10, Meridian): follow-up questions go from **0.519 to 0.783** nDCG@10
(hit@1 0.33 → 0.53, evidence@10 0.40 → 0.77). Every other question type is identical, because the step only runs when
there is earlier conversation. Overall 0.784 → 0.799, with no latency change for single-turn questions.

**Document supersession** (the 18 Meridian `supersedes` links set with `--link-supersedes`):

| Mode | Overall | Superseded questions | Superseded hit@1 | Filter questions |
|---|---|---|---|---|
| No links | 0.784 | 0.722 | 0.42 | 0.971 |
| Demote | **0.814** | **0.908** | **0.92** | 0.971 |
| Hide | 0.812 | 0.898 | 0.92 | 0.971 |
| Demote + cross-encoder | **0.831** | 0.849 | 0.81 | 0.983 |

- The first run (`r5-supersede-*`) replaced old versions even when the question scoped to them, by attaching the old
  document or filtering on its `superseded` label. That dropped filter questions from 0.971 to 0.749. An explicit
  scope now wins: the old chunk is kept and marked outdated. `r5c-*` is the corrected run.
- With the cross-encoder, superseded questions score lower than with Demote alone (0.849 vs 0.908), because rerank can
  push the inserted replacement chunk down. The combination is still the best overall.

**Structured cells with title and heading headers** (ParagraphBased text cells, threshold 0, compared with flat
ParagraphBased from round 4b):

| Dataset | Flat nDCG@10 | Structured | Evidence@10 | Reachability |
|---|---|---|---|---|
| Meridian | 0.800 | 0.800 | 0.773 → 0.776 | 99.4% → 99.6% |
| AssistantHub-docs | 0.830 | 0.815 | 0.756 → 0.686 | 100% → 100% |

The result is mixed. Confusable (0.798 → 0.845), detail and paraphrase questions improve. Follow-up (0.556 → 0.381),
lexical and table questions get worse. On AssistantHub-docs, section cells leave 76 tiny chunks and 18 duplicates.
`CellMode` stays `Flat` and `ContextHeader` stays `None` by default. Both are available per ingestion rule.

**Not measured here:** answer retry and degenerate-answer regeneration (unit-tested; a chat round against the loaded
model proxy is the next check), utility timeouts under load, extraction settings (no scanned or spreadsheet fixture in
the datasets), and near-duplicate detection (Meridian has no duplicates).

## Retrieval (defaults, Hybrid shown next to the default Vector)

nDCG@10, answerable questions, 95% bootstrap CI in brackets:

| Dataset | Mode | Round 0 | Round 1 | Round 2 |
|---|---|---|---|---|
| assistanthub-docs | Vector (default) | 0.456 [0.377, 0.536] | 0.772 [0.720, 0.822] | 0.768 [0.712, 0.822] |
| | FullText | 0.000 | 0.000 | 0.000 |
| | **Hybrid** | 0.699 [0.623, 0.768] | 0.853 [0.815, 0.890] | **0.847** [0.804, 0.887] |
| meridian | Vector (default) | 0.317 [0.268, 0.366] | 0.717 [0.679, 0.753] | 0.706 [0.664, 0.743] |
| | FullText | 0.000 | 0.000 | 0.000 |
| | **Hybrid** | 0.368 [0.318, 0.421] | 0.788 [0.755, 0.820] | **0.772** [0.738, 0.806] |
| MultiHop-RAG | Vector (default) | 0.369 | 0.551 | 0.538 |
| | **Hybrid** | 0.569 | 0.637 | **0.634** [0.600, 0.668] |
| Qasper (scoped to the paper) | Vector (default) | 0.409 | 0.503 | 0.638 |
| | **Hybrid** | 0.476 | 0.597 | **0.987** [0.966, 1.000] |

What moved the numbers:

- **Round 0 → 1: silent empty retrievals.** In round 0, 41–57% of answerable questions returned *no chunks at all*.
  Partio's query-embedding endpoint rejected requests over its concurrency of 2 with `429`, and retrieval returned
  nothing without retrying. With 4 concurrent requests, which is not heavy load, that is most queries. The server
  logged 542 such failures during round 0. A retry with short, jittered backoff fixed nearly all of them. The new
  `embedding_failed` flag shows the rest: 0.3–0.8% in round 1, and 1–3% in round 2 when the shared Ollama was busier.
  That flag also explains the small round-1-to-round-2 drop on Meridian: the difference is the queries whose embedding
  failed.
- **Round 1 → 2: attached-document scoping.** A single attached document was sent to RecallDB as `DocumentId`, which
  RecallDB ignores. The search ran across the whole collection, and the attachment safety filter then removed every
  chunk, so 40% of Qasper questions got nothing. With `DocumentIds`, Qasper document-level nDCG is 0.987 and
  evidence@10 rises from 0.167 to 0.482.

### By question type (round 2, Hybrid)

| Type | assistanthub-docs n | nDCG@10 | evidence@10 | meridian n | nDCG@10 | evidence@10 |
|---|---|---|---|---|---|---|
| detail (deep in a long doc) | 15* | 0.89 | 0.93 | 33 | 0.91 | 0.79 |
| filter (metadata / attachment) | 8* | 0.98 | 0.88 | 18* | 1.00 | 0.89 |
| table | 10* | 0.83 | 0.80 | 32 | 0.79 | 0.84 |
| lexical (exact identifiers) | 25 | 0.85 | 0.74 | 34 | 0.82 | 0.62 |
| paraphrase | 25 | 0.81 | 0.64 | 46 | 0.71 | 0.70 |
| multi (2–3 documents) | 15* | 0.81 | 0.58 | 35 | 0.73 | 0.62 |
| confusable (sibling models, regions) | – | – | – | 35 | 0.75 | 0.63 |
| superseded (current vs old version) | – | – | – | 26 | 0.73 | 0.81 |
| **followup** (elliptical turn) | 7* | 0.79 | 0.50 | 15* | **0.46** | 0.43 |

\* fewer than 20 questions: indicative only.

Follow-ups are the weakest type. The retrieval query is only the last user message ("and for the 220?"), so the model
of the product is lost. Superseded questions retrieve both versions, and the older one ranks first a quarter of the
time. On Meridian, keyword-only search beats Hybrid on lexical questions (0.92 vs 0.82), which shows the text leg is
under-weighted.

Filter precision was 1.000 in every configuration: no chunk outside a metadata filter or attachment scope was
returned.

## What the knobs do (round 2 code, Hybrid unless stated)

Each sweep reuses the ingested collection. Every value is its own assistant.

**Score threshold (H1).** The threshold is applied to the store's score in every mode. For FullText that score is a
TsRank value below 0.1:

| Dataset | FullText @ 0.3 (default) | FullText @ 0.05 | FullText @ 0.01 | FullText @ 0 | Hybrid @ 0.3 | Hybrid @ 0 |
|---|---|---|---|---|---|---|
| assistanthub-docs | 0.000 | 0.401 | 0.787 | 0.787 | 0.844 | 0.847 |
| meridian | 0.000 | 0.379 | 0.790 | 0.790 | 0.788 | 0.785 |
| MultiHop-RAG | 0.000 | – | – | 0.680 | 0.634 | 0.619 |
| Qasper | 0.000 | – | – | 0.987 | 0.987 | 0.966 |

Keyword search is as good as vector search on these corpora (0.79 vs 0.71–0.79), and better on MultiHop-RAG news, but
the default threshold switches it off. For Vector and Hybrid the threshold barely matters once embedding failures are
fixed (±0.01).

**Hybrid text weight (H4).**

| Text weight | 0.1 | 0.3 (default) | 0.5 | 0.7 |
|---|---|---|---|---|
| assistanthub-docs | 0.812 | 0.829 | **0.840** | 0.825 |
| meridian | 0.742 | 0.783 | 0.794 | **0.812** |

**Candidate depth (`RetrievalTopK`).**

| k | 5 | 10 (default) | 20 | 40 |
|---|---|---|---|---|
| assistanthub-docs nDCG@10 / recall@10 | 0.816 / 0.861 | 0.831 / 0.907 | 0.866 / 0.964 | 0.866 / 0.980 |
| meridian nDCG@10 / recall@10 | 0.747 / 0.816 | 0.785 / 0.898 | 0.792 / 0.916 | 0.796 / 0.924 |

**Neighbor expansion.** This doesn't change ranking. It changes what reaches the prompt: `context_evidence` is the
share of evidence passages present in the injected text.

| Neighbors | 0 (default) | 1 | 2 |
|---|---|---|---|
| assistanthub-docs | 0.721 | 0.782 | 0.797 |
| meridian | 0.723 | 0.829 | 0.836 |

**Ingestion variants** (new collections, Hybrid, threshold 0):

| Variant | assistanthub-docs reachability | nDCG@10 | evidence@10 | meridian reachability | nDCG@10 | evidence@10 |
|---|---|---|---|---|---|---|
| FixedTokenCount 256, overlap 0 (default) | 89.9% | 0.834 | 0.711 | 96.2% | 0.785 | 0.719 |
| overlap 32 | 96.8% | 0.836 | 0.716 | 99.2% | 0.782 | 0.728 |
| 128-token chunks | 88.0% | 0.834 | 0.651 | 94.0% | 0.778 | 0.637 |
| SentenceBased | 98.7% | 0.806 | **0.762** | 99.2% | **0.791** | **0.749** |

A 32-token overlap recovers what the boundary drift loses, at no ranking cost. Sentence-based chunking keeps almost
every passage intact and puts the most evidence in the top 10 chunks on both corpora. On AssistantHub-docs its many
tiny chunks (115 under 40 characters) dilute document-level ranking. Smaller 128-token chunks lose evidence.

**Embedding model.** `nomic-embed-text` (768 dimensions) against `all-minilm` (384), on the same datasets and
defaults. These runs used the GB10 endpoints; see "Model endpoints" below.

| Dataset | all-minilm Vector | nomic Vector | all-minilm Hybrid | nomic Hybrid |
|---|---|---|---|---|
| assistanthub-docs (threshold 0 for Hybrid) | 0.768 | **0.809** | 0.834 | **0.853** |
| meridian (threshold 0 for Hybrid) | 0.706 | 0.734* | 0.785 | **0.815** |
| MultiHop-RAG | 0.538 | **0.631** | 0.634 | **0.688** |
| Qasper (Hybrid, threshold 0) | – | – | 0.966 | **0.993** |

\* 3 of 160 documents missing from that run.

`nomic-embed-text` lifts Hybrid by 0.02–0.05 and Vector by 0.03–0.09. It is trained to be used with `search_query:` and
`search_document:` prefixes, which AssistantHub does not add, so this is a lower bound for the model.

## Industry anchors: BEIR SciFact and NFCorpus

Full corpora with `nomic-embed-text` (SciFact is missing 5 of 5,183 documents and NFCorpus 3 of 3,633, after retries
against the GB10 proxy). Document-level nDCG@10:

| Dataset | Vector | FullText (threshold 0) | Hybrid (threshold 0) | FullText (default threshold) | Published BM25 (BEIR paper) |
|---|---|---|---|---|---|
| SciFact (300 queries) | 0.694 | 0.589 | **0.715** | 0.000 | 0.665 |
| NFCorpus (323 queries) | 0.303 | 0.295 | **0.321** | 0.000 | 0.325 |

AssistantHub's pipeline lands where the reference numbers say it should. PostgreSQL TsRank full text trails BM25 by
0.03–0.08, and Hybrid is at or above BM25. Chunking, extraction and storage lose nothing measurable on these short
documents.

## Can a score say "nothing relevant"?

AUROC of the top-hit score between answerable and unanswerable questions (round 2): 0.5 is chance, 1.0 is a perfect
threshold.

| Dataset | Hybrid fused score | Raw `vector_score` | FullText (threshold 0) |
|---|---|---|---|
| assistanthub-docs | 0.62 | 0.73 | 0.76 |
| meridian | 0.78 | 0.68 | 0.74 |
| MultiHop-RAG | 0.82 | 0.94 | – |

None of these is a reliable abstention gate on the enterprise-style corpora. MultiHop-RAG's null questions are about
topics absent from the news corpus, which is easier. See the answerability check below.

## Pipeline stages with an LLM

These runs used 108 answerable and 12 unanswerable Meridian questions (stratified), Hybrid, threshold 0, with
`gemma3:4b` as the utility model. Each row compares stages within the same run, so the questions are identical.

| Stage | nDCG@10 before → after | Evidence in kept chunks before → after | Unusable replies | Added latency (p50) |
|---|---|---|---|---|
| LLM rerank, 10 candidates → keep 5 | 0.789 → **0.694** | 0.797 → 0.560 | 14% | +5.1 s |
| LLM rerank, 30 candidates → keep 10 | – (every call failed) | – | **100%** | +0.1 s |
| Query rewrite (multi-query + RRF) | 0.789 → 0.799 | 0.797 → 0.796 | 0% | +2.5 s |
| Answerability check | – | – | 0% | +3.6 s |
| Retrieval gate (all 308 questions; only follow-ups call it) | 0.788 → 0.788 | – | 0% | – |

- **The LLM reranker makes retrieval worse** with a 4B utility model. It reorders good lists badly and throws away
  half the evidence.
- **At 30 candidates the rerank prompt exceeds what the utility call can finish:** every call failed, and the pipeline
  fell back to retrieval order.
- **The answerability check never said "unsupported"**: 0 of 12 unanswerable questions were caught, and precision and
  recall for "unsupported" were both 0.
- **Rerank-score AUROC** (answerable vs unanswerable) was 0.64, no better than the raw scores.

In an earlier attempt on the shared Ollama, 23 of 24 rerank calls timed out at `InferenceService`'s fixed 100 s
`HttpClient` limit. The new `rerank_parse_failed` flag made that visible instead of silent.

## Chat

Questions were asked through `POST /v1.0/assistants/{id}/chat` (non-streaming, citations on, Hybrid, threshold 0).
`gemma3:4b` answered and `gpt-oss:20b` judged. The judge is called directly, never through AssistantHub. Each run used
stratified subsets, 95% bootstrap CIs.

| | assistanthub-docs (60 questions, 2 repeats) | meridian (60 questions) |
|---|---|---|
| Accuracy, answerable | **0.673** [0.558, 0.788] | **0.472** |
| Abstention, unanswerable | 0.429 (3 of 7) | 0.667 (4 of 6) |
| Faithfulness (every claim supported by the injected context) | 0.692 | 0.717 |
| Evidence document in the prompt | 1.000 | 0.962 |
| Accuracy when the evidence was in the prompt | 0.673 | 0.490 |
| Citation precision / recall | 0.792 / 0.615 | 0.688 / 0.780 |
| Run-to-run spread | 0.639 ± 0.008 over 2 repeats | – |
| Latency p50 | 4.4 s | 6.5 s |

**Retrieval is no longer the bottleneck for chat; answer generation is.** The evidence reached the prompt for 96–100%
of questions, yet only 47–67% of answers were right. Three failure modes stand out in the wrong answers:

- **Degenerate cited answers.** With citation instructions on, `gemma3:4b` stopped after a few tokens on 7 of 59
  Meridian answers ("According to sources", "[4]"). Ollama reported 5 completion tokens. With citations off, the same
  question produced the correct 82-token answer. Excluding those, Meridian accuracy is 0.54.
- **Wrong fact picked from a correct context.** For example, the answer used a sibling cost centre, the wrong port,
  or a default from the wrong table.
- **Unanswerable questions answered with confident inventions.** For example, "33 GB of GPU memory" for gemma3:4b,
  and "the dashboard does not support SSO".

**Judge calibration.** 50 of the AssistantHub-docs answers were sampled with `label-sample` and graded independently
as reference labels (`labels/assistanthub-docs-reference.json`). These labels were produced by Claude reviewing each
question, gold answer and response, not by a human rater. The `gpt-oss:20b` judge agreed on 47 of 50 (0.94, **κ 0.87**):
it was lenient twice (partially right answers) and strict once. Replace them with human labels for headline claims.

**Chat ablations** (the same 60 stratified Meridian questions, Hybrid, threshold 0):

| Configuration | Accuracy | Abstention | Faithfulness | Citation P / R |
|---|---|---|---|---|
| Citations on (baseline) | 0.472 | 0.667 | 0.717 | 0.688 / 0.780 |
| + one neighbor chunk | 0.558 | 0.600 | 0.720 | 0.581 / 0.635 |
| **Citations off** | **0.633** | **0.833** | 0.691 | – |
| `gpt-oss:20b` answering, citations on | **0.741** | 0.500 | **0.849** | 0.644 / 0.858 |

The citation instructions cost a 4B answer model 16 points of accuracy and make it worse at declining. A 20B answer
model handles the same cited prompt at 0.741 accuracy and 0.849 faithfulness, at about 24 s per answer. In that row
the judge is the same model as the answerer, so some self-preference may inflate it. Neighbor
expansion helps accuracy (+9 points) as the retrieval results predicted.

On AssistantHub-docs, the shipped defaults (Vector, threshold 0.3, citations on; a separate 40-question sample)
answered 0.545 correctly and declined none of the unanswerable questions, against 0.673 for Hybrid at threshold 0.

## In-product Eval vs the independent judge (H6)

`eval-run` exported 20 stratified AssistantHub-docs questions as EvalFacts and ran AssistantHub's own Eval (ChatRail,
`gemma3:4b` answering and judging). It then re-graded every Eval answer with the independent `gpt-oss:20b` judge:

| In-product pass rate | Independent pass rate | Agreement | Cohen's κ | Eval pass / judge fail | Eval fail / judge pass |
|---|---|---|---|---|---|
| 0.80 | 0.85 | 0.95 | 0.83 | 0 | 1 |

On this sample the in-product Eval is trustworthy. Its self-judging did not inflate scores, and the one disagreement
was a correct decline that Eval marked as a failure. H6 (Eval scores are not independent) is not supported at n = 20.
A separate judge endpoint remains a nice-to-have rather than a fix.

## Load

Closed loop against assistanthub-docs, retrieve route, 30 s per level. **Stub models**
(`load --stub --stub-latency-ms 5`) take the model out of the measurement:

| Concurrency | Ops/s | Error rate | p50 ms | p95 ms |
|---|---|---|---|---|
| 1 | 10.5 | 0% | 86 | 157 |
| 4 | 23.8 | 0% | 144 | 327 |
| 16 | 21.1 | 0% | 703 | 1,415 |
| 32 | 40.4 | 0% | 802 | 1,316 |

The server stage breakdown attributes the time:

- At c=1, `retrieval.search` takes 85 ms, of which the RecallDB query (`vector.query`) is 25 ms.
- At c=16, search takes 715 ms and the RecallDB query 161 ms.

The rest is the query-embedding round trip through Partio, even against a 5 ms stub, so Partio is the throughput
ceiling. The dip at c=16 coincided with other workloads on the machine.

With the real `all-minilm` (Hybrid, threshold 0), throughput was 5.5 / 6.0 / 11.6 ops/s at c=1/4/8, with p95 of 336 /
1,645 / 2,651 ms and no errors, thanks to the round-1 retry.

**Round 4b, same stub configuration**, with queries embedded through `/v1.0/embed` and cached in process:

| Concurrency | Ops/s | Error rate | p50 ms | p95 ms |
|---|---|---|---|---|
| 1 | 47.0 | 0% | 18 | 56 |
| 4 | 147.7 | 0% | 26 | 35 |
| 16 | 250.8 | 0% | 62 | 90 |
| 32 | 288.5 | 0% | 110 | 153 |

The load runner repeats a fixed question set, so after the first pass nearly every query embedding is a cache hit
and the Partio round trip, the old ceiling, drops out. Real traffic repeats less, so expect a smaller gain in
production, in proportion to how often questions recur.

## Defects found by the benchmarks

| Found in | Defect | Symptom | Status |
|---|---|---|---|
| Smoke run | Partio calls used `HttpClient`'s default 100 s timeout | The 200 KB `REST_API.md` failed ingestion every time | Fixed: `Chunking.RequestTimeoutMs` |
| Smoke run | Extracted text reassembled with `Environment.NewLine` | Chunk text differed between Windows and Linux hosts | Fixed |
| Round 0 | Query embedding did not retry `429` and retrieval silently returned nothing | 41–57% of answerable questions got no context at 4 concurrent requests | Fixed: retries plus `embedding_failed` |
| Round 1 | A single attached document was sent as `DocumentId`, which RecallDB ignores | Attached-document questions got no context (40% of Qasper) | Fixed: always `DocumentIds` |
| Smoke run | Partio fixed-token chunking advances by a re-tokenized count | About 1% of text dropped or duplicated at chunk boundaries; reachability capped near 90% | Upstream (Partio) |
| Setup | Admin-key `PUT /v1.0/collections` has no tenant | Creates a collection with Id `"collections"` | Open |
| Round 1 | Utility inference uses a fixed 100 s timeout | Rerank calls time out on a slow model server | Open |
| GB10 runs | Partio reports an upstream model proxy's `429` as `500 InternalError`, and ingestion did not retry 500 | 5–6% of documents failed ingestion against a load-shedding model proxy | Fixed in AssistantHub (a wrapped 429/5xx is treated as transient); Partio should propagate the status |
| Chat runs | The answer-model call is not retried on a transient 429/502 | Against a load-shedding model proxy, most chat requests in a burst failed | Open (the harness retries and counts) |
| Phase 0 | API route-contract test out of date | Three earlier routes are missing from OpenAPI, Postman and REST docs | Fixed (the API suite passes) |
| Round 3 | Query embedding used the 15-minute ingestion timeout | SciFact and NFCorpus Hybrid runs stalled for 50 minutes against a slow model proxy | Fixed: `Chunking.QueryEmbeddingTimeoutMs` (30 s, no retry on timeout) and keyword fallback in Hybrid |
| Round 3 audit | Queries were embedded through Partio's chunking route and only the first chunk was used | Queries longer than 256 tokens silently truncated | Fixed: `/v1.0/embed` |
| Round 3 audit | The Verbex tool sent `RequiredTerms`/`ExcludedTerms`, which Verbex does not have | Both silently ignored | Fixed: resolved by side queries |
| Round 4 | First version of the hybrid threshold rule thresholded every chunk with a vector score | AssistantHub-docs Hybrid 0.853 → 0.827 | Fixed before round 4b |
