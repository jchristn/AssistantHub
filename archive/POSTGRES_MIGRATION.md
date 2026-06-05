# Postgres Migration Plan

This plan moves the fresh `docker/` deployment to default every service backend database to PostgreSQL instead of SQLite. It assumes no data migration from existing SQLite databases is required. The target is a clean development/test deployment with deterministic startup, separate databases per service, and a single database initialization verifier that runs before database-backed services start.

## Scope

- [x] Replace SQLite defaults in `docker/compose.yaml` with PostgreSQL-backed service configuration.
- [x] Use separate PostgreSQL databases and service roles for each database-backed service.
- [x] Keep one PostgreSQL container for the deployment, using a pgvector-capable image because RecallDB requires the `vector` extension.
- [x] Add an initialization container that creates/verifies roles, databases, grants, required extensions, and login connectivity.
- [x] Update service config files under `docker/` and `docker/factory/`.
- [x] Remove SQLite initialization, SQLite database files, and SQLite bind mounts from the fresh deployment path.
- [x] Update backend/server code only where a service cannot already use PostgreSQL correctly.
- [x] Update documentation, changelog, API/deployment references, reset/update scripts, and test coverage.
- [x] Validate fresh startup from an empty PostgreSQL data volume.

## Implementation Progress

- [x] Audited AssistantHub, Less3, Partio, and RecallDB source trees for PostgreSQL backend support.
- [x] Converted `docker/compose.yaml` to a `postgres` service using `pgvector/pgvector:pg17`.
- [x] Added `docker/postgres/init.sh` and wired `postgres-init` into every database-backed service dependency path.
- [x] Added `docker/.env` with local-only PostgreSQL defaults and mirrored those credentials in mounted JSON config files.
- [x] Converted AssistantHub, Less3, Partio, and RecallDB Docker configs to PostgreSQL.
- [x] Converted factory JSON configs and reset scripts to Postgres-first behavior.
- [x] Removed factory SQLite database assets from the fresh deployment path.
- [x] Updated README, CHANGELOG, Docker links, and Docker PostgreSQL docs.
- [x] Validate `docker compose config`.
- [x] Validate a clean Docker startup against an empty `postgres-data` volume.
- [x] Add or adjust automated tests only if compose/runtime validation exposes a backend code gap.
- [x] Capture final verification notes.

## Verification Notes

- [x] `docker compose -f docker\compose.yaml config` succeeded.
- [x] Edited Docker JSON configs parsed successfully with PowerShell `ConvertFrom-Json`.
- [x] Local stack was stopped, `docker_postgres-data` was removed, and `docker compose -f docker\compose.yaml up -d` started successfully while preserving the Ollama model volume.
- [x] `postgres-init` created the four service roles/databases and installed `vector` in `recalldb`.
- [x] `docker compose ps` showed PostgreSQL, AssistantHub, Less3, Partio, RecallDB, DocumentAtom, and dashboards running; database-backed services were healthy.
- [x] PostgreSQL verification found nonzero public table counts: AssistantHub `13`, Partio `6`, Less3 `11`, RecallDB `8`.
- [x] `recalldb_app` could read the `vector` extension, and app-role cross-database connection attempts were denied.
- [x] AssistantHub API smoke checks passed for authentication, `whoami`, assistant enumeration, Partio endpoint enumeration, RecallDB collection enumeration, and Less3 bucket enumeration.
- [x] A PostgreSQL assistant-settings boolean bug was found during live assistant creation, fixed in `Postgresql/Implementations/AssistantSettingsMethods.cs`, rebuilt into the local `jchristn77/assistanthub-server:v0.13.0` image, and verified with assistant CRUD.
- [x] A temporary assistant chat request persisted chat history and appeared in assistant analytics overview.
- [x] `dotnet build src\AssistantHub.sln /warnaserror:CS1591` passed.
- [x] `dotnet run --project src\Test.Automated\Test.Automated.csproj --no-build` passed.
- [x] `dotnet test src\Test.Xunit\Test.Xunit.csproj --no-build` passed.
- [x] `dotnet test src\Test.Nunit\Test.Nunit.csproj --no-build` passed.

## Non-Goals

- [x] Do not migrate existing SQLite data into PostgreSQL.
- [x] Do not preserve existing local `docker/*/*.db` state.
- [x] Do not remove SQLite support from product binaries unless explicitly decided later.
- [x] Do not change object/blob storage semantics for Less3 disk storage; only move Less3 metadata/config database storage to PostgreSQL.

## Original Database Inventory

| Service | Original DB | Original config | Original storage | Target DB |
| --- | --- | --- | --- | --- |
| Less3 | SQLite | `docker/less3/system.json` | `./less3/less3.db:/app/less3.db` | PostgreSQL database `less3` |
| Partio | SQLite | `docker/partio/partio.json` | `./partio/data/:/app/data/` with `/app/data/partio.db` | PostgreSQL database `partio` |
| RecallDB | PostgreSQL/pgvector | `docker/recalldb/recalldb.json` and compose env | `pgvector-data` named volume | PostgreSQL database `recalldb` with `vector` extension |
| AssistantHub | SQLite | `docker/assistanthub/assistanthub.json` | `./assistanthub/data/:/app/data/` with `assistanthub.db` | PostgreSQL database `assistanthub` |
| DocumentAtom | none observed | `docker/documentatom/documentatom.json` | logs only | no database change |
| AssistantHub MCP | none observed | `docker/assistanthub-mcp/assistanthub-mcp.json` | backups/temp/logs | no database change |
| Dashboards/UIs | none | environment only | none | no database change |
| Ollama | model storage only | compose env | `ollama-models` named volume | no database change |

## Local Source References

Use these local source trees as references when validating backend database support, schema initialization, image readiness, and service-specific tests:

- [x] Less3 source: `C:\Code\Less3\Less3-2.1`
- [x] Partio source: `C:\Code\Partio\Partio`
- [x] RecallDB source: `C:\Code\RecallDB`
- [x] AssistantHub source: `C:\Code\AssistantHub`

When implementation requires subordinate service fixes, make those changes in the owning source tree first, validate that service independently against PostgreSQL, then update the AssistantHub `docker/` deployment to consume the corrected image/tag.

## Target PostgreSQL Topology

- [x] Rename the current `pgvector` service to `postgres` or keep `pgvector` with a clear alias. Prefer `postgres` for clarity and add a network alias only if needed.
- [x] Pin the PostgreSQL image to a known pgvector-capable tag instead of `latest`.
- [x] Store PostgreSQL data in a named Docker volume, for example `postgres-data`.
- [x] Expose port `5432:5432` for local dev tooling.
- [x] Use one PostgreSQL superuser only for initialization.
- [x] Use one application role per service:
  - [x] `assistanthub_app`
  - [x] `partio_app`
  - [x] `less3_app`
  - [x] `recalldb_app`
- [x] Use one database per service:
  - [x] `assistanthub`, owner `assistanthub_app`
  - [x] `partio`, owner `partio_app`
  - [x] `less3`, owner `less3_app`
  - [x] `recalldb`, owner `recalldb_app`
- [x] Enable `CREATE EXTENSION IF NOT EXISTS vector;` only in `recalldb` unless another service explicitly needs pgvector.
- [x] Grant each service role access only to its own database.
- [x] Keep schemas at `public` unless service code requires a configurable schema.

## Credential Model

- [x] Add a Docker-only credential source, preferably `docker/.env`, with fresh-deployment defaults:
  - [x] `POSTGRES_SUPERUSER=postgres`
  - [x] `POSTGRES_SUPERPASS=password`
  - [x] `ASSISTANTHUB_DB_NAME=assistanthub`
  - [x] `ASSISTANTHUB_DB_USER=assistanthub_app`
  - [x] `ASSISTANTHUB_DB_PASS=assistanthub_password`
  - [x] `PARTIO_DB_NAME=partio`
  - [x] `PARTIO_DB_USER=partio_app`
  - [x] `PARTIO_DB_PASS=partio_password`
  - [x] `LESS3_DB_NAME=less3`
  - [x] `LESS3_DB_USER=less3_app`
  - [x] `LESS3_DB_PASS=less3_password`
  - [x] `RECALLDB_DB_NAME=recalldb`
  - [x] `RECALLDB_DB_USER=recalldb_app`
  - [x] `RECALLDB_DB_PASS=recalldb_password`
- [x] Decide whether service JSON files will contain deterministic dev credentials directly or be generated from templates at container startup.
- [x] Template rendering was not added; static local-dev JSON credentials were selected and documented.
- [x] If using static dev JSON credentials, document that these are local-only defaults and must not be reused outside dev/test.

## Compose Changes

### PostgreSQL Service

- [x] Replace `pgvector` service with a pinned pgvector-capable PostgreSQL service.
- [x] Add a healthcheck using `pg_isready -U "$POSTGRES_USER" -d postgres`.
- [x] Keep the named volume isolated from Windows bind mounts.
- [x] Example target shape:

```yaml
postgres:
  image: <pinned-pgvector-postgres-image>
  container_name: assistanthub-postgres
  environment:
    POSTGRES_USER: ${POSTGRES_SUPERUSER:-postgres}
    POSTGRES_PASSWORD: ${POSTGRES_SUPERPASS:-password}
    POSTGRES_DB: postgres
  ports:
    - "5432:5432"
  volumes:
    - postgres-data:/var/lib/postgresql/data
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U $${POSTGRES_USER} -d postgres"]
    interval: 5s
    timeout: 2s
    retries: 10
  restart: unless-stopped
```

### Database Initialization Container

- [x] Replace `recalldb-init` and `assistanthub-db-init` with a single `postgres-init` container.
- [x] Base the init container on the same PostgreSQL/pgvector image so `psql` is available.
- [x] Depend on `postgres` health.
- [x] Create missing roles and databases idempotently.
- [x] Verify each role can connect to its database.
- [x] Verify each service database has expected ownership.
- [x] Verify `vector` exists in `recalldb`.
- [x] Exit nonzero if any database, credential, grant, or extension check fails.
- [x] Print a concise startup report showing created/existing status for each database.
- [x] Do not initialize SQLite files anywhere in this path.

Recommended init behavior:

```bash
set -euo pipefail

create_role_if_missing "$ASSISTANTHUB_DB_USER" "$ASSISTANTHUB_DB_PASS"
create_role_if_missing "$PARTIO_DB_USER" "$PARTIO_DB_PASS"
create_role_if_missing "$LESS3_DB_USER" "$LESS3_DB_PASS"
create_role_if_missing "$RECALLDB_DB_USER" "$RECALLDB_DB_PASS"

create_database_if_missing "$ASSISTANTHUB_DB_NAME" "$ASSISTANTHUB_DB_USER"
create_database_if_missing "$PARTIO_DB_NAME" "$PARTIO_DB_USER"
create_database_if_missing "$LESS3_DB_NAME" "$LESS3_DB_USER"
create_database_if_missing "$RECALLDB_DB_NAME" "$RECALLDB_DB_USER"

psql_as_superuser "$RECALLDB_DB_NAME" "CREATE EXTENSION IF NOT EXISTS vector;"

verify_login "$ASSISTANTHUB_DB_NAME" "$ASSISTANTHUB_DB_USER" "$ASSISTANTHUB_DB_PASS"
verify_login "$PARTIO_DB_NAME" "$PARTIO_DB_USER" "$PARTIO_DB_PASS"
verify_login "$LESS3_DB_NAME" "$LESS3_DB_USER" "$LESS3_DB_PASS"
verify_login "$RECALLDB_DB_NAME" "$RECALLDB_DB_USER" "$RECALLDB_DB_PASS"
```

### Service Dependencies

- [x] Make every database-backed service depend on `postgres-init` with `condition: service_completed_successfully`.
- [x] Remove direct service dependencies on the old `pgvector` service where `postgres-init` is sufficient.
- [x] Keep service-specific runtime dependencies intact:
  - [x] Partio still depends on `ollama-init`.
  - [x] AssistantHub still depends on Less3, Partio, RecallDB, DocumentAtom, and PostgreSQL initialization.
  - [x] RecallDB still depends on PostgreSQL initialization.

### SQLite Mount Removal

- [x] Remove `./less3/less3.db:/app/less3.db`.
- [x] Remove `./partio/data/:/app/data/` if its only required purpose is SQLite database storage.
- [x] If Partio still needs `/app/data/` for non-database files, replace the broad bind mount with explicit non-DB directories.
- [x] Remove `./assistanthub/data/:/app/data/`.
- [x] Keep non-database mounts:
  - [x] Less3 `./less3/logs`, `./less3/temp`, `./less3/disk`
  - [x] Partio `./partio/logs`, `./partio/request-history`
  - [x] AssistantHub `./assistanthub/logs`, `./assistanthub/processing-logs`, `./assistanthub/crawl-enumerations`
- [x] Remove `assistanthub-db-init`.
- [x] Remove `recalldb-init` after `postgres-init` replaces it.
- [x] Rename volume `pgvector-data` to `postgres-data` or document why it remains named `pgvector-data`.

## Configuration File Changes

### `docker/assistanthub/assistanthub.json`

- [x] Change `Database.Type` from `Sqlite` to `Postgresql`.
- [x] Clear or remove `Database.Filename`.
- [x] Set:
  - [x] `Hostname`: `postgres`
  - [x] `Port`: `5432`
  - [x] `DatabaseName`: `assistanthub`
  - [x] `Username`: `assistanthub_app`
  - [x] `Password`: matching Docker dev password
  - [x] `Schema`: `public`
  - [x] `RequireEncryption`: `false`
  - [x] `LogQueries`: `false`
- [x] Confirm the first-run tenant/admin setup works on an empty PostgreSQL database.
- [x] Confirm startup migrations run on PostgreSQL and do not depend on SQLite-only pragmas or table inspection.

### `docker/partio/partio.json`

- [x] Change `Database.Type` from `Sqlite` to `Postgresql`.
- [x] Clear or remove `Database.Filename`.
- [x] Set:
  - [x] `Hostname`: `postgres`
  - [x] `Port`: `5432`
  - [x] `DatabaseName`: `partio`
  - [x] `Username`: `partio_app`
  - [x] `Password`: matching Docker dev password
  - [x] `Schema`: `public`
  - [x] `RequireEncryption`: `false`
  - [x] `LogQueries`: `false`
- [x] Confirm Partio default tenant/user/credential setup works on an empty PostgreSQL database.
- [x] Confirm Partio default embedding and inference endpoints are inserted on empty PostgreSQL.
- [x] Confirm request-history JSON storage is still filesystem-backed and unaffected by database changes.

### `docker/less3/system.json`

- [x] Change `Database.Type` from `Sqlite` to `Postgresql`.
- [x] Clear or remove `Database.Filename`.
- [x] Set:
  - [x] `Hostname`: `postgres`
  - [x] `Port`: `5432`
  - [x] `DatabaseName`: `less3`
  - [x] `Username`: `less3_app`
  - [x] `Password`: matching Docker dev password
- [x] Preserve Less3 disk object storage:
  - [x] `Storage.StorageType`: `Disk`
  - [x] `Storage.DiskDirectory`: `./disk/`
- [x] Confirm Less3 bucket/config/object metadata tables initialize on PostgreSQL.
- [ ] Confirm S3-compatible operations still write object payloads to `./less3/disk` and metadata to PostgreSQL.

### `docker/recalldb/recalldb.json`

- [x] Update `Database.Hostname` from `pgvector` to `postgres` if the service is renamed.
- [x] Update `Database.DatabaseName` to `recalldb`.
- [x] Update `Database.Username` to `recalldb_app`.
- [x] Update `Database.Password` to matching Docker dev password.
- [x] Preserve `Schema`, `RequireEncryption`, and logging defaults unless a backend requirement says otherwise.
- [x] Confirm RecallDB does not require superuser privileges after the `vector` extension is installed by `postgres-init`.

### `docker/factory/`

- [x] Update `docker/factory/assistanthub.json` to PostgreSQL defaults.
- [x] Update `docker/factory/partio.json` to PostgreSQL defaults.
- [x] Add or update a factory Less3 config if Less3 factory state is represented outside `docker/less3/system.json`.
- [x] Remove factory SQLite databases:
  - [x] `docker/factory/assistanthub.db`
  - [x] `docker/factory/partio.db`
  - [x] `docker/factory/less3.db`
- [x] Remove factory SQLite sidecar handling:
  - [x] `*.db-shm`
  - [x] `*.db-wal`
- [x] Update `docker/factory/reset.bat`.
- [x] Update `docker/factory/reset.sh`.
- [x] Ensure factory reset recreates a fresh PostgreSQL volume or instructs the user to run `docker compose down -v` before reset.

## Backend Service Readiness

### AssistantHub

- [x] Verify `DatabaseTypeEnum.Postgresql` is supported in current server binaries.
- [x] Verify PostgreSQL table creation covers all tables, columns, indexes, and startup migration paths.
- [x] Verify recent analytics/telemetry schema changes exist in PostgreSQL table queries and startup code.
- [ ] Add a PostgreSQL-backed integration test for fresh first-run setup.
- [ ] Add a PostgreSQL-backed integration test for request history and assistant analytics telemetry.
- [ ] Add a PostgreSQL-backed integration test for ingestion metadata updates if supported by the test harness.

### Partio

- [x] Verify `DatabaseTypeEnum.Postgresql` is supported in current Partio server binaries.
- [x] Verify PostgreSQL table creation covers tenants, users, credentials, embedding endpoints, completion endpoints, request history metadata, and endpoint concurrency fields.
- [x] Verify default endpoint seeding works on empty PostgreSQL.
- [ ] Verify endpoint max concurrency fields persist and are honored.
- [ ] Add or update Partio PostgreSQL integration tests in the Partio repo.
- [x] No Partio image fix was required before changing AssistantHub compose defaults.

### Less3

- [x] Verify Less3 `DatabaseTypeEnum.Postgresql` is supported in the image currently used by compose.
- [x] Verify Less3 PostgreSQL table creation covers buckets, objects, object versions, tags, ACLs, users/groups/config, and seed data.
- [x] Verify Less3 can run with PostgreSQL metadata and disk object storage simultaneously.
- [ ] Add Less3 PostgreSQL integration tests in the Less3 repo if coverage does not already exist.
- [x] No Less3 image fix was required before changing AssistantHub compose defaults.

### RecallDB

- [x] Keep RecallDB on PostgreSQL.
- [x] Stop using the superuser application credential.
- [x] Verify RecallDB works with `recalldb_app` after `vector` is installed by the init container.
- [x] Add a startup check that vector extension is present and accessible.
- [ ] Add or update RecallDB integration tests for non-superuser database ownership.

## Scripts and Operational Tooling

- [x] Update `docker/update.bat` and `docker/update.sh` if they need to account for `postgres-init`.
- [x] Add `docker/postgres/init.sh` or an equivalent embedded init script.
- [x] Prefer a real script file over a long inline compose shell block once behavior exceeds simple checks.
- [x] Add `docker/postgres/README.md` or document initialization in the main Docker docs.
- [x] Add helper commands for fresh reset:
  - [x] `docker compose down -v`
  - [x] remove non-database runtime directories only when explicitly intended
  - [x] `docker compose pull`
  - [x] `docker compose up -d`
- [x] Add a verification script or documented command that checks:
  - [x] all expected databases exist
  - [x] all expected roles exist
  - [x] each role can connect only to its intended database
  - [x] `vector` exists in `recalldb`
  - [x] all DB-backed services are healthy
- [x] Remove SQLite creation logic from Docker startup scripts.
- [x] Remove instructions that tell users to inspect or copy SQLite files.

## Documentation Updates

- [x] Update root `README.md` Docker deployment section.
- [x] Update Docker setup instructions to explain PostgreSQL is the default backend.
- [x] Document database names, role names, and local-only default credentials.
- [x] Document the fresh deployment reset path.
- [x] Document that SQLite is no longer the Docker default.
- [x] Update troubleshooting:
  - [x] PostgreSQL container unhealthy
  - [x] `postgres-init` failed
  - [x] credential mismatch between `.env` and JSON config
  - [x] `vector` extension missing
  - [x] stale SQLite files in local `docker/` folders
- [x] Update `CHANGELOG.md`.
- [x] Update `REST_API.md` only if setup/deployment examples mention SQLite or backend database assumptions.
- [x] Update Postman docs/environments only if they mention local reset, database setup, or local deployment state.
- [x] Update SDK docs only if local integration-test setup or sample Docker deployment references SQLite.
- [x] Update any docs under `docs/`, `archive/`, `migrations/`, or `docker/` that mention `assistanthub.db`, `partio.db`, `less3.db`, `pgvector`, or SQLite as Docker default.

## Test Plan

### Static Checks

- [x] `rg -n "Sqlite|SQLite|assistanthub.db|partio.db|less3.db|pgvector" docker README.md REST_API.md CHANGELOG.md docs postman sdk src`
- [x] Confirm remaining SQLite mentions are either product capability references or intentional migration notes.
- [x] Confirm compose has no SQLite DB file mounts.
- [x] Confirm compose has no SQLite init containers.
- [x] Confirm service configs use `Postgresql`.

### Fresh Docker Startup

- [x] Stop existing stack.
- [x] Remove old containers and the PostgreSQL database volume while preserving the Ollama model volume.
- [x] Remove stale tracked local SQLite runtime files from `docker/`; leave unrelated recovery artifacts untouched.
- [x] Start fresh with `docker compose up -d`.
- [x] Confirm `postgres` is healthy.
- [x] Confirm `postgres-init` exits `0`.
- [x] Confirm Less3 is healthy.
- [x] Confirm Partio is healthy.
- [x] Confirm RecallDB is healthy.
- [x] Confirm AssistantHub is healthy.
- [x] Confirm dashboards start.

### Database Verification

- [x] Connect as superuser and list databases.
- [x] Connect as `assistanthub_app` to `assistanthub`; verify tables exist.
- [x] Connect as `partio_app` to `partio`; verify tables exist.
- [x] Connect as `less3_app` to `less3`; verify tables exist.
- [x] Connect as `recalldb_app` to `recalldb`; verify tables exist and `vector` extension exists.
- [x] Verify each app user cannot connect to the other service databases unless intentionally permitted.

### Service Verification

- [x] Less3: create/list/delete bucket or use existing health/API checks.
- [x] DocumentAtom: confirm health unaffected.
- [x] Partio: enumerate embedding endpoints and inference endpoints.
- [ ] Partio: run a small chunk/embed path that writes metadata.
- [x] RecallDB: create tenant/collection or run existing health/integration path.
- [x] AssistantHub: first-run tenant/admin exists.
- [x] AssistantHub: create/read/update assistant.
- [x] AssistantHub: run a chat request and verify chat history persists.
- [x] AssistantHub: verify assistant analytics can read persisted history.
- [ ] AssistantHub MCP: verify it can call AssistantHub after startup.

### Automated Tests

- [ ] Extend AssistantHub test infrastructure so PostgreSQL can be started by the test runner when needed.
- [ ] Add a PostgreSQL backend variant for relevant database/service suites.
- [x] Add Docker composition validation tests if the repo has an appropriate harness.
- [x] Run `dotnet build src/AssistantHub.sln /warnaserror:CS1591`.
- [x] Run `dotnet run --project src/Test.Automated/Test.Automated.csproj --no-build`.
- [x] Run `dotnet test src/Test.Xunit/Test.Xunit.csproj --no-build`.
- [x] Run `dotnet test src/Test.Nunit/Test.Nunit.csproj --no-build`.
- [ ] Run frontend dashboard build if docs or dashboard deployment files are touched.
- [ ] Run service-specific test suites in Partio and Less3 after their configs/images are updated.

## Implementation Sequence

1. [x] Inventory exact PostgreSQL support in AssistantHub, Partio, Less3, and RecallDB images currently referenced by compose.
2. [x] Fix service backend gaps in their owning repos first when required; AssistantHub required a PostgreSQL boolean-literal fix.
3. [x] Publish or locally build image tags that contain required PostgreSQL fixes; local `jchristn77/assistanthub-server:v0.13.0` was rebuilt for validation.
4. [x] Add `docker/.env` with local PostgreSQL defaults or add documented template generation.
5. [x] Replace `pgvector`/`recalldb-init`/`assistanthub-db-init` with `postgres`/`postgres-init`.
6. [x] Update service dependencies to wait on `postgres-init`.
7. [x] Convert `docker/less3/system.json`.
8. [x] Convert `docker/partio/partio.json`.
9. [x] Convert `docker/recalldb/recalldb.json`.
10. [x] Convert `docker/assistanthub/assistanthub.json`.
11. [x] Remove SQLite file mounts and stale database directories from compose.
12. [x] Update `docker/factory/` configs and reset scripts.
13. [x] Update docs and changelog.
14. [x] Add/extend tests.
15. [x] Run fresh deployment validation from an empty Docker volume state.
16. [x] Run automated tests.
17. [ ] Capture final verification notes in the PR/commit message.

## Acceptance Criteria

- [x] A clean `docker compose up -d` starts with no SQLite database files mounted for Less3, Partio, RecallDB, or AssistantHub, and no tracked SQLite runtime database files remain in the Docker default path.
- [x] One PostgreSQL/pgvector container backs all DB-backed services.
- [x] Each DB-backed service uses a separate database and service-specific PostgreSQL role.
- [x] The init container exits successfully only after databases, roles, grants, and required extensions are verified.
- [x] Less3 metadata, Partio metadata, RecallDB vectors, and AssistantHub metadata/history all persist in PostgreSQL.
- [x] No application service uses the PostgreSQL superuser at runtime.
- [ ] Fresh factory reset behavior is documented and works.
- [x] Documentation no longer presents SQLite as the default backend for Docker deployment.
- [x] Tests cover the PostgreSQL-backed fresh deployment path at the composition/configuration level; live startup/API validation covers the runtime path.

## Risks and Mitigations

- [x] Risk: a service image advertises PostgreSQL support but has an untested schema path.
  Mitigation: validate every database-backed service against an empty PostgreSQL volume in the integrated Docker stack.
- [x] Risk: JSON configs and `.env` credentials drift.
  Mitigation: either generate JSON from templates or document static dev credentials as the source of truth.
- [x] Risk: `vector` extension requires superuser privileges.
  Mitigation: install the extension in `postgres-init` as superuser; run RecallDB as `recalldb_app`.
- [x] Risk: existing local SQLite files confuse validation.
  Mitigation: fresh deployment validation must start from `docker compose down -v` and remove stale `*.db`, `*.db-wal`, and `*.db-shm` files from `docker/`.
- [x] Risk: reset scripts accidentally delete non-database object storage.
  Mitigation: separate database reset from Less3 disk object storage reset and require explicit user intent for destructive object cleanup.
