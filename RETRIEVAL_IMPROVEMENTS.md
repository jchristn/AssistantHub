# Retrieval improvements

The benchmark suite in [benchmarks/](benchmarks/README.md) and its results in
[benchmarks/RESULTS.md](benchmarks/RESULTS.md) point to where retrieval quality can be won. This page lists every
candidate fix. Each gets a value score (1–10: expected effect on the benchmark and on real users) and a simplicity
score (1–10: effort and risk to build and ship). The score is value plus simplicity, with ties broken by value.
Anything with simplicity 8 or more is scheduled for the next round. The Status column is updated as work lands, and
measured effects are recorded against the round they landed in.

"Evidence" cites the round and configuration the claim comes from. All numbers are nDCG@10 on answerable questions
unless stated otherwise. Round 2 is the current code: the four defects below are fixed and the defaults are unchanged
(Vector, `RetrievalScoreThreshold` 0.3, `TextWeight` 0.3, `RetrievalTopK` 10, no neighbors).

## Defects found and fixed while benchmarking

These were fixed in this change, because they made the measurements meaningless. Each is covered by a test.

| Found in | Defect | Effect before the fix | Fix |
|---|---|---|---|
| Smoke run | Partio calls used `HttpClient`'s default 100 s timeout | `REST_API.md` (200 KB) could never be ingested; its 30+ answers were unretrievable | `Chunking.RequestTimeoutMs` (default 15 min) |
| Round 0 | A `429` from Partio's query-embedding endpoint (default concurrency 2, queue 0) was not retried, and retrieval silently returned nothing | At 4 concurrent requests, 41–57% of answerable queries got **no context** (AssistantHub-docs Vector 0.456 → 0.768, Meridian Hybrid 0.368 → 0.788 once fixed) | Retry 408/429/502/503/504 with short jittered backoff; `embedding_failed` flag in retrieve and chat responses |
| Round 1 | A single attached document was sent to RecallDB as `DocumentId`, which RecallDB ignores; the attachment safety filter then removed every out-of-scope chunk | Attached-document questions often got nothing: Qasper Hybrid 0.597 → **0.987**, evidence@10 0.167 → 0.482 | Always send `DocumentIds` |
| Smoke run | Extracted text was reassembled with `Environment.NewLine` | Windows hosts produced CRLF text and Linux hosts LF | Always `\n` |
| GB10 runs | Partio reports an upstream model proxy's `429` as `500 InternalError`, which ingestion and query embedding did not retry | 5–6% of documents failed ingestion against a load-shedding model proxy | Treat a Partio 500 that wraps 429/502/503/504 as transient |

Found but not fixed here:

| Defect | Where | Effect | Suggested fix |
|---|---|---|---|
| Fixed-token chunk boundaries drift | Partio `ChunkingHelpers.ChunkByTokenSpans` advances by `CountTokens(sliceText)` (a re-tokenization of a slice that starts or ends mid-word) instead of the tokens consumed | About 1% of text is dropped or duplicated at boundaries ("unparseabl" / "utput"). It caps evidence reachability at 90% on AssistantHub-docs. | Advance by the requested token count. This is a Partio change. |
| Global-admin collection create builds the RecallDB path with a null tenant | `CollectionHandler.PutCollectionAsync` (`BuildRecallDbPath(auth.TenantId, …)` with the admin API key) | Creates a collection whose Id is `"collections"` | Require a tenant for admin-key calls, or resolve the default tenant |
| Utility inference calls use `HttpClient`'s default 100 s timeout | `InferenceService` | On a slow or shared model server, rerank calls time out (23 of 24 in one run); the pipeline falls back, which the new `rerank_parse_failed` flag now shows | A configurable per-endpoint timeout |
| Route contract test out of date | `ApiSuite` "API route contracts" | Three earlier routes (`/v1.0/analytics/ingestion`, document performance, document reprocess) are missing from OpenAPI, Postman and REST docs, so the API suite fails | Document the three routes |

## Candidate fixes

| Rank | Fix | Evidence | Value | Simplicity | Score | Status |
|---|---|---|---|---|---|---|
| 1 | **Stop applying `RetrievalScoreThreshold` to full-text scores.** Use 0 for FullText (or compare against `vector_score` only). | FullText is **0.000** at the default threshold on every dataset, and 0.787 / 0.790 / 0.680 / 0.987 at threshold 0 (AssistantHub-docs / Meridian / MultiHop-RAG / Qasper). TsRank scores sit below 0.1. | 9 | 10 | 19 | Next round |
| 2 | **Make Hybrid the default search mode** for new assistants | Round 2, Hybrid vs Vector: +0.08 AssistantHub-docs, +0.07 Meridian, +0.10 MultiHop-RAG, +0.35 Qasper | 9 | 10 | 19 | Next round |
| 3 | **Raise the default `TextWeight` from 0.3 to 0.5** | Meridian 0.783 → 0.794 (0.7: 0.812), AssistantHub-docs 0.829 → 0.840. Keyword-only FullText beats default Hybrid on Meridian lexical questions (0.92 vs 0.82) and on MultiHop-RAG (0.680 vs 0.634). | 7 | 10 | 17 | Next round |
| 4 | **Default `RetrievalIncludeNeighbors` to 1** | Share of evidence that reaches the prompt: AssistantHub-docs 0.721 → 0.782, Meridian 0.723 → 0.829. Ranking is unchanged; prompts grow. | 7 | 10 | 17 | Next round |
| 5 | **Queue rather than reject in the shipped Partio config** (`MaxQueueDepth` > 0, and concurrency above 2 for the embedding endpoint) | Even with retries, 0.3–3% of queries still lose their embedding under load; a queue removes the failure mode | 6 | 10 | 16 | Next round |
| 6 | **Make follow-up questions standalone before retrieval**: retrieve with the previous user turn appended, or have the rewrite step see the conversation | Follow-up questions are the weakest type: Meridian 0.46–0.51, AssistantHub-docs evidence@10 0.50. Retrieval uses only the last message ("and for the 220?"). | 8 | 7 | 15 | Later |
| 7 | **Make cited answers robust on small models**: detect a degenerate completion (a few tokens, or only citation markers) and regenerate, or shorten the citation instructions | With citations on, `gemma3:4b` stopped after about 5 tokens on 12% of Meridian answers ("According to sources"). On the same 60 questions, turning citations off raises accuracy from 0.472 to 0.633 and abstention from 0.667 to 0.833. | 8 | 7 | 15 | Next round |
| 8 | **Hybrid falls back to keyword-only search when the query embedding fails** (today it returns nothing) | 0.3–3% of queries (round 1 and round 2 `embedding_failed` rates). FullText alone scores within 0.05 of Hybrid. | 7 | 8 | 15 | Next round |
| 9 | **Retry the answer model on transient failures** (429/502/503/504 from the completion endpoint), as ingestion and query embedding now do | Against a load-shedding model proxy, 37 of 40 chat requests in one run failed outright with `Ollama API returned 429`. The harness now retries them itself and counts the retries (13 in the AssistantHub-docs run). | 7 | 8 | 15 | Next round |
| 10 | **Warn when LLM rerank is enabled with a small utility model**, or change the default `RerankerTopK` 5 to keep all candidates, until #15 lands | Same evidence as #15. Turning rerank on today makes retrieval worse. | 6 | 9 | 15 | Next round |
| 11 | **Default the ingestion rule to a 32-token overlap, or sentence-based chunking** | Reachability 89.9% → 96.8% (overlap) / 98.7% (sentence) on AssistantHub-docs, and 96.2% → 99.2% on Meridian. Sentence chunking gives the best evidence@10 (0.762 / 0.749). 128-token chunks lose evidence. | 6 | 9 | 15 | Next round |
| 12 | **Fix Partio chunk-boundary drift** (see defects) | Reachability 89.9% on AssistantHub-docs; lost text breaks exact-identifier matches | 8 | 6 | 14 | Report upstream |
| 13 | **Retrieve more candidates (k 20) than are injected** | AssistantHub-docs k 10 → 20: 0.831 → 0.866 and recall@10 0.907 → 0.964; Meridian +0.007 | 6 | 8 | 14 | Next round |
| 14 | **Switch the default embedding model to `nomic-embed-text`** and add query/document prefixes (`search_query:` / `search_document:`) | Hybrid +0.03 Meridian (0.785 → 0.815), +0.05 MultiHop-RAG (0.634 → 0.688), +0.03 Qasper (0.966 → 0.993). The prefixes the model expects are not sent today. | 7 | 6 | 13 | Later (needs a re-embed) |
| 15 | **Cross-encoder rerank endpoint** with calibrated scores, and a rerank candidate pool separate from `RerankerTopK` | The LLM rerank (`gemma3:4b`) lowers nDCG@10 from 0.789 to 0.694 on the same Meridian questions, halves the evidence kept (0.797 → 0.560), fails on 14% of replies at 10 candidates and on 100% at 30, and adds 5 s. Its score AUROC is 0.64. | 8 | 4 | 12 | Later |
| 16 | **Document supersession**: a `supersedes` link or version metadata so the newer document wins | Meridian superseded questions 0.73. Both versions are retrieved and the old one often ranks first. | 7 | 5 | 12 | Later |
| 17 | **"Nothing relevant" gate on a calibrated score** (needs #15 or `vector_score`) | The answerability check (`gemma3:4b`) flagged 0 of 12 unanswerable Meridian questions as unsupported. Top `vector_score` separates better (AUROC 0.73 / 0.68 / 0.94 on AssistantHub-docs / Meridian / MultiHop-RAG). | 6 | 6 | 12 | Later |
| 18 | Query rewrite by default | +0.010 nDCG (0.789 → 0.799) for +2.5 s per query on Meridian | 3 | 9 | 12 | Not recommended as a default |
| 19 | Separate judge endpoint for in-product Eval (`EvalJudgeInferenceEndpointId`) | In-product Eval agreed with the independent `gpt-oss:20b` judge on 19 of 20 answers (κ 0.83), so self-judging did not inflate scores on this sample | 3 | 7 | 10 | Low priority |

## How to verify a fix

Run the fix's dataset and configuration before and after the change, then compare the two reports:

```bash
B="dotnet run --project src/Test.Benchmark -c Release --no-build --"
$B retrieval --dataset benchmarks/datasets/meridian.json --modes Hybrid --label before
# ...apply the change, rebuild, restart the bench server...
$B retrieval --dataset benchmarks/datasets/meridian.json --modes Hybrid --label after
$B compare --baseline benchmarks/results/<before>.json --candidate benchmarks/results/<after>.json
```

Record the result in the Status column and in benchmarks/RESULTS.md. `benchmarks/ci-gate.sh` guards against later
regressions.
