#!/bin/bash
set -e

echo "Checking if database 'recalldb' exists..."
DB_EXISTS=$(psql -h pgvector -U postgres -tAc "SELECT 1 FROM pg_database WHERE datname = 'recalldb'")

if [ "$DB_EXISTS" != "1" ]; then
  echo "Creating database 'recalldb'..."
  psql -h pgvector -U postgres -c "CREATE DATABASE recalldb;"
  echo "Database 'recalldb' created."
else
  echo "Database 'recalldb' already exists."
fi

echo "Ensuring vector extension is enabled..."
psql -h pgvector -U postgres -d recalldb -c "CREATE EXTENSION IF NOT EXISTS vector;"
echo "RecallDB initialization complete."
