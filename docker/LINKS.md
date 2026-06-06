# AssistantHub Links

## Dashboards

| Service       | URL                          |
|---------------|------------------------------|
| AssistantHub  | http://localhost:8801        |
| Less3         | http://localhost:8001        |
| DocumentAtom  | http://localhost:8302        |
| Partio        | http://localhost:8322        |
| RecallDB      | http://localhost:8402        |
| Verbex        | http://localhost:8502        |

## AssistantHub Dashboard Configuration

The AssistantHub dashboard server URL should be set to `http://localhost:8801` (which proxies API requests to the backend via nginx).

## Startup Note

On a fresh `docker compose up -d` or after `factory/reset.bat`, `postgres-init` must complete before database-backed services start, and `assistanthub-server` waits for `partio-server` to report healthy before starting. This is expected and prevents AssistantHub from failing early during database and chunking/embeddings connectivity validation.

Use `status.bat` or `status.sh` from this directory to list container ID, name, creation time, status, and published ports for the local Docker deployment.

## Default Credentials

### AssistantHub

| Field         | Value                |
|---------------|----------------------|
| Email         | admin@assistanthub   |
| Password      | password             |

### Less3 (S3-Compatible Storage)

| Field         | Value                |
|---------------|----------------------|
| Admin API Key | less3admin           |
| Access Key    | default              |
| Secret Key    | default              |

### DocumentAtom

No authentication configured by default.

### Partio (Chunking)

| Field          | Value                |
|----------------|----------------------|
| Admin API Key  | partioadmin          |
| Tenant ID      | default              |
| Email          | admin@partio         |
| Password       | password             |
| Bearer Token   | default              |

### RecallDB (Vector Database)

| Field          | Value                |
|----------------|----------------------|
| Admin API Key  | recalldbadmin        |
| Tenant ID      | default              |
| Email          | admin@recall         |
| Password       | password             |
| Bearer Token   | default              |

### Verbex (Inverted Index)

| Field          | Value                |
|----------------|----------------------|
| Admin API Key  | verbexadmin          |

### PostgreSQL

| Service | Database | Username | Password |
|---------|----------|----------|----------|
| Superuser | postgres | postgres | password |
| AssistantHub | assistanthub | assistanthub_app | assistanthub_password |
| Less3 | less3 | less3_app | less3_password |
| Partio | partio | partio_app | partio_password |
| RecallDB | recalldb | recalldb_app | recalldb_password |
| Verbex | verbex | verbex_app | verbex_password |

## Backend Services

| Service       | URL                          |
|---------------|------------------------------|
| AssistantHub  | http://localhost:8800        |
| Less3         | http://localhost:8000        |
| DocumentAtom  | http://localhost:8301        |
| Partio        | http://localhost:8321        |
| RecallDB      | http://localhost:8401        |
| Verbex        | http://localhost:8501        |
| Ollama        | http://localhost:11434       |
| PostgreSQL    | localhost:5432               |
