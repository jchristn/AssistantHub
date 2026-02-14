# AssistantHub

<p align="center">
  <img src="assets/logo.png" alt="AssistantHub Logo" width="256">
</p>

AssistantHub is a self-hosted RAG (Retrieval-Augmented Generation) data and chatbot platform. It enables you to create AI assistants that can answer questions based on your uploaded documents, powered by vector embeddings and large language models.

## Features

- **Assistants** -- Create and manage multiple AI assistants, each with their own configuration, personality, and knowledge base.
- **Documents** -- Upload documents (PDF, text, HTML, and more) to build a knowledge base for each assistant. Documents are automatically chunked, embedded, and indexed.
- **Embeddings** -- Leverages pgvector and RecallDb for vector storage and similarity search, enabling accurate context retrieval from your document corpus.
- **Chat** -- Public-facing chat endpoint that retrieves relevant context from your documents and generates responses using configurable LLM providers (OpenAI, Ollama).
- **Feedback** -- Collect thumbs-up/thumbs-down feedback and free-text comments on assistant responses to monitor quality and improve over time.
- **Multi-Tenant** -- User and credential management with admin and standard user roles. Each user owns their own assistants and documents.
- **Dashboard** -- Browser-based management UI for configuring assistants, uploading documents, viewing feedback, and testing chat.

## Quick Start (Docker Compose)

The fastest way to run AssistantHub and all its dependencies is with Docker Compose.

```bash
cd docker
docker compose up -d
```

This starts all services:

| Service                | Port  | Description                        |
|------------------------|-------|------------------------------------|
| pgvector               | 5432  | PostgreSQL with vector extension   |
| ollama                 | 11434 | LLM inference (Ollama)             |
| less3                  | 8000  | S3-compatible object storage       |
| less3-ui               | 8001  | Less3 management dashboard         |
| documentatom-server    | 8301  | Document processing service        |
| documentatom-dashboard | 8302  | Document processing dashboard      |
| partio-server          | 8321  | Partitioning/chunking service      |
| partio-dashboard       | 8322  | Partitioning dashboard             |
| recalldb-server        | 8401  | Vector database service            |
| recalldb-dashboard     | 8402  | Vector database dashboard          |
| assistanthub-server    | 8800  | AssistantHub REST API              |
| assistanthub-dashboard | 8801  | AssistantHub management dashboard  |

Once running, open [http://localhost:8801](http://localhost:8801) to access the dashboard.

## Manual Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- PostgreSQL with the `pgvector` extension (or SQLite/SQL Server/MySQL for the application database)

### Build and Run the Server

```bash
cd src/AssistantHub.Server
dotnet build -c Release
dotnet run -c Release
```

On first launch, the server creates a default `assistanthub.json` settings file. Update it with your database connection, S3 storage, and inference provider details, then restart.

### Build and Run the Dashboard

```bash
cd dashboard
npm install
npm run dev
```

The dashboard dev server starts on [http://localhost:5173](http://localhost:5173) by default and proxies API calls to the server on port 8800.

For a production build:

```bash
npm run build
```

The compiled output is placed in `dashboard/dist/` and can be served by any static file server (nginx, etc.).

## Configuration

The server reads configuration from `assistanthub.json` in the working directory. The file is created automatically on first run with default values.

```json
{
  "Webserver": {
    "Hostname": "localhost",
    "Port": 8800,
    "Ssl": false
  },
  "Database": {
    "Type": "Sqlite",
    "Filename": "assistanthub.db",
    "Hostname": null,
    "Port": 0,
    "DatabaseName": null,
    "Username": null,
    "Password": null
  },
  "S3": {
    "Region": "us-west-1",
    "BucketName": "assistanthub",
    "AccessKey": "default",
    "SecretKey": "default",
    "EndpointUrl": "http://localhost:8000",
    "UseSsl": false,
    "BaseUrl": "http://localhost:8000"
  },
  "DocumentAtom": {
    "Endpoint": "http://localhost:8301"
  },
  "Chunking": {
    "MaxChunkSize": 512,
    "OverlapSize": 50
  },
  "Inference": {
    "Provider": "Ollama",
    "Endpoint": "http://localhost:11434",
    "ApiKey": "",
    "DefaultModel": "gemma3:4b"
  },
  "RecallDb": {
    "Endpoint": "http://localhost:8401"
  },
  "Logging": {
    "ConsoleLogging": true,
    "MinimumSeverity": 1,
    "Servers": []
  }
}
```

### Key Settings

| Section        | Description                                                                 |
|----------------|-----------------------------------------------------------------------------|
| `Webserver`    | Hostname, port, and SSL toggle for the HTTP listener.                       |
| `Database`     | Database type (`Sqlite`, `Postgresql`, `SqlServer`, `Mysql`) and connection details. |
| `S3`           | S3-compatible object storage (Less3) for uploaded documents.                |
| `DocumentAtom` | Endpoint for the DocumentAtom document-processing service.                  |
| `Chunking`     | Maximum chunk size and overlap for document splitting.                      |
| `Inference`    | LLM provider (`Ollama` or `OpenAI`), endpoint, API key, and default model. |
| `RecallDb`     | Endpoint for the RecallDb vector database service.                          |
| `Logging`      | Console logging toggle, minimum severity, and optional syslog servers.      |

## Default Credentials

On first run (when no users exist), the server creates a default admin account and prints the credentials to the console:

| Field        | Value                        |
|--------------|------------------------------|
| Email        | `admin@assistanthub.local`   |
| Password     | `admin`                      |
| Bearer Token | *(auto-generated, see logs)* |

**Important:** Change the default password immediately after first login.

## API Overview

AssistantHub exposes a versioned REST API at `/v1.0/`. All authenticated endpoints require a bearer token in the `Authorization` header.

For complete endpoint documentation including request/response schemas and examples, see [REST_API.md](REST_API.md).

### Endpoint Summary

| Category              | Endpoints                                                 |
|-----------------------|-----------------------------------------------------------|
| Health                | `GET /`, `HEAD /`                                         |
| Authentication        | `POST /v1.0/authenticate`                                 |
| Users (admin)         | `PUT/GET /v1.0/users`, `GET/PUT/DELETE/HEAD /v1.0/users/{id}` |
| Credentials (admin)   | `PUT/GET /v1.0/credentials`, `GET/PUT/DELETE/HEAD /v1.0/credentials/{id}` |
| Buckets (admin)       | `PUT/GET /v1.0/buckets`, `GET/DELETE/HEAD /v1.0/buckets/{name}` |
| Bucket Objects (admin)| `GET/DELETE /v1.0/buckets/{name}/objects`, `GET .../metadata`, `GET .../download` |
| Collections (admin)   | `PUT/GET /v1.0/collections`, `GET/PUT/DELETE/HEAD /v1.0/collections/{id}` |
| Collection Records    | `GET /v1.0/collections/{id}/records`, `GET/DELETE .../records/{recordId}` |
| Assistants            | `PUT/GET /v1.0/assistants`, `GET/PUT/DELETE/HEAD /v1.0/assistants/{id}` |
| Assistant Settings    | `GET/PUT /v1.0/assistants/{id}/settings`                  |
| Documents             | `PUT/GET /v1.0/documents`, `GET/DELETE/HEAD /v1.0/documents/{id}` |
| Feedback              | `GET /v1.0/feedback`, `GET/DELETE /v1.0/feedback/{id}`    |
| Models                | `GET /v1.0/models`, `POST /v1.0/models/pull`              |
| Public Chat           | `POST /v1.0/assistants/{id}/chat`                         |
| Public Feedback       | `POST /v1.0/assistants/{id}/feedback`                     |
| Public Info           | `GET /v1.0/assistants/{id}/public`                        |

## Architecture

```
                           +------------------+
                           |    Dashboard     |
                           |   (React/Vite)   |
                           |    Port 8801     |
                           +--------+---------+
                                    |
                                    | HTTP/REST
                                    v
                           +------------------+
                           | AssistantHub     |
                           | Server (.NET 10) |
                           | Port 8800        |
                           +--+-----+------+--+
                              |     |      |
              +---------------+     |      +----------------+
              |                     |                       |
              v                     v                       v
   +------------------+  +------------------+    +------------------+
   |   DocumentAtom   |  |    RecallDb      |    |      Less3       |
   | (Doc Processing) |  | (Vector Search)  |    | (S3 Storage)     |
   |   Port 8301      |  |   Port 8401      |    |   Port 8000      |
   +--------+---------+  +--------+---------+    +------------------+
            |                      |
            v                      v
   +------------------+  +------------------+
   |     Partio       |  |    pgvector      |
   |   (Chunking)     |  |  (PostgreSQL)    |
   |   Port 8321      |  |   Port 5432      |
   +--------+---------+  +------------------+
            |
            v
   +------------------+
   |     Ollama       |
   |   (Inference)    |
   |   Port 11434     |
   +------------------+
```

**Data flow for document ingestion:**
1. User uploads a document via the API or dashboard.
2. The document is stored in S3-compatible object storage.
3. DocumentAtom extracts text content from the document.
4. Partio splits the text into overlapping chunks.
5. Chunks are embedded and stored in RecallDb (backed by pgvector).

**Data flow for chat:**
1. User sends a message to the chat endpoint.
2. The server queries RecallDb for relevant document chunks using vector similarity search.
3. Retrieved chunks are assembled into context.
4. The context and user message are sent to the configured LLM (OpenAI or Ollama).
5. The LLM response and source references are returned to the user.

## Tech Stack

- **Backend:** .NET 10 (C#), WatsonWebserver
- **Frontend:** React, Vite, JavaScript
- **Database:** PostgreSQL with pgvector (also supports SQLite, SQL Server, MySQL)
- **Vector Search:** RecallDb
- **Document Processing:** DocumentAtom, Partio
- **Object Storage:** Less3 (S3-compatible)
- **Inference Providers:** Ollama, OpenAI
- **Containerization:** Docker, Docker Compose
- **Web Server (Dashboard):** nginx

## License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.

Copyright (c) 2025 Joel Christner.
