# Performance Improvements

## Scope

Examined the local Docker deployment for chat history records:

- `chist_mpymnlxq_PXgVqn7oURbFRigfQ`
- `chist_mpympw0p_AeyDIeuhRonZoZAPt`

No application code or deployment configuration was changed for this analysis.

## Evidence Reviewed

Primary evidence came from `docker/assistanthub/data/assistanthub.db`, especially the `chat_history`, `assistant_settings`, and `request_history` tables. Downstream correlation came from `docker/partio/data/partio.db`, `docker/less3/less3.db`, and Docker log files under `docker/*/logs`.

The assistant used for both requests was:

- Assistant: `asst_mpyml40x_Kb5T7DQhm88CSV97nR` (`Botox`)
- Thread: `thr_mpymm6f1_v5tQxnZdiUV25ZhgoED`
- Collection: `default`
- RAG enabled: yes
- Retrieval gate enabled: yes
- Query rewrite enabled: yes
- Reranking enabled: yes
- Retrieval top K: 10
- Reranker top K: 5
- Inference endpoint: `cep_MzPNUDfil6wvFIkJ0OeBcJ1oryV8WlduhNXadoYoQ7ub`
- Inference model: `gpt-oss:120b` through Ollama at `http://192.168.86.31:11434`
- Embedding endpoint: `default`
- Embedding model: `all-minilm` through Ollama at `http://ollama:11434`

Partio and ReCallDB were not the slow components in these two requests. Around the request windows, Partio `/v1.0/process` calls were roughly 45-66 ms and ReCallDB searches were roughly 29-51 ms. AssistantHub request-history rows for the chat endpoint recorded `duration_ms = 0`, so the useful end-to-end timings are the dedicated `chat_history` timing columns.

## Request Timing Summary

### `chist_mpymnlxq_PXgVqn7oURbFRigfQ`

Prompt: `What can you tell me about botox?`

Measured timings:

| Item | Duration |
| --- | ---: |
| Query rewrite | 30,652.83 ms |
| Final inference, time to last token | 21,489.71 ms |
| Rerank | 13,895.92 ms |
| Final inference, time to first token | 7,049.20 ms |
| Inference connection | 2,888.16 ms |
| Retrieval | 591.02 ms |
| Endpoint resolution | 11.09 ms |
| Compaction | 1.36 ms |
| Retrieval gate | 0.00 ms |

The longest wall-clock contributors were query rewrite, final answer generation, and LLM reranking. The query rewrite alone consumed about 30.7 seconds before retrieval could proceed.

### `chist_mpympw0p_AeyDIeuhRonZoZAPt`

Prompt: `Give me an example treatment plan and how I should handle certain side effects`

Measured timings:

| Item | Duration |
| --- | ---: |
| Final inference, time to last token | 52,667.88 ms |
| Rerank | 14,453.95 ms |
| Query rewrite | 13,597.95 ms |
| Final inference, time to first token | 6,651.37 ms |
| Inference connection | 3,832.56 ms |
| Retrieval gate | 670.09 ms |
| Retrieval | 132.82 ms |
| Endpoint resolution | 11.90 ms |
| Compaction | 0.01 ms |

The longest wall-clock contributor was final answer generation. The answer was much longer than the first response: 1,878 estimated completion tokens versus 660, and the final prompt was also larger at 3,660 estimated prompt tokens versus 2,952.

## Hot-Path Bottlenecks

### 1. Query Rewrite Uses The Full `gpt-oss:120b` Completion Endpoint

Query rewrite took 30.65 seconds in the first request and 13.60 seconds in the second. This is serial work before retrieval, so it directly delays every later stage. The current path resolves the assistant inference endpoint and uses the same completion model as the final answer path. For this assistant, that means `gpt-oss:120b`.

Three approaches:

1. Add separate utility endpoints for query rewrite and retrieval gate work.
   - Let assistant settings choose a cheaper/smaller model for query rewrite, such as `gemma3:4b`, while keeping `gpt-oss:120b` for final answer generation.
   - This keeps answer quality where it matters but removes a large model call from the pre-retrieval critical path.

2. Replace generative query rewrite with deterministic or bounded rewrites.
   - For short prompts, skip rewrite entirely or use simple expansion rules.
   - For multi-turn prompts, rewrite only when the latest message contains pronouns, ellipsis, or strong dependency on previous context.
   - Store a per-assistant option such as `QueryRewriteMode = Disabled | Heuristic | Llm`.

3. Cache query rewrites by assistant, collection, normalized user query, and relevant conversation hash.
   - The Botox assistant is likely to receive repeated or near-repeated questions.
   - A cache hit eliminates the most expensive pre-retrieval step.
   - Cache entries should include model and prompt-template version keys so they invalidate when behavior changes.

### 2. LLM Reranking Uses The Full `gpt-oss:120b` Completion Endpoint

Reranking took 13.90 seconds and 14.45 seconds. It reranked 10 retrieved chunks in both requests and is also serial before final answer generation. The evidence shows vector retrieval itself was fast, while the LLM rerank stage dominated the retrieval-side hot path.

Three approaches:

1. Use a dedicated reranker instead of a generative LLM.
   - Use a cross-encoder, embeddings-based reranker, or another scoring endpoint that returns numeric relevance directly.
   - This avoids asking a 120B chat model to generate and parse JSON scores.
   - It also makes latency more predictable and reduces parse-failure fallback risk.

2. Make reranking conditional.
   - Skip reranking when vector scores are already confidently separated.
   - Skip reranking for simple fact queries or when top K is small.
   - Run reranking only when retrieval confidence is ambiguous, there are many near-ties, or query complexity warrants it.

3. Reduce rerank input size and model cost.
   - Lower rerank candidate count below 10 for interactive chat.
   - Shorten per-chunk text further or send extracted titles/snippets instead of 500 characters per chunk.
   - Allow a separate rerank endpoint/model, so reranking can use a smaller low-temperature model even if final inference uses `gpt-oss:120b`.

### 3. Final Answer Generation Dominates The Longer Request

Final inference took 21.49 seconds for the first request and 52.67 seconds for the second. The second request generated 1,878 estimated completion tokens at about 40.81 generation tokens/sec, so output length was the dominant factor. Time to first token was still high in both requests at roughly 6.7-7.0 seconds, indicating prompt ingestion/model scheduling overhead before generation starts.

Three approaches:

1. Control response length from assistant policy and request settings.
   - Lower default `max_tokens` for interactive assistant answers.
   - Add concise-answer system guidance for normal questions and require the user to ask for expanded plans.
   - For treatment-plan style questions, generate a compact structured plan first and optionally support a separate expansion action.

2. Reduce prompt size and retrieved context before final inference.
   - The final prompts were estimated at 2,952 and 3,660 tokens.
   - Use fewer reranked chunks, tighter chunk snippets, and more aggressive context budgeting.
   - Prefer source diversity and high-signal excerpts over passing large merged content blocks.

3. Tune the serving path for `gpt-oss:120b`.
   - Keep the model warm and avoid endpoint contention from health checks, query rewrite, rerank, and final inference sharing the same model.
   - Align AssistantHub and Partio concurrency limits with Ollama capacity.
   - If interactive latency is the priority, route final chat to a smaller or quantized model and reserve `gpt-oss:120b` for explicitly deep responses.

### 4. Time To First Token And Connection Setup Are Noticeable

Time to first token was 7.05 seconds and 6.65 seconds. Inference connection duration was 2.89 seconds and 3.83 seconds. These values are smaller than full generation but directly affect perceived responsiveness because the user sees no answer until this stage completes.

Three approaches:

1. Stream earlier status events for expensive pre-inference work.
   - The request already performs query rewrite, retrieval, and rerank before final inference.
   - Emit client-visible progress events such as rewriting, searching, reranking, and generating.
   - This does not reduce compute time, but it reduces apparent dead time and makes long RAG requests diagnosable from the UI.

2. Reuse HTTP clients and avoid per-call connection setup.
   - Audit the Ollama and Partio call paths for per-request `HttpClient` construction and missing connection pooling.
   - Use `IHttpClientFactory` or long-lived clients with sane timeouts.
   - This primarily targets the 2.9-3.8 second connection/setup component.

3. Separate background health checks from active inference capacity.
   - Logs show frequent endpoint health checks around the chat windows.
   - Health checks are cheap individually, but they target the same Ollama host and same models.
   - Reduce health-check frequency, avoid heavyweight model-touching checks, or make checks capacity-aware while inference is active.

## Recommended Prioritization

1. Split utility LLM work from final inference.
   - Query rewrite and rerank should not use `gpt-oss:120b` by default.
   - This addresses 27-45 seconds of serial pre-answer work across the two sampled requests.

2. Replace or conditionally skip LLM reranking.
   - Retrieval was already fast; the rerank model call was the expensive part.
   - A deterministic or specialized reranker is the highest-confidence RAG-path improvement.

3. Add stricter answer-length and context budgets.
   - The second request's final generation time was driven by a long answer.
   - Reducing completion tokens and prompt tokens will improve both total latency and time to first token.

4. Improve observability for streamed chat.
   - AssistantHub's `request_history.duration_ms` was not useful for these chat requests.
   - The dedicated `chat_history` columns were useful, but the UI/API should make them easy to inspect per chat turn.
   - Add phase-level timing to streamed events or response metadata so bottlenecks are visible without direct DB inspection.
