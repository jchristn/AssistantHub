# Docker PostgreSQL

The Docker deployment uses one `pgvector/pgvector:pg17` PostgreSQL container for all database-backed services.

`postgres-init` runs on startup after PostgreSQL is healthy. It creates and verifies one database and one application role for each service:

| Service | Database | Role |
| --- | --- | --- |
| AssistantHub | `assistanthub` | `assistanthub_app` |
| Partio | `partio` | `partio_app` |
| Less3 | `less3` | `less3_app` |
| RecallDB | `recalldb` | `recalldb_app` |

RecallDB is the only service that requires the `vector` extension. The init container installs it as the PostgreSQL superuser before RecallDB starts.

The local defaults live in `docker/.env` and are mirrored in the mounted JSON files under `docker/`. If you change database names or credentials, update both places unless a config templating step is added later.

For a completely fresh database state:

```bash
cd docker
docker compose down -v
docker compose up -d
```

To verify the database topology after startup:

```bash
docker exec assistanthub-postgres psql -U postgres -d postgres -c "\l"
docker exec assistanthub-postgres psql -U postgres -d postgres -c "\du"
docker exec -e PGPASSWORD=assistanthub_password assistanthub-postgres psql -U assistanthub_app -d assistanthub -c "SELECT current_database(), current_user;"
docker exec -e PGPASSWORD=partio_password assistanthub-postgres psql -U partio_app -d partio -c "SELECT current_database(), current_user;"
docker exec -e PGPASSWORD=less3_password assistanthub-postgres psql -U less3_app -d less3 -c "SELECT current_database(), current_user;"
docker exec -e PGPASSWORD=recalldb_password assistanthub-postgres psql -U recalldb_app -d recalldb -c "SELECT current_database(), current_user;"
docker exec assistanthub-postgres psql -U postgres -d recalldb -c "SELECT extname FROM pg_extension WHERE extname = 'vector';"
docker logs assistanthub-postgres-init
docker compose ps
```
