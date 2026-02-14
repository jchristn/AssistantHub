# AssistantHub Links

## Dashboards

| Service       | URL                          |
|---------------|------------------------------|
| AssistantHub  | http://localhost:8801        |
| Less3         | http://localhost:8001        |
| DocumentAtom  | http://localhost:8302        |
| Partio        | http://localhost:8322        |
| RecallDB      | http://localhost:8402        |

## AssistantHub Dashboard Configuration

The AssistantHub dashboard server URL should be set to `http://localhost:8801` (which proxies API requests to the backend via nginx).

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

### PostgreSQL

| Field         | Value                |
|---------------|----------------------|
| Username      | postgres             |
| Password      | password             |
| Database      | postgres             |

## Backend Services

| Service       | URL                          |
|---------------|------------------------------|
| AssistantHub  | http://localhost:8800        |
| Less3         | http://localhost:8000        |
| DocumentAtom  | http://localhost:8301        |
| Partio        | http://localhost:8321        |
| RecallDB      | http://localhost:8401        |
| Ollama        | http://localhost:11434       |
| PostgreSQL    | localhost:5432               |
