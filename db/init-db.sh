#!/usr/bin/env bash
# Local dev only - runs once against the docker-compose 'db' service to
# create the Attendance database and apply db/seed/*.sql in order. See
# docker-compose.yml (db-init service) and docs/DOTNET10_MIGRATION_PLAN.md.
set -euo pipefail

SQLCMD=/opt/mssql-tools18/bin/sqlcmd
HOST=db

echo "Waiting for SQL Server ($HOST) to accept connections..."
until "$SQLCMD" -S "$HOST" -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1; do
  sleep 2
done

"$SQLCMD" -S "$HOST" -U sa -P "$MSSQL_SA_PASSWORD" -C -d master \
  -Q "IF DB_ID('Attendance') IS NULL CREATE DATABASE Attendance;"

for f in /seed/*.sql; do
  echo "Applying $f..."
  "$SQLCMD" -S "$HOST" -U sa -P "$MSSQL_SA_PASSWORD" -C -d Attendance -i "$f"
done

echo "Database seed complete."
