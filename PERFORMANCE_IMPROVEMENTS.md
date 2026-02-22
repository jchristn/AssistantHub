# AssistantHub Performance Improvements Plan

Based on analysis of the end-to-end chat data flow documented in `CHAT_DATA_FLOW.md`.

---

## Completed

### Ollama Parallel Request & Model Loading Configuration
**Status:** Done

Added `OLLAMA_NUM_PARALLEL: 4` and `OLLAMA_MAX_LOADED_MODELS: 4` environment variables to the Ollama service in both `docker/compose.yaml` (AssistantHub) and `c:\code\partio\partio\docker\compose.yaml` (Partio standalone). This allows Ollama to handle 4 concurrent requests per loaded model and keep up to 4 models resident in memory simultaneously, eliminating embedding (`all-minilm`) / inference (`gemma3:4b`) swap thrashing on the GPU.

### Lightweight Generate Endpoint for Title Generation
**Status:** Done

Added a new `POST /v1.0/assistants/{id}/generate` endpoint that performs inference only — no RAG retrieval, no system prompt injection, no conversation compaction, and no chat history persistence. The dashboard's title generation (`generateChatTitle` in `ChatView.jsx`) now uses `ApiClient.generate()` instead of `ApiClient.chat()`, avoiding the full pipeline (embedding + vector search + compaction check + history write) for a simple title generation prompt.

### Pre-emptive Client-Side Compaction
**Status:** Done

After each chat response, the client now checks the `usage` data returned in the final SSE chunk. If `total_tokens / context_window >= 75%`, the client automatically calls the existing `/compact` endpoint before the user sends their next message. The compacted message list replaces the client's in-memory history, so the next chat request sends an already-compact payload — avoiding the blocking server-side summarization inference call that previously doubled latency on compaction turns. This approach keeps the server stateless and the chat streaming API spec-compliant (no custom SSE events).

**Files changed:**
- `dashboard/src/views/ChatView.jsx` — added pre-emptive compaction check after chat response in `handleSend()`

---

## Proposed Improvements

### 4. Embedding via Partio is a Double Network Hop (Medium Priority)

**Problem:** For RAG query embedding, the flow is: AssistantHub Server -> Partio -> Ollama -> Partio -> Server (Step 4.1). Partio acts as an intermediary that adds network latency.

**Current behavior:** The server calls Partio's `/v1.0/process` endpoint, which internally calls Ollama for the embedding, then returns the result.

**Proposed solution:** When the embedding endpoint resolves to a local Ollama instance, call Ollama's `/api/embed` endpoint directly from AssistantHub, bypassing Partio for query-time embedding. Partio would still be used for document ingestion (chunking + embedding), where its processing pipeline adds value.

**Trade-offs:** Introduces a second code path for embeddings and couples AssistantHub more directly to Ollama's API. The current indirection through Partio keeps embedding logic centralized.

**Files involved:**
- `src/AssistantHub.Core/Services/RetrievalService.cs` — `EmbedQueryAsync()`

---

### 5. No Caching of Assistant/Settings Database Reads (Low Priority)

**Problem:** Every chat request queries SQLite for both the assistant record and its settings (Steps 3.2, 3.4). For a frequently-used assistant, these are redundant reads of rarely-changing data.

**Proposed solution:** Add a short-TTL in-memory cache (e.g., 30-60 seconds) for `Assistant` and `AssistantSettings` objects keyed by `assistantId`. Invalidate on settings update API calls.

**Files involved:**
- `src/AssistantHub.Server/Handlers/ChatHandler.cs` — `PostChatAsync()`
- `src/AssistantHub.Server/Handlers/AssistantSettingsHandler.cs` — cache invalidation on update

---

### 6. Ollama Requests Omit Sampling Parameters (Low Priority)

**Problem:** Ollama inference requests do not include `temperature`, `top_p`, or `max_tokens` (noted at Step 7A.3). This means the assistant's configured values for these parameters have no effect when using the Ollama provider — only Ollama's built-in defaults apply.

**Proposed solution:** Map AssistantHub parameters to Ollama's request format:
- `temperature` -> `options.temperature`
- `top_p` -> `options.top_p`
- `max_tokens` -> `options.num_predict`

**Files involved:**
- `src/AssistantHub.Core/Services/InferenceService.cs` — `GenerateOllamaStreamingAsync()`, `GenerateOllamaNonStreamingAsync()`

---

### 7. Duplicate Database Reads on First Message (Low Priority)

**Problem:** On the first message, the thread creation call (Step 1.4) reads the assistant from the DB, and then the subsequent chat call reads both the assistant and settings again. This results in three DB reads where fewer would suffice.

**Proposed solutions:**
- **Option A:** Have the thread creation response return the assistant settings, and pass them along with the chat request to avoid re-reading.
- **Option B:** Defer thread creation to the server side — the chat endpoint creates a thread implicitly if no `X-Thread-ID` is provided, returning the new `threadId` in the response headers.

**Files involved:**
- `src/AssistantHub.Server/Handlers/ChatHandler.cs` — `PostCreateThreadAsync()`, `PostChatAsync()`
- `dashboard/src/views/ChatView.jsx` — `handleSend()`

---

## Deferred — Not a Material Performance Impact

### 3. Full Conversation History Re-Sent Every Request

**Problem:** The browser sends the entire message history array on every chat request (Step 1.5). As conversations grow, request payloads increase linearly. Compaction mitigates this eventually, but there is no server-side conversation state.

**Current behavior:** The server is stateless with respect to conversation history. The client is the sole owner of message state and must re-transmit everything each time.

**Proposed solution:** Implement server-side thread message storage:
- On each chat request, persist user and assistant messages to a `thread_messages` table keyed by `threadId`.
- The client sends only the new user message; the server reconstructs the full history from the database.
- Compaction can then operate entirely server-side without client awareness.

**Trade-offs:** Adds database writes per turn and increases server complexity. The current stateless design is simpler and avoids DB growth. This is most valuable for long conversations or bandwidth-constrained clients.

**Reason deferred:** This primarily reduces data transmitted over the network rather than improving server-side processing latency. The payload sizes involved in typical conversations are small relative to the inference time that dominates overall response latency.

**Files involved:**
- `src/AssistantHub.Server/Handlers/ChatHandler.cs`
- `src/AssistantHub.Core/Database/` — new table and methods
- `dashboard/src/views/ChatView.jsx` — reduce payload to latest message only
- `dashboard/src/utils/api.js`

---

## Summary

| # | Improvement | Priority | Complexity | Latency Impact |
|---|------------|----------|------------|----------------|
| -- | Ollama env vars (`NUM_PARALLEL`, `MAX_LOADED_MODELS`) | High | Low | Eliminates model reload stalls |
| -- | Lightweight `/generate` endpoint for title generation | Medium | Low | Saves embedding + vector search on first message |
| -- | Pre-emptive client-side compaction | High | Low | Removes blocking summarization call on compaction turns |
| 4 | Direct Ollama embedding (bypass Partio) | Medium | Medium | Removes one network hop per RAG query |
| 5 | Assistant/settings caching | Low | Low | Saves 2 DB reads per request |
| 6 | Pass sampling params to Ollama | Low | Low | Prevents unnecessarily long generations |
| 7 | Reduce duplicate DB reads on first message | Low | Low | Saves 1 DB read on first message only |
