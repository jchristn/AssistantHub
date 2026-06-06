# AssistantHub Python SDK

A Python client library for the [AssistantHub](https://github.com/jchristn/AssistantHub) REST API. Provides both synchronous and asynchronous clients with full type annotations.

## Installation

```bash
pip install assistanthub-sdk
```

**Requirements:** Python 3.10+, httpx, pydantic >= 2.0

## Quick Start

```python
from assistanthub_sdk import AssistantHubClient

client = AssistantHubClient(
    base_url="http://localhost:8800",
    api_key="your-api-key",
)

# List assistants
result = client.list_assistants()
for assistant in result.objects:
    print(assistant.name)

# Send a chat message
from assistanthub_sdk import ChatCompletionMessage

response = client.send_message(
    assistant_id="ast_123",
    messages=[ChatCompletionMessage(role="user", content="Hello!")],
)
print(response.choices[0].message.content)

client.close()
```

### Using a Context Manager

```python
with AssistantHubClient(base_url="http://localhost:8800", api_key="key") as client:
    assistant = client.get_assistant("ast_123")
    print(assistant.name)
```

## Async Client

```python
import asyncio
from assistanthub_sdk import AsyncAssistantHubClient, ChatCompletionMessage

async def main():
    async with AsyncAssistantHubClient(
        base_url="http://localhost:8800",
        api_key="your-api-key",
    ) as client:
        # List assistants
        result = await client.list_assistants()
        for assistant in result.objects:
            print(assistant.name)

        # Send a message
        response = await client.send_message(
            assistant_id="ast_123",
            messages=[ChatCompletionMessage(role="user", content="Hello!")],
        )
        print(response.choices[0].message.content)

asyncio.run(main())
```

## Streaming

Both clients support streaming responses via SSE.

### Sync Streaming

```python
from assistanthub_sdk import AssistantHubClient, ChatCompletionMessage

with AssistantHubClient(base_url="http://localhost:8800", api_key="key") as client:
    for chunk in client.send_message_stream(
        assistant_id="ast_123",
        messages=[ChatCompletionMessage(role="user", content="Tell me a story")],
    ):
        print(chunk, end="", flush=True)
```

### Async Streaming

```python
async with AsyncAssistantHubClient(base_url="http://localhost:8800", api_key="key") as client:
    async for chunk in client.send_message_stream(
        assistant_id="ast_123",
        messages=[ChatCompletionMessage(role="user", content="Tell me a story")],
    ):
        print(chunk, end="", flush=True)
```

## Error Handling

The SDK raises typed exceptions for common HTTP errors:

```python
from assistanthub_sdk import (
    AssistantHubClient,
    AssistantHubError,
    AuthenticationError,
    NotFoundError,
    ValidationError,
)

client = AssistantHubClient(base_url="http://localhost:8800", api_key="key")

try:
    assistant = client.get_assistant("nonexistent")
except NotFoundError as e:
    print(f"Not found: {e} (status={e.status_code})")
except AuthenticationError as e:
    print(f"Auth failed: {e}")
except ValidationError as e:
    print(f"Bad request: {e}")
except AssistantHubError as e:
    print(f"API error: {e} (status={e.status_code})")
```

## Request History

```python
from assistanthub_sdk import AssistantHubClient, RequestHistorySearchFilter

with AssistantHubClient(base_url="http://localhost:8800", api_key="key") as client:
    summary = client.get_request_history_summary(
        RequestHistorySearchFilter(max_results=25, path_contains="/v1.0/assistants")
    )
    print(summary.total_count)

    page = client.list_request_history(RequestHistorySearchFilter(max_results=10))
    first = page.objects[0] if page.objects else None
    if first:
        detail = client.get_request_history_detail(first.id)
        print(detail.path, detail.status_code)
```

## Assistant Analytics

```python
from assistanthub_sdk import AssistantAnalyticsQuery, AssistantHubClient

with AssistantHubClient(base_url="http://localhost:8800", api_key="key") as client:
    query = AssistantAnalyticsQuery(range="lastDay", metrics=["request_count", "p95_duration_ms"])
    overview = client.get_assistant_analytics_overview("asst_abc123", query)
    series = client.get_assistant_analytics_time_series("asst_abc123", query)
    endpoints = client.get_assistant_analytics_endpoints("asst_abc123", {"range": "lastWeek", "limit": 10})

    print(overview.request_count, len(series.series or []), len(endpoints.endpoints or []))
```

## Available Methods

### Assistants

- `list_assistants()` -- List all assistants (paginated)
- `get_assistant(assistant_id)` -- Get a single assistant
- `create_assistant(assistant)` -- Create a new assistant
- `update_assistant(assistant_id, assistant)` -- Update an assistant
- `delete_assistant(assistant_id)` -- Delete an assistant

### Chat

- `send_message(assistant_id, messages, ...)` -- Send a chat message
- `send_message_stream(assistant_id, messages, ...)` -- Stream a chat response
- `search(assistant_id, query, ...)` -- RAG search via chat
- `generate(assistant_id, messages, ...)` -- Generate without RAG
- `generate_stream(assistant_id, messages, ...)` -- Stream generation without RAG
- `open_assistant_chat(assistant_id)` -- Notify AssistantHub that a chat window opened and warm configured endpoint models when enabled

### Documents

- `list_documents(collection_id, ...)` -- List documents (paginated)
- `get_document(document_id)` -- Get a single document
- `upload_document(ingestion_rule_id, content, ...)` -- Upload a document
- `delete_document(document_id)` -- Delete a document
- `bulk_delete_documents(document_ids)` -- Delete multiple documents
- `reindex_document(document_id)` -- Reindex one completed document into Verbex
- `reindex_documents(request, ...)` -- Reindex a bounded page or explicit list of completed documents into Verbex

### Ingestion Rules

- `list_ingestion_rules(...)` -- List ingestion rules (paginated)
- `get_ingestion_rule(rule_id)` -- Get a single rule
- `create_ingestion_rule(rule)` -- Create a rule
- `update_ingestion_rule(rule_id, rule)` -- Update a rule
- `delete_ingestion_rule(rule_id)` -- Delete a rule

### Collections

- `list_collections(...)` -- List collections (paginated)
- `get_collection(collection_id)` -- Get a single collection
- `create_collection(collection)` -- Create a collection
- `update_collection(collection_id, collection)` -- Update a collection
- `delete_collection(collection_id)` -- Delete a collection
- `search_collection(collection_id, request)` -- Search RecallDB collection records through AssistantHub

### Indices

- `list_indices(...)` -- List Verbex inverted indices
- `create_index(index)` -- Create a Verbex inverted index
- `get_index(index_id)` -- Get Verbex index metadata
- `index_exists(index_id)` -- Check whether a Verbex index exists
- `update_index(index_id, index)` -- Update Verbex index metadata
- `delete_index(index_id)` -- Delete a Verbex index
- `update_index_labels(index_id, labels)` -- Replace Verbex index labels
- `update_index_tags(index_id, tags)` -- Replace Verbex index tags
- `update_index_custom_metadata(index_id, metadata)` -- Replace Verbex index custom metadata
- `get_index_top_terms(index_id, max_results=None)` -- Get top terms for a Verbex index
- `search_index(index_id, request)` -- Search a Verbex index

### Index Records

- `list_index_records(index_id, ...)` -- List records in a Verbex index
- `create_index_record(index_id, record)` -- Create one Verbex index record
- `create_index_records_batch(index_id, records)` -- Create Verbex index records in batch
- `check_index_records_exist(index_id, record_ids)` -- Check multiple Verbex record IDs
- `get_index_record(index_id, record_id)` -- Get one Verbex index record
- `index_record_exists(index_id, record_id)` -- Check whether a Verbex index record exists
- `delete_index_record(index_id, record_id)` -- Delete one Verbex index record
- `delete_index_records(index_id, record_ids)` -- Delete multiple Verbex index records
- `update_index_record_labels(index_id, record_id, labels)` -- Replace Verbex record labels
- `update_index_record_tags(index_id, record_id, tags)` -- Replace Verbex record tags
- `update_index_record_custom_metadata(index_id, record_id, metadata)` -- Replace Verbex record custom metadata

### Threads

- `list_threads(assistant_id, ...)` -- List threads
- `get_thread(assistant_id, thread_id)` -- Get thread history
- `create_thread(assistant_id)` -- Create a new thread
- `delete_thread(thread_id)` -- Delete a thread

### Embedding Endpoints

- `list_embedding_endpoints(...)` -- List endpoints (paginated)
- `get_embedding_endpoint(endpoint_id)` -- Get a single endpoint
- `create_embedding_endpoint(endpoint)` -- Create an endpoint
- `update_embedding_endpoint(endpoint_id, endpoint)` -- Update an endpoint
- `delete_embedding_endpoint(endpoint_id)` -- Delete an endpoint
- `check_embedding_health()` -- Check all endpoint health
- `check_embedding_endpoint_health(endpoint_id)` -- Check single endpoint health

### Completion Endpoints

- `list_completion_endpoints(...)` -- List endpoints (paginated)
- `get_completion_endpoint(endpoint_id)` -- Get a single endpoint
- `create_completion_endpoint(endpoint)` -- Create an endpoint
- `update_completion_endpoint(endpoint_id, endpoint)` -- Update an endpoint
- `delete_completion_endpoint(endpoint_id)` -- Delete an endpoint
- `check_completion_health()` -- Check all endpoint health
- `check_completion_endpoint_health(endpoint_id)` -- Check single endpoint health

### Inference / Models

- `list_models(assistant_id)` -- List available models
- `pull_model(model_name)` -- Start pulling a model
- `get_pull_status()` -- Check pull progress
- `delete_model(model_name)` -- Delete a model

### Evaluation

- `list_eval_facts(...)` -- List eval facts (paginated)
- `get_eval_fact(fact_id)` -- Get a single fact
- `create_eval_fact(fact)` -- Create a fact
- `update_eval_fact(fact_id, fact)` -- Update a fact
- `delete_eval_fact(fact_id)` -- Delete a fact
- `start_eval_run(run_request)` -- Start an eval run
- `list_eval_runs(...)` -- List eval runs (paginated)
- `get_eval_run(run_id)` -- Get a single run
- `delete_eval_run(run_id)` -- Delete a run
- `list_eval_results(run_id)` -- Get run results
- `get_eval_run_results(run_id)` -- Get run results
- `get_eval_result(result_id)` -- Get a single result
- `stream_eval_run(run_id)` -- Stream run updates via SSE
- `get_default_judge_prompt()` -- Get the default judge prompt

### Request History

- `list_request_history(filter=None)` -- List request-history entries
- `get_request_history_summary(filter=None)` -- Summarize request-history entries
- `get_request_history(request_id)` -- Get a request-history entry
- `get_request_history_detail(request_id)` -- Get detailed request/response payloads
- `delete_request_history(request_id)` -- Delete a single request-history entry
- `delete_request_history_bulk(filter=None)` -- Delete request-history entries matching a filter

### Assistant Analytics

- `get_assistant_analytics_overview(assistant_id, query=None)` -- Summary metrics for an assistant
- `get_assistant_analytics_time_series(assistant_id, query=None)` -- Chart-ready time-series metrics
- `get_assistant_analytics_stages(assistant_id, query=None)` -- Stage bucket summaries
- `get_assistant_analytics_endpoints(assistant_id, query=None)` -- Endpoint/model/provider summaries
- `get_assistant_analytics_slowest(assistant_id, query=None)` -- Slowest assistant requests
- `get_assistant_analytics_feedback(assistant_id, query=None)` -- Feedback trend and totals

### Crawl Plans

- `list_crawl_plans(...)` -- List crawl plans (paginated)
- `get_crawl_plan(plan_id)` -- Get a single plan
- `create_crawl_plan(plan)` -- Create a plan
- `update_crawl_plan(plan_id, plan)` -- Update a plan
- `delete_crawl_plan(plan_id)` -- Delete a plan
- `start_crawl(plan_id)` -- Start a crawl
- `stop_crawl(plan_id)` -- Stop a crawl
- `test_crawl_connectivity(plan_id)` -- Test connectivity
- `enumerate_crawl_contents(plan_id)` -- Enumerate available contents

### Crawl Operations

- `list_crawl_operations(plan_id, ...)` -- List operations (paginated)
- `get_crawl_operation(plan_id, operation_id)` -- Get a single operation
- `delete_crawl_operation(plan_id, operation_id)` -- Delete an operation
- `get_crawl_plan_statistics(plan_id)` -- Get plan statistics
- `get_crawl_operation_statistics(plan_id, operation_id)` -- Get operation statistics

### Configuration

- `get_config()` -- Get server configuration
- `update_config(config)` -- Update server configuration

### Health / Status

- `health_check()` -- Check server health
- `whoami()` -- Get authenticated user info

### Tenants

- `list_tenants(...)` -- List tenants (paginated)
- `get_tenant(tenant_id)` -- Get a single tenant
- `create_tenant(tenant)` -- Create a tenant
- `update_tenant(tenant_id, tenant)` -- Update a tenant
- `delete_tenant(tenant_id)` -- Delete a tenant

### Users

- `list_users(tenant_id, ...)` -- List users (paginated)
- `get_user(tenant_id, user_id)` -- Get a single user
- `create_user(tenant_id, user)` -- Create a user
- `update_user(tenant_id, user_id, user)` -- Update a user
- `delete_user(tenant_id, user_id)` -- Delete a user

### Credentials

- `list_credentials(tenant_id, ...)` -- List credentials (paginated)
- `get_credential(tenant_id, credential_id)` -- Get a single credential
- `create_credential(tenant_id, credential)` -- Create a credential
- `update_credential(tenant_id, credential_id, credential)` -- Update a credential
- `delete_credential(tenant_id, credential_id)` -- Delete a credential
