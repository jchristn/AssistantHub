# AssistantHub Chat Data Flow

## Complete End-to-End Trace: User Clicks "Send" Through First Response Token

This document exhaustively traces every action, API call, database query, and internal processing step that occurs from the moment a user clicks "Send" in the AssistantHub chat window until the first response token is returned to the user.

---

## Architecture Overview

```
┌──────────────────┐       ┌──────────────┐       ┌────────────────────┐
│  Browser (React) │──────►│ Nginx :8801  │──────►│ AssistantHub :8800 │
│  ChatView.jsx    │       │ Reverse Proxy│       │ (C# / Watson)      │
└──────────────────┘       └──────────────┘       └────────┬───────────┘
                                                           │
                              ┌────────────────────────────┼────────────────────────────┐
                              │                            │                            │
                              ▼                            ▼                            ▼
                   ┌──────────────────┐        ┌───────────────────┐        ┌──────────────────┐
                   │ Partio :8400     │        │ RecallDB :8600    │        │ Ollama :11434    │
                   │ (Embed/Chunk)    │        │ (Vector+Text DB)  │        │ (LLM Inference)  │
                   │                  │        │                   │        │                  │
                   │ Uses Ollama for  │        │ Uses pgvector     │        │ Runs models like │
                   │ embeddings       │        │ (PostgreSQL)      │        │ gemma3:4b        │
                   └──────────────────┘        └───────────────────┘        └──────────────────┘
```

**Services involved (from `docker/compose.yaml`):**

| Service | Container | Internal Port | External Port | Role |
|---------|-----------|---------------|---------------|------|
| `assistanthub-dashboard` | `assistanthub-dashboard` | 8801 | 8801 | Nginx serving React SPA + API proxy |
| `assistanthub-server` | `assistanthub-server` | 8800 | 8800 | Core API server (C#) |
| `partio-server` | `assistanthub-partio-server` | 8400 | 8321 | Embedding & chunking service |
| `recalldb-server` | `assistanthub-recalldb-server` | 8600 | 8401 | Vector + full-text database (pgvector wrapper); supports vector, full-text, and hybrid search |
| `ollama` | `ollama` | 11434 | 11434 | Local LLM inference engine |
| `pgvector` | `assistanthub-pgvector` | 5432 | 5432 | PostgreSQL with vector extension |

---

## PHASE 1: Browser — User Clicks "Send"

**Source file:** `dashboard/src/views/ChatView.jsx` — `handleSend()` function

### Step 1.1: Input Validation

```javascript
// ChatView.jsx, handleSend()
const trimmed = input.trim();
if (!trimmed || loading) return;
```

- Trims whitespace from input
- Blocks send if input is empty or a previous request is in-flight (`loading` state)

### Step 1.2: Slash Command Interception

Before any API call, the frontend checks for local slash commands:

| Command | Action | Makes API Call? |
|---------|--------|-----------------|
| `/clear` | Clears all messages, deletes thread from localStorage, resets threadId state | No |
| `/compact` | Calls `ApiClient.compact()` to force conversation compaction | Yes (see Phase 6) |
| `/context` | Displays current context usage info as a system message | No |
| `/help` | Displays help text as a system message | No |

If the input starts with `/` and matches a command, processing stops here. Otherwise, flow continues.

### Step 1.3: User Message Added to UI

```javascript
const userMessage = { role: 'user', content: trimmed };
setMessages(prev => [...prev, userMessage]);
setInput('');
setLoading(true);
```

- The user message is immediately rendered in the chat window (optimistic UI update)
- Input field is cleared
- Loading spinner is shown

### Step 1.4: Thread Creation (First Message Only)

**Condition:** No `threadId` exists in state (this is the first message in a new conversation)

---

#### OUTBOUND API CALL #1: Create Thread

```
POST {serverUrl}/v1.0/assistants/{assistantId}/threads
```

**Source:** `dashboard/src/utils/api.js` — `ApiClient.createThread()`

**Request:**
```http
POST /v1.0/assistants/asst_abc123/threads HTTP/1.1
Host: localhost:8801
Content-Type: application/json

(empty body)
```

**Nginx proxies to:** `http://assistanthub-server:8800/v1.0/assistants/asst_abc123/threads`

**Server handler:** `ChatHandler.PostCreateThreadAsync()` (`src/AssistantHub.Server/Handlers/ChatHandler.cs`)

**Server processing:**
1. Extract `assistantId` from URL
2. Read assistant from SQLite database: `Database.Assistant.ReadAsync(assistantId)`
3. Verify assistant exists and `assistant.Active == true`
4. Generate new thread ID: `IdGenerator.NewThreadId()` (produces `thr_` prefixed GUID)

**Response:**
```http
HTTP/1.1 201 Created
Content-Type: application/json

{
  "ThreadId": "thr_xK9mR2pL4qN7vB3wY5sT8"
}
```

**Client-side post-processing:**
```javascript
const result = await ApiClient.createThread(serverUrl, assistantId);
setThreadId(result.ThreadId);
// Persisted to localStorage as `ah_thread_${assistantId}`
```

---

### Step 1.5: Build Chat Messages Array

```javascript
const chatMessages = messages
  .filter(m => !m.isError && m.role !== 'system')
  .map(m => ({ role: m.role, content: m.content }));
```

- Filters out error messages and system messages
- Maps to `{ role, content }` shape
- Includes the full conversation history (all prior user + assistant messages)

### Step 1.6: Token Estimation (Client-Side)

```javascript
const totalChars = chatMessages.reduce((sum, m) => sum + (m.content?.length || 0), 0);
const estimatedTokens = Math.ceil(totalChars / 4);
```

- Rough client-side estimate: 4 characters per token
- Used only for UI display, not for server-side decisions

---

## PHASE 2: Browser → Nginx → AssistantHub Server

### Step 2.1: Chat API Call

---

#### OUTBOUND API CALL #2: Send Chat Message

```
POST {serverUrl}/v1.0/assistants/{assistantId}/chat
```

**Source:** `dashboard/src/utils/api.js` — `ApiClient.chat()`

**Request:**
```http
POST /v1.0/assistants/asst_abc123/chat HTTP/1.1
Host: localhost:8801
Content-Type: application/json
X-Thread-ID: thr_xK9mR2pL4qN7vB3wY5sT8

{
  "messages": [
    {
      "role": "user",
      "content": "What is the capital of France?"
    }
  ]
}
```

**Notes:**
- `X-Thread-ID` header is only included if a threadId exists
- The `messages` array contains the **full conversation history** (all user + assistant turns), not just the latest message
- No authentication is required (this is a public/unauthenticated route)

**Multi-turn example (subsequent messages):**
```json
{
  "messages": [
    { "role": "user", "content": "What is the capital of France?" },
    { "role": "assistant", "content": "The capital of France is Paris." },
    { "role": "user", "content": "What is its population?" }
  ]
}
```

### Step 2.2: Nginx Reverse Proxy

**Source:** `docker/dashboard-nginx.conf`

```nginx
location /v1.0/ {
    proxy_pass http://assistanthub-server:8800;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;

    # SSE / streaming support
    proxy_http_version 1.1;
    proxy_buffering off;
    proxy_cache off;
    proxy_read_timeout 300s;
}
```

- Nginx on port `8801` proxies all `/v1.0/*` requests to `assistanthub-server:8800`
- Buffering is disabled to support SSE streaming
- Read timeout is 300 seconds (5 minutes) to accommodate slow LLM responses
- Forwarding headers (`X-Real-IP`, `X-Forwarded-For`, `X-Forwarded-Proto`) are set

---

## PHASE 3: AssistantHub Server — Request Processing

**Source:** `src/AssistantHub.Server/Handlers/ChatHandler.cs` — `PostChatAsync()`

### Step 3.1: Extract and Validate Parameters

```csharp
string assistantId = ctx.Request.Url.Parameters["assistantId"];
// Validates assistantId is not null/empty → 400 Bad Request if missing
```

### Step 3.2: Load Assistant from Database

```csharp
Assistant assistant = await Database.Assistant.ReadAsync(assistantId);
```

- **Database:** SQLite at `./data/assistanthub.db`
- **Table:** `assistants`
- **Query:** SELECT by primary key `assistantId`
- Validates `assistant != null && assistant.Active == true` → 404 Not Found if missing/inactive

### Step 3.3: Parse Request Body

```csharp
string body = ctx.Request.DataAsString;
ChatCompletionRequest chatReq = Serializer.DeserializeJson<ChatCompletionRequest>(body);
```

**Expected shape (`ChatCompletionRequest`):**
```json
{
  "model": "string (optional, overrides assistant default)",
  "messages": [
    { "role": "user|assistant|system", "content": "string" }
  ],
  "stream": false,
  "temperature": 0.7,
  "top_p": 1.0,
  "max_tokens": 4096
}
```

- Validates at least one message is present → 400 Bad Request if empty

### Step 3.4: Load Assistant Settings from Database

```csharp
AssistantSettings settings = await Database.AssistantSettings.ReadByAssistantIdAsync(assistantId);
```

- **Database:** SQLite at `./data/assistanthub.db`
- **Table:** `assistant_settings`
- **Query:** SELECT WHERE `assistant_id = assistantId`
- Returns 500 Internal Error if settings not found

**AssistantSettings fields used in chat flow:**

| Field | Default | Purpose |
|-------|---------|---------|
| `Temperature` | 0.7 | LLM sampling temperature |
| `TopP` | 1.0 | Nucleus sampling |
| `SystemPrompt` | "You are a helpful assistant..." | Base system prompt |
| `MaxTokens` | 4096 | Max generation tokens |
| `ContextWindow` | 8192 | Total context window size |
| `Model` | "gemma3:4b" | Default model name |
| `EnableRag` | false | Whether RAG retrieval is enabled |
| `CollectionId` | null | Vector DB collection for RAG |
| `RetrievalTopK` | 10 | Number of chunks to retrieve |
| `RetrievalScoreThreshold` | 0.3 | Minimum cosine similarity score |
| `SearchMode` | "Vector" | RAG search mode: Vector, FullText, or Hybrid |
| `TextWeight` | 0.3 | Full-text weight in hybrid scoring |
| `FullTextSearchType` | "TsRank" | Ranking function |
| `FullTextLanguage` | "english" | Text search language |
| `FullTextNormalization` | 32 | Score normalization |
| `FullTextMinimumScore` | null | Minimum text score threshold |
| `InferenceEndpointId` | null | Custom completion endpoint (via Partio) |
| `EmbeddingEndpointId` | null | Custom embedding endpoint (via Partio) |
| `Streaming` | true | Whether to use SSE streaming |

### Step 3.5: Extract Thread ID

```csharp
string threadId = ctx.Request.Headers[Constants.ThreadIdHeader]; // "X-Thread-ID"
```

### Step 3.6: Extract Last User Message

```csharp
string lastUserMessage = null;
for (int i = chatReq.Messages.Count - 1; i >= 0; i--)
{
    if (String.Equals(chatReq.Messages[i].Role, "user", StringComparison.OrdinalIgnoreCase))
    {
        lastUserMessage = chatReq.Messages[i].Content;
        break;
    }
}
```

- Iterates backward through messages to find the most recent user message
- This text is used as the RAG query

---

## PHASE 4: RAG Retrieval (Conditional)

**Condition:** `settings.EnableRag == true && settings.CollectionId != null && lastUserMessage != null`

If RAG is disabled, this entire phase is skipped and flow jumps to Phase 5.

**Source:** `src/AssistantHub.Core/Services/RetrievalService.cs` — `RetrieveAsync()`

```csharp
retrievalStartUtc = DateTime.UtcNow;
Stopwatch retrievalSw = Stopwatch.StartNew();

List<RetrievalChunk> retrieved = await Retrieval.RetrieveAsync(
    settings.CollectionId,        // e.g. "col_abc123"
    lastUserMessage,              // e.g. "What is the capital of France?"
    settings.RetrievalTopK,       // e.g. 10
    settings.RetrievalScoreThreshold, // e.g. 0.3
    default,
    settings.EmbeddingEndpointId, // e.g. null (uses global default)
    new RetrievalSearchOptions
    {
        SearchMode = settings.SearchMode,           // "Vector", "FullText", or "Hybrid"
        TextWeight = settings.TextWeight,            // e.g. 0.3
        FullTextSearchType = settings.FullTextSearchType, // "TsRank" or "TsRankCd"
        FullTextLanguage = settings.FullTextLanguage,     // e.g. "english"
        FullTextNormalization = settings.FullTextNormalization, // e.g. 32
        FullTextMinimumScore = settings.FullTextMinimumScore   // e.g. null
    }
);

retrievalSw.Stop();
retrievalDurationMs = Math.Round(retrievalSw.Elapsed.TotalMilliseconds, 2);
```

### Step 4.1: Embed the User Query

**Note:** When `SearchMode == "FullText"`, the embedding step is skipped entirely. No call to Partio `/v1.0/process` is made, and no embedding endpoint is needed. This makes full-text-only retrieval faster since it avoids the embedding round-trip.

---

#### OUTBOUND API CALL #3: Embed Query via Partio (skipped for FullText mode)

```
POST {ChunkingSettings.Endpoint}/v1.0/process
```

**Source:** `RetrievalService.EmbedQueryAsync()`

**Actual URL:** `http://partio-server:8400/v1.0/process` (internal Docker network)

**Request:**
```http
POST /v1.0/process HTTP/1.1
Host: partio-server:8400
Authorization: Bearer partioadmin
Content-Type: application/json

{
  "Type": "Text",
  "Text": "What is the capital of France?",
  "EmbeddingConfiguration": {
    "EmbeddingEndpointId": "ep_xRuezoahg0uLsKpRNPyjA49sU6mmyyUS2UrvAy5GfSAGN"
  }
}
```

**Notes:**
- `EmbeddingEndpointId` is resolved from: the assistant's `EmbeddingEndpointId` setting → falls back to the global `ChunkingSettings.EndpointId` from `assistanthub.json`
- The access key `partioadmin` is from `assistanthub.json` → `Chunking.AccessKey`

**Partio internal processing:**
1. Partio receives the text
2. Partio calls the configured embedding model via Ollama
3. **Partio → Ollama embedding call:**
   - Model: `all-minilm` (from `partio.json` default embedding endpoint config)
   - Endpoint: `http://ollama:11434` (internal Docker)
   - Partio generates vector embeddings for the input text

**Expected Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "GUID": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "Type": "Text",
  "Text": "What is the capital of France?",
  "Chunks": [
    {
      "CellGUID": "f1e2d3c4-b5a6-7890-fedc-ba9876543210",
      "Text": "What is the capital of France?",
      "Embeddings": [0.0123, -0.0456, 0.0789, ... ]
    }
  ],
  "Children": null
}
```

**Extraction:**
```csharp
ProcessResponse processResult = JsonSerializer.Deserialize<ProcessResponse>(responseBody, _JsonOptions);
return processResult.Chunks[0].Embeddings; // List<double> — the embedding vector
```

- Takes `Chunks[0].Embeddings` — the first (and typically only) chunk's embedding vector
- Vector dimensionality depends on the embedding model (e.g., `all-minilm` produces 384-dimensional vectors)

### Step 4.2: Search in RecallDB

---

#### OUTBOUND API CALL #4: Search RecallDB

```
POST {RecallDbSettings.Endpoint}/v1.0/tenants/{tenantId}/collections/{collectionId}/search
```

**Source:** `RetrievalService.ExecuteSearchAsync()`

**Actual URL:** `http://recalldb-server:8600/v1.0/tenants/default/collections/col_abc123/search`

The request body varies based on the configured `SearchMode`:

**Vector mode request (default):**
```json
{
  "Vector": {
    "SearchType": "CosineSimilarity",
    "Embeddings": [0.0123, -0.0456, 0.0789, ... ]
  },
  "MaxResults": 10
}
```

**FullText mode request:**
```json
{
  "FullText": {
    "Query": "What is the capital of France?",
    "SearchType": "TsRank",
    "Language": "english",
    "Normalization": 32,
    "MinimumScore": null
  },
  "MaxResults": 10
}
```

**Hybrid mode request:**
```json
{
  "Vector": {
    "SearchType": "CosineSimilarity",
    "Embeddings": [0.0123, -0.0456, 0.0789, ... ]
  },
  "FullText": {
    "Query": "What is the capital of France?",
    "SearchType": "TsRank",
    "Language": "english",
    "Normalization": 32,
    "TextWeight": 0.3,
    "MinimumScore": null
  },
  "MaxResults": 10
}
```

**Notes:**
- `tenantId` is `"default"` from `assistanthub.json` → `RecallDb.TenantId`
- `collectionId` comes from `AssistantSettings.CollectionId`
- `Embeddings` is the vector from Step 4.1 (absent in FullText mode)
- `MaxResults` is from `AssistantSettings.RetrievalTopK` (default: 10)
- Access key `recalldbadmin` is from `assistanthub.json` → `RecallDb.AccessKey`
- **Hybrid fallback:** When hybrid search returns 0 results, the system automatically retries with vector-only search (embeddings are already computed). This prevents empty results when text terms are too restrictive.

**RecallDB full search capabilities:**

RecallDB supports vector similarity, full-text, and hybrid search. The full `SearchQuery` model accepts:

| Field | Type | Purpose | Used by AssistantHub? |
|-------|------|---------|----------------------|
| `Vector.SearchType` | enum | `CosineSimilarity`, `CosineDistance`, `EuclideanSimilarity`, `EuclideanDistance`, `InnerProduct` | Yes — `CosineSimilarity` only |
| `Vector.Embeddings` | `List<double>` | Query embedding vector | Yes — when SearchMode is Vector or Hybrid |
| `FullText.Query` | string | Full-text search query | Yes — when SearchMode is FullText or Hybrid |
| `FullText.SearchType` | string | Ranking function: `TsRank` or `TsRankCd` | Yes — configurable via FullTextSearchType |
| `FullText.Language` | string | Text search language | Yes — configurable via FullTextLanguage |
| `FullText.Normalization` | int | Score normalization bitmask | Yes — configurable via FullTextNormalization |
| `FullText.TextWeight` | double | Text weight in hybrid scoring | Yes — configurable via TextWeight (Hybrid only) |
| `FullText.MinimumScore` | double? | Minimum text relevance threshold | Yes — configurable via FullTextMinimumScore |
| `Terms.Required` | `List<string>` | Terms that MUST appear in content (case-insensitive `ILIKE '%term%'`) | **No** |
| `Terms.Excluded` | `List<string>` | Terms that must NOT appear in content (case-insensitive `NOT ILIKE '%term%'`) | **No** |
| `LabelFilter` | object | Include/exclude by document labels | **No** |
| `TagFilter` | object | Filter by tag key-value conditions | **No** |
| `CreatedBefore` / `CreatedAfter` | DateTime | Date range filtering | **No** |
| `DocumentIds` | `List<string>` | Restrict to specific document IDs | **No** |
| `MinimumScore` / `MaximumScore` | double | Score thresholds | **No** |
| `MinimumDistance` / `MaximumDistance` | double | Distance thresholds | **No** |
| `MaxResults` | int | Pagination limit | Yes |
| `ContinuationToken` | string | Pagination cursor | **No** |

All conditions are combined with AND logic in the SQL WHERE clause.

AssistantHub supports three search modes, configurable per-assistant:
- **Vector** (default): Semantic similarity search using cosine similarity on embedding vectors.
- **FullText**: Keyword matching using PostgreSQL tsvector full-text search with configurable ranking and language.
- **Hybrid**: Combines both vector and full-text search with a configurable `TextWeight` that blends scores. Formula: `Score = (1 - TextWeight) * vectorScore + TextWeight * textScore`.

**RecallDB internal processing (for this request):**
1. RecallDB receives the search request
2. Performs cosine similarity search against pgvector (PostgreSQL with vector extension) using the `<=>` operator
3. Returns top-K most similar document chunks ranked by cosine similarity score

**Expected Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "Documents": [
    {
      "DocumentId": "adoc_7Hj3kL9mN2pQ4rS",
      "Score": 0.892341,
      "Content": "Paris is the capital and largest city of France, with a population of over 2 million..."
    },
    {
      "DocumentId": "adoc_5Tx8wY1zA3bC6dE",
      "Score": 0.754892,
      "Content": "France is a country in Western Europe with its capital in Paris..."
    },
    {
      "DocumentId": "adoc_9Fg2hI4jK6lM8nO",
      "Score": 0.231456,
      "Content": "The Eiffel Tower is a wrought-iron lattice tower..."
    }
  ]
}
```

### Step 4.3: Score Filtering

```csharp
foreach (SearchResult result in searchResults)
{
    if (result.Score >= scoreThreshold)  // default: 0.3
    {
        if (!String.IsNullOrEmpty(result.Content))
        {
            results.Add(new RetrievalChunk
            {
                DocumentId = result.DocumentId,
                Score = Math.Round(result.Score, 6),
                TextScore = result.TextScore.HasValue ? Math.Round(result.TextScore.Value, 6) : null,
                Content = result.Content
            });
        }
    }
}
```

- Filters out results below the score threshold (default 0.3)
- In the example above, the third result (score 0.231456) would be filtered out
- Rounds scores to 6 decimal places

**Output of Phase 4:**
```csharp
List<RetrievalChunk> retrievalChunks = [
    { DocumentId: "adoc_7Hj3kL9mN2pQ4rS", Score: 0.892341, TextScore: 0.654321, Content: "Paris is the capital..." },
    { DocumentId: "adoc_5Tx8wY1zA3bC6dE", Score: 0.754892, TextScore: 0.432100, Content: "France is a country..." }
];
```

---

## PHASE 5: System Message Construction & Prompt Assembly

**Source:** `ChatHandler.PostChatAsync()` and `InferenceService.BuildSystemMessage()`

### Step 5.1: Extract Context Chunk Content

```csharp
List<string> contextChunks = retrievalChunks.Select(c => c.Content).ToList();
```

### Step 5.2: Build Message List

```csharp
List<ChatCompletionMessage> messages = new List<ChatCompletionMessage>(chatReq.Messages);
```

### Step 5.3: System Message Injection

**Scenario A: No system message in request (typical for chat UI)**

```csharp
if (!hasSystemMessage && !String.IsNullOrEmpty(settings.SystemPrompt))
{
    string fullSystemMessage = Inference.BuildSystemMessage(settings.SystemPrompt, contextChunks);
    messages.Insert(0, new ChatCompletionMessage { Role = "system", Content = fullSystemMessage });
}
```

**Scenario B: System message already exists in request**

```csharp
else if (hasSystemMessage && contextChunks.Count > 0)
{
    // Find and update existing system message with RAG context appended
    messages[i] = new ChatCompletionMessage
    {
        Role = "system",
        Content = Inference.BuildSystemMessage(messages[i].Content, contextChunks)
    };
}
```

### Step 5.4: BuildSystemMessage Logic

**Source:** `InferenceService.BuildSystemMessage()`

```csharp
public string BuildSystemMessage(string systemPrompt, List<string> contextChunks)
{
    StringBuilder sb = new StringBuilder();

    if (!String.IsNullOrEmpty(systemPrompt))
        sb.Append(systemPrompt);

    if (contextChunks != null && contextChunks.Count > 0)
    {
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("Use the following context to answer the user's question:");
        sb.AppendLine();
        sb.AppendLine("Context:");

        foreach (string chunk in contextChunks)
        {
            sb.AppendLine("---");
            sb.AppendLine(chunk);
        }

        sb.AppendLine("---");
    }

    return sb.ToString();
}
```

**Example output (with RAG):**
```
You are a helpful assistant. Use the provided context to answer questions accurately.

Use the following context to answer the user's question:

Context:
---
Paris is the capital and largest city of France, with a population of over 2 million...
---
France is a country in Western Europe with its capital in Paris...
---
```

**Example output (without RAG):**
```
You are a helpful assistant. Use the provided context to answer questions accurately.
```

### Step 5.5: Resolve Parameters

```csharp
string model = !String.IsNullOrEmpty(chatReq.Model) ? chatReq.Model : settings.Model;
double temperature = chatReq.Temperature ?? settings.Temperature;
double topP = chatReq.TopP ?? settings.TopP;
int maxTokens = chatReq.MaxTokens ?? settings.MaxTokens;
```

- Request-level overrides take precedence over assistant settings
- Defaults: model=`gemma3:4b`, temperature=`0.7`, topP=`1.0`, maxTokens=`4096`

### Step 5.6: Resolve Inference Endpoint

```csharp
Enums.InferenceProviderEnum inferenceProvider = Settings.Inference.Provider;  // Ollama
string inferenceEndpoint = Settings.Inference.Endpoint;                       // http://ollama:11434
string inferenceApiKey = Settings.Inference.ApiKey;                           // default
```

**If `settings.InferenceEndpointId` is set (custom completion endpoint via Partio):**

---

#### OUTBOUND API CALL #5 (Conditional): Resolve Completion Endpoint via Partio

```
GET {ChunkingSettings.Endpoint}/v1.0/endpoints/completion/{endpointId}
```

**Source:** `ChatHandler.ResolveCompletionEndpointAsync()`

**Request:**
```http
GET /v1.0/endpoints/completion/ep_customEndpointId HTTP/1.1
Host: partio-server:8400
Authorization: Bearer partioadmin
```

**Expected Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "Id": "ep_customEndpointId",
  "ApiFormat": "OpenAI",
  "Endpoint": "https://api.openai.com/v1",
  "ApiKey": "sk-...",
  "Model": "gpt-4"
}
```

**Extraction:**
```csharp
string apiFormat = ep.TryGetProperty("ApiFormat", out JsonElement af) ? af.GetString() : null;
string epUrl = ep.TryGetProperty("Endpoint", out JsonElement eu) ? eu.GetString() : null;
string apiKey = ep.TryGetProperty("ApiKey", out JsonElement ak) ? ak.GetString() : null;

// Determine provider from ApiFormat
InferenceProviderEnum provider = apiFormat.Equals("OpenAI", ...)
    ? InferenceProviderEnum.OpenAI
    : InferenceProviderEnum.Ollama;
```

---

## PHASE 6: Conversation Compaction (Conditional)

**Source:** `ChatHandler.CompactIfNeeded()`

**Condition:**
```csharp
int estimatedTokens = EstimateTokenCount(messages);    // ~4 chars per token + 4 per message overhead
int availableTokens = settings.ContextWindow - settings.MaxTokens;  // e.g. 8192 - 4096 = 4096

// Compaction triggers if:
// - estimatedTokens > availableTokens, AND
// - messages.Count > 3
// OR if force flag is set
```

If compaction is NOT needed, this phase is skipped entirely.

### Step 6.1: Send Compaction Status via SSE (If Streaming)

If the response is streaming AND compaction is needed, a status event is sent to the client before compaction begins:

```
data: {"id":"chatcmpl-xxx","object":"chat.completion.chunk","created":1234567890,"model":"gemma3:4b","choices":[{"index":0,"delta":{"content":""}}],"status":"Compacting the conversation..."}
```

### Step 6.2: Separate Messages

```csharp
ChatCompletionMessage systemMessage = messages[0];  // if role == "system"
ChatCompletionMessage lastUserMessage = /* last message with role == "user" */;
List<ChatCompletionMessage> compactableMessages = /* everything between system and last user */;
```

### Step 6.3: Build Summarization Prompt

```csharp
StringBuilder conversationText = new StringBuilder();
foreach (ChatCompletionMessage msg in compactableMessages)
{
    conversationText.AppendLine(msg.Role + ": " + msg.Content);
}

List<ChatCompletionMessage> summaryMessages = new List<ChatCompletionMessage>
{
    new ChatCompletionMessage
    {
        Role = "system",
        Content = "You are a helpful assistant that summarizes conversations concisely."
    },
    new ChatCompletionMessage
    {
        Role = "user",
        Content = "Summarize the following conversation preserving key facts, decisions, and context:\n\n" + conversationText.ToString()
    }
};
```

### Step 6.4: Summarization Inference Call

---

#### OUTBOUND API CALL #6 (Conditional): Compaction Summarization

This is a separate inference call to summarize the conversation history.

**For Ollama (default provider):**

```
POST http://ollama:11434/api/chat
```

**Request:**
```http
POST /api/chat HTTP/1.1
Host: ollama:11434
Content-Type: application/json

{
  "model": "gemma3:4b",
  "messages": [
    {
      "role": "system",
      "content": "You are a helpful assistant that summarizes conversations concisely."
    },
    {
      "role": "user",
      "content": "Summarize the following conversation preserving key facts, decisions, and context:\n\nuser: What is the capital of France?\nassistant: The capital of France is Paris.\nuser: What about Germany?\nassistant: The capital of Germany is Berlin."
    }
  ],
  "stream": false
}
```

**Parameters:** temperature=`0.3` (low for deterministic output), maxTokens=`1024`, topP=`1.0`

**Expected Response:**
```json
{
  "message": {
    "role": "assistant",
    "content": "The user asked about European capitals. Key facts: France's capital is Paris, Germany's capital is Berlin."
  }
}
```

### Step 6.5: Rebuild Compacted Messages

```csharp
List<ChatCompletionMessage> compactedMessages = new List<ChatCompletionMessage>();

// 1. Original system message (with RAG context)
compactedMessages.Add(systemMessage);

// 2. Summary as a second system message
compactedMessages.Add(new ChatCompletionMessage
{
    Role = "system",
    Content = "[Conversation Summary]\n" + summaryResult.Content
});

// 3. Last user message
compactedMessages.Add(lastUserMessage);
```

---

## PHASE 7: LLM Inference

**Source:** `ChatHandler.HandleStreamingResponse()` / `HandleNonStreamingResponse()` and `InferenceService`

### Timing Measurements

The following timing measurements are captured during the chat pipeline and persisted to `chat_history`:

| Measurement | Field | What It Measures |
|---|---|---|
| Retrieval Duration | `retrieval_duration_ms` | Time spent in the retrieval phase (Phase 4) |
| Endpoint Resolution | `endpoint_resolution_duration_ms` | HTTP GET to Partio to resolve inference endpoint details (only when `InferenceEndpointId` is configured) |
| Compaction | `compaction_duration_ms` | Time spent in `CompactIfNeeded` — may include a full LLM summarization call if context window is exceeded |
| Inference Connection | `inference_connection_duration_ms` | Time from `HttpClient.SendAsync` to receiving response headers — network latency + model loading |
| Time to First Token | `time_to_first_token_ms` | Time from `inferenceSw.Start()` to first `onDelta` callback — includes connection + prompt processing |
| Time to Last Token | `time_to_last_token_ms` | Time from `inferenceSw.Start()` to stream completion — total inference duration |

**Derived metrics** (computed in the dashboard, not stored):
- **Prompt Processing** = TTFT - Connection time (time the model spent processing the prompt)
- **Token Generation** = TTLT - TTFT (streaming generation phase)

### Step 7.0: Pre-Inference Metrics

```csharp
int promptTokenEstimate = EstimateTokenCount(messages);  // ~chars/4 + 4/message
DateTime promptSentUtc = DateTime.UtcNow;
Stopwatch inferenceSw = Stopwatch.StartNew();
```

### Decision Point: Streaming vs Non-Streaming

```csharp
if (settings.Streaming)  // default: true
    await HandleStreamingResponse(...);
else
    await HandleNonStreamingResponse(...);
```

---

### PATH A: Streaming Response (Default)

#### Step 7A.1: Set SSE Response Headers

```csharp
ctx.Response.StatusCode = 200;
ctx.Response.ContentType = "text/event-stream";
ctx.Response.Headers.Add("Cache-Control", "no-cache");
ctx.Response.Headers.Add("Connection", "keep-alive");
ctx.Response.ChunkedTransfer = true;
```

#### Step 7A.2: Send Initial SSE Chunk (Role Announcement)

```csharp
ChatCompletionResponse initialChunk = new ChatCompletionResponse
{
    Id = completionId,           // "chatcmpl-{guid}"
    Object = "chat.completion.chunk",
    Created = created,           // Unix timestamp
    Model = model,               // "gemma3:4b"
    Choices = new List<ChatCompletionChoice>
    {
        new ChatCompletionChoice
        {
            Index = 0,
            Delta = new ChatCompletionMessage { Role = "assistant" }
        }
    }
};
await WriteSseEvent(ctx, initialChunk);
```

**Sent to client:**
```
data: {"id":"chatcmpl-a1b2c3d4","object":"chat.completion.chunk","created":1708531200,"model":"gemma3:4b","choices":[{"index":0,"delta":{"role":"assistant"}}]}

```

#### Step 7A.3: Call Inference Provider (Streaming)

---

#### OUTBOUND API CALL #7: LLM Inference (Streaming)

**For Ollama (default):**

```
POST http://ollama:11434/api/chat
```

**Source:** `InferenceService.GenerateOllamaStreamingAsync()`

**Request:**
```http
POST /api/chat HTTP/1.1
Host: ollama:11434
Content-Type: application/json

{
  "model": "gemma3:4b",
  "messages": [
    {
      "role": "system",
      "content": "You are a helpful assistant. Use the provided context to answer questions accurately.\n\nUse the following context to answer the user's question:\n\nContext:\n---\nParis is the capital and largest city of France...\n---\nFrance is a country in Western Europe...\n---"
    },
    {
      "role": "user",
      "content": "What is the capital of France?"
    }
  ],
  "stream": true
}
```

**For OpenAI-compatible endpoint (if configured):**

```
POST {endpoint}/chat/completions
```

**Source:** `InferenceService.GenerateOpenAIStreamingAsync()`

**Request:**
```http
POST /chat/completions HTTP/1.1
Host: api.openai.com
Authorization: Bearer sk-...
Content-Type: application/json

{
  "model": "gpt-4",
  "messages": [
    {
      "role": "system",
      "content": "You are a helpful assistant..."
    },
    {
      "role": "user",
      "content": "What is the capital of France?"
    }
  ],
  "max_tokens": 4096,
  "temperature": 0.7,
  "top_p": 1.0,
  "stream": true
}
```

**Note:** Ollama requests do NOT include `max_tokens`, `temperature`, or `top_p` — these are only sent for OpenAI-format requests.

---

### Step 7A.4: Process Streaming Response

**Ollama streaming response format (one JSON object per line):**

```
{"done":false,"message":{"content":"The"}}
{"done":false,"message":{"content":" capital"}}
{"done":false,"message":{"content":" of"}}
{"done":false,"message":{"content":" France"}}
{"done":false,"message":{"content":" is"}}
{"done":false,"message":{"content":" Paris"}}
{"done":false,"message":{"content":"."}}
{"done":true,"message":{"role":"assistant","content":""}}
```

**OpenAI streaming response format (SSE):**

```
data: {"choices":[{"delta":{"content":"The"}}]}
data: {"choices":[{"delta":{"content":" capital"}}]}
data: {"choices":[{"delta":{"content":" of"}}]}
...
data: [DONE]
```

**Processing logic (Ollama):**
```csharp
while ((line = await reader.ReadLineAsync(token)) != null)
{
    JsonDocument doc = JsonDocument.Parse(line);
    JsonElement root = doc.RootElement;

    // Check if done
    if (root.TryGetProperty("done", out JsonElement doneElement) && doneElement.GetBoolean())
    {
        await onComplete(fullContent.ToString());
        return;
    }

    // Extract delta content
    if (root.TryGetProperty("message", out JsonElement messageElement) &&
        messageElement.TryGetProperty("content", out JsonElement contentElement))
    {
        string deltaContent = contentElement.GetString();
        fullContent.Append(deltaContent);
        await onDelta(deltaContent);  // Triggers SSE event to client
    }
}
```

#### Step 7A.5: Forward Each Token to Client

For each `onDelta` callback, a SSE event is sent to the client:

```csharp
onDelta: async (deltaContent) =>
{
    if (!firstTokenCaptured && inferenceSw != null)
    {
        timeToFirstTokenMs = Math.Round(inferenceSw.Elapsed.TotalMilliseconds, 2);
        firstTokenCaptured = true;
    }

    ChatCompletionResponse deltaChunk = new ChatCompletionResponse
    {
        Id = completionId,
        Object = "chat.completion.chunk",
        Created = created,
        Model = model,
        Choices = new List<ChatCompletionChoice>
        {
            new ChatCompletionChoice
            {
                Index = 0,
                Delta = new ChatCompletionMessage { Content = deltaContent }
            }
        }
    };
    await WriteSseEvent(ctx, deltaChunk);
}
```

**Each SSE event sent to client:**
```
data: {"id":"chatcmpl-a1b2c3d4","object":"chat.completion.chunk","created":1708531200,"model":"gemma3:4b","choices":[{"index":0,"delta":{"content":"The"}}]}

data: {"id":"chatcmpl-a1b2c3d4","object":"chat.completion.chunk","created":1708531200,"model":"gemma3:4b","choices":[{"index":0,"delta":{"content":" capital"}}]}

data: {"id":"chatcmpl-a1b2c3d4","object":"chat.completion.chunk","created":1708531200,"model":"gemma3:4b","choices":[{"index":0,"delta":{"content":" of"}}]}

```

#### Step 7A.6: On Complete — Send Finish Chunk + Usage + [DONE]

```csharp
onComplete: async (fullContent) =>
{
    inferenceSw.Stop();
    timeToLastTokenMs = Math.Round(inferenceSw.Elapsed.TotalMilliseconds, 2);

    // Fire-and-forget: write chat history (see Phase 8)
    _ = WriteChatHistoryAsync(...);

    // Send finish chunk with usage stats
    int finishPromptTokens = EstimateTokenCount(messages);
    int finishCompletionTokens = EstimateTokenCount(fullContent);

    ChatCompletionResponse finishChunk = new ChatCompletionResponse
    {
        Id = completionId,
        Object = "chat.completion.chunk",
        Created = created,
        Model = model,
        Choices = new List<ChatCompletionChoice>
        {
            new ChatCompletionChoice
            {
                Index = 0,
                Delta = new ChatCompletionMessage(),
                FinishReason = "stop"
            }
        },
        Usage = new ChatCompletionUsage
        {
            PromptTokens = finishPromptTokens,
            CompletionTokens = finishCompletionTokens,
            TotalTokens = finishPromptTokens + finishCompletionTokens,
            ContextWindow = settings.ContextWindow
        },
        Retrieval = settings.EnableRag ? new ChatCompletionRetrieval
        {
            CollectionId = collectionId,
            DurationMs = retrievalDurationMs,
            ChunksReturned = retrievalChunks.Count,
            Chunks = retrievalChunks
        } : null
    };
    await WriteSseEvent(ctx, finishChunk);

    // Send terminal [DONE] marker
    await ctx.Response.SendChunk(Encoding.UTF8.GetBytes("data: [DONE]\n\n"), true);
}
```

**Finish event sent to client:**
```
data: {"id":"chatcmpl-a1b2c3d4","object":"chat.completion.chunk","created":1708531200,"model":"gemma3:4b","choices":[{"index":0,"delta":{},"finishReason":"stop"}],"usage":{"promptTokens":45,"completionTokens":8,"totalTokens":53,"contextWindow":8192},"retrieval":{"collectionId":"col_abc123","durationMs":23.5,"chunksReturned":2,"chunks":[{"documentId":"adoc_7Hj3kL9mN2pQ4rS","score":0.892341,"content":"Paris is the capital..."},{"documentId":"adoc_5Tx8wY1zA3bC6dE","score":0.754892,"content":"France is a country..."}]}}

data: [DONE]

```

---

### PATH B: Non-Streaming Response

#### Step 7B.1: Call Inference Provider (Non-Streaming)

---

#### OUTBOUND API CALL #7 (alternate): LLM Inference (Non-Streaming)

**For Ollama:**

```
POST http://ollama:11434/api/chat
```

**Request:**
```http
POST /api/chat HTTP/1.1
Host: ollama:11434
Content-Type: application/json

{
  "model": "gemma3:4b",
  "messages": [
    {
      "role": "system",
      "content": "You are a helpful assistant..."
    },
    {
      "role": "user",
      "content": "What is the capital of France?"
    }
  ],
  "stream": false
}
```

**Expected Response (Ollama):**
```json
{
  "message": {
    "role": "assistant",
    "content": "The capital of France is Paris."
  }
}
```

**For OpenAI-compatible:**

```
POST {endpoint}/chat/completions
```

**Request:**
```http
POST /chat/completions HTTP/1.1
Host: api.openai.com
Authorization: Bearer sk-...
Content-Type: application/json

{
  "model": "gpt-4",
  "messages": [
    { "role": "system", "content": "You are a helpful assistant..." },
    { "role": "user", "content": "What is the capital of France?" }
  ],
  "max_tokens": 4096,
  "temperature": 0.7,
  "top_p": 1.0
}
```

**Expected Response (OpenAI):**
```json
{
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "The capital of France is Paris."
      }
    }
  ]
}
```

#### Step 7B.2: Build and Send JSON Response

```csharp
ChatCompletionResponse response = new ChatCompletionResponse
{
    Id = IdGenerator.NewChatCompletionId(),   // "chatcmpl-{guid}"
    Object = "chat.completion",
    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    Model = model,
    Choices = new List<ChatCompletionChoice>
    {
        new ChatCompletionChoice
        {
            Index = 0,
            Message = new ChatCompletionMessage { Role = "assistant", Content = inferenceResult.Content },
            FinishReason = "stop"
        }
    },
    Usage = new ChatCompletionUsage
    {
        PromptTokens = responsePromptTokens,
        CompletionTokens = completionTokens,
        TotalTokens = responsePromptTokens + completionTokens,
        ContextWindow = settings.ContextWindow
    },
    Retrieval = settings.EnableRag ? new ChatCompletionRetrieval { ... } : null
};
```

**Response sent to client:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "chatcmpl-a1b2c3d4-e5f6-7890",
  "object": "chat.completion",
  "created": 1708531200,
  "model": "gemma3:4b",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "The capital of France is Paris."
      },
      "finishReason": "stop"
    }
  ],
  "usage": {
    "promptTokens": 45,
    "completionTokens": 8,
    "totalTokens": 53,
    "contextWindow": 8192
  },
  "retrieval": {
    "collectionId": "col_abc123",
    "durationMs": 23.5,
    "chunksReturned": 2,
    "chunks": [
      {
        "documentId": "adoc_7Hj3kL9mN2pQ4rS",
        "score": 0.892341,
        "content": "Paris is the capital..."
      }
    ]
  }
}
```

---

## PHASE 8: Chat History Persistence (Fire-and-Forget)

**Source:** `ChatHandler.WriteChatHistoryAsync()`

This runs asynchronously and does NOT block the response to the client.

```csharp
_ = WriteChatHistoryAsync(threadId, assistantId, collectionId,
    userMessageUtc, lastUserMessage,
    retrievalStartUtc, retrievalDurationMs, retrievalContext,
    promptSentUtc, promptTokens, timeToFirstTokenMs, timeToLastTokenMs,
    fullContent);
```

**Database write:**
```csharp
ChatHistory history = new ChatHistory();
history.Id = IdGenerator.NewChatHistoryId();        // "chist_{guid}"
history.ThreadId = threadId;                         // "thr_xK9mR2pL4q..."
history.AssistantId = assistantId;                   // "asst_abc123"
history.CollectionId = collectionId;                 // "col_abc123" or null
history.UserMessageUtc = userMessageUtc;             // DateTime when user sent message
history.UserMessage = userMessage;                   // "What is the capital of France?"
history.RetrievalStartUtc = retrievalStartUtc;       // DateTime or null
history.RetrievalDurationMs = retrievalDurationMs;   // e.g. 23.5
history.RetrievalContext = retrievalContext;          // JSON string of RetrievalChunk[]
history.PromptSentUtc = promptSentUtc;               // DateTime when inference started
history.PromptTokens = promptTokens;                 // Estimated token count
history.EndpointResolutionDurationMs = endpointResolutionMs; // e.g. 45.12 (Partio HTTP GET)
history.CompactionDurationMs = compactionMs;         // e.g. 3200.50 (may involve LLM call)
history.InferenceConnectionDurationMs = inferenceConnectionMs; // e.g. 850.00 (HTTP connect + model load)
history.TimeToFirstTokenMs = timeToFirstTokenMs;     // e.g. 150.32
history.TimeToLastTokenMs = timeToLastTokenMs;       // e.g. 2345.67
history.AssistantResponse = assistantResponse;       // "The capital of France is Paris."

await Database.ChatHistory.CreateAsync(history);
```

- **Database:** SQLite at `./data/assistanthub.db`
- **Table:** `chat_history`
- Only written if `threadId` is not null/empty
- Failures are caught and logged but do not affect the user response

---

## PHASE 9: Browser — Response Processing

**Source:** `dashboard/src/views/ChatView.jsx` and `dashboard/src/utils/api.js`

### Streaming Response Processing

#### Step 9.1: Content-Type Detection

```javascript
const contentType = response.headers.get('content-type') || '';

if (!contentType.includes('text/event-stream')) {
    return response.json();  // Non-streaming path
}
```

#### Step 9.2: SSE Stream Reading

```javascript
const reader = response.body.getReader();
const decoder = new TextDecoder();
let fullContent = '';
let buffer = '';
let usage = null;

while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split('\n');
    buffer = lines.pop(); // keep incomplete line

    for (const line of lines) {
        if (!line.startsWith('data: ')) continue;
        const data = line.substring(6);
        if (data === '[DONE]') continue;

        const chunk = JSON.parse(data);

        if (chunk.usage) usage = chunk.usage;
        if (chunk.status && onDelta) onDelta({ status: chunk.status });

        const delta = chunk.choices?.[0]?.delta;
        if (delta?.content) {
            fullContent += delta.content;
            if (onDelta) onDelta({ content: delta.content });
        }
    }
}
```

#### Step 9.3: onDelta Callback — Real-Time UI Updates

```javascript
const onDelta = (delta) => {
    if (delta.status === 'Compacting the conversation...') {
        compactionDetected = true;
    }

    if (delta.content) {
        setMessages(prev => {
            const updated = [...prev];
            if (streamingIndex === null) {
                // First token: create placeholder assistant message
                streamingIndex = updated.length;
                updated.push({
                    role: 'assistant',
                    content: delta.content,
                    isStreaming: true
                });
            } else {
                // Subsequent tokens: append to existing message
                updated[streamingIndex] = {
                    ...updated[streamingIndex],
                    content: updated[streamingIndex].content + delta.content
                };
            }
            return updated;
        });
    }
};
```

#### Step 9.4: Return Unified Response

```javascript
// ApiClient.chat() returns the same shape regardless of streaming
return {
    choices: [{
        index: 0,
        message: { role: 'assistant', content: fullContent },
        finish_reason: 'stop'
    }],
    usage
};
```

#### Step 9.5: Post-Response Processing

```javascript
// Finalize streaming message (remove isStreaming flag)
if (streamingIndex !== null) {
    setMessages(prev => {
        const updated = [...prev];
        updated[streamingIndex] = {
            ...updated[streamingIndex],
            isStreaming: false,
            content: result.choices[0].message.content
        };
        return updated;
    });
}

// Update context usage display
if (result.usage) {
    setContextUsage(result.usage);
}

setLoading(false);
```

#### Step 9.6: Auto-Generate Chat Title (After First Response)

If this is the first successful response in the conversation, the frontend automatically generates a title using the lightweight `/generate` endpoint:

---

#### OUTBOUND API CALL #8: Title Generation

```
POST {serverUrl}/v1.0/assistants/{assistantId}/generate
```

**Source:** `dashboard/src/utils/api.js` — `ApiClient.generate()`

**Request:**
```http
POST /v1.0/assistants/asst_abc123/generate HTTP/1.1
Content-Type: application/json

{
  "messages": [
    { "role": "user", "content": "What is the capital of France?" },
    { "role": "assistant", "content": "The capital of France is Paris." },
    { "role": "user", "content": "Generate a short title (max 6 words) for this conversation. Reply with ONLY the title text, nothing else." }
  ]
}
```

**Note:** This uses the `/generate` endpoint instead of `/chat`. The `/generate` endpoint skips RAG retrieval, system prompt injection, conversation compaction, and chat history persistence — it only performs inference. This avoids the overhead of Phases 4-6 and Phase 8 for a simple title generation prompt, resulting in a short title like "European Capital Cities".

---

## COMPLETE SEQUENCE DIAGRAM

```
Browser                  Nginx:8801          AssistantHub:8800        Partio:8400              RecallDB:8600            Ollama:11434
  │                         │                      │                      │                        │                       │
  │  User clicks "Send"     │                      │                      │                        │                       │
  │                         │                      │                      │                        │                       │
  ├─POST /threads──────────►├─────────────────────►│                      │                        │                       │
  │                         │                      │  DB: Read Assistant   │                        │                       │
  │                         │                      │  DB: Read Settings    │                        │                       │
  │                         │                      │  Generate thr_ ID    │                        │                       │
  │◄─201 {ThreadId}─────────┤◄─────────────────────┤                      │                        │                       │
  │                         │                      │                      │                        │                       │
  │  Store threadId in      │                      │                      │                        │                       │
  │  localStorage           │                      │                      │                        │                       │
  │                         │                      │                      │                        │                       │
  ├─POST /chat─────────────►├─────────────────────►│                      │                        │                       │
  │  X-Thread-ID: thr_...   │                      │                      │                        │                       │
  │  {messages: [...]}      │                      │  DB: Read Assistant   │                        │                       │
  │                         │                      │  DB: Read Settings    │                        │                       │
  │                         │                      │  Extract last user msg│                        │                       │
  │                         │                      │                      │                        │                       │
  │                         │                      │  [IF RAG ENABLED]     │                        │                       │
  │                         │                      ├─POST /v1.0/process──►│                        │                       │
  │                         │                      │  {Text, EmbConfig}   │  Embed text via         │                       │
  │                         │                      │                      │  Ollama all-minilm──────┼──────────────────────►│
  │                         │                      │                      │                        │                       │
  │                         │                      │                      │◄─────embedding vector───┼───────────────────────┤
  │                         │                      │◄─{Chunks[0].Embed}───┤                        │                       │
  │                         │                      │                      │                        │                       │
  │                         │                      ├─POST /search─────────┼───────────────────────►│                       │
  │                         │                      │  {Vector, MaxResults} │                        │  pgvector cosine      │
  │                         │                      │                      │                        │  similarity search     │
  │                         │                      │◄─{Documents: [...]}──┼────────────────────────┤                       │
  │                         │                      │                      │                        │                       │
  │                         │                      │  Filter by score     │                        │                       │
  │                         │                      │  threshold (>= 0.3)  │                        │                       │
  │                         │                      │  [END IF RAG]        │                        │                       │
  │                         │                      │                      │                        │                       │
  │                         │                      │  Build system msg    │                        │                       │
  │                         │                      │  + RAG context       │                        │                       │
  │                         │                      │                      │                        │                       │
  │                         │                      │  [IF CUSTOM ENDPOINT]│                        │                       │
  │                         │                      ├─GET /endpoints/...──►│                        │                       │
  │                         │                      │◄─{Provider,URL,Key}──┤                        │                       │
  │                         │                      │  [END IF]            │                        │                       │
  │                         │                      │                      │                        │                       │
  │                         │                      │  [IF COMPACTION      │                        │                       │
  │                         │                      │   NEEDED]            │                        │                       │
  │◄─SSE: status "Compact.."┤◄─────────────────────┤                      │                        │                       │
  │                         │                      ├─POST /api/chat──────┼────────────────────────┼──────────────────────►│
  │                         │                      │  (summarize conv)    │                        │                       │
  │                         │                      │◄─{summary}──────────┼────────────────────────┼───────────────────────┤
  │                         │                      │  Rebuild messages    │                        │                       │
  │                         │                      │  [END IF]            │                        │                       │
  │                         │                      │                      │                        │                       │
  │◄─SSE: initial {role}────┤◄─────────────────────┤                      │                        │                       │
  │                         │                      ├─POST /api/chat──────┼────────────────────────┼──────────────────────►│
  │                         │                      │  stream: true        │                        │                       │
  │                         │                      │                      │                        │          LLM generates │
  │                         │                      │◄─{"message":{"content":"The"}}────────────────┼───────────────────────┤
  │◄─SSE: delta "The"───────┤◄─────────────────────┤                      │                        │                       │
  │                         │                      │◄─{"message":{"content":" capital"}}───────────┼───────────────────────┤
  │◄─SSE: delta " capital"──┤◄─────────────────────┤                      │                        │                       │
  │                         │                      │◄─{"message":{"content":" of"}}────────────────┼───────────────────────┤
  │◄─SSE: delta " of"───────┤◄─────────────────────┤                      │                        │                       │
  │                         │                      │  ...more tokens...   │                        │                       │
  │                         │                      │◄─{"done":true}───────┼────────────────────────┼───────────────────────┤
  │                         │                      │                      │                        │                       │
  │                         │                      │  DB: Write ChatHistory│                       │                       │
  │                         │                      │  (fire-and-forget)   │                        │                       │
  │                         │                      │                      │                        │                       │
  │◄─SSE: finish + usage────┤◄─────────────────────┤                      │                        │                       │
  │◄─SSE: [DONE]────────────┤◄─────────────────────┤                      │                        │                       │
  │                         │                      │                      │                        │                       │
  │  Render final message   │                      │                      │                        │                       │
  │  Update context usage   │                      │                      │                        │                       │
  │                         │                      │                      │                        │                       │
  │  [TITLE GENERATION]     │                      │                      │                        │                       │
  ├─POST /generate (title)─►├─────────────────────►│  (inference only,    │                        │                       │
  │                         │                      │   no RAG/compact/    │                        │                       │
  │                         │                      │   history)           │                        │                       │
  │                         │                      ├─POST /api/chat──────┼────────────────────────┼──────────────────────►│
  │                         │                      │◄─{title response}───┼────────────────────────┼───────────────────────┤
  │◄─response───────────────┤◄─────────────────────┤                      │                        │                       │
  │                         │                      │                      │                        │                       │
```

---

## SUMMARY: ALL API CALLS IN ORDER

| # | Direction | Method | URL | When | Blocking? |
|---|-----------|--------|-----|------|-----------|
| 1 | Browser → Server | `POST` | `/v1.0/assistants/{id}/threads` | First message only | Yes |
| 2 | Browser → Server | `POST` | `/v1.0/assistants/{id}/chat` | Every message | Yes |
| 3 | Server → Partio | `POST` | `http://partio-server:8400/v1.0/process` | If RAG enabled | Yes |
| 3a | Partio → Ollama | (internal) | `http://ollama:11434` (embedding) | If RAG enabled | Yes |
| 4 | Server → RecallDB | `POST` | `http://recalldb-server:8600/v1.0/tenants/default/collections/{id}/search` | If RAG enabled | Yes |
| 5 | Server → Partio | `GET` | `http://partio-server:8400/v1.0/endpoints/completion/{id}` | If custom endpoint | Yes |
| 6 | Server → Ollama | `POST` | `http://ollama:11434/api/chat` (summarize) | If compaction needed | Yes |
| 7 | Server → Ollama | `POST` | `http://ollama:11434/api/chat` (stream) | Always (main inference) | Yes (streaming) |
| 8 | Server → SQLite | INSERT | `chat_history` table | After response complete | No (fire-and-forget) |
| 9 | Browser → Server | `POST` | `/v1.0/assistants/{id}/generate` (title) | After first response | No (background) |

---

## CONFIGURATION REFERENCE

### `docker/assistanthub/assistanthub.json` (Key Sections)

```json
{
  "Webserver": {
    "Hostname": "*",
    "Port": 8800,
    "Ssl": false
  },
  "Database": {
    "Type": "Sqlite",
    "Filename": "./data/assistanthub.db"
  },
  "Inference": {
    "Provider": "Ollama",
    "Endpoint": "http://ollama:11434",
    "DefaultModel": "gemma3:4b",
    "ApiKey": "default"
  },
  "Chunking": {
    "Endpoint": "http://partio-server:8400",
    "AccessKey": "partioadmin",
    "EndpointId": "ep_xRuezoahg0uLsKpRNPyjA49sU6mmyyUS2UrvAy5GfSAGN"
  },
  "RecallDb": {
    "Endpoint": "http://recalldb-server:8600",
    "TenantId": "default",
    "AccessKey": "recalldbadmin"
  }
}
```

### `docker/partio/partio.json` (Key Sections)

```json
{
  "Rest": {
    "Hostname": "*",
    "Port": 8400
  },
  "DefaultEmbeddingModel": {
    "Model": "all-minilm",
    "Endpoint": "http://ollama:11434",
    "Format": "Ollama"
  }
}
```

### `docker/recalldb/recalldb.json` (Key Sections)

```json
{
  "Webserver": {
    "Hostname": "*",
    "Port": 8600
  },
  "Database": {
    "Host": "pgvector",
    "Port": 5432,
    "DatabaseName": "recalldb",
    "User": "postgres",
    "Password": "password"
  }
}
```

---

## ERROR SCENARIOS

| Error | HTTP Status | Response Body |
|-------|-------------|---------------|
| Missing `assistantId` | 400 | `{"ErrorCode":"BadRequest"}` |
| Assistant not found or inactive | 404 | `{"ErrorCode":"NotFound"}` |
| No messages in request | 400 | `{"ErrorCode":"BadRequest","Details":"At least one message is required."}` |
| Settings not configured | 500 | `{"ErrorCode":"InternalError","Details":"Assistant settings not configured."}` |
| Embedding service failure | (logged) | RAG retrieval returns empty; chat continues without context |
| RecallDB search failure | (logged) | RAG retrieval returns empty; chat continues without context |
| Inference provider error | 502 | `{"ErrorCode":"InternalError","Details":"Ollama API returned 500: ..."}` |
| Streaming inference error | — | `data: [DONE]\n\n` (connection closed) |
| Chat history write failure | (logged) | Does not affect user response |

---

## TOKEN ESTIMATION

Both client and server use a simple heuristic:

```
tokens ≈ ceil(characterCount / 4) + (4 × messageCount)
```

- 4 characters per token (rough average)
- 4 tokens overhead per message (role, delimiters)
- This is an estimate only — actual tokenization depends on the model's tokenizer
