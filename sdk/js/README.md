# AssistantHub JavaScript/TypeScript SDK

A complete JavaScript/TypeScript SDK for the [AssistantHub](https://github.com/assistanthub) REST API. Provides typed methods for every API endpoint, including assistants, documents, chat completions (with streaming), collections, endpoints, evaluations, crawl plans, and more.

## Installation

```bash
npm install assistanthub-sdk
```

## Quick Start

```typescript
import { AssistantHubClient } from "assistanthub-sdk";

const client = new AssistantHubClient({
  baseUrl: "http://localhost:8800",
  apiKey: "your-api-key",
});

// List assistants
const result = await client.listAssistants();
console.log(result.Objects);

// Get an assistant
const assistant = await client.getAssistant("asst_abc123");
```

## Authentication

The SDK supports bearer token authentication. You can either pass an API key directly or authenticate with email/password first:

```typescript
// Direct API key
const client = new AssistantHubClient({
  baseUrl: "http://localhost:8800",
  apiKey: "your-bearer-token",
});

// Or authenticate to get a token
const unauthClient = new AssistantHubClient({ baseUrl: "http://localhost:8800" });
const authResult = await unauthClient.authenticate({
  Email: "user@example.com",
  Password: "password",
  TenantId: "default",
});

const client = new AssistantHubClient({
  baseUrl: "http://localhost:8800",
  apiKey: authResult.Credential?.BearerToken,
});
```

## Usage Examples

### Assistants

```typescript
// Create an assistant
const assistant = await client.createAssistant({ Name: "My Assistant" });

// Update settings
await client.updateAssistantSettings(assistant.Id!, {
  Temperature: 0.7,
  EnableRag: true,
  SystemPrompt: "You are a helpful assistant.",
});

// Delete
await client.deleteAssistant(assistant.Id!);
```

### Chat Completions

```typescript
// Non-streaming
const response = await client.chatCompletion("asst_abc123", {
  Messages: [{ role: "user", content: "Hello!" }],
});
console.log(response.choices[0].message?.content);

// Streaming
for await (const chunk of client.chatCompletionStream("asst_abc123", {
  Messages: [{ role: "user", content: "Tell me a story" }],
})) {
  const delta = chunk.choices[0]?.delta?.content;
  if (delta) process.stdout.write(delta);
}
```

### Threads

```typescript
// Create a thread
const thread = await client.createThread("asst_abc123");

// Chat within a thread
const response = await client.chatCompletion(
  "asst_abc123",
  { Messages: [{ role: "user", content: "Hello!" }] },
  thread.ThreadId
);

// Get history
const history = await client.getThreadHistory("asst_abc123", thread.ThreadId);
```

### Documents

```typescript
// Upload a document
const doc = await client.createDocument({
  Name: "guide.pdf",
  ContentType: "application/pdf",
  Base64Content: btoa("...file content..."),
  IngestionRuleId: "irule_abc123",
});

// List documents
const docs = await client.listDocuments({ maxResults: 50 });

// Download
const response = await client.downloadDocument(doc.Id!);
const blob = await response.blob();
```

### Ingestion Rules

```typescript
const rule = await client.createIngestionRule({
  Name: "PDF Ingestion",
  Bucket: "my-bucket",
  CollectionId: "col_abc123",
  Chunking: { Strategy: "FixedTokenCount", FixedTokenCount: 512 },
  Labels: ["docs"],
});
```

### Crawl Plans

```typescript
import { ScheduleInterval, RepositoryType } from "assistanthub-sdk";

const plan = await client.createCrawlPlan({
  Name: "Website Crawl",
  RepositoryType: RepositoryType.Web,
  RepositorySettings: {
    StartUrl: "https://docs.example.com",
    MaxDepth: 3,
  },
  Schedule: {
    IntervalType: ScheduleInterval.Days,
    IntervalValue: 1,
  },
});

// Start crawling
await client.startCrawl(plan.Id!);

// Check operations
const ops = await client.listCrawlOperations(plan.Id!);
```

### Collections

```typescript
const collection = await client.createCollection({ Name: "knowledge-base" });
const collections = await client.listCollections();

// Records
await client.createCollectionRecord(collection.Id!, { content: "..." });
const records = await client.listCollectionRecords(collection.Id!);
```

### Endpoints

```typescript
import { ApiFormat } from "assistanthub-sdk";

// Create embedding endpoint
await client.createEmbeddingEndpoint({
  Name: "embeddings",
  Endpoint: "http://embeddings:8321",
  ApiFormat: ApiFormat.OpenAI,
  Model: "text-embedding-3-small",
});

// Create completion endpoint
await client.createCompletionEndpoint({
  Name: "completions",
  Endpoint: "http://llm:8321",
  ApiFormat: ApiFormat.OpenAI,
  Model: "gpt-4",
});

// Health checks
const health = await client.getEmbeddingEndpointHealth();
```

### Evaluation

```typescript
// Create eval facts
await client.createEvalFact({
  AssistantId: "asst_abc123",
  Question: "What is RAG?",
  ExpectedFacts: JSON.stringify(["Retrieval-Augmented Generation"]),
});

// Run evaluation
const run = await client.createEvalRun({ AssistantId: "asst_abc123" });

// Stream results
for await (const result of client.streamEvalRunResults(run.Id!)) {
  console.log(result.Question, result.OverallPass ? "PASS" : "FAIL");
}
```

### Request History

```typescript
const summary = await client.getRequestHistorySummary({
  maxResults: 25,
  pathContains: "/v1.0/assistants",
});

console.log(summary.TotalCount);

const entries = await client.listRequestHistory({ maxResults: 10 });
const first = entries.Objects[0];
if (first?.Id) {
  const detail = await client.getRequestHistoryDetail(first.Id);
  console.log(detail.Path, detail.StatusCode);
}
```

### Assistant Analytics

```typescript
const overview = await client.getAssistantAnalyticsOverview("asst_abc123", {
  range: "lastDay",
});

const latency = await client.getAssistantAnalyticsTimeSeries("asst_abc123", {
  range: "lastDay",
  metrics: ["request_count", "p95_duration_ms"],
});

const endpoints = await client.getAssistantAnalyticsEndpoints("asst_abc123", {
  range: "lastWeek",
  limit: 10,
});

console.log(overview.RequestCount, latency.Series?.length, endpoints.Endpoints?.length);
```

### Tenants, Users, and Credentials

```typescript
// Create tenant
const { Tenant } = await client.createTenant({ Name: "My Org" });

// Create user
const user = await client.createUser(Tenant.Id!, {
  Email: "user@example.com",
  FirstName: "Jane",
  LastName: "Doe",
});

// Create credential
const cred = await client.createCredential(Tenant.Id!, {
  UserId: user.Id!,
  Name: "API Key",
});
```

### Pagination

All list methods support pagination:

```typescript
let continuationToken: string | undefined;

do {
  const page = await client.listAssistants({
    maxResults: 10,
    continuationToken,
    ordering: "CreatedDescending",
  });
  for (const assistant of page.Objects) {
    console.log(assistant.Name);
  }
  continuationToken = page.ContinuationToken ?? undefined;
} while (continuationToken);
```

### Error Handling

```typescript
import { AssistantHubApiError } from "assistanthub-sdk";

try {
  await client.getAssistant("asst_nonexistent");
} catch (error) {
  if (error instanceof AssistantHubApiError) {
    console.log(error.statusCode); // 404
    console.log(error.body?.Error); // "NotFound"
  }
}
```

## API Reference

The SDK exposes methods for all AssistantHub API endpoints organized by resource group:

| Resource | Methods |
|---|---|
| Health | `health()`, `healthHead()` |
| Auth | `authenticate()`, `whoami()` |
| Tenants | `createTenant()`, `listTenants()`, `getTenant()`, `updateTenant()`, `deleteTenant()`, `tenantExists()` |
| Users | `createUser()`, `listUsers()`, `getUser()`, `updateUser()`, `deleteUser()`, `userExists()` |
| Credentials | `createCredential()`, `listCredentials()`, `getCredential()`, `updateCredential()`, `deleteCredential()`, `credentialExists()` |
| Assistants | `createAssistant()`, `listAssistants()`, `getAssistant()`, `updateAssistant()`, `deleteAssistant()`, `assistantExists()`, `getAssistantPublic()` |
| Settings | `getAssistantSettings()`, `updateAssistantSettings()`, `verifySlack()` |
| Documents | `createDocument()`, `listDocuments()`, `getDocument()`, `deleteDocument()`, `documentExists()`, `bulkDeleteDocuments()`, `reindexDocument()`, `reindexDocuments()`, `getDocumentProcessingLog()`, `downloadDocument()`, `downloadDocumentPublic()` |
| Ingestion Rules | `createIngestionRule()`, `listIngestionRules()`, `getIngestionRule()`, `updateIngestionRule()`, `deleteIngestionRule()`, `ingestionRuleExists()` |
| Chat | `chatCompletion()`, `chatCompletionStream()`, `submitFeedback()`, `compact()`, `generate()`, `openAssistantChat()` |
| Threads | `createThread()`, `getThreadHistory()`, `listThreads()` |
| Labels/Tags | `getDistinctLabels()`, `getDistinctTags()` |
| Feedback | `listFeedback()`, `getFeedback()`, `deleteFeedback()` |
| History | `listHistory()`, `getHistory()`, `deleteHistory()` |
| Request History | `listRequestHistory()`, `getRequestHistorySummary()`, `getRequestHistory()`, `getRequestHistoryDetail()`, `deleteRequestHistory()`, `deleteRequestHistoryBulk()` |
| Embedding Endpoints | `createEmbeddingEndpoint()`, `listEmbeddingEndpoints()`, `getEmbeddingEndpoint()`, `updateEmbeddingEndpoint()`, `deleteEmbeddingEndpoint()`, `embeddingEndpointExists()`, `getEmbeddingEndpointHealth()`, `getEmbeddingEndpointHealthById()`, `testEmbeddingEndpoint()` |
| Completion Endpoints | `createCompletionEndpoint()`, `listCompletionEndpoints()`, `getCompletionEndpoint()`, `updateCompletionEndpoint()`, `deleteCompletionEndpoint()`, `completionEndpointExists()`, `getCompletionEndpointHealth()`, `getCompletionEndpointHealthById()`, `testCompletionEndpoint()` |
| Models | `listModels()`, `pullModel()`, `getPullStatus()`, `deleteModel()` |
| Collections | `createCollection()`, `listCollections()`, `getCollection()`, `updateCollection()`, `deleteCollection()`, `collectionExists()`, `getCollectionDistinctLabels()`, `getCollectionDistinctTags()` |
| Collection Search | `searchCollection()` |
| Collection Records | `createCollectionRecord()`, `listCollectionRecords()`, `getCollectionRecord()`, `deleteCollectionRecord()`, `batchDeleteCollectionRecords()` |
| Indices | `listIndices()`, `createIndex()`, `getIndex()`, `updateIndex()`, `deleteIndex()`, `indexExists()`, `updateIndexLabels()`, `updateIndexTags()`, `updateIndexCustomMetadata()`, `getIndexTopTerms()`, `searchIndex()` |
| Index Records | `listIndexRecords()`, `createIndexRecord()`, `createIndexRecordsBatch()`, `checkIndexRecordsExist()`, `getIndexRecord()`, `indexRecordExists()`, `deleteIndexRecord()`, `deleteIndexRecords()`, `updateIndexRecordLabels()`, `updateIndexRecordTags()`, `updateIndexRecordCustomMetadata()` |
| Buckets | `createBucket()`, `listBuckets()`, `getBucket()`, `deleteBucket()`, `bucketExists()`, `putBucketObject()`, `listBucketObjects()`, `deleteBucketObject()`, `getBucketObjectMetadata()`, `downloadBucketObject()` |
| Crawl Plans | `createCrawlPlan()`, `listCrawlPlans()`, `getCrawlPlan()`, `updateCrawlPlan()`, `deleteCrawlPlan()`, `crawlPlanExists()`, `startCrawl()`, `stopCrawl()`, `testCrawlConnectivity()`, `enumerateCrawl()` |
| Crawl Operations | `listCrawlOperations()`, `getCrawlPlanStatistics()`, `getCrawlOperation()`, `getCrawlOperationStatistics()`, `deleteCrawlOperation()`, `getCrawlOperationEnumeration()` |
| Eval Facts | `createEvalFact()`, `listEvalFacts()`, `getEvalFact()`, `updateEvalFact()`, `deleteEvalFact()` |
| Eval Runs | `createEvalRun()`, `listEvalRuns()`, `getEvalRun()`, `deleteEvalRun()`, `getEvalRunResults()`, `streamEvalRunResults()`, `getEvalResult()`, `getDefaultJudgePrompt()` |
| Configuration | `getConfiguration()`, `updateConfiguration()` |

## License

MIT
