# AssistantHub benchmarks

This directory holds a reproducible benchmark suite for AssistantHub's retrieval-augmented chat. It answers four
questions:

1. Does ingestion keep the answer? Extraction and chunking decide whether a fact is retrievable at all.
2. Does retrieval find the right documents and passages, and what does each pipeline stage add?
3. Are answers grounded in those passages correct, faithful and properly cited?
4. What does all of this cost in latency and throughput?

The plan behind it is [BENCHMARKING.md](../BENCHMARKING.md). The numbers that matter, round by round, are in
[RESULTS.md](RESULTS.md). The ranked list of fixes those numbers point to is in
[RETRIEVAL_IMPROVEMENTS.md](../archive/RETRIEVAL_IMPROVEMENTS_2026-09.md).

The harness (`src/Test.Benchmark`) is black-box. It talks to AssistantHub only over REST, and it calls model endpoints
directly for the independent judge. It never references AssistantHub assemblies, so the same commands can measure a
working tree or a deployment.

| Command | Measures |
|---|---|
| `ingest` | Documents through the real pipeline (upload → S3 → DocumentAtom → Partio chunk and embed → RecallDB). Reports success by content type, docs/s, chunk statistics, and **evidence reachability**: the share of gold evidence passages present in any stored chunk, which caps what retrieval can achieve. |
| `retrieval` | Each query through `POST /v1.0/assistants/{id}/retrieve`, the chat rail's retrieval stages without final inference. Scores document-level Hit@1, Recall@1/5/10, All@5/10, MRR@10 and nDCG@10 (BEIR-compatible), chunk-level evidence recall, and per-stage lift (search → fusion → rerank). Also reports score AUROC on unanswerable questions, answerability-classifier precision and recall, filter precision, pipeline flag rates and latency. Headline metrics carry 95% bootstrap CIs. |
| `chat` | End-to-end answers via `POST /v1.0/assistants/{id}/chat`, graded by an independent LLM judge called directly. Reports accuracy, abstention, faithfulness to the injected context, citation precision and recall, and accuracy split by whether the evidence reached the prompt. Supports repeats (variance) and hand labels (judge κ). |
| `load` | Closed-loop throughput and latency per concurrency level for retrieve, chat or mixed workloads, optionally against an in-process stub model server so AssistantHub, Partio and RecallDB are measured without the model. |
| `history` | Lists the run ledger (`benchmarks/history/runs.jsonl`) with each metric's change against the previous run of the same configuration. |
| `compare` | Diffs two retrieval or chat reports. It flags a regression only when a metric drops by more than a tolerance *and* a paired bootstrap test over shared queries gives p < α, and exits non-zero so it can gate CI. |
| `eval-export` / `eval-run` / `eval-import` | Turns a dataset into in-product EvalFacts. `eval-run` also runs AssistantHub's Eval and measures its agreement with the independent judge. `eval-import` turns an assistant's existing EvalFacts into a dataset that `chat --assistant-id` can run against that assistant, graded by the independent judge. |
| `prepare`, `validate`, `label-sample`, `stub` | Convert public datasets, check a dataset's labels against its source text, sample answers for hand labelling, and run the stub model server on its own. |

## 1. Stand up the isolated stack

The benchmark stack runs its own PostgreSQL/pgvector, RecallDB, Partio, DocumentAtom, Less3, Verbex and an OTel
collector on 38xxx ports. It never touches the development stack in `docker/`. It uses the host's Ollama, and
AssistantHub itself runs from the working tree.

```bash
docker compose -f benchmarks/docker/compose.yaml up -d
dotnet build src/AssistantHub.sln -c Release
benchmarks/start-bench-server.sh            # REST on 127.0.0.1:38800, tenant token "default" (.bat on Windows)
ollama pull all-minilm                      # embedding model the stack's Partio is seeded with (384 dims)
ollama pull gemma3:4b                       # completion model and default judge
```

`docker compose -f benchmarks/docker/compose.yaml down -v` tears the stack down and discards all benchmark data.
The server's SQLite database and logs are under `benchmarks/.run/server/`.

Use `127.0.0.1`, not `localhost`. On Windows, .NET tries `::1` first, and a refused IPv6 connect costs about two
seconds before it falls back to IPv4. The bench configuration and harness defaults already use `127.0.0.1`.

## 2. Datasets

Every dataset uses one JSON format, described in [datasets/SCHEMA.md](datasets/SCHEMA.md). Each corpus becomes its own
AssistantHub collection. Queries carry:

- the relevant document ids, with optional graded relevance
- verbatim **evidence** passages, for chunk-level scoring
- a gold answer
- a retrieval-difficulty `type` and a question `category` (aligned with `EvalFact.RecommendedCategories`)
- optionally a metadata filter, attached documents, or prior conversation turns

| Dataset | In the repo? | Size | What it tests |
|---|---|---|---|
| `datasets/assistanthub-docs.json` | yes | 18 real AssistantHub docs (snapshot 2026-09-24; MCP tool names updated to the v0.17.0 underscore form on 2026-09-30), 120 questions | Real documents: exact identifiers (routes, settings), long reference docs, tables, archive-vs-current filters, follow-ups, negatives |
| `datasets/meridian.json` | yes (built by `datasets/meridian/build.py`) | 160 documents in md/txt/html/pdf/docx, 308 questions | Synthetic enterprise knowledge base: superseded policy versions, regional and model near-duplicates, tables, long runbooks, label/tag filters, attachments, follow-ups, negatives. Questions were written by a separate pass from the corpus. |
| BEIR SciFact | downloaded | 5,183 abstracts, 300 queries | Sanity check against published nDCG@10 (BM25 ≈ 0.665; all-MiniLM-L6-v2 ≈ 0.645) |
| BEIR NFCorpus | downloaded | 3,633 documents, 323 queries | Graded relevance, heavy lexical mismatch (BM25 ≈ 0.325) |
| MultiHop-RAG | downloaded | 609 news articles, 300 of 2,556 queries (stratified) | Multi-document RAG: inference, comparison and temporal questions plus null (unanswerable) queries; evidence is the dataset's verbatim facts |
| Qasper | downloaded | 50 NLP papers, ~170 questions | Long-document QA; each question is scoped to its paper with `attached_document_ids`; evidence is the annotators' evidence paragraphs |

Rebuild Meridian after editing its sources with `python benchmarks/datasets/meridian/build.py`. It uses the standard
library only.

The public datasets are downloaded at run time into the git-ignored `benchmarks/data/` and are not redistributed.
Check their licenses upstream (BEIR, MultiHop-RAG, Qasper) before publishing derived data. To download and convert
them:

```bash
B="dotnet run --project src/Test.Benchmark -c Release --no-build --"
cd benchmarks/data
curl -LO https://public.ukp.informatik.tu-darmstadt.de/thakur/BEIR/datasets/scifact.zip && unzip -o scifact.zip
curl -LO https://public.ukp.informatik.tu-darmstadt.de/thakur/BEIR/datasets/nfcorpus.zip && unzip -o nfcorpus.zip
mkdir -p multihop-rag && curl -L -o multihop-rag/corpus.json https://huggingface.co/datasets/yixuantt/MultiHopRAG/resolve/main/corpus.json \
  && curl -L -o multihop-rag/MultiHopRAG.json https://huggingface.co/datasets/yixuantt/MultiHopRAG/resolve/main/MultiHopRAG.json
curl -L -o qasper.tgz https://qasper-dataset.s3.us-west-2.amazonaws.com/qasper-train-dev-v0.3.tgz && mkdir -p qasper && tar -xzf qasper.tgz -C qasper
cd ../..
$B prepare --format beir --input benchmarks/data/scifact --name scifact --output benchmarks/data/scifact.json
$B prepare --format beir --input benchmarks/data/nfcorpus --name nfcorpus --output benchmarks/data/nfcorpus.json
$B prepare --format multihop-rag --input benchmarks/data/multihop-rag --limit 300 --seed 7 --output benchmarks/data/multihop-rag-300.json
$B prepare --format qasper --input benchmarks/data/qasper/qasper-dev-v0.3.json --papers 50 --seed 7 --output benchmarks/data/qasper-50.json
```

`validate --dataset <file>` checks ids, relevance labels, filters, and that every evidence passage appears in its
relevant documents' source text. Run it after editing any dataset.

## 3. Run

`run-baseline.sh` (or `run-baseline.bat`) runs the standard suite: retrieval on every dataset present, chat on the
committed datasets, and the stub load test. Individual commands:

```bash
B="dotnet run --project src/Test.Benchmark -c Release --no-build --"

# Ingestion and reachability
$B ingest --dataset benchmarks/datasets/meridian.json

# Retrieval, all three modes (the defaults otherwise match a new assistant: topK 10, threshold 0.3, text weight 0.3)
$B retrieval --dataset benchmarks/datasets/meridian.json --reachability
# Sweeps reuse the ingested collection; each value becomes its own assistant
$B retrieval --dataset benchmarks/datasets/meridian.json --modes Hybrid --sweep threshold=0,0.1,0.2,0.3 --label threshold
$B retrieval --dataset benchmarks/datasets/meridian.json --modes Hybrid --threshold 0 --rerank --label rerank
# Chunking variants get their own collection (the configuration hash is part of its name)
$B retrieval --dataset benchmarks/datasets/meridian.json --modes Hybrid --chunk-overlap 32 --label overlap32

# Chat, graded by an independent judge (use a stronger judge for headline numbers)
$B chat --dataset benchmarks/datasets/assistanthub-docs.json --mode Hybrid --threshold 0 --judge-model gemma3:4b
$B chat --dataset benchmarks/datasets/meridian.json --repeats 3 --limit 100
# Judge calibration: sample 50 answers, label "human": true/false by hand, then pass the file back
$B label-sample --report benchmarks/results/<chat>.json --count 50 --output benchmarks/labels/meridian.json
$B chat --dataset benchmarks/datasets/meridian.json --hand-labels benchmarks/labels/meridian.json

# Load: stub models isolate AssistantHub, Partio and RecallDB; drop --stub for end-to-end capacity
$B load --dataset benchmarks/datasets/assistanthub-docs.json --stub --scenario retrieve --concurrency 1,4,16,32 --duration 30

# In-product Eval vs the independent judge, and a team's own Eval set through the independent judge
$B eval-run --dataset benchmarks/datasets/assistanthub-docs.json --limit 60
$B eval-import --assistant-id <asst_id> --output benchmarks/data/eval-mine.json
$B chat --dataset benchmarks/data/eval-mine.json --assistant-id <asst_id>

# Regression gate
$B compare --baseline benchmarks/results/<old>.json --candidate benchmarks/results/<new>.json --tolerance 0.01 --alpha 0.05 --latency-tolerance 0.25

# Against the previous run with the same configuration fingerprint, taken from the ledger
$B compare --baseline previous --candidate benchmarks/results/<new>.json

# Run history: every run, with Δ against the previous run of the same fingerprint
$B history --dataset meridian --kind retrieval --configuration Hybrid --metric ndcg@10
$B history --kind chat --metric accuracy --last 20
$B history --rebuild      # regenerate the ledger from benchmarks/results/*.json
```

### Run history

Every command that writes a report also appends a summary line to `benchmarks/history/runs.jsonl`, one line per
configuration (a retrieval mode, a chat run, a load level). Each line carries the run id, commit, dataset hash, label,
settings, headline metrics and a **fingerprint**: a hash of the kind, dataset, mode and every setting that changes the
result (ingestion and assistant configuration, `--limit`, `--repeats`), but not the label. Runs with the same
fingerprint are repeats of one experiment, so `history` shows Δ against the previous one and
`compare --baseline previous` finds its report. The ledger is small and committed; the full reports it points to stay
in the git-ignored `results/` directory, so report paths only resolve on the machine that ran them. Pass
`--no-history` to keep a run out of the ledger (the CI gate does this).

Each run writes `benchmarks/results/<utc-stamp>-<kind>-<name>[-label].json` for machines and a `.md` for people. The
directory is git-ignored because reports are machine-specific; RESULTS.md carries the numbers that matter.

Every report records:

- the git commit (`-dirty` when there are uncommitted changes)
- the machine
- the container image digests of the bench stack
- the Ollama model digests
- the dataset hash
- the full ingestion and assistant configuration

Collection, ingestion-rule and assistant names are derived from the dataset, the corpus and a hash of the
configuration. A rerun therefore reuses ingested data and uploads only what is missing or failed. Pass `--reingest`
after changing extraction or chunking code, since the configuration hash cannot see code changes.

### Options

Assistant defaults below are the product defaults the ledger started with (Vector, text weight 0.3, no neighbors,
rerank top K 5), not the current defaults for new assistants (Hybrid, 0.5, 1 neighbor, 10). They are kept so earlier
runs stay comparable; pass the options to measure the current defaults.

| Option | Default | Meaning |
|---|---|---|
| `--url`, `--token` | `http://127.0.0.1:38800`, `default` | AssistantHub and a tenant-admin bearer token |
| `--metrics-url` | `http://127.0.0.1:38889/metrics` | Collector Prometheus endpoint for the server stage breakdown (`none` to skip) |
| `--modes` | `Vector,FullText,Hybrid` | Search modes (retrieval) |
| `--mode` | `Vector` | Search mode (chat, load, eval) |
| `--k`, `--threshold`, `--text-weight`, `--fulltext-type`, `--neighbors` | 10, 0.3, 0.3, TsRank, 0 | Assistant retrieval settings |
| `--fusion`, `--rrf-k`, `--candidate-pool`, `--recency-weight`, `--context-order` | Rrf, 60, store default, 0, Score | Hybrid fusion and prompt context order. Non-default values join the configuration fingerprint; defaults do not, so runs from before these settings existed stay comparable |
| `--rewrite`, `--rerank`, `--rerank-k`, `--rerank-threshold`, `--gate`, `--answerability`, `--answerability-mode`, `--citations` | off, off, 5, 3, off, off, LogOnly, off (chat: on) | Pipeline stages |
| `--inference-endpoint`, `--utility-endpoint`, `--embedding-endpoint` | `default` | Completion endpoint for answers, for utility calls, and embedding endpoint |
| `--rerank-type`, `--rerank-endpoint`, `--rerank-candidates`, `--rerank-min-score` | Llm, first configured, 20, none | Reranker: `CrossEncoder` uses a reranker from the server's `Rerankers` settings (the bench stack configures `bench-cross-encoder`, a TEI MiniLM cross-encoder on port 38087). Candidates are retrieved and scored before `--rerank-k` are kept |
| `--conversation-rewrite`, `--supersession`, `--task-prefixes` | off, Demote, off | Rewrite follow-ups into standalone questions (uses the utility endpoint), how superseded documents are treated, and the embedding model's query prefix |
| `--sweep name=v1,v2` | | Vary one of threshold, k, text-weight, neighbors, rerank-k, rerank-threshold, fulltext-type, rrf-k, candidate-pool, recency-weight, fusion |
| `--chunk-strategy`, `--chunk-tokens`, `--chunk-overlap`, `--context-prefix`, `--summarize-endpoint`, `--dimensions`, `--l2-normalize`, `--scope-suffix` | FixedTokenCount, 256, 0 | Ingestion rule (dashboard defaults) |
| `--pipeline` | 2 | Extraction pipeline version the collection must carry. Uploads are tagged `bench_pipeline`; documents from another version are re-ingested into the same collection, so the fingerprint does not change and the ledger compares runs across the change. Pass the older version to reuse its collections; anything the harness uploads is still tagged with the version the server runs |
| `--ingest-concurrency`, `--date-order`, `--reingest` | 4, off, off | Upload window, date-ordered ingest, rebuild |
| `--cell-mode`, `--table-strategy`, `--list-strategy`, `--context-header` | Flat, RowGroupWithHeaders, WholeList, None | Structured cells (one Partio cell per section, table and list) and the context header embedded with each chunk. Non-default values join the collection hash |
| `--link-supersedes` | off | Record each dataset document's `supersedes` link through `PUT /v1.0/documents/{id}/supersedes` after ingest. Without it the links are cleared, so a shared collection only carries links for runs that ask for them |
| `--limit`, `--concurrency`, `--repeats` | all, 4 (chat 1), 1 | Stratified subset, parallel requests, chat repeats |
| `--judge-format`, `--judge-url`, `--judge-model`, `--judge-api-key` | Ollama, `http://127.0.0.1:11434`, `gemma3:4b` | Independent judge (`--judge-format OpenAI` for any OpenAI-compatible endpoint, `none` to skip) |
| `--label`, `--output-dir` | | Report name suffix and directory |

### Using other model endpoints

Any Ollama- or OpenAI-compatible endpoint can serve embeddings, answers and utility calls, including a remote,
bearer-token-protected one. Register it in Partio through AssistantHub, then pass its id to the harness:

```bash
curl -X PUT -H "Authorization: Bearer default" http://127.0.0.1:38800/v1.0/endpoints/embedding   -d '{"Name":"remote-nomic","Model":"nomic-embed-text:latest","Endpoint":"http://host:port/base/","ApiFormat":"Ollama","ApiKey":"<token>","MaxConcurrentRequests":2,"MaxQueueDepth":1000}'
# ...and the same under /v1.0/endpoints/completion for answer/utility models

$B retrieval --dataset <d> --embedding-endpoint <eep_id> --dimensions 768 --label remote-embed
$B chat --dataset <d> --inference-endpoint <cep_id> --utility-endpoint <cep_id>   --judge-url http://host:port/base --judge-model gpt-oss:20b --judge-api-key <token>
```

A new embedding endpoint gets its own collection automatically, because the endpoint id is part of the configuration
hash. Keep `MaxConcurrentRequests` at what the remote side can absorb, and set `MaxQueueDepth` above 0 so Partio queues
rather than rejects. Tokens passed with `--judge-api-key` are never written to reports.

## 4. Reading the results

**Reachability comes first.** A passage that is not in any stored chunk cannot be retrieved, whatever the ranking
does. Low reachability for one content type points at extraction; low reachability everywhere points at chunking.

**Document level vs chunk level.** Document metrics collapse the chunk list to documents by first occurrence (MaxP),
so they compare directly with BEIR. `evidence@k` asks the question RAG actually cares about: did the passage that
answers the question reach the top k chunks? `context_evidence` measures the same over the final chunks, including
neighbor expansion, which is exactly what chat injects.

**Stage lift.** With `include_stages`, the retrieve route returns the ranked list after each stage:

- `1-search`: the first issued query's raw search
- `2-fused`: after multi-query fusion; this list is the re-rank input
- `3-rerank`: the re-ranker's output

Comparing rows shows what each stage adds. `evidence@all` on `2-fused` is the re-ranker's ceiling.

**Score separation.** AUROC of the top score between answerable and unanswerable questions: 0.5 is chance, 1.0 is a
perfect threshold. In hybrid mode `score` is RecallDB's rank-fused score, so the raw `vector_score` is reported
separately.

**Chat.** Accuracy is reported separately for questions where retrieval did and did not put a relevant document in
the prompt, which tells retrieval misses from generation misses. Faithfulness asks whether every claim is supported
by the injected context, which catches right answers taken from the model's own knowledge. The default judge is the
same small local model that answers. It is cheap but noisy, so for headline numbers use a stronger judge and report κ
against about 50 hand labels.

**Uncertainty.** Metrics are means with 95% bootstrap intervals (1,000 resamples, fixed seed). Groups with fewer than
20 queries are marked as indicative. `compare` requires both a tolerance and p < α before it calls a change real.

**Hardware.** Absolute latency and throughput depend on the machine and on whatever else shares the model server.
Compare runs from the same machine, and use `compare` rather than absolute thresholds.
