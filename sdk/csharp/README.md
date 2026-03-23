# AssistantHub C# SDK

A .NET 8.0 client library for the [AssistantHub](../../README.md) REST API. Provides typed models, async methods, streaming support, and structured error handling for all AssistantHub endpoints.

## Installation

Add a project reference to the SDK:

```bash
dotnet add reference path/to/AssistantHub.Sdk/AssistantHub.Sdk.csproj
```

Or, if the SDK is published as a NuGet package:

```bash
dotnet add package AssistantHub.Sdk
```

## Quick Start

```csharp
using AssistantHub.Sdk;
using AssistantHub.Sdk.Models;

using (AssistantHubClient client = new AssistantHubClient("http://localhost:8800", "your-api-key"))
{
    // List all assistants
    List<Assistant> assistants = await client.ListAssistantsAsync();

    // Send a chat message
    ChatCompletionRequest request = new ChatCompletionRequest
    {
        Messages = new List<ChatCompletionMessage>
        {
            new ChatCompletionMessage { Role = "user", Content = "Hello!" }
        }
    };

    ChatCompletionResponse response = await client.SendMessageAsync(assistants[0].Id, request);
    Console.WriteLine(response.Choices[0].Message.Content);
}
```

## Authentication

All API calls require an API key passed to the client constructor:

```csharp
using AssistantHub.Sdk;

// Client creates and owns its own HttpClient
using (AssistantHubClient client = new AssistantHubClient("http://localhost:8800", "your-api-key"))
{
    // Use client...
}

// Or provide your own HttpClient (client does not dispose it)
using (HttpClient httpClient = new HttpClient())
{
    using (AssistantHubClient client = new AssistantHubClient("http://localhost:8800", httpClient, "your-api-key"))
    {
        // Use client...
    }
}
```

## Assistant Management

```csharp
using AssistantHub.Sdk;
using AssistantHub.Sdk.Models;

using (AssistantHubClient client = new AssistantHubClient("http://localhost:8800", "your-api-key"))
{
    // Create an assistant
    Assistant assistant = new Assistant
    {
        Name = "My Assistant",
        Description = "A helpful assistant for answering questions"
    };
    Assistant created = await client.CreateAssistantAsync(assistant);
    Console.WriteLine($"Created assistant: {created.Id}");

    // Get an assistant
    Assistant fetched = await client.GetAssistantAsync(created.Id);

    // Update an assistant
    fetched.Description = "Updated description";
    await client.UpdateAssistantAsync(fetched.Id, fetched);

    // List all assistants
    List<Assistant> assistants = await client.ListAssistantsAsync();

    // Delete an assistant
    await client.DeleteAssistantAsync(created.Id);
}
```

## Chat (Non-Streaming)

```csharp
using AssistantHub.Sdk;
using AssistantHub.Sdk.Models;

using (AssistantHubClient client = new AssistantHubClient("http://localhost:8800", "your-api-key"))
{
    ChatCompletionRequest request = new ChatCompletionRequest
    {
        Messages = new List<ChatCompletionMessage>
        {
            new ChatCompletionMessage { Role = "user", Content = "What is RAG?" }
        },
        Temperature = 0.7,
        MaxTokens = 1024
    };

    // Send without a thread (stateless)
    ChatCompletionResponse response = await client.SendMessageAsync("asst_your-id", request);
    Console.WriteLine(response.Choices[0].Message.Content);

    // Send with a thread (conversation history is preserved)
    string threadId = await client.CreateThreadAsync("asst_your-id");
    ChatCompletionResponse threadResponse = await client.SendMessageAsync("asst_your-id", request, threadId);

    // Access retrieval metadata and citations
    if (threadResponse.Retrieval != null)
    {
        Console.WriteLine($"Retrieval took {threadResponse.Retrieval.DurationMs}ms");
    }
    if (threadResponse.Citations != null)
    {
        foreach (CitationSource source in threadResponse.Citations.Sources)
        {
            Console.WriteLine($"  [{source.Index}] {source.DocumentName} (score: {source.Score})");
        }
    }
}
```

## Chat (Streaming)

The SDK provides `IAsyncEnumerable<string>` for streaming responses. Each yielded string is a raw JSON chunk from the SSE stream.

```csharp
using AssistantHub.Sdk;
using AssistantHub.Sdk.Models;

using (AssistantHubClient client = new AssistantHubClient("http://localhost:8800", "your-api-key"))
{
    ChatCompletionRequest request = new ChatCompletionRequest
    {
        Messages = new List<ChatCompletionMessage>
        {
            new ChatCompletionMessage { Role = "user", Content = "Explain vector search." }
        },
        Stream = true
    };

    await foreach (string chunk in client.SendMessageStreamAsync("asst_your-id", request))
    {
        Console.Write(chunk);
    }
    Console.WriteLine();
}
```

## Document Upload and Search

```csharp
using AssistantHub.Sdk;
using AssistantHub.Sdk.Models;

using (AssistantHubClient client = new AssistantHubClient("http://localhost:8800", "your-api-key"))
{
    // Upload a document from a byte array
    byte[] content = File.ReadAllBytes("document.pdf");
    AssistantDocument uploaded = await client.UploadDocumentAsync(
        "irule_your-rule-id",
        content,
        "document.pdf",
        "application/pdf");

    Console.WriteLine($"Uploaded: {uploaded.Id}, Status: {uploaded.Status}");

    // Upload from a stream
    using (FileStream stream = File.OpenRead("large-file.pdf"))
    {
        AssistantDocument streamUploaded = await client.UploadDocumentAsync(
            "irule_your-rule-id",
            stream,
            "large-file.pdf",
            "application/pdf");
    }

    // List documents with pagination
    EnumerationQuery query = new EnumerationQuery { MaxResults = 10 };
    EnumerationResult<AssistantDocument> documents = await client.ListDocumentsAsync(query);
    foreach (AssistantDocument doc in documents.Objects)
    {
        Console.WriteLine($"  {doc.OriginalFilename} - {doc.Status}");
    }

    // Search documents
    ChatCompletionRequest searchRequest = new ChatCompletionRequest
    {
        Messages = new List<ChatCompletionMessage>
        {
            new ChatCompletionMessage { Role = "user", Content = "quarterly revenue" }
        }
    };
    ChatCompletionResponse results = await client.SearchAsync("asst_your-id", searchRequest);
}
```

## Error Handling

The SDK throws typed exceptions that map to HTTP status codes:

| Exception | HTTP Status | Description |
|---|---|---|
| `AuthenticationException` | 401 | Invalid or missing API key |
| `NotFoundException` | 404 | Resource not found |
| `ValidationException` | 400 | Invalid request data |
| `AssistantHubException` | Other | Base exception for all other errors |

```csharp
using AssistantHub.Sdk;
using AssistantHub.Sdk.Exceptions;

using (AssistantHubClient client = new AssistantHubClient("http://localhost:8800", "your-api-key"))
{
    try
    {
        Assistant assistant = await client.GetAssistantAsync("asst_nonexistent");
    }
    catch (AuthenticationException ex)
    {
        Console.WriteLine($"Authentication failed: {ex.Message}");
    }
    catch (NotFoundException ex)
    {
        Console.WriteLine($"Not found: {ex.Message}");
    }
    catch (ValidationException ex)
    {
        Console.WriteLine($"Validation error: {ex.Message}");
    }
    catch (AssistantHubException ex)
    {
        Console.WriteLine($"API error (HTTP {ex.StatusCode}): {ex.Message}");
    }
}
```

## Available Methods

### Assistants

| Method | Description |
|---|---|
| `ListAssistantsAsync()` | List all assistants |
| `GetAssistantAsync(assistantId)` | Get an assistant by ID |
| `CreateAssistantAsync(assistant)` | Create a new assistant |
| `UpdateAssistantAsync(assistantId, assistant)` | Update an assistant |
| `DeleteAssistantAsync(assistantId)` | Delete an assistant |

### Collections

| Method | Description |
|---|---|
| `ListCollectionsAsync()` | List all collections |
| `GetCollectionAsync(collectionId)` | Get a collection by ID |
| `CreateCollectionAsync(collection)` | Create a new collection |
| `UpdateCollectionAsync(collectionId, collection)` | Update a collection |
| `DeleteCollectionAsync(collectionId)` | Delete a collection |

### Chat and Generate

| Method | Description |
|---|---|
| `SendMessageAsync(assistantId, request, threadId?)` | Send a chat message (with RAG) |
| `SendMessageStreamAsync(assistantId, request, threadId?)` | Stream a chat response (with RAG) |
| `GenerateAsync(assistantId, request, threadId?)` | Send a message (no RAG) |
| `GenerateStreamAsync(assistantId, request, threadId?)` | Stream a response (no RAG) |
| `SearchAsync(assistantId, request, threadId?)` | Search documents |

### Threads

| Method | Description |
|---|---|
| `ListThreadsAsync(assistantId)` | List threads for an assistant |
| `GetThreadAsync(assistantId, threadId)` | Get thread message history |
| `CreateThreadAsync(assistantId)` | Create a new thread |
| `DeleteThreadAsync(threadId)` | Delete a thread |

### Documents

| Method | Description |
|---|---|
| `ListDocumentsAsync(query?)` | List documents with pagination |
| `GetDocumentAsync(documentId)` | Get a document by ID |
| `UploadDocumentAsync(ruleId, content, filename, contentType)` | Upload a document (byte array) |
| `UploadDocumentAsync(ruleId, stream, filename, contentType)` | Upload a document (stream) |
| `DownloadDocumentAsync(documentId)` | Download document content |
| `DeleteDocumentAsync(documentId)` | Delete a document |
| `BulkDeleteDocumentsAsync(documentIds)` | Delete multiple documents |

### Ingestion Rules

| Method | Description |
|---|---|
| `ListIngestionRulesAsync()` | List all ingestion rules |
| `GetIngestionRuleAsync(ruleId)` | Get an ingestion rule by ID |
| `CreateIngestionRuleAsync(rule)` | Create a new ingestion rule |
| `UpdateIngestionRuleAsync(ruleId, rule)` | Update an ingestion rule |
| `DeleteIngestionRuleAsync(ruleId)` | Delete an ingestion rule |

### Embedding Endpoints

| Method | Description |
|---|---|
| `ListEmbeddingEndpointsAsync(query?)` | List embedding endpoints |
| `GetEmbeddingEndpointAsync(endpointId)` | Get an embedding endpoint |
| `CreateEmbeddingEndpointAsync(endpoint)` | Create an embedding endpoint |
| `UpdateEmbeddingEndpointAsync(endpointId, endpoint)` | Update an embedding endpoint |
| `DeleteEmbeddingEndpointAsync(endpointId)` | Delete an embedding endpoint |
| `CheckEmbeddingHealthAsync()` | Check embedding endpoint health |

### Completion Endpoints

| Method | Description |
|---|---|
| `ListCompletionEndpointsAsync(query?)` | List completion endpoints |
| `GetCompletionEndpointAsync(endpointId)` | Get a completion endpoint |
| `CreateCompletionEndpointAsync(endpoint)` | Create a completion endpoint |
| `UpdateCompletionEndpointAsync(endpointId, endpoint)` | Update a completion endpoint |
| `DeleteCompletionEndpointAsync(endpointId)` | Delete a completion endpoint |
| `CheckCompletionHealthAsync()` | Check completion endpoint health |

### Models

| Method | Description |
|---|---|
| `ListModelsAsync()` | List available inference models |
| `PullModelAsync(modelName)` | Pull/download a model |
| `GetPullStatusAsync()` | Get model pull progress |
| `DeleteModelAsync(modelName)` | Delete a model |

### Evaluation

| Method | Description |
|---|---|
| `ListEvalFactsAsync()` | List all eval facts |
| `GetEvalFactAsync(factId)` | Get an eval fact |
| `CreateEvalFactAsync(fact)` | Create an eval fact |
| `UpdateEvalFactAsync(factId, fact)` | Update an eval fact |
| `DeleteEvalFactAsync(factId)` | Delete an eval fact |
| `StartEvalRunAsync(request)` | Start an evaluation run |
| `ListEvalRunsAsync()` | List all eval runs |
| `GetEvalRunAsync(runId)` | Get an eval run |
| `DeleteEvalRunAsync(runId)` | Delete an eval run |
| `ListEvalResultsAsync(runId)` | List results for a run |
| `GetDefaultJudgePromptAsync()` | Get the default judge prompt |

### Crawl Plans

| Method | Description |
|---|---|
| `ListCrawlPlansAsync()` | List all crawl plans |
| `GetCrawlPlanAsync(planId)` | Get a crawl plan |
| `CreateCrawlPlanAsync(plan)` | Create a crawl plan |
| `UpdateCrawlPlanAsync(planId, plan)` | Update a crawl plan |
| `DeleteCrawlPlanAsync(planId)` | Delete a crawl plan |
| `StartCrawlAsync(planId)` | Start a crawl |
| `StopCrawlAsync(planId)` | Stop a running crawl |

### Crawl Operations

| Method | Description |
|---|---|
| `ListCrawlOperationsAsync(planId)` | List operations for a crawl plan |
| `GetCrawlOperationAsync(planId, operationId)` | Get a crawl operation |
| `DeleteCrawlOperationAsync(planId, operationId)` | Delete a crawl operation |
| `GetCrawlStatisticsAsync()` | Get global crawl statistics |
| `GetCrawlOperationStatisticsAsync(planId, operationId)` | Get operation statistics |

### Configuration and Health

| Method | Description |
|---|---|
| `GetConfigAsync()` | Get server configuration |
| `UpdateConfigAsync(configuration)` | Update server configuration |
| `HealthCheckAsync()` | Check if the server is healthy |
| `WhoAmIAsync()` | Get the authenticated user's identity |

### Tenants

| Method | Description |
|---|---|
| `ListTenantsAsync()` | List all tenants |
| `GetTenantAsync(tenantId)` | Get a tenant |
| `CreateTenantAsync(metadata)` | Create a new tenant |
| `UpdateTenantAsync(tenantId, metadata)` | Update a tenant |
| `DeleteTenantAsync(tenantId)` | Delete a tenant |

### Users

| Method | Description |
|---|---|
| `ListUsersAsync(tenantId)` | List users in a tenant |
| `GetUserAsync(tenantId, userId)` | Get a user |
| `CreateUserAsync(tenantId, user)` | Create a user |
| `UpdateUserAsync(tenantId, userId, user)` | Update a user |
| `DeleteUserAsync(tenantId, userId)` | Delete a user |

### Credentials

| Method | Description |
|---|---|
| `ListCredentialsAsync(tenantId)` | List credentials in a tenant |
| `GetCredentialAsync(tenantId, credentialId)` | Get a credential |
| `CreateCredentialAsync(tenantId, credential)` | Create a credential |
| `UpdateCredentialAsync(tenantId, credentialId, credential)` | Update a credential |
| `DeleteCredentialAsync(tenantId, credentialId)` | Delete a credential |

## Configuration Options

The `AssistantHubClient` constructor accepts:

| Parameter | Type | Required | Description |
|---|---|---|---|
| `baseUrl` | `string` | Yes | The AssistantHub server URL (e.g., `http://localhost:8800`) |
| `apiKey` | `string` | No | API key for authentication |
| `httpClient` | `HttpClient` | No | An externally-managed HttpClient instance |

When you provide your own `HttpClient`, the client will not dispose it -- you are responsible for its lifecycle. When the client creates its own `HttpClient`, it will be disposed when the client is disposed.

### JSON Serialization

The SDK uses `System.Text.Json` with the following defaults:

- Case-insensitive property matching
- Null values are ignored during serialization
- Enums are serialized as strings (via `JsonStringEnumConverter`)

## Requirements

- .NET 8.0 or later
- `System.Text.Json` 8.0.5 (included as a package dependency)
