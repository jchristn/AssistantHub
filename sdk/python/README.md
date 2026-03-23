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
    base_url="http://localhost:8000",
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
with AssistantHubClient(base_url="http://localhost:8000", api_key="key") as client:
    assistant = client.get_assistant("ast_123")
    print(assistant.name)
```

## Async Client

```python
import asyncio
from assistanthub_sdk import AsyncAssistantHubClient, ChatCompletionMessage

async def main():
    async with AsyncAssistantHubClient(
        base_url="http://localhost:8000",
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

with AssistantHubClient(base_url="http://localhost:8000", api_key="key") as client:
    for chunk in client.send_message_stream(
        assistant_id="ast_123",
        messages=[ChatCompletionMessage(role="user", content="Tell me a story")],
    ):
        print(chunk, end="", flush=True)
```

### Async Streaming

```python
async with AsyncAssistantHubClient(base_url="http://localhost:8000", api_key="key") as client:
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

client = AssistantHubClient(base_url="http://localhost:8000", api_key="key")

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

### Documents

- `list_documents(collection_id, ...)` -- List documents (paginated)
- `get_document(document_id)` -- Get a single document
- `upload_document(ingestion_rule_id, content, ...)` -- Upload a document
- `delete_document(document_id)` -- Delete a document
- `bulk_delete_documents(document_ids)` -- Delete multiple documents

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
- `get_eval_result(result_id)` -- Get a single result
- `stream_eval_run(run_id)` -- Stream run updates via SSE
- `get_default_judge_prompt()` -- Get the default judge prompt

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
