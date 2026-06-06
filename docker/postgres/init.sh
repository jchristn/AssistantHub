#!/usr/bin/env bash
set -euo pipefail

psql_super() {
  local database="$1"
  local sql="$2"
  PGPASSWORD="${POSTGRES_SUPERPASS}" psql -v ON_ERROR_STOP=1 -h postgres -U "${POSTGRES_SUPERUSER}" -d "${database}" -c "${sql}"
}

role_exists() {
  local role="$1"
  PGPASSWORD="${POSTGRES_SUPERPASS}" psql -v ON_ERROR_STOP=1 -h postgres -U "${POSTGRES_SUPERUSER}" -d postgres -tAc "SELECT 1 FROM pg_roles WHERE rolname='${role}'" | grep -q 1
}

database_exists() {
  local database="$1"
  PGPASSWORD="${POSTGRES_SUPERPASS}" psql -v ON_ERROR_STOP=1 -h postgres -U "${POSTGRES_SUPERUSER}" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='${database}'" | grep -q 1
}

create_role() {
  local role="$1"
  local password="$2"

  if role_exists "${role}"; then
    echo "Role '${role}' already exists; updating password."
    psql_super postgres "ALTER ROLE \"${role}\" WITH LOGIN PASSWORD '${password}';"
  else
    echo "Creating role '${role}'."
    psql_super postgres "CREATE ROLE \"${role}\" WITH LOGIN PASSWORD '${password}';"
  fi
}

create_database() {
  local database="$1"
  local owner="$2"

  if database_exists "${database}"; then
    echo "Database '${database}' already exists; verifying ownership and grants."
    psql_super postgres "ALTER DATABASE \"${database}\" OWNER TO \"${owner}\";"
  else
    echo "Creating database '${database}' owned by '${owner}'."
    psql_super postgres "CREATE DATABASE \"${database}\" OWNER \"${owner}\";"
  fi

  psql_super postgres "REVOKE CONNECT ON DATABASE \"${database}\" FROM PUBLIC; GRANT CONNECT, TEMPORARY ON DATABASE \"${database}\" TO \"${owner}\";"
  psql_super "${database}" "ALTER SCHEMA public OWNER TO \"${owner}\"; GRANT ALL ON SCHEMA public TO \"${owner}\";"
}

verify_login() {
  local database="$1"
  local role="$2"
  local password="$3"

  echo "Verifying login for '${role}' on '${database}'."
  PGPASSWORD="${password}" psql -v ON_ERROR_STOP=1 -h postgres -U "${role}" -d "${database}" -tAc "SELECT current_database(), current_user;" >/dev/null
}

verify_denied() {
  local database="$1"
  local role="$2"
  local password="$3"

  if PGPASSWORD="${password}" psql -h postgres -U "${role}" -d "${database}" -tAc "SELECT 1;" >/dev/null 2>&1; then
    echo "Role '${role}' unexpectedly connected to '${database}'."
    exit 1
  fi
}

echo "Initializing AssistantHub PostgreSQL databases."

create_role "${ASSISTANTHUB_DB_USER}" "${ASSISTANTHUB_DB_PASS}"
create_role "${PARTIO_DB_USER}" "${PARTIO_DB_PASS}"
create_role "${LESS3_DB_USER}" "${LESS3_DB_PASS}"
create_role "${RECALLDB_DB_USER}" "${RECALLDB_DB_PASS}"
create_role "${VERBEX_DB_USER}" "${VERBEX_DB_PASS}"

create_database "${ASSISTANTHUB_DB_NAME}" "${ASSISTANTHUB_DB_USER}"
create_database "${PARTIO_DB_NAME}" "${PARTIO_DB_USER}"
create_database "${LESS3_DB_NAME}" "${LESS3_DB_USER}"
create_database "${RECALLDB_DB_NAME}" "${RECALLDB_DB_USER}"
create_database "${VERBEX_DB_NAME}" "${VERBEX_DB_USER}"

echo "Ensuring pgvector extension exists in '${RECALLDB_DB_NAME}'."
psql_super "${RECALLDB_DB_NAME}" "CREATE EXTENSION IF NOT EXISTS vector;"

verify_login "${ASSISTANTHUB_DB_NAME}" "${ASSISTANTHUB_DB_USER}" "${ASSISTANTHUB_DB_PASS}"
verify_login "${PARTIO_DB_NAME}" "${PARTIO_DB_USER}" "${PARTIO_DB_PASS}"
verify_login "${LESS3_DB_NAME}" "${LESS3_DB_USER}" "${LESS3_DB_PASS}"
verify_login "${RECALLDB_DB_NAME}" "${RECALLDB_DB_USER}" "${RECALLDB_DB_PASS}"
verify_login "${VERBEX_DB_NAME}" "${VERBEX_DB_USER}" "${VERBEX_DB_PASS}"

verify_denied "${PARTIO_DB_NAME}" "${ASSISTANTHUB_DB_USER}" "${ASSISTANTHUB_DB_PASS}"
verify_denied "${LESS3_DB_NAME}" "${PARTIO_DB_USER}" "${PARTIO_DB_PASS}"
verify_denied "${RECALLDB_DB_NAME}" "${LESS3_DB_USER}" "${LESS3_DB_PASS}"
verify_denied "${ASSISTANTHUB_DB_NAME}" "${RECALLDB_DB_USER}" "${RECALLDB_DB_PASS}"
verify_denied "${ASSISTANTHUB_DB_NAME}" "${VERBEX_DB_USER}" "${VERBEX_DB_PASS}"

echo "PostgreSQL initialization complete."
