#!/usr/bin/env bash
# Starts (or reuses) a local, disposable SQL Server container for dev, and
# applies db/seed/*.sql. Requires Docker. Local-dev-only - never used for
# deployment (see Dockerfile / docs/DOTNET10_MIGRATION_PLAN.md).
set -euo pipefail

CONTAINER_NAME="davidsonrfb-attendance-sql"
SA_PASSWORD="${SA_PASSWORD:-LocalDev_Passw0rd!}"
HOST_PORT="${SQL_HOST_PORT:-14330}"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if docker ps --format '{{.Names}}' | grep -qx "${CONTAINER_NAME}"; then
  echo "SQL Server container '${CONTAINER_NAME}' already running."
elif docker ps -a --format '{{.Names}}' | grep -qx "${CONTAINER_NAME}"; then
  echo "Starting existing SQL Server container '${CONTAINER_NAME}'..."
  docker start "${CONTAINER_NAME}" >/dev/null
else
  echo "Creating SQL Server container '${CONTAINER_NAME}' (port ${HOST_PORT})..."
  docker run -d --name "${CONTAINER_NAME}" \
    -e ACCEPT_EULA=Y \
    -e MSSQL_SA_PASSWORD="${SA_PASSWORD}" \
    -p "${HOST_PORT}:1433" \
    -v davidsonrfb-attendance-sql-data:/var/opt/mssql \
    mcr.microsoft.com/mssql/server:2022-latest >/dev/null
fi

SQLCMD=(docker exec "${CONTAINER_NAME}" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${SA_PASSWORD}" -C)

echo "Waiting for SQL Server to accept connections..."
until "${SQLCMD[@]}" -Q "SELECT 1" >/dev/null 2>&1; do
  sleep 2
done

"${SQLCMD[@]}" -d master -Q "IF DB_ID('Attendance') IS NULL CREATE DATABASE Attendance;"

for f in "${REPO_ROOT}"/db/seed/*.sql; do
  name="$(basename "$f")"
  docker cp "$f" "${CONTAINER_NAME}:/tmp/${name}"
  echo "Applying ${name}..."
  "${SQLCMD[@]}" -d Attendance -i "/tmp/${name}"
done

echo "SQL Server ready on localhost:${HOST_PORT} (user: sa)"
