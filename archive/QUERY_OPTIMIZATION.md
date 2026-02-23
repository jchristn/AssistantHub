# Query Optimization: Intelligent Retrieval Gating

## Problem Statement

Currently, `ChatHandler.PostChatAsync` triggers RAG retrieval on **every user message** when `EnableRag` is enabled. This is wasteful for follow-up prompts that can be answered from existing conversation context — e.g., "Now sort that table by price descending" does not require a new vector search.

The goal is to determine, before retrieval, whether the user's prompt requires new information or can be answered from what's already in the conversation.

---

## Plan: LLM-Based Retrieval Gate

### Overview

Use an LLM call to classify whether the user's prompt requires retrieval. The gate call uses the same model and endpoint already configured for the assistant's chat completion, ensuring consistent behavior and zero additional model configuration. This is the most accurate approach and the easiest to implement without modifying architecture.

### How It Works

Before the retrieval step in `ChatHandler.PostChatAsync` (line ~161), insert a "gate" call:

```
User prompt + recent conversation context
        ↓
  LLM gate call (same model as chat completion, low max_tokens)
        ↓
  Decision: RETRIEVE or SKIP
        ↓
  If RETRIEVE → proceed with RetrievalService.RetrieveAsync()
  If SKIP → go straight to inference with existing context
```

### Implementation Details

**Where to add:** `ChatHandler.cs`, immediately before the retrieval block (line ~161), after extracting `lastUserMessage`.

**Gate prompt template:**

```
You are a retrieval classifier. Given a conversation and the user's latest message,
decide whether answering the latest message requires searching an external knowledge base
for new information, or whether the answer can be constructed entirely from the existing
conversation context.

Respond with exactly one word: RETRIEVE or SKIP

Rules:
- RETRIEVE: The user is asking about new topics, new entities, new data points,
  or information not already present in the conversation.
- SKIP: The user is asking to reformat, reorder, summarize, compare, explain,
  or otherwise manipulate information already provided in the conversation.
  Also SKIP for greetings, meta-questions about the conversation, or clarifications
  about previously retrieved content.

Conversation context (last few turns):
{recentMessages}

Latest user message:
{lastUserMessage}

Decision:
```

**Messages to send to the gate call:**
- A system message containing the gate prompt above
- Inject the last N messages (e.g., last 4–6 messages, or last ~2000 tokens) as the `{recentMessages}` block
- Inject `lastUserMessage` as the current query

**Gate call parameters:**
- `max_tokens`: 3 (only need one word)
- `temperature`: 0.0 (deterministic classification)
- `model`: The same model configured for the assistant's chat completion (`settings.Model`)
- `endpoint`: The same endpoint configured for the assistant's chat completion (`settings.CompletionEndpointId`)
- `provider`: The same provider configured for the assistant (`settings.InferenceProvider`)

Using the same model ensures the gate has the same reasoning capability as the chat completion itself. Since the gate call uses `max_tokens: 3`, the overhead is minimal regardless of model size — the cost is dominated by prompt token processing, not generation.

**Parsing the response:**
- Trim and uppercase the response
- If it contains "RETRIEVE" → proceed with retrieval
- If it contains "SKIP" → bypass retrieval, proceed to inference with whatever context is already in the system message
- Default to RETRIEVE on any ambiguous/malformed response (fail-open)

**Preserving context on SKIP:**
- When skipping retrieval, the system message still needs RAG context from the prior turn
- Two sub-approaches:
  - **A. Rely on conversation history:** The assistant's previous response already contains the retrieved information in its answer. The LLM can use it from the conversation history. No extra work needed.
  - **B. Cache last retrieval context:** Store the last `RetrievalContext` (already saved in `ChatHistory`) and re-inject it into the system message on SKIP. This is more accurate but requires passing the cached context forward.

**Approach A is simpler and usually sufficient** — the prior assistant response already contains the synthesized retrieval data, so the LLM can reference it.

### Configuration

Add to `AssistantSettings.cs`:

| Setting | Type | Default | Purpose |
|---------|------|---------|---------|
| `EnableRetrievalGate` | bool | false | Master toggle for the retrieval gate |

No model or endpoint override settings are needed — the gate always uses the assistant's existing chat completion model and endpoint.

### Telemetry

Add to `ChatHistory.cs`:

| Field | Type | Purpose |
|-------|------|---------|
| `RetrievalGateDecision` | string | "RETRIEVE", "SKIP", or null (gate disabled) |
| `RetrievalGateDurationMs` | double | Latency of the gate call |

This allows you to audit gate accuracy and measure latency overhead.

### Trade-offs

**Advantages:**
- Most accurate classification — LLMs understand nuance and conversational context
- Easy to implement — single additional inference call, ~30 lines of new code in `ChatHandler`
- Tunable — prompt can be refined over time based on observed misclassifications
- No training data needed
- No additional model configuration — uses the same model the assistant already runs
- Fail-open safety — defaults to RETRIEVE on ambiguous responses, so worst case is current behavior

**Costs:**
- Adds latency (one extra LLM round-trip per message, though small with `max_tokens: 3`)
- Consumes additional tokens/compute for the gate prompt processing
- Gate could misclassify edge cases (mitigated by fail-open default)

### Estimated Latency Impact

- Ollama local (max_tokens=3): ~100–300ms depending on model size
- OpenAI (max_tokens=3): ~200–500ms
- This is typically far less than a full retrieval cycle (embedding + vector search), so skipping retrieval on context-reuse prompts yields a net latency reduction

---

## Implementation Checklist

- [ ] Add `EnableRetrievalGate` to `AssistantSettings.cs`
- [ ] Add `RetrievalGateDecision` (string) and `RetrievalGateDurationMs` (double) to `ChatHistory.cs`
- [ ] Add gate prompt as a constant in `ChatHandler.cs` (or a shared constants file)
- [ ] Add gate logic in `ChatHandler.PostChatAsync()` before the retrieval block (~line 161)
  - Resolve the assistant's completion model, endpoint, provider, and API key (same as used for chat)
  - Build the gate messages (system prompt with recent context + last user message)
  - Call `InferenceService.GenerateResponseAsync()` with `max_tokens: 3`, `temperature: 0.0`
  - Parse response to determine RETRIEVE or SKIP
  - On first message in a thread (no prior conversation), always RETRIEVE — skip the gate entirely
- [ ] Wire up gate telemetry to the `ChatHistory` record being saved
- [ ] Add UI toggle for `EnableRetrievalGate` in assistant settings panel
- [ ] Add gate metrics display in the chat telemetry/debug view
- [ ] Test with representative conversation pairs:
  - First message: "What is the price of bananas?" → should RETRIEVE
  - Follow-up: "Sort that by price descending" → should SKIP
  - Follow-up: "What about apples?" → should RETRIEVE (new entity)
  - Follow-up: "Explain that in simpler terms" → should SKIP
  - Follow-up: "Now find me all fruits under $2" → should RETRIEVE (new query)
  - Follow-up: "Thanks!" → should SKIP
  - Follow-up: "Actually, what vegetables do you have?" → should RETRIEVE (new topic)
